﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace VideoRemise
{
    public enum PlaybackEvent
    {
        PlayPause,
        Forward,
        Backward,
        FrameForward,
        FrameBackward,
        Live,
        ForwardTag,
        BackwardTag,
        Tag,
        Escape,
        ReTrigger,
        Speed10,
        Speed20,
        Speed30,
        Speed40,
        Speed50,
        Speed60,
        Speed70,
        Speed80,
        Speed90,
        Speed100,
    }

    internal class VideoChannel : IDisposable
    {
        public const string SaveSubfolderName = "Fencing Matches";

        private MainPage mainPage;
        private int gridColumn;
        private CaptureElement captureElement;
        private MediaFrameSourceGroup currentSourceGroup;
        private MediaCapture currentCapture;
        private MediaPlayerElement mediaPlayerElement;
        private DisplayRequest currentRequest;
        private StorageFile currentFile;
        private List<PhraseRecording> actions;
        private LowLagMediaRecording mediaRecording;
        private InMemoryRandomAccessStream currentRecordingStream;
        private IMediaExtension recordEffect;
        private IMediaExtension previewEffect;
        private VideoGridManager manager;
        private double relativeWidth;
        private int currentReplay = -1;
        private string baseName;

        private bool isPreviewing = false;
        private bool isRecording = false;
        private bool showingLive = true;
        private bool playing = false;
        private bool escaped = false;
        private bool focused = false;

        public MediaPlayerElement PlayerElement => mediaPlayerElement;
        public CaptureElement CaptureElement => captureElement;
        public MediaCapture Capture => currentCapture;
        public int GridColumn => gridColumn;

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
                    (captureElement?.Visibility == Visibility.Collapsed))
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

        static public IReadOnlyList<MediaFrameSourceGroup> CurrentSources;
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

            actions = new List<PhraseRecording>();

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
            if (mediaPlayerElement?.MediaPlayer != null)
            {
                mediaPlayerElement?.MediaPlayer.Dispose();
            }
            if (currentRecordingStream != null)
            {
                currentRecordingStream.Dispose();
            }

            if (captureElement != null)
            {
                captureElement.Visibility = Visibility.Collapsed;
            }
            if (mediaPlayerElement != null)
            {
                mediaPlayerElement.Visibility = Visibility.Collapsed;
            }
            mainPage.LayoutGrid.ColumnDefinitions.RemoveAt(gridColumn);
        }

        // Various methods for controlling playback
        internal void OnPlaybackEvent(PlaybackEvent playbackEvent)
        {
            if (!playing && playbackEvent != PlaybackEvent.Backward && playbackEvent != PlaybackEvent.Forward)
            {
                return;
            }

            switch (playbackEvent)
            {
                case PlaybackEvent.PlayPause:
                    PlayPause();
                    break;
                case PlaybackEvent.Forward:
                    Forward();
                    break;
                case PlaybackEvent.Backward:
                    Back();
                    break;
                case PlaybackEvent.FrameForward:
                    FrameForward();
                    break;
                case PlaybackEvent.FrameBackward:
                    FrameBackward();
                    break;
                case PlaybackEvent.Live:
                    Live();
                    break;
                case PlaybackEvent.ForwardTag:
                    ForwardTag();
                    break;
                case PlaybackEvent.BackwardTag:
                    BackwardTag();
                    break;
                case PlaybackEvent.Tag:
                    Tag();
                    break;
                case PlaybackEvent.Escape:
                    Escape();
                    break;
                case PlaybackEvent.ReTrigger:
                    ReTrigger();
                    break;
                case PlaybackEvent.Speed10:
                    SetSpeed(.1);
                    break;
                case PlaybackEvent.Speed20:
                    SetSpeed(.2);
                    break;
                case PlaybackEvent.Speed30:
                    SetSpeed(.3);
                    break;
                case PlaybackEvent.Speed40:
                    SetSpeed(.4);
                    break;
                case PlaybackEvent.Speed50:
                    SetSpeed(.5);
                    break;
                case PlaybackEvent.Speed60:
                    SetSpeed(.6);
                    break;
                case PlaybackEvent.Speed70:
                    SetSpeed(.7);
                    break;
                case PlaybackEvent.Speed80:
                    SetSpeed(.8);
                    break;
                case PlaybackEvent.Speed90:
                    SetSpeed(.9);
                    break;
                case PlaybackEvent.Speed100:
                    SetSpeed(1);
                    break;
            }
        }

        private void ReTrigger()
        {
            if (!escaped)
            {
                return;
            }

            mediaPlayerElement.MediaPlayer.Pause();
            var phrase = actions.ElementAt(currentReplay);
            var splitTime = mediaPlayerElement.MediaPlayer.PlaybackSession.Position;

            // This always splits at the current time, allowing replay overlap (but I don't think that's a bad thing)
            var newPhrase = phrase.SplitAt(splitTime);
            actions.Insert(currentReplay, newPhrase);
            PlayFromFile();
        }

        private async void Escape()
        {
            var phrase = actions.ElementAt(currentReplay);
            var duration = mediaPlayerElement.MediaPlayer.PlaybackSession.NaturalDuration;
            if (phrase.ReplayLength >= duration) {
                await new MessageDialog("WARNING: Nothing to escape, the phrase recording is shorter than the replay length").ShowAsync();
                return;
            }
            escaped = true;
            mediaPlayerElement.MediaPlayer.Pause();
            mediaPlayerElement.MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
            mediaPlayerElement.MediaPlayer.Play();
        }

        internal void PlayPause()
        {
            if (mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
            {
                mediaPlayerElement.MediaPlayer.Play();
            }
            else
            {
                mediaPlayerElement.MediaPlayer.Pause();
            }
        }

        internal void Resume()
        {
            mediaPlayerElement.MediaPlayer.Play();
        }

        internal void SetSpeed(double speed)
        {
            mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackRate = speed;
        }

        internal void FrameForward()
        {
            mediaPlayerElement.MediaPlayer.StepForwardOneFrame();
        }

        internal void FrameBackward()
        {
            mediaPlayerElement.MediaPlayer.StepBackwardOneFrame();
        }

        internal void Back()
        {
            currentReplay = Math.Max(currentReplay - 1, 0);
            PlayFromFile();
        }

        internal void Forward()
        {
            currentReplay = Math.Min(currentReplay + 1, actions.Count - 1);
            PlayFromFile();
        }

        internal void Live()
        {
            captureElement.Visibility = Visibility.Visible;
            mediaPlayerElement.Visibility = Visibility.Collapsed;
            showingLive = true;
            playing = false;
            escaped = false;
            focused = false;
            mainPage.CurrentMode = Mode.Recording;
        }

        internal async void BackwardTag()
        {
            for (var i = currentReplay - 1; i >= 0; i--)
            {
                if (actions.ElementAt(i).Tag)
                {
                    currentReplay = i;
                    PlayFromFile();
                    return;
                }
            }

            await (new MessageDialog("No more tagged videos before this one")).ShowAsync();
        }

        internal async void ForwardTag()
        {
            for (var i = currentReplay + 1; i < actions.Count; i++)
            {
                if (actions.ElementAt(i).Tag)
                {
                    currentReplay = i;
                    PlayFromFile();
                    return;
                }
            }

            await (new MessageDialog("No more tagged videos after this one")).ShowAsync();
        }

        internal void Tag()
        {
            if (focused)
            {
                focused = false;
                return;
            }

            var phrase = actions.ElementAt(currentReplay);
            phrase.Tag = true;
            focused = true;
        }

        internal async Task<IAsyncAction> StartRecording(string fileBaseName)
        {
            baseName = fileBaseName;
            return await StartPhrase();
        }

        internal async Task<IAsyncAction> StartPhrase()
        {
            var myVideos = await StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
            var fencingVideos = await myVideos.SaveFolder.CreateFolderAsync(SaveSubfolderName,
                CreationCollisionOption.OpenIfExists);
            currentFile = await fencingVideos.CreateFileAsync($"{baseName}-{ChannelName}.mp4",
                CreationCollisionOption.GenerateUniqueName);
            mediaRecording = await currentCapture.PrepareLowLagRecordToStorageFileAsync(MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), currentFile);
            isRecording = true;
            return mediaRecording.StartAsync();
        }

        internal async Task EndPhrase(TimeSpan length, TriggerType triggerType)
        {
            if (!isRecording)
            {
                return;
            }

            await StopRecording();
            PhraseRecording pr = new PhraseRecording(currentFile, length, null, triggerType);
            actions.Add(pr);
        }

        internal async Task<IAsyncAction> StopRecording()
        {
            isRecording = false;
            await mediaRecording?.StopAsync();
            return mediaRecording?.FinishAsync();
        }

        internal async void Trigger(TimeSpan length, TriggerType triggerType)
        {
            if (!isRecording)
            {
                return;
            }

            await EndPhrase(length, triggerType);
            await StartPhrase();
            if (!focused) {
                StartPlayback();
            }
        }

        // Todo: Kinda ugly
        private void PlayFromFile()
        {
            captureElement.Visibility = Visibility.Collapsed;
            mediaPlayerElement.Visibility = Visibility.Visible;
            showingLive = false;
            playing = true;
            escaped = false;
            mainPage.CurrentMode = Mode.Replaying;

            var phrase = actions.ElementAt(currentReplay);
            var source = phrase.GetSource();
            var start = phrase.ReplayStart;
            var length = phrase.ReplayLength;

            void StartLoop (MediaPlayer player) {
                player.Pause();
                if (escaped)
                {
                    player.PlaybackSession.Position = TimeSpan.Zero;
                }
                else if (start == null)
                {
                    var timeToBegin = player.PlaybackSession.NaturalDuration - length;
                    player.PlaybackSession.Position = TSMax(TimeSpan.Zero, timeToBegin);
                }
                else
                {
                    player.PlaybackSession.Position = start.Value;
                }

                player.Play();
            };

            // This has to come before the two event additions because otherwise there's
            // an odd runtime error adding the event hadler that "this" is null in the 
            // handler delegate. Maybe the MediaPlayer element doesn't get initialized
            // until the source is set?
            mediaPlayerElement.Source = source;
            mediaPlayerElement.MediaPlayer.MediaOpened += (MediaPlayer sender, object args) =>
            {
                StartLoop(sender);
            };
            mediaPlayerElement.MediaPlayer.MediaEnded += (MediaPlayer sender, object args) =>
            {
                StartLoop(sender);
            };

            if (start != null) {
                mediaPlayerElement.MediaPlayer.PlaybackSession.PositionChanged += (MediaPlaybackSession session, object args) =>
                {
                    if (session.Position > start + length)
                    {
                        if (escaped)
                        {
                            session.Position = TimeSpan.Zero;
                        }
                        else
                        {
                            session.Position = start.Value;
                        }
                    }
                };
            }
        }

        internal void StartPlayback()
        {
            currentReplay = actions.Count - 1;
            PlayFromFile();
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

        internal async Task SetSource(string id)
        {
            MediaFrameSourceGroup FindSource(string sourceId)
            {
                foreach (var frameSource in CurrentSources)
                {
                    if (sourceId == frameSource.Id)
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

            await ClearSource();

            currentSourceGroup = FindSource(id);
            if (currentSourceGroup != null)
            {
                try
                {
                    currentCapture = new MediaCapture();
                    var settings = new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = GetVideoDeviceId(currentSourceGroup),
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

                    var lightEffect = new VideoEffectDefinition("LightDisplayVisualEffect.LightDisplayEffect");
                    if (currentCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.AllStreamsIdentical ||
                        currentCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.PreviewRecordStreamsIdentical)
                    {
                        // This effect will modify both the preview and the record streams, because they are the same stream.
                        recordEffect = await currentCapture.AddVideoEffectAsync(lightEffect, MediaStreamType.VideoRecord);
                        previewEffect = null;
                    }
                    else
                    {
                        recordEffect = await currentCapture.AddVideoEffectAsync(lightEffect, MediaStreamType.VideoRecord);
                        previewEffect = await currentCapture.AddVideoEffectAsync(lightEffect, MediaStreamType.VideoPreview);
                    }

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

        public void SetProperty(string propertyName, object value)
        {
            var effectProperties = new PropertySet();
            effectProperties[propertyName] = value;
            if (recordEffect != null)
            {
                recordEffect.SetProperties(effectProperties);
            }
            if (previewEffect != null)
            {
                previewEffect.SetProperties(effectProperties);
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
                        // EndPhrase will ensure the current recording is saved in case of accidental shutdown
                        // It will ensure the recording is closed
                        await EndPhrase(TimeSpan.Zero, TriggerType.Halt);
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

        internal static TimeSpan TSMax(TimeSpan ts1, TimeSpan ts2)
        {
            if (ts1 > ts2)
            {
                return ts1;
            }
            else
            {
                return ts2;
            }
        }

        internal static TimeSpan TSMin(TimeSpan ts1, TimeSpan ts2)
        {
            if (ts1 < ts2)
            {
                return ts1;
            }
            else
            {
                return ts2;
            }
        }
    }
}
