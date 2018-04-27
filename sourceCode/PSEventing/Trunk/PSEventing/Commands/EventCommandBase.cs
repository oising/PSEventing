using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Nivot.PowerShell.Eventing.Commands
{
    public abstract class EventCommandBase : PSCmdlet
    {
        private string m_variableName = null;
        private string[] m_eventNames = null;
        private PSVariable m_variable = null;
        private SwitchParameter m_caseSensitive = false;
        private readonly PSVariableHelper m_variableHelper;

        protected const string ParamSetVarName = "VarName";
        protected const string ParamSetVar = "Var";

        protected EventCommandBase()
        {
            m_variableHelper = new PSVariableHelper(this);            
        }

        protected override void EndProcessing()
        {
            if (ParameterSetName == ParamSetVarName)
            {
                Debug.Assert(m_variableName != null, "m_variableName != null");

                m_variable = m_variableHelper.GetVariableByName(m_variableName);

                if ((m_variable == null) || (m_variable.Value == null))
                {
                    ThrowTerminatingError(
                        new ErrorRecord(new ArgumentException("Variable is $null or does not exist."), "InvalidVariable",
                                        ErrorCategory.InvalidArgument, null));
                }
            }
            else // ParamSetVar
            {
                Debug.Assert(m_variable != null, "m_variable != null");
                // ... PSVariable was passed
            }
        }

        protected PSVariableHelper VariableHelper
        {
            get { return m_variableHelper;  }
        }

        protected PSVariable ContextVariable
        {
            get { return m_variable; }
        }

        [Parameter(
            Mandatory = true, Position = 0, ParameterSetName = ParamSetVar, ValueFromPipeline = true,
            HelpMessage = "The PSVariable object representing the .NET event source.")
        ]
        [ValidateNotNull]
        public PSVariable Variable
        {
            get
            {
                return m_variable;
            }
            set
            {
                m_variable = value;
            }
        }

        [Parameter(
            Mandatory = true, Position = 0, ParameterSetName = ParamSetVarName, ValueFromPipelineByPropertyName = true,
            HelpMessage = "The name of the variable which represents the .NET event source.")
        ]
        [ValidateNotNullOrEmpty]
        public string VariableName
        {
            get
            {
                return m_variableName;
            }
            set
            {
                m_variableName = value;
            }
        }

        [Parameter(
            Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true,
            HelpMessage = "One or more event names exposed by the source object.")
        ]
        [ValidateNotNullOrEmpty]
        public string[] EventName
        {
            get
            {
                return m_eventNames;
            }
            set
            {
                m_eventNames = value;
            }
        }

        [Parameter(HelpMessage = "Match the event name using ordinal case-sensitive rules.")]
        public SwitchParameter CaseSensitive
        {
            get
            {
                return m_caseSensitive;
            }
            set
            {
                m_caseSensitive = value;
            }
        }

        [Conditional("DEBUG")]
        protected void Dump(string format, params object[] parameters)
        {
            int tid = Thread.CurrentThread.ManagedThreadId;
            Debug.WriteLine(string.Format(format, parameters), this.GetType().Name + " tid:" + tid);
        }
    }
}
