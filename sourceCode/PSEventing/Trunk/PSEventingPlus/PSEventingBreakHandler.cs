using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace Nivot.PowerShell.Eventing {
    internal sealed class PSEventingBreakHandler : CriticalFinalizerObject, IDisposable {
        /// <summary>
        /// Handler to be called when a console event occurs.
        /// </summary>
        internal delegate bool ControlEventHandler(ConsoleEvent consoleEvent);

        private bool _isHooked = false;
        private bool _captureCtrlC;
        private bool _ctrlCHit = false;
        private ControlEventHandler _breakHandler;

        internal PSEventingBreakHandler(bool hookNow)
            : this(hookNow, false) // do not handle ctrl+C
        {
        }

        internal PSEventingBreakHandler(bool hookNow, bool handleCtrlC) {
            _breakHandler = new ControlEventHandler(BreakHandler);
            _captureCtrlC = handleCtrlC;

            if (hookNow) {
                Hook();
            }
        }

        private bool IsHooked {
            get { return _isHooked; }
        }

        /// <summary>
        /// 
        /// </summary>
        internal bool CtrlCHit {
            get {
                bool hit = _ctrlCHit;
                _ctrlCHit = false; // reset 

                return hit;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal bool CaptureCtrlC {
            get { return _captureCtrlC; }
            set { _captureCtrlC = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool Hook() {
            if (!_isHooked) {
                _isHooked = NativeMethods.SetConsoleCtrlHandler(_breakHandler, true);
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId, "PSEventingBreakHandler hooked: " + _isHooked);
            }
            else {
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId, "PSEventingBreakHandler already hooked!");
            }

            return _isHooked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private bool Unhook() {
            bool success;

            if (_isHooked) {
                int lastError = 0;
                success = NativeMethods.SetConsoleCtrlHandler(_breakHandler, false);
                if (!success) {
                    // error
                    lastError = Marshal.GetLastWin32Error(); // call getlastwin32error as early as possible
                    Debug.WriteLine(String.Format("Error {0:X8}", lastError), "PSEventingBreakHandler");
                }
                else {
                    // success
                    _isHooked = false;
                }
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId, "PSEventingBreakHandler unhooked: " + success);
            }
            else {
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId,
                                "PSEventingBreakHandler unhook called, but not currently hooked!");

                success = true;
            }

            return success;
        }

        private bool BreakHandler(ConsoleEvent consoleEvent) {
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId, String.Format("PSEventingBreakHandler event: {0}", consoleEvent));

            switch (consoleEvent) {
                case ConsoleEvent.CtrlC:

                    _ctrlCHit = true;

                    if (_captureCtrlC) {
                        string emptyGuid = Guid.Empty.ToString();
                        // PSQueueHelper.Instance.DefaultQueue.EnqueueEvent(emptyGuid, typeof(PSEventingBreakHandler), consoleEvent.ToString(), EventArgs.Empty);
                        _ctrlCHit = false; // bugfix: http://www.codeplex.com/PSEventing/WorkItem/View.aspx?WorkItemId=4856

                        // ctrl+c handling ends here (doens't get to powershell)
                        return true;
                    }
                    break;

                case ConsoleEvent.CtrlBreak:
                case ConsoleEvent.CtrlClose:
                case ConsoleEvent.CtrlLogoff:
                    // unhook, we're going down
                    Unhook();
                    break;

                default: // shutdown
                    break;

            }
            // chain to next handler (e.g. consolehost)
            return false;
        }
        
        ~PSEventingBreakHandler() {
            try {
                if (_isHooked) {
                    bool unhooked = Unhook();
                    Debug.WriteLine(Thread.CurrentThread.ManagedThreadId,
                                    "PSEventingBreakHandler Finalizer unhook: " + unhooked);
                }
                else {
                    Debug.WriteLine(Thread.CurrentThread.ManagedThreadId,
                                    "PSEventingBreakHandler Finalizer - nothing to do.");
                }
            }
            catch {
                Trace.WriteLine("Finalizer threw an exception.", "PSEventingBreakHandler");
            }
        }

        void IDisposable.Dispose() {
            if (_isHooked) {
                bool unhooked = Unhook();
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId,
                                "PSEventingBreakHandler Dispose unhook: " + unhooked);
                _breakHandler = null;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The event that occurred. 
        /// </summary>
        internal enum ConsoleEvent {
            CtrlC = 0, CtrlBreak = 1, CtrlClose = 2, CtrlLogoff = 5, CtrlShutdown = 6
        }
    }
}