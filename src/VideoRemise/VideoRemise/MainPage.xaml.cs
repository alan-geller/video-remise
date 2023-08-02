using System;
using System.Threading.Tasks;
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
            this.InitializeComponent();

            gridManager = new VideoGridManager(this);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Frame.SizeChanged += HandleResize;

            await VideoChannel.Initialize();

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

            AdjustVideoWidths(Frame.ActualWidth);

            Playing = false;
            CurrentMode = Mode.Idle;

            SetStatus();
        }

        private void AdjustVideoWidths(double frameWidth)
        {
            var lightWidth = Math.Max(frameWidth / 3.0, 100);
            leftLight.Width = lightWidth;
            rightLight.Width = lightWidth;
            lightSpacer.Width = frameWidth - leftLight.ActualWidth - rightLight.ActualWidth;

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

                leftLight.Opacity = 0.50;
                rightLight.Opacity = 0.50;

                //PauseBtn.IsEnabled = true;
                PlayBtn.IsEnabled = true;
                TriggerBtn.IsEnabled = true;
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
                PlayBtn.IsEnabled = true;
                TriggerBtn.IsEnabled = false;
                CurrentMode = Mode.Idle;
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

        private async void OnPlay(object sender, RoutedEventArgs e)
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

        private async void OnTrigger(object sender, RoutedEventArgs e)
        {
            if (CurrentMode != Mode.Idle)
            {
                if (config.ReplayMillisAfterTrigger[CurrentWeapon] > 0)
                {
                    await Task.Delay(config.ReplayMillisAfterTrigger[CurrentWeapon]);
                    //foreach (var channel in channels)
                    //{
                    //    await channel.StopRecording();
                    //}
                }
                //foreach (var channel in channels)
                //{
                //    channel.StartLoop(config.ReplaySecondsAfterTrigger[CurrentWeapon] +
                //        config.ReplaySecondsBeforeTrigger[CurrentWeapon]);
                //}
                CurrentMode = Mode.Replaying;
                SetStatus();
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

        private void SetStatus()
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

