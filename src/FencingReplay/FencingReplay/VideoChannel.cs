using System;
using System.Collections.Generic;
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
    public sealed partial class MainPage
    {
        class VideoChannel
        {
            MainPage mainPage;
            int gridColumn;
            ListBox sourceSelector;
            CaptureElement captureElement;
            MediaFrameSourceGroup currentSource;
            MediaCapture currentCapture;
            MediaPlayerElement mediaPlayerElement;
            DisplayRequest currentRequest;
            LowLagMediaRecording mediaRecording;
            InMemoryRandomAccessStream currentRecordingStream;
            MediaSource activeSource;

            bool isPreviewing = false;
            bool isRecording = false;

            public string VideoSource 
            { 
                get { return currentSource.DisplayName; }
                set 
                {
                    var source = FindMediaSource(value);
                    if (source != null)
                    {
                        SetSource(source);
                    }
                    else
                    {
                        ClearSource();
                    }
                }
            }

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

                mediaPlayerElement = new MediaPlayerElement();
                mediaPlayerElement.Visibility = Visibility.Collapsed;
                mainPage.LayoutGrid.Children.Add(mediaPlayerElement);
                Grid.SetColumn(mediaPlayerElement, gridColumn);
                Grid.SetRow(mediaPlayerElement, 1);

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
                activeSource = MediaSource.CreateFromStream(currentRecordingStream, MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto).ToString());
                mediaPlayerElement.Source = activeSource;
                var duration = activeSource.Duration?.TotalSeconds ?? 0;
                mediaPlayerElement.MediaPlayer.PlaybackSession.Position = System.TimeSpan.FromSeconds(Math.Max(duration - length, 0));
                mediaPlayerElement.MediaPlayer.Play();
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
                        currentRecordingStream.Dispose();
                        currentRecordingStream = null;
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
    }
}

