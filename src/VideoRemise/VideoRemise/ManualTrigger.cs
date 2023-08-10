using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Input;
using Windows.System;
using LightDisplayVisualEffect;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace VideoRemise
{
    internal class ManualTrigger : Trigger
    {
        class TimerData
        {
            public Lights ResetLights { get; set; }
            public Timer ResetTimer { get; set; }
        }

        private MainPage mainPage;
        private bool clockRunning = false;
        private int pendingCount = 0;
        private const int lightsOnMillis = 1500;
        private Lights currentLights = Lights.None;
        private object timerLock = new object();

        public ManualTrigger(MainPage main)
        {
            mainPage = main;
            //mainPage.KeyDown += OnKeyDown;
            Window.Current.CoreWindow.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(CoreWindow sender, KeyEventArgs args)
        {
            void HandleLights(Lights l)
            {
                Lights oldLights;
                lock (timerLock)
                {
                    pendingCount++;
                    oldLights = currentLights;
                    currentLights |= l;
                }
                FireLightEvent(currentLights);
                var data = new TimerData { ResetLights = oldLights };
                var t = new Timer(LightsTimeout, data, lightsOnMillis, Timeout.Infinite);
                data.ResetTimer = t;
            }

            switch (args.VirtualKey)
            {
                case VirtualKey.Pause:
                    if (clockRunning)
                    {
                        FireClockStopEvent();
                    }
                    else
                    {
                        FireClockStartEvent();
                    }
                    break;
                case VirtualKey.S:
                    HandleLights(Lights.Red);
                    break;
                case VirtualKey.X:
                    HandleLights(Lights.LeftWhite);
                    break;
                case VirtualKey.D:
                    HandleLights(Lights.Green);
                    break;
                case VirtualKey.C:
                    HandleLights(Lights.RightWhite);
                    break;
                default:
                    break;
            }
        }

        private void LightsTimeout(object d)
        {
            bool fire = false;
            var data = (TimerData)d;
            lock (timerLock)
            {
                if (--pendingCount <= 0)
                {
                    currentLights = data.ResetLights;
                    fire = true;
                }
            }
            if (fire)
            {
                FireLightEvent(data.ResetLights);
            }
            data.ResetTimer?.Dispose();
        }

        public override Task Initialize(VideoRemiseConfig config)
        {
            return Task.CompletedTask;
        }
    }
}
