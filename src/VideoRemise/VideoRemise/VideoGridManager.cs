using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LightDisplayVisualEffect;
using Windows.Devices.Sensors;

namespace VideoRemise
{
    internal class VideoGridManager
    {
        private MainPage mainPage;
        private VideoRemiseConfig config;
        private Grid grid;
        private List<VideoChannel> channels;
        private bool zoomed = false;

        public int ChannelCount => channels.Count;

        internal VideoGridManager(MainPage mp)
        {
            mainPage = mp;
            grid = mp.LayoutGrid;

            channels = new List<VideoChannel>();
        }

        internal async Task UpdateGridAsync()
        {
            config = (Application.Current as App).Config;

            foreach (var channel in channels)
            {
                await channel.ShutdownAsync();
                grid.Children.Remove(channel.PlayerElement);
            }
            channels.Clear();

            int i = 0;
            double totalWidth = 0.0;
            foreach (var source in config.VideoSources)
            {
                var channel = new VideoChannel(i++, mainPage, this);
                await channel.SetSource(source);
                channel.SetProperty(LightDisplayEffect.RedLightColorProperty,
                    config.RedLightColor);
                channel.SetProperty(LightDisplayEffect.GreenLightColorProperty,
                    config.GreenLightColor);
                channel.SetProperty(LightDisplayEffect.LightStatusProperty, Lights.None);
                channels.Add(channel);
                totalWidth += channel.AspectRatio;
            }

            if (double.IsNaN(totalWidth) || (totalWidth == 0.0))
            {
                foreach (var channel in channels)
                {
                    channel.RelativeWidth = 1.0 / channels.Count;
                }
            }
            else
            {
                foreach (var channel in channels)
                {
                    channel.RelativeWidth = channel.AspectRatio / totalWidth;
                }
            }
        }

        internal void AdjustWidths(double frameWidth)
        {
            mainPage.LayoutGrid.Width = frameWidth;

            foreach (var channel in channels)
            {
                var videoWidth = frameWidth * channel.RelativeWidth;

                mainPage.LayoutGrid.ColumnDefinitions[channel.GridColumn].Width = new GridLength(videoWidth);
                channel.CaptureElement.Width = videoWidth;
                channel.PlayerElement.Width = videoWidth;
            }
        }

        internal void ToggleZoom(int column)
        {
            zoomed = !zoomed;
            for (int n = 0; n < channels.Count; n++)
            {
                if (zoomed && (n != column))
                {
                    channels[n].Visibility = Visibility.Collapsed;
                }
                else
                {
                    channels[n].Visibility = Visibility.Visible;
                }
            }
        }

        internal async Task StartRecording(string fileName)
        {
            //List<Task> done = new List<Task>();
            foreach (var channel in channels)
            {
                //done.Add(channel.StartRecording("test"));
                await channel.StartRecording(fileName);
            }
            //Task.WaitAll(done.ToArray());
        }

        internal async Task StopRecording()
        {
            //List<Task> done = new List<Task>();
            foreach (var channel in channels)
            {
                //done.Add(channel.StopRecording());
                await channel.StopRecording();
            }
            //Task.WaitAll(done.ToArray());
        }

        internal void StartPlayback()
        {
            foreach (var channel in channels)
            {
                channel.StartPlayback();
            }
        }

        internal void AddTrigger(Trigger activeTrigger)
        {
            activeTrigger.OnLight += OnLightStatus;
        }

        private void OnLightStatus(object sender, Trigger.LightEventArgs args)
        {
            foreach (var channel in channels)
            {
                channel.SetProperty(LightDisplayEffect.LightStatusProperty, args.LightsOn);
            }
        }
    }
}
