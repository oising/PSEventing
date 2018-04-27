using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Nivot.PowerShell.Eventing
{
    /// <summary>
    /// 
    /// </summary>
    public class PSEventArgs : EventArgs
    {
        private readonly PSObject m_data;

        internal PSEventArgs(PSObject data)
        {
            m_data = data;
        }

        public PSObject Data
        {
            get { return m_data; }
        }
    }
}
