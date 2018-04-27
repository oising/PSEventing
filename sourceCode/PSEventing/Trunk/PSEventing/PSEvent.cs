using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Text;

namespace Nivot.PowerShell.Eventing
{    
    /// <summary>
    /// 
    /// </summary>
    public class PSEvent
    {
        private readonly string m_bindingID;
        private readonly DateTime m_occurred;
        private readonly object m_source;
        private readonly string m_name;
        private readonly EventArgs m_args;

        /// <summary>
        /// When did the event occur?
        /// </summary>
        public DateTime Occurred
        {
            get { return m_occurred; }
        }

        /// <summary>
        /// [KeyHandler], [BreakHandler], PSVariable instance, ...
        /// </summary>
        public object Source
        {
            get { return m_source; }
        }

        /// <summary>
        /// KeyDown, KeyUp, Clicked, Deleted, ...
        /// </summary>
        public string Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// FileSystemWatcherEventArgs, KeyEventArgs, ...
        /// </summary>
        public EventArgs Args
        {
            get { return m_args; }
        }

        internal string BindingID
        {
            get { return m_bindingID; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventSource"></param>
        /// <param name="eventName"></param>
        /// <param name="eventArgs"></param>
        /// <param name="bindingID"></param>      
        internal PSEvent(string bindingID, object eventSource, string eventName, EventArgs eventArgs)
        {
            Debug.WriteLine(String.Format("bindingid: {0}, source: {1}, eventname: {2}; args: {3}",
                bindingID, eventSource, eventName, eventArgs), "PSEvent .ctor");

            m_bindingID = bindingID;

            if (String.IsNullOrEmpty(bindingID))
            {
                // not a PSVariable generated event
                m_source = eventSource;
            }
            else
            {                
                m_source = PSBindingManager.GetVariableByBindingID(m_bindingID);
            }

            // public fields
            m_occurred = DateTime.Now;
            //m_source = eventSource;
            m_name = eventName;            
            m_args = eventArgs;
        }
    }
}
