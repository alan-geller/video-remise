using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
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
        class VideoChannel
        {
            MainPage mainPage;
            int gridColumn;
            ListBox sourceSelector;
            CaptureElement captureElement;
            MediaFrameSourceGroup currentSource;
            MediaCapture currentCapture;
            DisplayRequest currentRequest;
            LowLagMediaRecording mediaRecording;

            bool isPreviewing = false;

            static IReadOnlyList<MediaFrameSourceGroup> CurrentSources;

            internal VideoChannel(MainPage page)
            {
                mainPage = page;

                gridColumn = mainPage.LayoutGrid.ColumnDefinitions.Count;
                var columnWidth = 300;
                foreach (var cd in mainPage.LayoutGrid.ColumnDefinitions)
                {
                    cd.MaxWidth = columnWidth;
                }
                mainPage.LayoutGrid.ColumnDefinitions.Add(new ColumnDefinition() { MaxWidth = columnWidth });

                sourceSelector = new ListBox();
                PopulateSourceList(sourceSelector);
                mainPage.LayoutGrid.Children.Add(sourceSelector);
                Grid.SetColumn(sourceSelector, gridColumn);
                Grid.SetRow(sourceSelector, 0);

                captureElement = new CaptureElement();
                mainPage.LayoutGrid.Children.Add(captureElement);
                Grid.SetColumn(captureElement, gridColumn);
                Grid.SetRow(captureElement, 1);

                Action<object, SelectionChangedEventArgs> eventHandler = (object sender, SelectionChangedEventArgs e) => SourceSelector_SelectionChanged(this, sender, e);
                sourceSelector.SelectionChanged += new SelectionChangedEventHandler(eventHandler);

                currentRequest = new DisplayRequest();
            }

            internal async Task Pause()
            {
            }

            internal async Task Resume()
            {

            }

            internal async Task<IAsyncAction> StartRecording(string fileBaseName)
            {
                var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
                StorageFile file = await myVideos.SaveFolder.CreateFileAsync($"{fileBaseName}-{gridColumn}.mp4", CreationCollisionOption.GenerateUniqueName);
                mediaRecording = await currentCapture.PrepareLowLagRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);
                return mediaRecording.StartAsync();
            }

            internal async Task<IAsyncAction> StopRecording()
            {
                await mediaRecording.StopAsync();
                return mediaRecording.FinishAsync();
            }

            private async void SourceSelector_SelectionChanged(VideoChannel channel, object sender, SelectionChangedEventArgs e)
            {
                var source = FindMediaSource(channel.sourceSelector.SelectedItem.ToString());
                if (source != null)
                {
                    channel.SetSource(source);
                }
                else
                {
                    channel.ClearSource();
                }
            }

            async void ClearSource()
            {
                if (currentCapture != null)
                {
                    if (isPreviewing)
                    {
                        await currentCapture.StopPreviewAsync();
                    }

                    await mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        captureElement.Source = null;
                        if (currentRequest != null)
                        {
                            currentRequest.RequestRelease();
                            currentRequest = new DisplayRequest();
                        }

                        currentCapture.Dispose();
                        currentCapture = null;
                    });

                    isPreviewing = false;
                }
            }

            async void SetSource(MediaFrameSourceGroup sourceGroup)
            {
                ClearSource();

                currentSource = sourceGroup;

                try
                {
                    currentCapture = new MediaCapture();
                    var settings = new MediaCaptureInitializationSettings { VideoDeviceId = GetVideoDeviceId(currentSource) };
                    await mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await currentCapture.InitializeAsync(settings);
                        currentRequest.RequestActive();
                        DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                        captureElement.Source = currentCapture;
                        await currentCapture.StartPreviewAsync();
                        isPreviewing = true;
                        mainPage.Paused = true;
                    });

                }
                catch (UnauthorizedAccessException)
                {
                    // This will be thrown if the user denied access to the camera in privacy settings
                    await (new MessageDialog("The app was denied access to the camera")).ShowAsync();
                    return;
                }
                catch (System.IO.FileLoadException)
                {
                    //currentCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
                }
            }

            static MediaFrameSourceGroup FindMediaSource(string displayName)
            {
                // This method will never get called when CurrentSources is uninitialized
                foreach (var frameSource in CurrentSources)
                {
                    if (displayName == frameSource.DisplayName)
                    {
                        return frameSource;
                    }
                }
                return null;
            }

            static string GetVideoDeviceId(MediaFrameSourceGroup sourceGroup)
            {
                foreach (var sourceInfo in sourceGroup.SourceInfos)
                {
                    if (sourceInfo.MediaStreamType == MediaStreamType.VideoRecord)
                    {
                        return sourceInfo.DeviceInformation.Id;
                    }
                }
                return "";
            }

            static async void PopulateSourceList(ListBox listBox)
            {
                if (CurrentSources == null)
                {
                    CurrentSources = await MediaFrameSourceGroup.FindAllAsync();
                }

                listBox.Items.Clear();
                foreach (var frameSource in CurrentSources)
                {
                    listBox.Items.Add(frameSource.DisplayName);
                }
                listBox.Items.Add("--none--");
            }
        }

        List<VideoChannel> channels;

        public bool Paused { get; set; }
        public bool Recording { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            channels = new List<VideoChannel>();
            channels.Add(new VideoChannel(this));
            channels.Add(new VideoChannel(this));

            Paused = false;
            Recording = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
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
    }
}

