using System.Management.Automation;
using Microsoft.PowerShell.Commands;
 
namespace Nivot.PowerShell.Eventing.Commands
{
    [Cmdlet(VerbsLifecycle.Register, "HotKeyEvent")]
    public class RegisterHotKeyEventCommand : ObjectEventRegistrationBase
    {
        protected override object GetSourceObject() {            
            return PSHotKeyManager.Instance.CreateListener(SourceIdentifier, PassThru, Global);
        }

        protected override string GetSourceObjectEventName() {
            return "HotKey";
        }

        [Parameter(Mandatory=true, Position = 100, HelpMessage="Key gesture to watch for, e.g. 'ctrl+shift+k'")]
        [ValidateNotNullOrEmpty]
        public new string SourceIdentifier {
            get {
                return base.SourceIdentifier;
            }
            set {
                base.SourceIdentifier = value;
            }
        } 

        [Parameter]
        public SwitchParameter Global { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }
    }
}
