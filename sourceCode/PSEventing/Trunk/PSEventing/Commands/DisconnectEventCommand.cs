using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Nivot.PowerShell.Eventing.Commands
{
    [Cmdlet(VerbsCommunications.Disconnect, "Event", DefaultParameterSetName = ParamSetVarName)]
    public class DisconnectEventCommand : EventCommandBase
    {
        private readonly PSBindingManager m_manager;

        public DisconnectEventCommand()
        {            
            m_manager = new PSBindingManager(this);
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();

            if (ContextVariable != null)
            {
                foreach (string eventName in this.EventName)
                {
                    if (m_manager.IsBound(ContextVariable, eventName))
                    {
                        m_manager.RemoveBinding(ContextVariable, eventName);
                        WriteVerbose("Event " + eventName + " has been unbound.");
                    } else
                    {
                        WriteWarning("Event " + eventName + " is not currently bound.");
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
    }
}
