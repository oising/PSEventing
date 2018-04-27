using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Nivot.PowerShell.Eventing
{
    // global event queue
    internal sealed partial class PSQueueHelper : CriticalFinalizerObject, IDisposable
    {
        private const int MAX_QUEUESIZE = 10000; // hard limit for queue
        internal const string DEFAULT_QUEUE = "_DEFAULT";

        private bool m_isDisposed = false;
        private bool m_captureKeys = false;
        private bool m_captureCtrlC = false;

        private readonly object m_syncRoot = new Object();
        private readonly Dictionary<String, PSEventQueue> m_queues;
        private readonly PSEventingBreakHandler m_breakHandler;
        private Thread m_keyHandlerThread;

        internal static readonly PSQueueHelper Instance = new PSQueueHelper();
        
        private PSQueueHelper()
        {
            Dump(".ctor");
            m_queues = new Dictionary<String, PSEventQueue>(StringComparer.InvariantCultureIgnoreCase);
            m_breakHandler = new PSEventingBreakHandler(true);
        }

        internal bool CaptureCtrlC
        {
            get
            {
                lock (m_syncRoot)
                {
                    return m_captureCtrlC;
                }
            }
            set
            {
                lock (m_syncRoot)
                {
                    m_captureCtrlC = value;
                }
                OnBreakHandlerStateChange();
            }
        }

        private void OnBreakHandlerStateChange()
        {
            m_breakHandler.CaptureCtrlC = m_captureCtrlC;
        }

        internal bool CtrlCHit
        {
            get { return m_breakHandler.CtrlCHit; }
        }

        public bool CaptureKeys
        {
            get
            {
                lock (m_syncRoot) { return m_captureKeys; }
            }

            set
            {
                lock (m_syncRoot)
                {
                    m_captureKeys = value;
                }
                OnKeyHandlerStateChange();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueName">Name of queue, or null for default queue.</param>
        /// <returns></returns>
        internal IPSEventQueue GetEventQueue(string queueName)
        {
            lock (m_syncRoot)
            {
                EnsureQueue(queueName);
                return m_queues[queueName];
            }
        }

        internal IPSEventQueue DefaultQueue
        {
            get { return GetEventQueue(DEFAULT_QUEUE); }
        }

        internal List<String> QueueNames
        {
            get
            {
                lock (m_syncRoot)
                {
                    return new List<String>(m_queues.Keys);
                }
            }
        }
        private void EnsureQueue(string name)
        {
            lock (m_syncRoot)
            {
                if (!m_queues.ContainsKey(name))
                {
                    m_queues.Add(name, (new PSEventQueue(name)));
                }
            }
        }

        private void EnsureKeyHandlerThread()
        {
            // TODO: make more robust (IsAlive)

            if (m_keyHandlerThread == null ||
                (m_keyHandlerThread.ThreadState == System.Threading.ThreadState.Stopped))
            {
                m_keyHandlerThread = new Thread(new ThreadStart(KeyListener));
                m_keyHandlerThread.IsBackground = false;
                Dump("Initialized new KeyHandler thread tid:{0}", m_keyHandlerThread.ManagedThreadId);
            }
        }

        private void OnKeyHandlerStateChange()
        {
            if (CaptureKeys)
            {

                EnsureKeyHandlerThread();

                // start listener thread 
                if (m_keyHandlerThread.ThreadState == System.Threading.ThreadState.Unstarted)
                {
                    Dump("Starting KeyHandler thread tid:{0}...", m_keyHandlerThread.ManagedThreadId);
                    m_keyHandlerThread.Start();
                    Dump("KeyHandler thread started.");
                }
            }
        }

        private void KeyListener()
        {
            EventHandler idleHandler = new EventHandler(Application_Idle);
            Application.Idle += idleHandler;
            try
            {
                // hook
                using (PSEventingKeyHandler keyHandler = new PSEventingKeyHandler(true))
                {
                    // start a message loop
                    Application.Run();
                } // unhook (same thread)
            }
            finally
            {
                Application.Idle -= idleHandler;
            }

        }

        private void Application_Idle(object sender, EventArgs e)
        {
            if (CaptureKeys == false)
            {
                Application.ExitThread();
            }
        }

        [Conditional("DEBUG")]
        internal static void Dump(string format, params object[] parameters)
        {
            int tid = Thread.CurrentThread.ManagedThreadId;
            Debug.WriteLine(string.Format(format, parameters), "PSQueueHelper tid:" + tid);
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!m_isDisposed)
            {
                if (disposing)
                {
                    //if (SynchronizingObject.InvokeRequired)
                    //{
                    //    // SetWindowsHookEx has thread affinity with WH_KEYBOARD_LL
                    //    // see: http://msdn2.microsoft.com/en-us/library/ms644985.aspx
                    //    SynchronizingObject.Invoke(
                    //        new CleanUpHandler(delegate { m_keyHandler.Dispose(); }), null);
                    //}
                    ((IDisposable)m_breakHandler).Dispose();
                    //m_synchronizer.Dispose();
                }
                m_isDisposed = true;
            }
        }

        ~PSQueueHelper()
        {
            Dispose(false);
        }

        #endregion
    }
}
