using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VideoRemise
{
    internal class VideoChannel : IDisposable
    {
        public const string SaveSubfolderName = "Fencing Matches";

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
        double relativeWidth;

        bool isPreviewing = false;
        bool isRecording = false;
        bool showingLive = true;

        public MediaPlayerElement PlayerElement => mediaPlayerElement;
        public CaptureElement CaptureElement => captureElement;
        public int GridColumn => gridColumn;
        //public string VideoSource => currentSource.DisplayName;


        public double AspectRatio { get; set; }

        public double RelativeWidth 
        { 
            get { return relativeWidth; } 
            set 
            {
                relativeWidth = value;
                mainPage.LayoutGrid.ColumnDefinitions[gridColumn].Width = new GridLength(relativeWidth, GridUnitType.Star);
            } 
        }

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

        public string ChannelName
        {
            get
            {
                switch (gridColumn)
                {
                    case 0:
                        return manager.ChannelCount == 1 ? "center" : "left";
                    case 1:
                        return manager.ChannelCount == 2 ? "right" : "center";
                    default:
                        return "right";
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
                mainPage.LayoutGrid.ColumnDefinitions.Add(new ColumnDefinition() 
                    { Width = new GridLength(1.0, GridUnitType.Star) });
            }

            captureElement = new CaptureElement();
            captureElement.HorizontalAlignment = HorizontalAlignment.Stretch;
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
            //currentRecordingStream = new InMemoryRandomAccessStream();
            var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
            var fencingVideos = await myVideos.SaveFolder.CreateFolderAsync(SaveSubfolderName,
                CreationCollisionOption.OpenIfExists);
            var file = await fencingVideos.CreateFileAsync($"{fileBaseName}-{ChannelName}.mp4", 
                CreationCollisionOption.GenerateUniqueName);
            mediaRecording = await currentCapture.PrepareLowLagRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);
            //mediaRecording = await currentCapture.PrepareLowLagRecordToStreamAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), currentRecordingStream);
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

        internal async Task StartLoop(int length)
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

        internal async Task ClearSource()
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

        internal async Task SetSource(string name)
        {
            MediaFrameSourceGroup FindSource(string sourceName)
            {
                foreach (var frameSource in CurrentSources)
                {
                    if (sourceName == frameSource.DisplayName)
                    {
                        return frameSource;
                    }
                }
                return null;
            }

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

            currentSource = FindSource(name);
            if (currentSource != null)
            {
                try
                {
                    currentCapture = new MediaCapture();
                    var settings = new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = GetVideoDeviceId(currentSource),
                        MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                        StreamingCaptureMode = StreamingCaptureMode.Video
                    };
                    // TODO: The following winds up with things finishing after the Task is completed; too many layers
                    // of async. This messes up future computations that rely on AspectRation being set.
                    // There's probably a way to wait for this cleanly, probably by creating an explicit task that this code
                    // awaits and that gets completed by the inner lambda here. 
                    // The problem with just running these calls inline is that they have to run on the UI thread, so now
                    // this routine has to get called from the UI thread, and if anything takes a long time to finish, the
                    // app will be unresponsive.
                    
                    //await mainPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    //{
                    //    await currentCapture.InitializeAsync(settings);
                    //    var props = new StreamPropertiesHelper(currentCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoRecord));
                    //    AspectRatio = props.AspectRatio;
                    //    currentRequest.RequestActive();
                    //    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                    //    captureElement.Source = currentCapture;

                    //    await currentCapture.StartPreviewAsync();
                    //    isPreviewing = true;
                    //});
                    await currentCapture.InitializeAsync(settings);
                    var props = new StreamPropertiesHelper(currentCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoRecord));
                    AspectRatio = props.AspectRatio;
                    currentRequest.RequestActive();
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                    captureElement.Source = currentCapture;

                    await currentCapture.StartPreviewAsync();
                    isPreviewing = true;
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
