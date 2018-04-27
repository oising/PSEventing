using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Nivot.PowerShell.Eventing.Commands
{
    [Cmdlet(VerbsCommon.New, "Event")]
    public class NewEventCommand : Cmdlet
    {
        private string m_eventName;
        private PSObject m_data = null;
        private IPSEventQueue m_queue;

        protected override void BeginProcessing()
        {
            m_queue = (this.QueueName == null) ?
              PSQueueHelper.Instance.DefaultQueue
              : PSQueueHelper.Instance.GetEventQueue(this.QueueName);
        }

        protected override void ProcessRecord()
        {
            m_queue.EnqueueEvent(null, null, m_eventName, new PSEventArgs(m_data));
        }

        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string EventName
        {
            get { return m_eventName; }
            set { m_eventName = value; }
        }

        [Parameter(Mandatory = false, Position = 1, ValueFromPipeline = true)]
        public PSObject Data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        [Parameter(Mandatory = false, Position = 2)]
        public string QueueName
        {
            get;
            set;
        }
    }
}
