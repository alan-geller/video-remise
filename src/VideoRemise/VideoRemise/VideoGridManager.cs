using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
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
        //private List<GridSplitter> splitters;
        private bool zoomed = false;

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
            //foreach (var splitter in splitters)
            //{
            //    grid.Children.Remove(splitter);
            //}
            //splitters.Clear();
            int i = 0;
            foreach (var source in config.VideoSources)
            {
                channels.Add(new VideoChannel(i++, mainPage, source, this));
            }

            //for (int n = 0; n < channels.Count - 1; n++)
            //{
            //    var splitter = new GridSplitter();
            //    splitter.ResizeDirection = GridSplitter.GridResizeDirection.Auto;
            //    splitter.ResizeBehavior = GridSplitter.GridResizeBehavior.BasedOnAlignment;
            //    splitter.CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnSplitterHover;
            //    splitter.Width = 10;
            //    splitter.GripperCursor = GridSplitter.GripperCursorType.Hand;
            //    splitter.Foreground = new SolidColorBrush(Colors.White);
            //    splitter.Background = new SolidColorBrush(Colors.Gray);
            //    splitter.HorizontalAlignment = HorizontalAlignment.Right;
            //    var content = new TextBlock();
            //    content.Text = "\uE76F";
            //    content.HorizontalAlignment = HorizontalAlignment.Center;
            //    content.VerticalAlignment = VerticalAlignment.Center;
            //    splitter.Element = content;

            //    grid.Children.Add(splitter);
            //    Grid.SetColumn(splitter, n);
            //    Grid.SetRow(splitter, 0);
            //    splitters.Add(splitter);
            //}
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
