using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Nivot.PowerShell.Eventing.Commands
{
    [Cmdlet(VerbsLifecycle.Stop, "KeyHandler")]
    public class StopKeyHandlerCommand : Cmdlet
    {
        protected override void EndProcessing()
        {
            PSQueueHelper.Instance.CaptureKeys = false;
            PSQueueHelper.Instance.CaptureCtrlC = false;
        }
    }
}
