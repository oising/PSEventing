using System;
using System.Collections.Generic;

namespace Nivot.PowerShell.Eventing
{
    internal interface IPSEventQueue
    {
        bool HasEvents { get; }
        int Count { get; }
        List<PSEvent> DequeueAll(bool peekOnly);
        void EnqueueEvent(string bindingID, object eventSource, string eventName, EventArgs eventArgs);
    }
}