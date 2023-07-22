using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Capture.Frames;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using YamlDotNet.Serialization;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FencingReplay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<VideoChannel> channels;

        public int CurrentWeapon { get; set; }
        public bool Paused { get; set; }
        public bool Recording { get; set; }

        private FencingReplayConfig config;

        private bool IsMatchSetUp { 
            get
            {
                return ((epeeBtn.IsChecked == true) ||
                    (foilBtn.IsChecked == true) ||
                    (saberBtn.IsChecked == true)) &&
                    !string.IsNullOrWhiteSpace(leftFencer.Text) &&
                    !string.IsNullOrWhiteSpace(righttFencer.Text);
            } 
        }

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await VideoChannel.Initialize();

            config = (Application.Current as App).Config;

            channels = new List<VideoChannel>();
            foreach (var source in config.VideoSources)
            {
                channels.Add(new VideoChannel(this) { VideoSource = source });
            }

            if (IsMatchSetUp)
            {
                matchSetupPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                matchSetupPanel.Visibility = Visibility.Visible;
            }
            UpdateMatchInfo();

            Paused = false;
            Recording = false;

            SetStatus("Ready");
        }

        private MediaFrameSourceGroup FindMediaSource(string displayName)
        {
            // We have to use the blocking GetResults to transition from asynch to synch
            var frameSources = MediaFrameSourceGroup.FindAllAsync().GetResults();
            foreach (var frameSource in frameSources)
            {
                if (displayName == frameSource.DisplayName)
                {
                    return frameSource;
                }
            }
            return null;
        }

        private void SetVideoSource(CaptureElement captureElement, string displayName)
        {

        }

        private async void OnStartRecording(object sender, RoutedEventArgs e)
        {
            //List<Task> done = new List<Task>();
            foreach (var channel in channels)
            {
                //done.Add(channel.StartRecording("test"));
                await channel.StartRecording("test");
            }
            //Task.WaitAll(done.ToArray());

            PauseBtn.IsEnabled = true;
            PlayBtn.IsEnabled = true;
            TriggerBtn.IsEnabled = true;
            Recording = true;
            SetStatus("Recording");
        }

        private async void OnStopRecording(object sender, RoutedEventArgs e)
        {
            //List<Task> done = new List<Task>();
            foreach (var channel in channels)
            {
                //done.Add(channel.StopRecording());
                await channel.StopRecording();
            }
            //Task.WaitAll(done.ToArray());

            PauseBtn.IsEnabled = false;
            PlayBtn.IsEnabled = true;
            TriggerBtn.IsEnabled = false;
            Recording = false;
            SetStatus("Ready");
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

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            foreach (var channel in channels)
            {
                channel.StartPlayback();
            }
            SetStatus("Playing");
        }

        private async void OnTrigger(object sender, RoutedEventArgs e)
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

        private void OnDeviceConfig(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ConfigPage));
        }

        private void OnSetupMatch(object sender, RoutedEventArgs e)
        {
            if (matchSetupPanel.Visibility == Visibility.Visible)
            {
                matchSetupPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                matchSetupPanel.Visibility = Visibility.Visible;
            }
        }

        private void UpdateMatchInfo()
        {
            var weapon = char.ToUpper(FencingReplayConfig.WeaponName(CurrentWeapon)[0]).ToString()
                + FencingReplayConfig.WeaponName(CurrentWeapon).Substring(1);
            matchInfo.Text = IsMatchSetUp ?
                $"{weapon}: {leftFencer.Text} vs. {righttFencer.Text}" :
                "Set up match";
        }

        private void SetStatus(string status)
        {
            var content = CommandBar.Content as TextBlock;
            content.Text = status;
        }

        private void epeeBtn_Click(object sender, RoutedEventArgs e)
        {
            CurrentWeapon = FencingReplayConfig.Epee;
            UpdateMatchInfo();
        }

        private void foilBtn_Click(object sender, RoutedEventArgs e)
        {
            CurrentWeapon = FencingReplayConfig.Foil;
            UpdateMatchInfo();
        }

        private void saberBtn_Click(object sender, RoutedEventArgs e)
        {
            CurrentWeapon = FencingReplayConfig.Saber;
            UpdateMatchInfo();
        }

        private void leftFencer_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateMatchInfo();
        }

        private void righttFencer_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateMatchInfo();
        }
    }
}

