using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;

namespace Nivot.PowerShell.Eventing.Commands
{
    [Cmdlet(VerbsCommon.Get, "EventQueue")]
    public class GetEventQueueCommand : PSCmdlet
    {
        protected override void EndProcessing()
        {
            IEnumerable queueNames = (this.QueueName ?? (IEnumerable)PSQueueHelper.Instance.QueueNames);
                                                 
            foreach (string queueName in queueNames)
            {
                IPSEventQueue queue = PSQueueHelper.Instance.GetEventQueue(queueName);

                if (queue.Count > 0 || ShowEmpty.IsPresent)
                {
                    PSObject obj = new PSObject();
                    obj.Properties.Add(new PSNoteProperty("QueueName", queueName));
                    obj.Properties.Add(new PSNoteProperty("Count", queue.Count));
                    //obj.Properties.Add(new PSNoteProperty("Last", queueName));
                    WriteObject(obj);
                }
                else
                {
                    WriteVerbose("Skipping empty queue: " + queueName);
                }
            }
        }

        [Parameter(Mandatory = false, Position = 0)]
        public string[] QueueName
        {
            get;
            set;
        }

        [Parameter]
        public SwitchParameter ShowEmpty
        {
            get;
            set;
        }
    }
}
