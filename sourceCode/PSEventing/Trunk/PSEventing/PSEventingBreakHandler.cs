using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Nivot.PowerShell.Eventing
{
    internal sealed class PSEventingBreakHandler : IDisposable
    {
        /// <summary>
        /// Handler to be called when a console event occurs.
        /// </summary>
        internal delegate bool ControlEventHandler(ConsoleEvent consoleEvent);

        private bool m_isHooked = false;
        private bool m_captureCtrlC;
        private bool m_ctrlCHit = false;
        private ControlEventHandler m_breakHandler;

        internal PSEventingBreakHandler(bool hookNow)
            : this(hookNow, false) // do not handle ctrl+C
        {
        }

        internal PSEventingBreakHandler(bool hookNow, bool handleCtrlC)
        {
            m_breakHandler = new ControlEventHandler(BreakHandler);
            m_captureCtrlC = handleCtrlC;

            if (hookNow)
            {
                Hook();
            }
        }

        private bool IsHooked
        {
            get { return m_isHooked; }
        }

        /// <summary>
        /// 
        /// </summary>
        internal bool CtrlCHit
        {
            get
            {
                bool hit = m_ctrlCHit;
                m_ctrlCHit = false; // reset 

                return hit;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal bool CaptureCtrlC
        {
            get { return m_captureCtrlC; }
            set { m_captureCtrlC = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool Hook()
        {
            if (!m_isHooked)
            {
                m_isHooked = NativeMethods.SetConsoleCtrlHandler(m_breakHandler, true);
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId, "PSEventingBreakHandler hooked: " + m_isHooked);
            }
            else
            {
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId, "PSEventingBreakHandler already hooked!");
            }

            return m_isHooked;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private bool Unhook()
        {
            bool success;

            if (m_isHooked)
            {
                int lastError = 0;
                success = NativeMethods.SetConsoleCtrlHandler(m_breakHandler, false);
                if (!success)
                {
                    // error
                    lastError = Marshal.GetLastWin32Error(); // call getlastwin32error as early as possible
                    Debug.WriteLine(String.Format("Error {0:X8}", lastError), "PSEventingBreakHandler");
                }
                else
                {
                    // success
                    m_isHooked = false;
                }
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId, "PSEventingBreakHandler unhooked: " + success);
            }
            else
            {
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId,
                                "PSEventingBreakHandler unhook called, but not currently hooked!");

                success = true;
            }

            return success;
        }

        private bool BreakHandler(ConsoleEvent consoleEvent)
        {
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId, String.Format("PSEventingBreakHandler event: {0}", consoleEvent));

            switch (consoleEvent)
            {
                case ConsoleEvent.CtrlC:

                    m_ctrlCHit = true;

                    if (m_captureCtrlC)
                    {
                        string emptyGuid = Guid.Empty.ToString();
                        PSQueueHelper.Instance.DefaultQueue.EnqueueEvent(emptyGuid, typeof(PSEventingBreakHandler), consoleEvent.ToString(), EventArgs.Empty);
                        m_ctrlCHit = false; // bugfix: http://www.codeplex.com/PSEventing/WorkItem/View.aspx?WorkItemId=4856

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

        ~PSEventingBreakHandler()
        {
            try
            {
                if (m_isHooked)
                {
                    bool unhooked = Unhook();
                    Debug.WriteLine(Thread.CurrentThread.ManagedThreadId,
                                    "PSEventingBreakHandler Finalizer unhook: " + unhooked);
                }
                else
                {
                    Debug.WriteLine(Thread.CurrentThread.ManagedThreadId,
                                    "PSEventingBreakHandler Finalizer - nothing to do.");
                }
            }
            catch
            {
                Trace.WriteLine("Finalizer threw an exception.", "PSEventingBreakHandler");
            }
        }

        void IDisposable.Dispose()
        {
            if (m_isHooked)
            {
                bool unhooked = Unhook();
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId,
                                "PSEventingBreakHandler Dispose unhook: " + unhooked);
                m_breakHandler = null;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The event that occurred. 
        /// </summary>
        internal enum ConsoleEvent
        {
            CtrlC = 0, CtrlBreak = 1, CtrlClose = 2, CtrlLogoff = 5, CtrlShutdown = 6
        }
    }
}