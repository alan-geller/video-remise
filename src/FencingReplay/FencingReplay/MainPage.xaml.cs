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

            bool isPreviewing = false;

            static IReadOnlyList<MediaFrameSourceGroup> CurrentSources;

            internal VideoChannel(MainPage page, int column)
            {
                mainPage = page;

                gridColumn = column;
                while (mainPage.LayoutGrid.ColumnDefinitions.Count <= gridColumn)
                {
                    mainPage.LayoutGrid.ColumnDefinitions.Add(new ColumnDefinition());
                }

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

            internal async void StartRecording(string fileBaseName)
            {
                var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
                StorageFile file = await myVideos.SaveFolder.CreateFileAsync($"{fileBaseName}{gridColumn}.mp4", CreationCollisionOption.GenerateUniqueName);

            }

            internal async void StopRecording()
            {

            }

            private async void SourceSelector_SelectionChanged(VideoChannel channel, object sender, SelectionChangedEventArgs e)
            {
                var source = FindMediaSource(channel.sourceSelector.SelectedItem.ToString());
                if (source != null)
                {
                    channel.SetSource(source);
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

                    await captureElement.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        captureElement.Source = null;
                        if (currentRequest != null)
                        {
                            currentRequest.RequestRelease();
                        }

                        currentCapture.Dispose();
                        currentCapture = null;
                    });

                    isPreviewing = false;
                }
            }

            async void SetSource(MediaFrameSourceGroup sourceGroup)
            {
                if (isPreviewing)
                {
                    ClearSource();
                }

                currentSource = sourceGroup;

                try
                {
                    currentCapture = new MediaCapture();
                    var settings = new MediaCaptureInitializationSettings { VideoDeviceId = GetVideoDeviceId(currentSource) };
                    await currentCapture.InitializeAsync(settings);

                    currentRequest.RequestActive();
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                }
                catch (UnauthorizedAccessException)
                {
                    // This will be thrown if the user denied access to the camera in privacy settings
                    await (new MessageDialog("The app was denied access to the camera")).ShowAsync();
                    return;
                }

                try
                {
                    captureElement.Source = currentCapture;
                    await currentCapture.StartPreviewAsync();
                    isPreviewing = true;
                    mainPage.Paused = true;
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

                    //FileSavePicker savePicker = new FileSavePicker();
                    //savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    //savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
                    //savePicker.SuggestedFileName = "sources";

                    //StorageFile file = await savePicker.PickSaveFileAsync();
                    //if (file != null)
                    //{
                    //    using (var buffer = new StringWriter())
                    //    {
                    //        foreach (var frameSource in frameSources)
                    //        {
                    //            buffer.WriteLine($"Source group {frameSource.DisplayName}:");
                    //            buffer.WriteLine($"  ID: {frameSource.Id}");
                    //            foreach (var src in frameSource.SourceInfos)
                    //            {
                    //                buffer.WriteLine($"    Source ID: {src.Id}");
                    //                buffer.WriteLine($"      Source Kind: {src.SourceKind}");
                    //                buffer.WriteLine($"      Stream type: {src.MediaStreamType}");
                    //            }
                    //        }
                    //        await FileIO.WriteTextAsync(file, buffer.ToString());
                    //    }
                    //}    
                }

                listBox.Items.Clear();
                foreach (var frameSource in CurrentSources)
                {
                    listBox.Items.Add(frameSource.DisplayName);
                } 
            }
        }

        List<VideoChannel> channels;

        public bool Paused { get; set; }
        public bool Recording { get; set; }

        public MainPage()
        {
            this.InitializeComponent();

            channels = new List<VideoChannel>();
            channels.Add(new VideoChannel(this, 0));
            channels.Add(new VideoChannel(this, 1));

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

        private void OnStartRecording(object sender, RoutedEventArgs e)
        {
            PauseBtn.IsEnabled = true;
            Recording = true;
        }

        private void OnStopRecording(object sender, RoutedEventArgs e)
        {
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

