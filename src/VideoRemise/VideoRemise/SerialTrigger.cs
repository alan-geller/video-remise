using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;

namespace VideoRemise
{
    internal abstract class SerialTrigger : Trigger, IDisposable
    {
        protected SerialDevice device;
        protected IInputStream inputStream;
        protected DataReader reader;
        private bool disposedValue;
        private CancellationTokenSource readCancellationTokenSource;
        private Object readCancelLock;
        protected uint readFrameLength;
        protected StorageFile logFile = null;
        private IAsyncAction readingAction;

        public SerialTrigger()
        {
        }

        protected async Task Log(string text)
        {
            if (logFile != null)
            {
                await FileIO.AppendTextAsync(logFile, text + "\n");
            }
        }

        public override async Task Initialize(VideoRemiseConfig config)
        {
            reader?.Dispose();
            inputStream?.Dispose();
            device?.Dispose();

            try
            {
                device = await SerialDevice.FromIdAsync(config.AdapterDeviceId);
            }
            catch
            {
                device = null;
            }
            if (device == null)
            {
                await new MessageDialog("Serial adapter not found").ShowAsync();
                config.AdapterDeviceId = "";
                return;
            }

            inputStream = device.InputStream;
            reader = new DataReader(inputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial 
                | InputStreamOptions.ReadAhead;
            readCancellationTokenSource = new CancellationTokenSource();
            readCancelLock = new Object();

            if (logFile == null)
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation =
                    Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                // Dropdown of file types the user can save the file as
                savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
                // Default file name if the user does not type one in or select a file to replace
                savePicker.SuggestedFileName = "Serial adapter log";
                logFile = await savePicker.PickSaveFileAsync();
                await Log("Initialized");
            }

            StartReadingAsync();
        }

        public void StartReadingAsync()
        {
            var cancellationToken = readCancellationTokenSource.Token;

            readingAction = Windows.System.Threading.ThreadPool.RunAsync
                (async _ => await ReadAsync(cancellationToken), 
                Windows.System.Threading.WorkItemPriority.High,
                Windows.System.Threading.WorkItemOptions.TimeSliced);
        }

        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            await Log("Starting read thread");
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
                        await ProcessBufferAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            await Log("Exiting read thread");
        }

        protected abstract Task ProcessBufferAsync();

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
                    readingAction.GetResults();
                    //readingTask.ContinueWith(
                    //    (_) => { 
                            reader?.Dispose();
                            inputStream?.Dispose();
                            device?.Dispose();
                        //});
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
