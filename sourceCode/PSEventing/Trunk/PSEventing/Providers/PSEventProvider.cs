using System;
using System.Collections.Generic;
using System.Management.Automation.Provider;
using System.Text;

namespace Nivot.PowerShell.Eventing.Providers
{
    class PSEventProvider : ItemCmdletProvider
    {
        protected override bool IsValidPath(string path)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
