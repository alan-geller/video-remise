using LightDisplayVisualEffect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace VideoRemise
{
    internal abstract class Trigger
    {
        public class LightEventArgs : EventArgs
        {
            public Lights LightsOn { get; set; }   // A sum of values from the Lights enum
        }

        public bool FiresClockEvents { get; set; }

        public event EventHandler OnClockStart;
        public event EventHandler OnClockStop;
        public event EventHandler<LightEventArgs> OnLight;

        public static Trigger ActiveTrigger;

        public static async Task Start(VideoRemiseConfig config)
        {
            if (config.TriggerProtocol == "Favero FA01")
            {
                ActiveTrigger = new FaveroFA01Trigger();
            }
            else
            {
                ActiveTrigger = null;
            }
            if (ActiveTrigger != null) // Implies manual triggering
            {
                await ActiveTrigger.Initialize(config);
            }
        }

        public abstract Task Initialize(VideoRemiseConfig config);

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
        protected void FireLightEvent(Lights lights)
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
