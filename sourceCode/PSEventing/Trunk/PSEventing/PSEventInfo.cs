using System;
using System.Collections.Generic;
using System.Text;

namespace Nivot.PowerShell.Eventing
{
    public struct PSEventInfo
    {
        public string Name;
        public string ArgsName;

        public PSEventInfo(string name, string argsName)
        {
            Name = name;
            ArgsName = argsName;
        }
    }
}
