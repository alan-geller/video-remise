using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace VideoRemise
{
    // These specific values match the Favero FA01 outputs, which makes that specific
    // driver slightly simpler, but it really doesn't matter.
    internal enum Lights
    {
        LeftWhite = 1,
        RightWhite = 2,
        Red = 4,            // Red is left on-target
        Green = 8
    }

    internal abstract class Trigger
    {
        public class LightEventArgs : EventArgs
        {
            public int LightsOn { get; set; }   // A sum of values from the Lights enum
        }

        public bool FiresClockEvents { get; set; }

        public event EventHandler OnClockStart;
        public event EventHandler OnClockStop;
        public event EventHandler<LightEventArgs> OnLight;

        protected void FireClockStartEvent()
        {
            var temp = OnClockStart;
            if (temp != null)
            {
                temp(this, new EventArgs());
            }
        }
        protected void FireClockStopEvent()
        {
            var temp = OnClockStop;
            if (temp != null)
            {
                temp(this, new EventArgs());
            }
        }
        protected void FireLightEvent(int lights)
        {
            var temp = OnLight;
            if (temp != null)
            {
                var args = new LightEventArgs() { LightsOn = lights };
                temp(this, args);
            }
        }
    }
}
