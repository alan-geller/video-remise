using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace VideoRemise
{
    internal class LightDisplay
    {
        private List<Trigger> triggers = new List<Trigger>();
        private Lights lightStatus;
        private MediaFrameReader mediaFrameReader;
        private VideoChannel channel;
        private SoftwareBitmap backBuffer;
        private bool taskRunning = false;
        private MediaSource processedSource;

        public MediaSource ProcessedSource => processedSource;

        public LightDisplay(VideoChannel ch)
        {
            channel = ch;
        }

        // Must be called after the source is set on the video channel
        public async Task Start()
        {
            var frameSource = channel.Capture.FrameSources.First().Value;
            mediaFrameReader = 
                await channel.Capture.CreateFrameReaderAsync(frameSource,
                MediaEncodingSubtypes.Rgb32);
            mediaFrameReader.FrameArrived += OnFrame;
            processedSource = MediaSource.CreateFromMediaFrameSource(frameSource);
        }

        public void Stop()
        {
            if (mediaFrameReader != null)
            {
                mediaFrameReader.FrameArrived -= OnFrame;
            }
            foreach (var trigger in triggers)
            {
                trigger.OnLight -= OnLightStatus;
            }
        }

        private void OnFrame(MediaFrameReader sender,
            MediaFrameArrivedEventArgs args)
        {
            var mediaFrameReference = sender.TryAcquireLatestFrame();
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

            if (softwareBitmap != null)
            {
                if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                // Swap the processed frame to _backBuffer and dispose of the unused image.
                softwareBitmap = Interlocked.Exchange(ref backBuffer, softwareBitmap);
                softwareBitmap?.Dispose();

                // Changes to XAML ImageElement must happen on UI thread through Dispatcher
                var task = channel.DisplayImage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        // Don't let two copies of this task run at the same time.
                        if (taskRunning)
                        {
                            return;
                        }
                        taskRunning = true;

                        // Keep draining frames from the backbuffer until the backbuffer is empty.
                        SoftwareBitmap latestBitmap;
                        while ((latestBitmap = Interlocked.Exchange(ref backBuffer, null)) != null)
                        {
                            var imageSource = (SoftwareBitmapSource)channel.DisplayImage.Source;
                            await imageSource.SetBitmapAsync(latestBitmap);
                            latestBitmap.Dispose();
                        }

                        taskRunning = false;
                    });
            }

            mediaFrameReference.Dispose();
        }

        public void AddTrigger(Trigger trigger)
        {
            triggers.Add(trigger);
            trigger.OnLight += OnLightStatus;
        }

        public void OnLightStatus(object sender, Trigger.LightEventArgs e)
        {
            lightStatus = e.LightsOn;
        }
    }
}
