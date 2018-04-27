using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using Microsoft.PowerShell.Commands;
using Nivot.PowerShell.Eventing.Commands;

namespace Nivot.PowerShell.Eventing
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class PSBindingManager
    {
        private readonly PSCmdlet m_command;
        private static readonly Dictionary<Guid, WeakReference> s_bindings;
        
        static PSBindingManager()
        {
            // for mapping bindingID guids to PSVariable instances
            // needed to provide a reference to the original PSVariable
            // in the target's event handler.
            s_bindings = new Dictionary<Guid, WeakReference>();
        }

        internal PSBindingManager() : this(null)
        {            
        }

        internal PSBindingManager(PSCmdlet command)
        {
            m_command = command;            
        }

        // from reflector (system.management.automation)
        //internal static T FromObjectAs<T>(object castObject)
        //{
        //    T local = default(T);
        //    PSObject obj2 = castObject as PSObject;
        //    if (obj2 == null)
        //    {
        //        try
        //        {
        //            return (T)castObject;
        //        }
        //        catch (InvalidCastException)
        //        {
        //            return default(T);
        //        }
        //    }
        //    try
        //    {
        //        return (T)obj2.BaseObject;
        //    }
        //    catch (InvalidCastException)
        //    {
        //        return default(T);
        //    }
        //}

        private void WriteDebug(string message)
        {
            if (m_command != null)
            {
                m_command.WriteDebug(message);
            }
        }


        internal void AddBinding(PSVariable variable, EventInfo eventInfo, string queueName)
        {            
            Guid bindindID = Guid.NewGuid();
            
            lock (s_bindings)
            {
                s_bindings.Add(bindindID, new WeakReference(variable));
                WriteDebug("Added PSVariable tracking entry.");
            }

            try
            {
                Delegate handler = PSEventHelper.BindEventSink(variable, eventInfo, bindindID, queueName);
                WriteDebug("Event sink bound.");

                PSEventBindingAttribute attr = new PSEventBindingAttribute(eventInfo.Name, handler, bindindID);
                variable.Attributes.Add(attr);
                WriteDebug("Added binding attribute to PSVariable.");
            }
            catch (Exception ex)
            {
                m_command.WriteWarning(String.Format("Error binding to {0} on {1}: {2}", eventInfo.Name, variable.Name, ex.Message));

                s_bindings.Remove(bindindID);
                WriteDebug("Removed PSVariable tracking entry.");
                throw;
            }                       
        }

        internal void RemoveBinding(PSVariable variable, string eventName)
        {
            // need for instead of foreach (need to modify collection)
            for (int index = 0; index < variable.Attributes.Count; index++)
            {
                PSEventBindingAttribute bindingAttr = variable.Attributes[index] as PSEventBindingAttribute;
                if (bindingAttr != null)
                {
                    if (bindingAttr.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase))
                    {
                        PSEventHelper.UnbindEventSink(variable, eventName, bindingAttr.Handler);
                        WriteDebug("Unbound event sink from variable.");

                        variable.Attributes.Remove(bindingAttr);
                        WriteDebug("Removed binding attribute from PSVariable.");

                        // remove PSVariable tracker
                        lock (s_bindings)
                        {
                            s_bindings.Remove(bindingAttr.BindingID);
                            WriteDebug("Removed PSVariable tracking entry.");
                        }
                    }
                }
            }
        }

        internal static PSVariable GetVariableByBindingID(string bindingID)
        {
            Debug.Assert(bindingID != null, "bindingID != null");
            Guid key = new Guid(bindingID);
            WeakReference reference;
            PSVariable variable = null;

            lock (s_bindings)
            {
                if (! s_bindings.TryGetValue(key, out reference))
                {
                    reference = null;
                }
            }

            if ((reference != null) && reference.IsAlive)
            {
                variable = reference.Target as PSVariable;
            }

            return variable;
        }

        internal List<PSEventBindingInfo> GetBindings(bool includeUnboundEvents)
        {
            Collection<PSObject> variables = m_command.InvokeCommand.InvokeScript("get-childitem variable:");
            WriteDebug("Returned " + variables.Count + " variables.");

            List<PSEventBindingInfo> allBindings = null;

            foreach (PSObject obj in variables)
            {
                Debug.Assert(obj != null, "obj != null");

                // not interested in psvariable derived types, so avoid "is" operator
                if (obj.BaseObject.GetType() == typeof(PSVariable))
                {
                    PSVariable variable = (PSVariable)obj.BaseObject;
                    List<PSEventBindingInfo> bindings = GetBindings(variable, includeUnboundEvents);
                    if (bindings != null)
                    {
                        if (allBindings == null)
                        {
                            allBindings = new List<PSEventBindingInfo>();
                        }
                        allBindings.AddRange(bindings);
                    }
                }
            }

            if (allBindings != null)
            {
                WriteDebug("Found " + allBindings.Count + " binding(s)");
            }
            else
            {
                WriteDebug("No bindings found.");
            }

            return allBindings;
        }

        internal List<PSEventBindingInfo> GetBindings(PSVariable variable, bool includeUnboundEvents)
        {                        
            object targetObject = variable.Value;
            if (targetObject == null)
            {
                return null;
            }

            if (targetObject is PSObject)
            {
                targetObject = ((PSObject) targetObject).BaseObject;
            }

            Type targetType = targetObject.GetType();

            List<PSEventBindingInfo> bindingInfos = new List<PSEventBindingInfo>();

            // find currently bound events
            foreach (Attribute attr in variable.Attributes)
            {
                PSEventBindingAttribute bindingAttr = attr as PSEventBindingAttribute;
                if (bindingAttr != null)
                {
                    WriteDebug("Found " + bindingAttr.EventName + " binding on " + variable.Name);

                    PSEventBindingInfo bindingInfo = new PSEventBindingInfo(variable.Name, bindingAttr.EventName, targetType.Name, true);
                    bindingInfos.Add(bindingInfo);
                }
            }

            if (includeUnboundEvents)
            {
                AddUnboundEvents(variable, targetType, bindingInfos);
            }

            // no point sorting an empty list, or one with a single value ;-)
            if (bindingInfos.Count > 1)
            {
                // sort by event name
                bindingInfos.Sort(new Comparison<PSEventBindingInfo>(
                    delegate(PSEventBindingInfo info1, PSEventBindingInfo info2)
                    {
                        return info1.EventName.CompareTo(info2.EventName);
                    }));
            }

            return bindingInfos;
        }

        private static void AddUnboundEvents(PSVariable variable, Type targetType, List<PSEventBindingInfo> bindingInfos) {
            BindingFlags flags = (BindingFlags.Instance | BindingFlags.Public);

            if (variable.Value != null)
            {
                // lets get all possible events, excluding already bound ones
                EventInfo[] eventInfos = targetType.GetEvents(flags);
                if (eventInfos != null)
                {
                    foreach (EventInfo eventInfo in eventInfos)
                    {
                        // check for this event in the "already bound" list (bindingInfos)                                                                         
                        if (bindingInfos.Find(info => info.EventName.Equals(eventInfo.Name,
                            StringComparison.OrdinalIgnoreCase)) == null)
                        {
                            // not already bound
                            bindingInfos.Add(
                                new PSEventBindingInfo(variable.Name, eventInfo.Name, targetType.Name, false));                                
                        }
                    }
                }
            }
        }

        internal bool IsBound(PSVariable variable, string eventName)
        {
            List<PSEventBindingInfo> bindings = GetBindings(variable, false);
            if (bindings != null)
            {
                foreach (PSEventBindingInfo binding in bindings)
                {
                    if (binding.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase))
                    {
                        m_command.WriteDebug("PSBindingManager.IsBound returning true.");
                        return true;
                    }
                }
            }
            m_command.WriteDebug("PSBindingManager.IsBound returning false.");
            return false;
        }

    }
}
