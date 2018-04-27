using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management.Automation;
using System.Text;

namespace Nivot.PowerShell.Eventing
{
    [RunInstaller(true)]
    public class PSEventingSnapIn : PSSnapIn
    {        
        public override string Name
        {
            get { return "PSEventing"; }
        }

        public override string Vendor
        {
            get { return "Oisin Grehan"; }
        }

        public override string Description
        {
            get { return "PowerShell Eventing Library 1.1"; }
        }
    }
}
