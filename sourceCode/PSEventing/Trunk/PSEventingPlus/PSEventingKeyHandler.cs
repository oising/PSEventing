using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace Nivot.PowerShell.Eventing {
    // see: http://msdn2.microsoft.com/en-us/library/ms644985.aspx
    // hooking WH_KEYBOARD_LL (or mouse variation) has thread affinity:
    // the same thread is used for callback - and most importantly, the same
    // thread _must_ be used to unhook the handler.

    internal sealed class PSEventingKeyHandler : CriticalFinalizerObject, IDisposable {
        internal delegate IntPtr LowLevelKeyboardProcHandler(int nCode, IntPtr wParam, IntPtr lParam);
        private IntPtr _hook = IntPtr.Zero;
        private readonly IntPtr _hCWnd;
        private readonly LowLevelKeyboardProcHandler _handler;
        private readonly int _managedThreadId;

        internal PSEventingKeyHandler(bool hookNow) {            
            _managedThreadId = Thread.CurrentThread.ManagedThreadId;
            _handler = HookCallback;
            _hCWnd = NativeMethods.GetConsoleWindow();

            if (hookNow) {
                Hook();
            }
        }

        private bool IsHooked {
            get { return (_hook != IntPtr.Zero); }
        }

        private bool Hook() {
            if (!IsHooked) {
                _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL,
                   _handler, new HandleRef(null, NativeMethods.GetModuleHandle(null)), 0);

                // failed?
                if (_hook == IntPtr.Zero) {
                    int hr = Marshal.GetLastWin32Error();
                    Tracer.Dump("Failed to hook WH_KEYBOARD_LL - last win32 error code was: 0x{0:x8}", hr);
                }
            }
            return IsHooked;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private bool Unhook() {
            if (Thread.CurrentThread.ManagedThreadId != _managedThreadId) {
                Tracer.Dump("Unhook fail: wrong thread id. Current: {0}; expected: {1}.", Thread.CurrentThread.ManagedThreadId, _managedThreadId);
                return false;
            }
            bool succeeded = true;

            if (IsHooked) {
                Tracer.Dump("Unhooking...");

                if (NativeMethods.UnhookWindowsHookEx(new HandleRef(null, _hook)) == false) {
                    // failed

                    int hresult = Marshal.GetLastWin32Error();

                    if (hresult == 0x57c) {
                        Tracer.Dump("Unhook fail: Invalid hook handle {0}", _hook);
                    }
                    else {
                        Tracer.Dump("Unhook fail: hresult: {0:X8}", hresult);
                    }

                    succeeded = false;
                    //throw Marshal.GetExceptionForHR(hresult);
                }
                else {
                    // ok

                    Tracer.Dump("Unhook success.");
                    _hook = IntPtr.Zero;
                }
            }
            return succeeded;
        }

        [ReliabilityContract(Consistency.MayCorruptProcess, Cer.MayFail)]
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            IntPtr hFocus = NativeMethods.GetForegroundWindow();

            //Tracer.Dump("GFW: {0} ; hCWnd: {1}", hFocus, _hCWnd);
            bool passThru = true;
            // are we focused? (e.g. was this our input?)
            bool global = (_hCWnd != hFocus);

            if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_KEYDOWN)) {
                try {
                    var keyInfo =
                        (KBDLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof (KBDLLHOOKSTRUCT));

                    bool shift = (NativeMethods.GetKeyState(NativeMethods.VK_SHIFT) & ~1) != 0;
                    bool control = (NativeMethods.GetKeyState(NativeMethods.VK_CONTROL) & ~1) != 0;
                    bool alt = (NativeMethods.GetKeyState(NativeMethods.VK_MENU) & ~1) != 0; // ALT

                    char keyChar = (char) NativeMethods.MapVirtualKey(
                        (uint) keyInfo.vkCode, NativeMethods.MAPVK_VK_TO_CHAR);

                    var consoleKeyInfo = new ConsoleKeyInfo(keyChar,
                        (ConsoleKey) keyInfo.vkCode, shift, alt, control);

                    var args = new ConsoleKeyEventArgs(consoleKeyInfo, global);
                    
                    // notify any listeners
                    PSHotKeyManager.Instance.OnConsoleKeyDown(args);

                    // eat keystroke or not?
                    passThru = !args.Cancel; 
                }
                catch (Exception ex) {

                    string message =
                        String.Format("HookCallback threw an exception: {0}. Disabling hook.", ex.Message);
                    Console.WriteLine(message);
                    Trace.WriteLine(message, "PSEventingKeyHandler");

                    // dispose/unhook/suppressfinalize
                    using (this) {}
                }
            }

            if (passThru) {                
                return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
            }

            // eat it
            return new IntPtr(1); // non zero to eat it
            //return IntPtr.Zero;
        }

        ~PSEventingKeyHandler() {
            bool success = Unhook();
            Trace.WriteLine("unhooked: " + success, "PSEventingKeyHandler.Finalize");
        }

        void IDisposable.Dispose() {
            bool success = Unhook();
            Trace.WriteLine("unhooked: " + success, "PSEventingKeyHandler.Dispose");
            GC.SuppressFinalize(this);
        }

        [StructLayout(LayoutKind.Sequential)]
        private class KBDLLHOOKSTRUCT {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }
    }
}
