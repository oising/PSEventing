using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;

namespace Nivot.PowerShell.Eventing
{
    public sealed class PSVariableHelper
    {
        private static readonly Regex reVarFixup = new Regex(@"(?:variable\:\\?)?(.+)", RegexOptions.IgnoreCase);
        private readonly PSCmdlet m_context;

        internal PSVariableHelper(PSCmdlet context)
        {
            m_context = context;
        }

        internal PSVariable GetVariableByName(string variableName)
        {
            if (String.IsNullOrEmpty(variableName))
            {
                throw new ArgumentException("variableName cannot be null or empty.");
            }

            if (reVarFixup.IsMatch(variableName))
            {
                variableName = reVarFixup.Match(variableName).Groups[1].Value;
            }            
            PSVariable variable = m_context.SessionState.PSVariable.Get(variableName);

            string message = "Path variable:" + variableName;

            if (variable == null)
            {
                m_context.WriteError(new ErrorRecord(null, "VarNotFound", ErrorCategory.InvalidArgument, message + " not found."));
                return null;
            }

            object target = GetBaseObject(variable);

            if (target == null)
            {
                m_context.WriteError(new ErrorRecord(null, "VarIsNull", ErrorCategory.InvalidArgument, message + " -eq $null."));
                return null;
            }

            if (target is PSCustomObject)
            {
                m_context.WriteError(new ErrorRecord(null, "VarIsCustomObject", ErrorCategory.InvalidArgument, message + " -is PSCustomObject."));
                return null;
            }

            return variable;
        }

        internal static object GetBaseObject(PSVariable variable)
        {
            object target = variable.Value;

            if (target is PSObject)
            {
                target = ((PSObject)target).BaseObject;
            }

            return target;
        }
    }
}
