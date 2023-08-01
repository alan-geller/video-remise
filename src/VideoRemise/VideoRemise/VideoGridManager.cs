using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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
            //splitters = new List<GridSplitter>();
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
                var channel = new VideoChannel(i++, mainPage, source, this);
                channels.Add(channel);
                totalWidth += channel.AspectRatio;
            }

            if (double.IsNaN(totalWidth))
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

        internal void AdjustWIdths(double frameWidth)
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

            //foreach (var sp in splitters)
            //{
            //    sp.Visibility = zoomed ? Visibility.Collapsed : Visibility.Visible;
            //}
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
    }
}
