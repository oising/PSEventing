using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Nivot.PowerShell.Eventing
{    
    /// <summary>
    /// Using this as an anchor for tracking variable binding: if the variable gets deleted, this goes away too.
    /// TODO: use finalizer to remove event handler from target variable
    /// </summary>
    public sealed class PSEventBindingAttribute : ArgumentTransformationAttribute
    {        
        private readonly string m_eventName;
        private readonly Delegate m_handler;
        private readonly Guid m_bindingID;
        
        private bool m_isInitialized = false;
        private readonly bool m_shouldCleanup = true;
        private object m_initialTarget;        


        public PSEventBindingAttribute(string eventName, Delegate handler, Guid bindingID)
        {
            m_eventName = eventName;
            m_handler = handler;
            m_bindingID = bindingID;           
        }

        public string EventName
        {
            get { return m_eventName; }
        }

        internal Guid BindingID
        {
            get { return m_bindingID; }
        }

        internal Delegate Handler
        {
            get { return m_handler;  }
        }

        // it appears this is called on every psvariable access, so lets abuse it!
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            Debug.WriteLine("Enter: " + m_eventName, "PSEventBindingAttribute.Transform");

            // does this attribute have a job to do?
            if (m_shouldCleanup)
            {
                object target = inputData;
                if (target != null)
                {
                    if (target is PSObject)
                    {
                        target = ((PSObject) inputData).BaseObject;
                    }
                    Debug.Assert(target != null, "target != null");
                }
                else
                {
                    // variable has been nulled out
                    Debug.WriteLine("null", "PSEventBindingAttribute.Transform");
                }
                
                if (m_isInitialized == false)
                {
                    Initialize(target);
                }
                else
                {
                    // unhook delegate for this attribute's associated event
                    CleanUp(target);
                }                
            }
            else
            {
                Debug.WriteLine("(redundant)", "PSEventBindingAttribute.Transform");
            }
            Debug.WriteLine("Exit: " + m_eventName, "PSEventBindingAttribute.Transform");

            return inputData;
        }

        private void CleanUp(object target) {
            Debug.WriteLine("Target check", "PSEventBindingAttribute.CleanUp");

            // not the first assignment, is it the same object?
            if (target != m_initialTarget) // will never hit null != null test (which is always true)
            {
                Debug.WriteLine("New target, unhooking handlers.", "PSEventBindingAttribute.Transform");

                // not the same object, unhook our handler so target can be GC'd
                UnhookEventHandler();

                /* this attribute has now served its purpose, as any future bindings will use a new attribute.
                   note: i would like to call GC.SuppressFinalize here, but I'm not sure if the KeepAlive
                         calls should be reachable in order to GC what they're guarding?
                */
                //GC.SuppressFinalize(this)
                //m_shouldCleanup = false;               
            }
            else
            {
                Debug.WriteLine("Same target, exiting.", "PSEventBindingAttribute.Transform");
            }
        }

        private void Initialize(object target) {

            Debug.Assert(target != null, "target != null");
            
            string message = String.Format("Initializing {0} {1}", target.GetType().Name, m_eventName);
            Debug.WriteLine(message, "PSEventBindingAttribute.Initialize");
                    
            // first assignment to this PSVariable instance
            m_initialTarget = target;
            m_isInitialized = true;
        }

        private void UnhookEventHandler() {
            Debug.WriteLine("Enter: " + m_eventName, "PSEventBindingAttribute.UnhookEventHandler");

            try
            {
                PSEventHelper.UnbindEventSinkFromTarget(m_initialTarget, m_eventName, m_handler);                

                string message = String.Format("Removed event handler for '{0}' from {1}", m_eventName, m_initialTarget.GetType());
                Debug.WriteLine(message, "PSEventBindingAttribute.UnhookEventHandler");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex, "PSEventBindingAttribute.UnhookEventHandler");
            }

            Debug.WriteLine("Exit: " + m_eventName, "PSEventBindingAttribute.UnhookEventHandler");
        }

        ~PSEventBindingAttribute()
        {            
            Debug.WriteLine("Finalizer", "PSEventBindingAttribute");

            if (m_shouldCleanup)
            {
                UnhookEventHandler();
            }

            // prevent premature collection/finalization of some essential clean-up fields.
            // see: http://blogs.msdn.com/cbrumme/archive/2003/04/19/51365.aspx
            GC.KeepAlive(m_shouldCleanup);
            GC.KeepAlive(m_initialTarget);
            GC.KeepAlive(m_eventName);
            GC.KeepAlive(m_handler);
        }
    }
}
