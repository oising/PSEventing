using System;
using System.Collections.Generic;
using System.Text;

namespace Nivot.PowerShell.Eventing.Test
{
    public class GenericEvents
    {
        private GenericEvents() {}

        public event EventHandler<PoxEventArgs> Pox = delegate { };

        public virtual void OnPox(PoxEventArgs args)
        {
            Pox(this, args);
        }

        public static readonly GenericEvents Instance = new GenericEvents();
    }

    public class PoxEventArgs : EventArgs
    {
        public string Datum
        {
            get;
            set;
        }
    }

    public class NameQueues
    {
        public static void TestNamedQueue(object sender, EventArgs args)
        {
            PSQueueHelper.Instance.GetEventQueue("queue").EnqueueEvent("bindingid", sender, "click", args);
        }
    }
}
