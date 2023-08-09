using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Input;
using Windows.System;
using LightDisplayVisualEffect;

namespace VideoRemise
{
    internal class ManualTrigger : Trigger
    {
        private MainPage mainPage;
        private bool clockRunning = false;

        ManualTrigger(MainPage main)
        {
            mainPage = main;
            mainPage.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Space:
                    if (clockRunning)
                    {
                        FireClockStopEvent();
                    }
                    else
                    {
                        FireClockStartEvent();
                    }
                    break;
                case VirtualKey.LeftShift:
                    FireLightEvent(Lights.Red);
                    break;
                case VirtualKey.LeftControl:
                    FireLightEvent(Lights.LeftWhite);
                    break;
                case VirtualKey.RightShift:
                    FireLightEvent(Lights.Green);
                    break;
                case VirtualKey.RightControl:
                    FireLightEvent(Lights.RightWhite);
                    break;
                default:
                    break;
            }
        }

        public override Task Initialize(VideoRemiseConfig config)
        {
            return Task.CompletedTask;
        }
    }
}
