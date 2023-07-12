using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace FencingReplay
{
    internal abstract class Trigger
    {
        public struct LightEventArgs
        {
            bool Red;
            bool LeftWhite;
            bool Green;
            bool RightWhite;
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
        protected void FireLightEvent(LightEventArgs args)
        {
            var temp = OnLight;
            if (temp != null)
            {
                temp(this, args);
            }
        }
    }
}
