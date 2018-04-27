using System;
using System.Collections.Generic;
using System.Text;

namespace Nivot.PowerShell.Eventing
{
    public class PSEventBindingInfo
    {
        private readonly string m_VariableName;
        private readonly string m_EventName;
        private readonly string m_TypeName;
        private readonly bool m_Listening;

        public string VariableName { get { return m_VariableName; } }
        public string EventName { get { return m_EventName; } }
        public string TypeName { get { return m_TypeName; } }
        public bool Listening { get { return m_Listening; } }

        internal PSEventBindingInfo(string variableName, string eventName, string typeName, bool listening)
        {
            m_VariableName = variableName;
            m_EventName = eventName;
            m_TypeName = typeName;
            m_Listening = listening;
        }
    }
}
