using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LightDisplayVisualEffect;
using System.Diagnostics;

namespace VideoRemise
{
    internal struct ReplaySegment
    {
        public TimeSpan start;
        public TimeSpan end;
    }

    internal class VideoGridManager
    {
        private MainPage mainPage;
        private VideoRemiseConfig config;
        private Grid grid;
        private List<VideoChannel> channels;
        private bool zoomed = false;
        private Stopwatch streamStopwatch = new Stopwatch();
        private TimeSpan replayStart;
        private TimeSpan replayEnd;
        private List<ReplaySegment> replays = new List<ReplaySegment>();
        private bool replayRecording = false;
        private int replayMillisBeforeTrigger;
        private int replayMillisAfterTrigger;

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
            replayMillisBeforeTrigger = config.ReplayMillisBeforeTrigger[mainPage.CurrentWeapon];
            replayMillisAfterTrigger = config.ReplayMillisAfterTrigger[mainPage.CurrentWeapon];
            //List<Task> done = new List<Task>();
            foreach (var channel in channels)
            {
                //done.Add(channel.StartRecording("test"));
                await channel.StartRecording(fileName);
            }
            streamStopwatch.Start();
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
            streamStopwatch.Stop();
            //Task.WaitAll(done.ToArray());
        }

        internal void StartPlayback()
        {
            foreach (var channel in channels)
            {
                channel.StartPlayback();
            }
            mainPage.CurrentMode = Mode.Replaying;
            mainPage.SetStatus();
        }

        internal void OnPlaybackEvent(PlaybackEvent playbackEvent)
        {
            foreach (var channel in channels)
            {
                channel.OnPlaybackEvent(playbackEvent);
            }
        }

        internal void AddTrigger(Trigger activeTrigger)
        {
            activeTrigger.OnLight += OnLightStatus;
        }

        internal async void OnHalt(TriggerType triggerType)
        {
            await Task.Delay(replayMillisAfterTrigger * 1000);
            foreach (var channel in channels)
            {
                channel.Trigger((replayMillisBeforeTrigger + replayMillisAfterTrigger), triggerType);
            }
            mainPage.CurrentMode = Mode.Replaying;
            mainPage.SetStatus();
        }

        private void OnLightStatus(object sender, Trigger.LightEventArgs args)
        {
            foreach (var channel in channels)
            {
                channel.SetProperty(LightDisplayEffect.LightStatusProperty, args.LightsOn);
            }
            if (args.LightsOn != Lights.None)
            {
                /*
                if (!replayRecording)
                {
                    replayStart = streamStopwatch.Elapsed - TimeSpan.FromMilliseconds(replayMillisBeforeTrigger);
                }
                replayEnd = streamStopwatch.Elapsed + TimeSpan.FromMilliseconds(replayMillisAfterTrigger);*/
                TriggerType tt = (TriggerType)args.LightsOn;
                OnHalt(tt);
            }
        }
    }
}
