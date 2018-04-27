using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Nivot.PowerShell.Eventing.Commands
{
    [Cmdlet(VerbsLifecycle.Start, "KeyHandler")]
    public class StartKeyHandlerCommand : Cmdlet
    {
        private SwitchParameter m_captureCtrlC;
        private SwitchParameter m_captureKeys;

        [Parameter]
        public SwitchParameter CaptureCtrlC
        {
            get { return m_captureCtrlC; }
            set { m_captureCtrlC = value; }
        }

        [Parameter]
        public SwitchParameter CaptureKeys
        {
            get { return m_captureKeys; }
            set { m_captureKeys = value; }
        }

        // maybe -CaptureKeyUp -CaptureKeyDown ?

        protected override void EndProcessing()
        {
            if (m_captureCtrlC.IsPresent || m_captureKeys.IsPresent)
            {
                PSQueueHelper.Instance.CaptureKeys = m_captureKeys.IsPresent;
                PSQueueHelper.Instance.CaptureCtrlC = m_captureCtrlC.IsPresent;
            }
            else
            {
                WriteWarning("Please supply at least one switch from -CaptureCtrlC and -CaptureKeys.");
            }
        }   
    }
}
