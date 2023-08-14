using System;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace VideoRemise
{
    public enum Mode
    {
        Idle,
        Recording,
        Replaying
    }

    public sealed partial class MainPage : Page
    {
        public int CurrentWeapon { get; set; }
        public Mode CurrentMode { get; set; }
        public bool Playing { get; set; }
        public bool Active { get; set; }


        private VideoRemiseConfig config;
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
            InitializeComponent();

            gridManager = new VideoGridManager(this);
            Window.Current.CoreWindow.KeyDown += OnKeyDown;
            CommandBar.PreviewKeyDown += (object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e) =>
            {
                if (CurrentMode != Mode.Idle)
                {
                    e.Handled = true;
                }
            };
            Active = false;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Active = true;

            config = (Application.Current as App).Config;

            if (!string.IsNullOrWhiteSpace(config.AdapterDeviceId))
            {
                await Trigger.Start(config);
            }

            await VideoChannel.Initialize();

            //gridManager.AddTrigger(Trigger.ActiveTrigger);
            if (config.ManualTriggerEnabled)
            {
                var manualTrigger = new ManualTrigger(this);
                gridManager.AddTrigger(manualTrigger);
            }

            Frame.SizeChanged += HandleResize;

            //if ((bool)e.Parameter)
            //{
                await gridManager.UpdateGridAsync();
            //}

            UpdateMatchInfo();

            AdjustVideoWidths(Frame.ActualWidth);

            gridManager.UpdateLightColors();

            Playing = false;
            CurrentMode = Mode.Idle;

            SetStatus();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            Active = false;
        }

        private void AdjustVideoWidths(double frameWidth)
        {
            //var lightWidth = Math.Max(frameWidth / 3.0, 100);
            //leftLight.Width = lightWidth;
            //rightLight.Width = lightWidth;
            //lightSpacer.Width = frameWidth - leftLight.ActualWidth - rightLight.ActualWidth;

            gridManager.AdjustWidths(frameWidth);
        }

        private void HandleResize(object sender, SizeChangedEventArgs e)
        {
            AdjustVideoWidths(e.NewSize.Width);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Frame.SizeChanged -= HandleResize;
        }

        private async void OnStartMatch(object sender, RoutedEventArgs e)
        {
            if (CurrentMode == Mode.Idle)
            {
                ////List<Task> done = new List<Task>();
                //foreach (var channel in channels)
                //{
                //    //done.Add(channel.StartRecording("test"));
                //    await channel.StartRecording("test");
                //}
                ////Task.WaitAll(done.ToArray());

                var fileName = IsMatchSetUp ?
                    $"{leftFencer.Text}-{rightFencer.Text}" :
                    $"No match";
                await gridManager.StartRecording(fileName);

                //leftLight.Opacity = 0.50;
                //rightLight.Opacity = 0.50;

                //PauseBtn.IsEnabled = true;
                //PlayBtn.IsEnabled = true;
                //TriggerBtn.IsEnabled = true;
                CurrentMode = Mode.Recording;
                SetStatus();
            }
        }

        private async void OnStopRecording(object sender, RoutedEventArgs e)
        {
            if (CurrentMode != Mode.Idle)
            {
                ////List<Task> done = new List<Task>();
                //foreach (var channel in channels)
                //{
                //    //done.Add(channel.StopRecording());
                //    await channel.StopRecording();
                //}
                ////Task.WaitAll(done.ToArray());
                await gridManager.StopRecording();

                //PauseBtn.IsEnabled = false;
                //PlayBtn.IsEnabled = true;
                //TriggerBtn.IsEnabled = false;
                CurrentMode = Mode.Idle;
                SetStatus();
            }
        }

        public void OnKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            if (Active)
            {
                switch (e.VirtualKey)
                {
                    case VirtualKey.PageDown:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Backward);
                        break;
                    case VirtualKey.PageUp:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Forward);
                        break;
                    case VirtualKey.Space:
                        gridManager.OnPlaybackEvent(PlaybackEvent.PlayPause);
                        break;
                    case VirtualKey.Left:
                        gridManager.OnPlaybackEvent(PlaybackEvent.FrameBackward);
                        break;
                    case VirtualKey.Right:
                        gridManager.OnPlaybackEvent(PlaybackEvent.FrameForward);
                        break;
                    case VirtualKey.Enter:
                        gridManager.OnHalt(TriggerType.Halt);
                        break;
                    case VirtualKey.Home:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Live);
                        SetStatus();
                        break;
                    case VirtualKey.Number0:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed100);
                        break;
                    case VirtualKey.Number1:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed10);
                        break;
                    case VirtualKey.Number2:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed20);
                        break;
                    case VirtualKey.Number3:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed30);
                        break;
                    case VirtualKey.Number4:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed40);
                        break;
                    case VirtualKey.Number5:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed50);
                        break;
                    case VirtualKey.Number6:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed60);
                        break;
                    case VirtualKey.Number7:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed70);
                        break;
                    case VirtualKey.Number8:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed80);
                        break;
                    case VirtualKey.Number9:
                        gridManager.OnPlaybackEvent(PlaybackEvent.Speed90);
                        break;
                    default:
                        break;
                }

                SetStatus();
            }
        }

        //private void OnTogglePauseRecording(object sender, RoutedEventArgs e)
        //{
        //    if (Recording)
        //    {
        //        if (!Paused)
        //        {
        //            List<Task> done = new List<Task>();
        //            foreach (var channel in channels)
        //            {
        //                done.Add(channel.Pause());
        //            }
        //            Task.WaitAll(done.ToArray());
        //            PauseBtn.Content = "Resume";
        //            Paused = true;
        //            SetStatus("Paused");
        //        }
        //        else
        //        {
        //            List<Task> done = new List<Task>();
        //            foreach (var channel in channels)
        //            {
        //                done.Add(channel.Resume());
        //            }
        //            Task.WaitAll(done.ToArray());
        //            PauseBtn.Content = "Pause";
        //            Paused = false;
        //            SetStatus("Recording");
        //        }
        //    }
        //}

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            if (CurrentMode == Mode.Idle)
            {
                //foreach (var channel in channels)
                //{
                //    channel.StartPlayback();
                //}
                //SetStatus("Playing");
                gridManager.StartPlayback();
            }
        }

        private void OnTrigger(object sender, RoutedEventArgs e)
        {
            if (CurrentMode != Mode.Idle)
            {
                /*
                if (config.ReplayMillisAfterTrigger[CurrentWeapon] > 0)
                {
                    await Task.Delay(config.ReplayMillisAfterTrigger[CurrentWeapon] * 1000);
                    //foreach (var channel in channels)
                    //{
                    //    await channel.StopRecording();
                    //}
                }*/
                //foreach (var channel in channels)
                //{
                //    channel.StartLoop(config.ReplaySecondsAfterTrigger[CurrentWeapon] +
                //        config.ReplaySecondsBeforeTrigger[CurrentWeapon]);
                //}
                gridManager.OnHalt(TriggerType.Halt);
                CurrentMode = Mode.Replaying;
                SetStatus();
            }
        }

        private void OnDeviceConfig(object sender, RoutedEventArgs e) => _ = Frame.Navigate(typeof(ConfigPage));

        private void OnHelp(object sender, RoutedEventArgs e) => _ = Frame.Navigate(typeof(HelpPage));

        private async void OnSetupMatch(object sender, RoutedEventArgs e)
        {
            Active = false;
            var result = await MatchSetupDialog.ShowAsync();
            Active = true;
            if (result == ContentDialogResult.Primary)
            {
                if (epeeBtn.IsChecked ?? false)
                {
                    CurrentWeapon = VideoRemiseConfig.Epee;
                }
                else if (foilBtn.IsChecked ?? false)
                {
                    CurrentWeapon = VideoRemiseConfig.Foil;
                }
                else if (saberBtn.IsChecked ?? false)
                {
                    CurrentWeapon = VideoRemiseConfig.Saber;
                }
                UpdateMatchInfo();
            }
        }

        private void UpdateMatchInfo()
        {
            var weapon = char.ToUpper(VideoRemiseConfig.WeaponName(CurrentWeapon)[0]).ToString()
                + VideoRemiseConfig.WeaponName(CurrentWeapon).Substring(1);
            matchInfo.Text = IsMatchSetUp ?
                $"{weapon}: {leftFencer.Text} vs. {rightFencer.Text}" :
                "Set up match";
        }

        internal void SetStatus()
        {
            var content = CommandBar.Content as TextBlock;
            switch (CurrentMode)
            {
                case Mode.Idle:
                    content.Text = "Idle";
                    break;
                case Mode.Replaying:
                    content.Text = "Replaying";
                    break;
                case Mode.Recording:
                    content.Text = "Recording";
                    break;
            }
        }
    }
}

