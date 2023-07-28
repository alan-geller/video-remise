using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YamlDotNet.Serialization;

namespace FencingReplay
{
    public sealed partial class MainPage : Page
    {
        List<VideoChannel> channels;

        public int CurrentWeapon { get; set; }
        public bool Paused { get; set; }
        public bool Recording { get; set; }
        public bool Playing { get; set; }

        private FencingReplayConfig config;
        private VideoGridManager gridManager;

        private bool IsMatchSetUp { 
            get
            {
                return ((epeeBtn.IsChecked == true) ||
                    (foilBtn.IsChecked == true) ||
                    (saberBtn.IsChecked == true)) &&
                    !string.IsNullOrWhiteSpace(leftFencer.Text) &&
                    !string.IsNullOrWhiteSpace(rightFencer.Text);
            } 
        }

        public MainPage()
        {
            this.InitializeComponent();

            gridManager = new VideoGridManager(this);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Frame.SizeChanged += HandleResize;

            await VideoChannel.Initialize();

            AdjustVideoWidths(Frame.ActualWidth);

            config = (Application.Current as App).Config;

            if ((bool)e.Parameter)
            {
                //if (channels != null)
                //{
                //    foreach (var channel in channels)
                //    {
                //        await channel.ShutdownAsync();
                //    }
                //}
                //channels = new List<VideoChannel>();
                //int i = 0;
                //foreach (var source in config.VideoSources)
                //{
                //    channels.Add(new VideoChannel(i++, this) { VideoSource = source });
                //}
                await gridManager.UpdateGridAsync();
            }

            UpdateMatchInfo();

            Paused = false;
            Recording = false;
            Playing = false;

            SetStatus("Ready");
        }

        private void AdjustVideoWidths(double frameWidth)
        {
            var lightWidth = Math.Max(frameWidth / 3.0, 100);
            leftLight.Width = lightWidth;
            rightLight.Width = lightWidth;
            lightSpacer.Width = frameWidth - leftLight.ActualWidth - rightLight.ActualWidth;

            //LayoutGrid.Width = frameWidth;
            //if (LayoutGrid.ColumnDefinitions.Count > 0)
            //{
            //    var videoWidth = Math.Max(frameWidth / LayoutGrid.ColumnDefinitions.Count, 150);
            //    foreach (var col in LayoutGrid.ColumnDefinitions)
            //    {
            //        col.Width = new GridLength(videoWidth);
            //    }
            //    foreach (var child in LayoutGrid.Children)
            //    {
            //        var mc = child as CaptureElement;
            //        if (mc != null)
            //        {
            //            mc.Width = videoWidth;
            //        }
            //        var mp = child as MediaPlayerElement;
            //        if (mp != null)
            //        {
            //            mp.Width = videoWidth;
            //        }
            //    }
            //}
        }

        private void HandleResize(object sender, SizeChangedEventArgs e)
        {
            AdjustVideoWidths(e.NewSize.Width);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Frame.SizeChanged -= HandleResize;
        }

        private async void OnStartRecording(object sender, RoutedEventArgs e)
        {
            if (!Recording)
            {
                ////List<Task> done = new List<Task>();
                //foreach (var channel in channels)
                //{
                //    //done.Add(channel.StartRecording("test"));
                //    await channel.StartRecording("test");
                //}
                ////Task.WaitAll(done.ToArray());
                await gridManager.StartRecording();

                leftLight.Opacity = 0.50;
                rightLight.Opacity = 0.50;

                PauseBtn.IsEnabled = true;
                PlayBtn.IsEnabled = true;
                TriggerBtn.IsEnabled = true;
                Recording = true;
                SetStatus("Recording");
            }
        }

        private async void OnStopRecording(object sender, RoutedEventArgs e)
        {
            if (Recording)
            {
                ////List<Task> done = new List<Task>();
                //foreach (var channel in channels)
                //{
                //    //done.Add(channel.StopRecording());
                //    await channel.StopRecording();
                //}
                ////Task.WaitAll(done.ToArray());
                await gridManager.StopRecording();

                PauseBtn.IsEnabled = false;
                PlayBtn.IsEnabled = true;
                TriggerBtn.IsEnabled = false;
                Recording = false;
                SetStatus("Ready");
            }
        }

        private void OnTogglePauseRecording(object sender, RoutedEventArgs e)
        {
            if (Recording)
            {
                if (!Paused)
                {
                    List<Task> done = new List<Task>();
                    foreach (var channel in channels)
                    {
                        done.Add(channel.Pause());
                    }
                    Task.WaitAll(done.ToArray());
                    PauseBtn.Content = "Resume";
                    Paused = true;
                    SetStatus("Paused");
                }
                else
                {
                    List<Task> done = new List<Task>();
                    foreach (var channel in channels)
                    {
                        done.Add(channel.Resume());
                    }
                    Task.WaitAll(done.ToArray());
                    PauseBtn.Content = "Pause";
                    Paused = false;
                    SetStatus("Recording");
                }
            }
        }

        private async void OnPlay(object sender, RoutedEventArgs e)
        {
            if (!Playing && !Recording)
            {
                //foreach (var channel in channels)
                //{
                //    channel.StartPlayback();
                //}
                //SetStatus("Playing");
                gridManager.StartPlayback();
            }
        }

        private async void OnTrigger(object sender, RoutedEventArgs e)
        {
            if (Recording)
            {
                if (config.ReplaySecondsAfterTrigger[CurrentWeapon] > 0)
                {
                    await Task.Delay(1000 * config.ReplaySecondsAfterTrigger[CurrentWeapon]);
                    foreach (var channel in channels)
                    {
                        await channel.StopRecording();
                    }
                    foreach (var channel in channels)
                    {
                        channel.StartLoop(config.ReplaySecondsAfterTrigger[CurrentWeapon] +
                            config.ReplaySecondsBeforeTrigger[CurrentWeapon]);
                        SetStatus("Replaying");
                    }
                }
            }
        }

        private void OnDeviceConfig(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ConfigPage));
        }

        private async void OnSetupMatch(object sender, RoutedEventArgs e)
        {
            var result = await MatchSetupDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (epeeBtn.IsChecked ?? false)
                {
                    CurrentWeapon = FencingReplayConfig.Epee;
                }
                else if (foilBtn.IsChecked ?? false)
                {
                    CurrentWeapon = FencingReplayConfig.Foil;
                }
                else if (saberBtn.IsChecked ?? false)
                {
                    CurrentWeapon = FencingReplayConfig.Saber;
                }
                UpdateMatchInfo();
            }
        }

        private void UpdateMatchInfo()
        {
            var weapon = char.ToUpper(FencingReplayConfig.WeaponName(CurrentWeapon)[0]).ToString()
                + FencingReplayConfig.WeaponName(CurrentWeapon).Substring(1);
            matchInfo.Text = IsMatchSetUp ?
                $"{weapon}: {leftFencer.Text} vs. {rightFencer.Text}" :
                "Set up match";
        }

        private void SetStatus(string status)
        {
            var content = CommandBar.Content as TextBlock;
            content.Text = status;
        }
    }
}

