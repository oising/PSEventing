using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Nivot.PowerShell.Eventing.Commands
{
    [Cmdlet(VerbsCommunications.Connect, "Event", DefaultParameterSetName = ParamSetVarName)]
    public class ConnectEventCommand : EventCommandBase
    {
        private readonly PSBindingManager m_manager;
        
        public ConnectEventCommand()
        {
             m_manager = new PSBindingManager(this);
        }

        [Parameter(Mandatory = false, Position = 2)]
        public string QueueName
        {
            get;
            set;
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            if (ContextVariable != null)
            {
                foreach (string eventName in this.EventName)
                {
                    if (m_manager.IsBound(ContextVariable, eventName) == false)
                    {
                        BindVariableToEvent(ContextVariable, eventName);
                    }
                    else
                    {
                        WriteWarning("Event " + eventName + " is already bound.");
                    }
                }
            }
            else
            {
                ThrowTerminatingError(
                    new ErrorRecord(new ArgumentException("Invalid Variable"), "InvalidVariable",
                                    ErrorCategory.InvalidArgument, null));
            }
        }

        private void BindVariableToEvent(PSVariable variable, string eventName)
        {
            Debug.Assert(variable != null, "variable != null");
            Debug.Assert(variable.Value != null, "variable.Value != null");

            object target = PSVariableHelper.GetBaseObject(variable);

            Type type = target.GetType();
            WriteVerbose("Target is a " + type.Name);

            try
            {
                EventInfo eventInfo = PSEventHelper.GetEventInfo(type, eventName, CaseSensitive.IsPresent);
                if (eventInfo == null)
                {
                    WriteWarning("Event '" + eventName + "' cannot be found on instance of " + type.Name);
                }
                else
                {
                    // bind the event source                    
                    m_manager.AddBinding(variable, eventInfo, (this.QueueName ?? PSQueueHelper.DEFAULT_QUEUE));

                    WriteVerbose("Now listening for '" + eventName + "' events from $" + VariableName);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex, "BindVariableToEvent Error");
                ThrowTerminatingError(new ErrorRecord(ex, "BindFailure", ErrorCategory.NotSpecified, type));
            }         
        }
    }
}
