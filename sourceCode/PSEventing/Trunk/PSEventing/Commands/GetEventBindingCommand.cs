using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Nivot.PowerShell.Eventing.Commands
{
    [Cmdlet(VerbsCommon.Get, "EventBinding", DefaultParameterSetName = ParamSetVar)]
    public class GetEventBindingCommand : PSCmdlet
    {
        private const string ParamSetVarName = "VarName";
        private const string ParamSetVar = "Var";

        private PSBindingManager m_manager;
        private PSVariableHelper m_variableHelper;
        
        private string[] m_variableNames;
        private PSVariable[] m_variables;
        private SwitchParameter m_includeUnboundEvents;

        [Parameter(
            Mandatory = false, Position = 0, ParameterSetName = ParamSetVarName, ValueFromPipeline = true,
            HelpMessage = "One or more variable names to examine for current (and/or potential) bindings. If not scope-qualified, the names will be resolved against the current scope.")
        ]
        [ValidateNotNullOrEmpty]
        public string[] VariableName
        {
            get { return m_variableNames; }
            set { m_variableNames = value; }
        }

        [Parameter(
            Mandatory = false, Position = 0, ParameterSetName = ParamSetVar, ValueFromPipeline = true,
            HelpMessage = "One or more PSVariable objects to examine for current (and/or potential) bindings.")
        ]
        [ValidateNotNull]
        public PSVariable[] Variable
        {
            get
            {
                return m_variables;
            }
            set
            {
                m_variables = value;
            }
        }

        [Parameter(HelpMessage = "If set, this command will return all potentially bindable events, as well as already bound events.")]
        public SwitchParameter IncludeUnboundEvents
        {
            get { return m_includeUnboundEvents; }
            set { m_includeUnboundEvents = value; }
        }

        protected override void BeginProcessing()
        {
            m_manager = new PSBindingManager(this);
            m_variableHelper = new PSVariableHelper(this);
        }

        protected override void ProcessRecord()
        {            
            if (ParameterSetName == "ParamSetVar")
            {
                if (m_variables != null)
                {
                    foreach (PSVariable variable in m_variables)
                    {
                        List<PSEventBindingInfo> bindings =
                            m_manager.GetBindings(variable, IncludeUnboundEvents.IsPresent);

                        if (bindings.Count == 0)
                        {
                            WriteVerbose("No bindings found for PSVariable named " + variable.Name);
                        } else
                        {
                            WriteObject(bindings, true);
                        }
                    }
                }
            }
            else // "ParamSetVarName"
            {
                if (m_variableNames != null)
                {
                    // get bindings for specified variable(s)
                    foreach (string variableName in m_variableNames)
                    {
                        PSVariable variable = m_variableHelper.GetVariableByName(variableName);
                        if (variable != null)
                        {
                            List<PSEventBindingInfo> bindings =
                                m_manager.GetBindings(variable, IncludeUnboundEvents.IsPresent);

                            if (bindings.Count == 0)
                            {
                                WriteVerbose("No bindings found for variable:" + variableName);
                            }
                            else
                            {
                                WriteObject(bindings, true);
                            }
                        }
                        else
                        {
                            string message = String.Format("{0} is null or invalid.", variableName);
                            WriteError(new ErrorRecord(new ArgumentException(message), "InvalidVariableName", ErrorCategory.InvalidArgument, variableName));
                        }
                    }
                }
                else
                {
                    // get all bindings
                    WriteObject(m_manager.GetBindings(IncludeUnboundEvents.IsPresent), true);
                }
            }
        }        
    }
}
