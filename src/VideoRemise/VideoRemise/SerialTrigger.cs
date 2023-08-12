using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace VideoRemise
{
    internal abstract class SerialTrigger : Trigger, IDisposable
    {
        protected SerialDevice device;
        protected IInputStream inputStream;
        protected DataReader reader;
        private Task readingTask;
        private bool disposedValue;
        private CancellationTokenSource readCancellationTokenSource;
        private Object readCancelLock;
        protected uint readFrameLength;

        public SerialTrigger()
        {
        }

        public override async Task Initialize(VideoRemiseConfig config)
        {
            device = await SerialDevice.FromIdAsync(config.AdapterDeviceId);

            inputStream = device.InputStream;
            reader = new DataReader(inputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial 
                | InputStreamOptions.ReadAhead;
            readCancellationTokenSource = new CancellationTokenSource();
            readCancelLock = new Object();
        }

        public void StartReadingAsync()
        {
            var cancellationToken = readCancellationTokenSource.Token;

            readingTask = Task.Run(() =>  ReadAsync(cancellationToken));
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                // Don't start any IO if we canceled the task
                lock (readCancelLock)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                // Cancellation Token will be used so we can stop the task
                // operation explicitly.
                try
                {
                    var readCount = await reader.LoadAsync(readFrameLength).AsTask(cancellationToken);
                    if (readCount > 0)
                    {
                        ProcessBuffer();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        protected abstract void ProcessBuffer();

        public void StopReading()
        {
            lock (readCancelLock)
            {
                readCancellationTokenSource.Cancel();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopReading();
                    // We want to make sure the current read is really done
                    // before we dispose of these items
                    readingTask.ContinueWith(
                        (_) => { 
                            reader.Dispose();
                            inputStream.Dispose();
                            device.Dispose();
                        });
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
