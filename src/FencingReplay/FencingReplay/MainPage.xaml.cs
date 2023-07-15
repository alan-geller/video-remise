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

        public bool Paused { get; set; }
        public bool Recording { get; set; }

        private FencingReplayConfig config;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            config = (Application.Current as App).Config;

            channels = new List<VideoChannel>();
            channels.Add(new VideoChannel(this));
            channels.Add(new VideoChannel(this));

            Paused = false;
            Recording = false;


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
                }
            }
        }

        private void OnPlay(object sender, RoutedEventArgs e)
        {
            foreach (var channel in channels)
            {
                channel.StartPlayback();
            }
        }

        private async void OnTrigger(object sender, RoutedEventArgs e)
        {
            if (config.ReplaySecondsAfterTrigger > 0)
            {
                await Task.Delay(1000 * config.ReplaySecondsAfterTrigger);
                foreach (var channel in channels)
                {
                    await channel.StopRecording();
                }
                foreach (var channel in channels)
                {
                    channel.StartLoop(config.ReplaySecondsAfterTrigger + config.ReplaySecondsBeforeTrigger);
                }
            }
        }

        private void OnDeviceConfig(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ConfigPage));
        }
    }
}

