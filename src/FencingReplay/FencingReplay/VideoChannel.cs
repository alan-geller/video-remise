using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FencingReplay
{
    internal class VideoChannel : IDisposable
    {
        MainPage mainPage;
        int gridColumn;
        CaptureElement captureElement;
        MediaFrameSourceGroup currentSource;
        MediaCapture currentCapture;
        MediaPlayerElement mediaPlayerElement;
        DisplayRequest currentRequest;
        LowLagMediaRecording mediaRecording;
        InMemoryRandomAccessStream currentRecordingStream;
        MediaSource activeSource;
        VideoGridManager manager;

        bool isPreviewing = false;
        bool isRecording = false;
        bool showingLive = true;

        public MediaPlayerElement PlayerElement => mediaPlayerElement;
        public CaptureElement CaptureElement => captureElement;

        public Visibility Visibility 
        {
            get
            {
                if ((mediaPlayerElement.Visibility == Visibility.Collapsed) &&
                    (captureElement.Visibility == Visibility.Collapsed))
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            } 
            set 
            { 
                if (showingLive)
                {
                    captureElement.Visibility = value;
                }
                else
                {
                    mediaPlayerElement.Visibility = value;
                }
            } 
        }

        public string VideoSource
        {
            get { return currentSource.DisplayName; }
            set
            {
                var flag = false;
                foreach (var frameSource in CurrentSources)
                {
                    if (value == frameSource.DisplayName)
                    {
                        SetSource(frameSource);
                        flag = true;
                    }
                }
                if (!flag)
                {
                    ClearSource();
                }
            }
        }

        static IReadOnlyList<MediaFrameSourceGroup> CurrentSources;
        private bool disposedValue;

        internal VideoChannel(int col, MainPage page, VideoGridManager mgr)
        {
            mainPage = page;
            gridColumn = col;

            while (mainPage.LayoutGrid.ColumnDefinitions.Count <= gridColumn)
            {
                mainPage.LayoutGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            }

            captureElement = new CaptureElement();
            mainPage.LayoutGrid.Children.Add(captureElement);
            Grid.SetColumn(captureElement, gridColumn);
            Grid.SetRow(captureElement, 0);
            captureElement.IsDoubleTapEnabled = true;
            captureElement.DoubleTapped += OnCoubleClick;

            mediaPlayerElement = new MediaPlayerElement();
            mediaPlayerElement.Visibility = Visibility.Collapsed;
            mainPage.LayoutGrid.Children.Add(mediaPlayerElement);
            Grid.SetColumn(mediaPlayerElement, gridColumn);
            Grid.SetRow(mediaPlayerElement, 0);
            mediaPlayerElement.IsDoubleTapEnabled = true;
            mediaPlayerElement.DoubleTapped += OnCoubleClick;

            //Action<object, SelectionChangedEventArgs> eventHandler = (object sender, SelectionChangedEventArgs e) => SourceSelector_SelectionChanged(this, sender, e);
            //sourceSelector.SelectionChanged += new SelectionChangedEventHandler(eventHandler);

            currentRequest = new DisplayRequest();
            manager = mgr;
        }

        internal void OnCoubleClick(object sender, RoutedEventArgs e)
        {
            manager.ToggleZoom(gridColumn);
        }

        internal async Task ShutdownAsync()
        {
            if (isRecording)
            {
                await StopRecording();
            }
            if (mediaPlayerElement.MediaPlayer != null)
            {
                mediaPlayerElement.MediaPlayer.Dispose();
            }
            if (currentRecordingStream != null)
            {
                currentRecordingStream.Dispose();
            }

            captureElement.Visibility = Visibility.Collapsed;
            mediaPlayerElement.Visibility = Visibility.Collapsed;
            mainPage.LayoutGrid.ColumnDefinitions.RemoveAt(gridColumn);
        }

        internal async Task Pause()
        {
        }

        internal async Task Resume()
        {

        }

        internal async Task<IAsyncAction> StartRecording(string fileBaseName)
        {
            currentRecordingStream = new InMemoryRandomAccessStream();
            //var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
            //StorageFile file = await myVideos.SaveFolder.CreateFileAsync($"{fileBaseName}-{gridColumn}.mp4", CreationCollisionOption.GenerateUniqueName);
            //mediaRecording = await currentCapture.PrepareLowLagRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);
            mediaRecording = await currentCapture.PrepareLowLagRecordToStreamAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), currentRecordingStream);
            isRecording = true;
            return mediaRecording.StartAsync();
        }

        internal async Task<IAsyncAction> StopRecording()
        {
            isRecording = false;
            await mediaRecording.StopAsync();
            return mediaRecording.FinishAsync();
        }

        internal void StartPlayback()
        {
            captureElement.Visibility = Visibility.Collapsed;
            mediaPlayerElement.Visibility = Visibility.Visible;
            showingLive = false;
            mediaPlayerElement.Source = MediaSource.CreateFromStream(currentRecordingStream, MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto).ToString());
            mediaPlayerElement.MediaPlayer.Play();
        }

        internal async void StartLoop(int length)
        {
            if (isRecording)
            {
                await StopRecording();
            }
            captureElement.Visibility = Visibility.Collapsed;
            mediaPlayerElement.Visibility = Visibility.Visible;
            showingLive = false;
            activeSource = MediaSource.CreateFromStream(currentRecordingStream, MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto).ToString());
            mediaPlayerElement.Source = activeSource;
            var duration = activeSource.Duration?.TotalSeconds ?? 0;
            mediaPlayerElement.MediaPlayer.PlaybackSession.Position = System.TimeSpan.FromSeconds(Math.Max(duration - length, 0));
            mediaPlayerElement.MediaPlayer.Play();
        }

        internal async void ClearSource()
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
                    currentRecordingStream.Dispose();
                    currentRecordingStream = null;
                });

                isPreviewing = false;
            }
        }

        internal async void SetSource(MediaFrameSourceGroup sourceGroup)
        {
            string GetVideoDeviceId(MediaFrameSourceGroup sg)
            {
                foreach (var sourceInfo in sg.SourceInfos)
                {
                    if (sourceInfo.MediaStreamType == MediaStreamType.VideoRecord)
                    {
                        return sourceInfo.DeviceInformation.Id;
                    }
                }
                return "";
            }

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

        public static async Task Initialize()
        {
            if (CurrentSources == null)
            {
                // We use the GetResults method to forcibly de-async; that is, to block.
                CurrentSources = await MediaFrameSourceGroup.FindAllAsync();
            }
        }

        protected virtual async void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (isRecording)
                    {
                        await StopRecording();
                    }
                    if (mediaPlayerElement.MediaPlayer != null)
                    {
                        mediaPlayerElement.MediaPlayer.Dispose();
                        mediaPlayerElement = null;
                    }
                    if (currentRecordingStream != null)
                    {
                        currentRecordingStream.Dispose();
                        currentRecordingStream = null;
                    }
                }

                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~VideoChannel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
