using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Reflection;
using System.Windows.Input;

namespace Nivot.PowerShell.Eventing {
    internal sealed class PSHotKeyManager : IDisposable {
        private readonly PSEventingKeyHandler _keyHandler;
        internal event EventHandler<ConsoleKeyEventArgs> ConsoleKeyDown;
        public static readonly PSHotKeyManager Instance = new PSHotKeyManager();
        private readonly IntPtr _owner;
        private readonly int _pid;

        private PSHotKeyManager() {
            // the powershell window may not actually be focused when
            // this cmdlet is run so derive the window handle from the PID.
            Process self = Process.GetCurrentProcess();
            _owner = self.MainWindowHandle;
            _pid = self.Id;
            _keyHandler = new PSEventingKeyHandler(true);            
        }

        internal void OnConsoleKeyDown(ConsoleKeyEventArgs keyEventArgs) {
            Tracer.Dump("PSHotKeyManager.OnConsoleKeyDown (Global: {0}) {1} {2} {3}",
                keyEventArgs.Global,
                keyEventArgs.KeyInfo.Key,
                keyEventArgs.KeyInfo.KeyChar,
                keyEventArgs.KeyInfo.Modifiers);

            var temp = ConsoleKeyDown;
            if (temp != null) {
                temp(this, keyEventArgs);
            }
        }
        
        internal int Pid {
            get { return _pid; }
        }

        internal IntPtr Owner {
            get { return _owner; }
        }

        internal HotKeyListener CreateListener(string sourceIdentifier, bool passThru, bool global) {
            Tracer.Dump("PSHotKeyManager.CreateListener '{0}' passThru: {1}; global: {2}",
                sourceIdentifier, passThru, global);

            var converter = new KeyGestureConverter();
            try {
                // convert from "ctrl+shift+t" etc to WPF gesture
                var gesture = (KeyGesture)converter.ConvertFromString(sourceIdentifier);
                Debug.Assert(gesture != null, "gesture != null");

                int virtualKey = KeyInterop.VirtualKeyFromKey(gesture.Key);
                char keyChar = (char)NativeMethods.MapVirtualKey((uint)virtualKey, NativeMethods.MAPVK_VK_TO_CHAR);

                bool shift = (gesture.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                bool alt = (gesture.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
                bool control = (gesture.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

                var hotKey = new ConsoleKeyInfo(keyChar, (ConsoleKey)virtualKey, shift, alt, control);
                
                return new HotKeyListener(hotKey, passThru, watchGlobal:global);
            }
            catch (ArgumentException ex) {
                throw new PSArgumentException("Incompatible or unrecognized key.", ex);
            }
            catch (NotSupportedException ex) {
                throw new PSArgumentException("Incompatible or unrecognized key sequence.", ex);
            }
        }

        internal static bool IsModifierKey(int virtualKeyCode) {
            if (((virtualKeyCode < 0x10) || (virtualKeyCode > 0x12)) && ((virtualKeyCode != 20) && (virtualKeyCode != 0x90))) {
                return (virtualKeyCode == 0x91);
            }
            return true;
        }

        internal sealed class HotKeyListener {
            private readonly object _syncLock = new object();
            private readonly Guid _listenerId;
            private readonly ConsoleKeyInfo _myHotKey;
            private readonly bool _passThru;
            private readonly bool _watchGlobal;
            private event EventHandler<ConsoleKeyEventArgs> HotKeyInternal = delegate { };

            internal HotKeyListener(ConsoleKeyInfo myHotKey, bool passThru, bool watchGlobal) {
                _listenerId = Guid.NewGuid();
                _myHotKey = myHotKey;
                _passThru = passThru;
                _watchGlobal = watchGlobal;                
                Tracer.Dump("HotKeyListener_{0} created.", _listenerId);
            }

            // using add/remove accessors to watch when powershell's native eventing subscribes/unsubscribes
            public event EventHandler<ConsoleKeyEventArgs> HotKey {
                add {
                    lock (_syncLock) {
                        // first subscription (only dummy delegate) 
                        if (HotKeyInternal.GetInvocationList().Length == 1) {
                            Instance.ConsoleKeyDown += OnConsoleKeyDown;
                            Tracer.Dump("HotKeyListener_{0} hooked PSHotKeyManager.", _listenerId);
                        }
                        HotKeyInternal += value;
                    }
                }
                remove {
                    lock (_syncLock) {
                        HotKeyInternal -= value;
                        if (HotKeyInternal.GetInvocationList().Length == 1) {
                            Instance.ConsoleKeyDown -= OnConsoleKeyDown;
                            Tracer.Dump("HotKeyListener_{0} unhooked PSHotKeyManager.", _listenerId);
                        }
                    }
                }
            }

            private void OnConsoleKeyDown(Object sender, ConsoleKeyEventArgs args) {
                Tracer.Dump("HotKeyListener_{0} OnConsoleKeyDown.", _listenerId);
                
                // if watching global or key is local, and key matches
                if ((_watchGlobal || !args.Global) && (args.KeyInfo == _myHotKey)) {
                    if (!_passThru) {
                        // eat the keystroke
                        args.Cancel = true;
                    }
                    Tracer.Dump("HotKeyListener_{0} HotKey triggered.", _listenerId);
                    
                    // notify powershell 2.0 eventing
                    HotKeyInternal(this, args); 
                }
            }
        }

        private void Dispose(bool disposing) {
            if (disposing) {
                using (_keyHandler) { }
            }
        }

        void IDisposable.Dispose() {
            Tracer.Dump("PSHotKeyManager Dispose.");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PSHotKeyManager() {
            Tracer.Dump("PSHotKeyManager Finalize.");
            Dispose(false);
        }
    }

    public class ConsoleKeyEventArgs : EventArgs {
        public readonly ConsoleKeyInfo KeyInfo;

        public ConsoleKeyEventArgs(ConsoleKeyInfo hotKeyInfo, bool global) {
            KeyInfo = hotKeyInfo;
            Cancel = false; // default to pass through keystroke
            Global = global; // global: owning powershell instance not focused
        }

        public bool Global { get; private set;  }
        public bool Cancel { get; set; }
    }
}
