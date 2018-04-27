using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Nivot.PowerShell.Eventing
{
    partial class PSQueueHelper
    {
        private class PSEventQueue : IPSEventQueue
        {
            private readonly object m_syncRoot = new object();
            private readonly Queue<PSEvent> m_queue;
            private readonly string m_name;
            private bool m_hasEvents = false;

            internal PSEventQueue(string queueName)
            {
                m_queue = new Queue<PSEvent>(500);
                m_name = queueName;
            }

            #region IPSEventQueue Members

            public bool HasEvents
            {
                get
                {
                    lock (m_syncRoot)
                    {
                        return m_hasEvents;
                    }
                }
            }

            public int Count
            {
                get
                {
                    lock (m_syncRoot)
                    {
                        return m_queue.Count;
                    }
                }
            }
            public List<PSEvent> DequeueAll(bool peekOnly)
            {
                Dump("DequeueAll '{0}' enter... peekOnly: {1}", m_name, peekOnly);

                List<PSEvent> events = null;

                lock (m_syncRoot)
                {
                    if (m_queue.Count != 0)
                    {
                        events = new List<PSEvent>(m_queue);

                        Dump("DequeueAll '{0}' returning {1} event(s).", m_name, events.Count);

                        if (!peekOnly)
                        {
                            m_queue.Clear();
                            m_hasEvents = false;
                        }
                    }
                    Dump("DequeueAll '{0}' exit.", m_name);

                    return events;
                }
            }

            public void EnqueueEvent(string bindingID, object eventSource, string eventName, EventArgs eventArgs)
            {
                try
                {
                    Dump("EnqueueEvent '{0}' enter...", m_name);

                    lock (m_syncRoot)
                    {
                        // future cmdlets will allow changing the queuesize
                        while (m_queue.Count >= MAX_QUEUESIZE)
                        {
                            // bin the oldest event
                            m_queue.Dequeue();
                        }

                        m_queue.Enqueue(new PSEvent(bindingID, eventSource, eventName, eventArgs));
                        m_hasEvents = true;

                        Dump("EnqueueEvent '{0}' exit", m_name);
                    }
                }
                catch
                {
                    Trace.WriteLine(String.Format("EnqueueEvent '{0}' {1} {2} {3}",
                       m_name, eventSource, eventName, eventArgs), "EnqueueEvent Error");
                }
            }

            #endregion
        }
    }
}
