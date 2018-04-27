using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Nivot.PowerShell.Eventing.Commands
{
    [Cmdlet(VerbsCommunications.Read, "Event", DefaultParameterSetName = "Read")]
    public class ReadEventCommand : Cmdlet, IDisposable
    {
        private SwitchParameter m_wait;

        protected override void EndProcessing()
        {
            Dump("Begin");
            IPSEventQueue queue = GetQueue();

            try
            {                               
                ReadEventInternal(queue);                
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "GetEvent Error");
                var error = new ErrorRecord(ex, "EventReadFailed", ErrorCategory.ReadError, null);
                ThrowTerminatingError(error);
            }
            Dump("End");
        }

        private void ReadEventInternal(IPSEventQueue queue)
        {
            List<PSEvent> eventList = null;

            if (Wait.IsPresent)
            {
                if (queue.HasEvents == false)
                {
                    // need to launch new thread here -- blocking the pipeline thread with a wait 
                    // deadlocks something, but joining does not - not sure what's going on here, yet.
                    var t = new Thread(WaitForEventOrBreak);
                    t.IsBackground = true;
                    t.Start(queue);
                    while (t.IsAlive)
                    {
                        // process messages
                        Application.DoEvents();
                        Thread.Sleep(100);
                    }
                    t.Join();
                }
            }
            eventList = queue.DequeueAll(this.NoFlush);

            if (eventList != null)
            {
                WriteObject(eventList, true);
            }
        }

        private IPSEventQueue GetQueue()
        {
            PSQueueHelper queueHelper = PSQueueHelper.Instance;
            return queueHelper.GetEventQueue(this.QueueName ?? PSQueueHelper.DEFAULT_QUEUE); // null = default queue
        }

        /// <summary>
        /// Worker thread for listening to spool
        /// </summary>
        private static void WaitForEventOrBreak(object state)
        {
            IPSEventQueue queue = (IPSEventQueue)state;
            PSQueueHelper helper = PSQueueHelper.Instance;

            while (queue.HasEvents == false && (!helper.CtrlCHit))
            {
                // com/sendmessage pumping
                Thread.CurrentThread.Join(100);
            }
        }

        [Parameter(ParameterSetName = "Read", HelpMessage = "If the event queue is empty, block until at least one tracked event occurs.")]
        public SwitchParameter Wait
        {
            get
            {
                return m_wait;
            }
            set
            {
                m_wait = value;
            }
        }

        [Parameter(Mandatory = false, Position = 0, HelpMessage = "If specified, attempts to read from a named queue; if not specified, reads will come from the default queue.")]
        public string QueueName
        {
            get;
            set;
        }

        [Parameter(ParameterSetName = "Read")]
        public SwitchParameter NoFlush
        {
            get;
            set;
        }

        [Conditional("DEBUG")]
        private static void Dump(string format, params object[] parameters)
        {
            int tid = Thread.CurrentThread.ManagedThreadId;
            Debug.WriteLine(string.Format(format, parameters), "GetEvent tid:" + tid);
        }

        public void Dispose()
        {
            //m_isDisposed = true;
            try
            {
                if (Wait.IsPresent)
                {
                    // ...
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "GetEventCommand.Dispose error");
            }

            Dump("Dispose");
        }
    }
}
