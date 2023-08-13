using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Devices.Sensors;
using Windows.UI;
using System.Runtime.InteropServices;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System.Runtime.CompilerServices;

namespace LightDisplayVisualEffect
{
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    // These specific values match the Favero FA01 outputs, which makes that specific
    // driver slightly simpler, but it really doesn't matter.
    public enum Lights
    {
        None = 0,
        LeftWhite = 1,
        RightWhite = 2,
        Red = 4,            // Red is left on-target
        Green = 8
    }

    public sealed class LightDisplayEffect : IBasicVideoEffect
    {
        public static string RedLightColorProperty => "redColor";
        public static string GreenLightColorProperty => "greenColor";
        public static string LightStatusProperty => "lights";

        private VideoEncodingProperties encodingProperties;
        private IDirect3DDevice device;
        private Lights lights = Lights.None;
        private Color redLightColor;
        private uint redLightARGB;
        private Color greenLightColor;
        private uint greenLightARGB;
        private Color whiteLightColor;
        private uint whiteLightARGB;
        private CanvasDevice canvasDevice;

        private uint ColorToARGBuint(Color color)
        {
            return (((uint)color.A) << 24) + (((uint)color.R) << 16) + (((uint)color.G) << 8) + (uint)color.B;
        }

        public LightDisplayEffect()
        {
            redLightColor = Colors.Red;
            redLightARGB = ColorToARGBuint(redLightColor);
            greenLightColor = Colors.Green;
            greenLightARGB = ColorToARGBuint(greenLightColor);
            whiteLightColor = Colors.White;
            whiteLightARGB = ColorToARGBuint(whiteLightColor);
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            this.encodingProperties = encodingProperties;
            this.device = device;
            if (device != null)
            {
                canvasDevice = CanvasDevice.CreateFromDirect3D11Device(device);
            }
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            if (context.InputFrame.SoftwareBitmap != null)
            {
                ProcessSoftwareBitmap(context);
            }
            else if (context.InputFrame.Direct3DSurface != null)
            {
                ProcessDirect3DSurface(context);
            }
        }

        // This implementation assumes (and is optimized for) ARGB32 encoding
        private unsafe void ProcessSoftwareBitmap(ProcessVideoFrameContext context)
        {
            using (BitmapBuffer buffer = context.InputFrame.SoftwareBitmap.LockBuffer(
                BitmapBufferAccessMode.Read))
            using (BitmapBuffer targetBuffer = context.OutputFrame.SoftwareBitmap.LockBuffer(
                BitmapBufferAccessMode.Write))
            {
                using (var reference = buffer.CreateReference())
                using (var targetReference = targetBuffer.CreateReference())
                {
                    byte* dataInBytes;
                    uint capacity;
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                    byte* targetDataInBytes;
                    uint targetCapacity;
                    ((IMemoryBufferByteAccess)targetReference).GetBuffer(out targetDataInBytes, 
                        out targetCapacity);

                    BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);
                    uint* inputInts = (uint*)dataInBytes;
                    uint* outputInts = (uint*)targetDataInBytes;
                    int start = bufferLayout.StartIndex / 4;
                    int stride = bufferLayout.Stride / 4;
                    int topBorder = bufferLayout.Height / 24;
                    int lightWidth = bufferLayout.Width / 4;
                    int leftStart = bufferLayout.Width / 8;
                    int leftEnd = leftStart + lightWidth;
                    int rightStart = 5 * bufferLayout.Width / 8;
                    int rightEnd = rightStart + lightWidth;
                    bool leftRed = (lights & Lights.Red) != 0;
                    bool leftWhite = (lights & Lights.LeftWhite) != 0;
                    bool leftOn = leftRed || leftWhite;
                    bool rightGreen = (lights & Lights.Green) != 0;
                    bool rightWhite = (lights & Lights.RightWhite) != 0;
                    uint rightColor = rightGreen ? greenLightARGB : whiteLightARGB;

                    // Optimize by hand by having multiple loops and pulling branches and
                    // calculations outside of the loops as much as we can
                    uint leftColor = leftRed ? redLightARGB : whiteLightARGB;
                    bool rightOn = rightGreen || rightWhite;
                    if (leftOn && rightOn)
                    {
                        for (int i = 0; i < topBorder; i++)
                        {
                            for (int j = 0; j < bufferLayout.Width; j++)
                            {
                                int idx = start + stride * i + j;
                                if (leftStart <= j && j < leftEnd)
                                {
                                    outputInts[idx] = leftColor;
                                }
                                else if (rightStart <= j && j < rightEnd)
                                {
                                    outputInts[idx] = rightColor;
                                }
                                else
                                {
                                    outputInts[idx] = inputInts[idx];
                                }
                            }
                        }
                        for (int i = topBorder; i < bufferLayout.Height; i++)
                        {
                            for (int j = 0; j < bufferLayout.Width; j++)
                            {
                                int idx = start + stride * i + j;
                                outputInts[idx] = inputInts[idx];
                            }
                        }
                    }
                    else if (leftOn)
                    {
                        for (int i = 0; i < topBorder; i++)
                        {
                            for (int j = 0; j < bufferLayout.Width; j++)
                            {
                                int idx = start + stride * i + j;
                                if (leftStart <= j && j < leftEnd)
                                {
                                    outputInts[idx] = leftColor;
                                }
                                else
                                {
                                    outputInts[idx] = inputInts[idx];
                                }
                            }
                        }
                        for (int i = topBorder; i < bufferLayout.Height; i++)
                        {
                            for (int j = 0; j < bufferLayout.Width; j++)
                            {
                                int idx = start + stride * i + j;
                                outputInts[idx] = inputInts[idx];
                            }
                        }
                    }
                    else if (rightOn)
                    {
                        for (int i = 0; i < topBorder; i++)
                        {
                            for (int j = 0; j < bufferLayout.Width; j++)
                            {
                                int idx = start + stride * i + j;
                                if (rightStart <= j && j < rightEnd)
                                {
                                    outputInts[idx] = rightColor;
                                }
                                else
                                {
                                    outputInts[idx] = inputInts[idx];
                                }
                            }
                        }
                        for (int i = topBorder; i < bufferLayout.Height; i++)
                        {
                            for (int j = 0; j < bufferLayout.Width; j++)
                            {
                                int idx = start + stride * i + j;
                                outputInts[idx] = inputInts[idx];
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < bufferLayout.Height; i++)
                        {
                            for (int j = 0; j < bufferLayout.Width; j++)
                            {
                                int idx = start + stride * i + j;
                                outputInts[idx] = inputInts[idx];
                            }
                        }
                    }
                }
            }
        }

        private void ProcessDirect3DSurface(ProcessVideoFrameContext context)
        {
            using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(canvasDevice, context.InputFrame.Direct3DSurface))
            using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(canvasDevice, context.OutputFrame.Direct3DSurface))
            using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            {
                ds.DrawImage(inputBitmap);
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
        }

        public void DiscardQueuedFrames()
        {
        }

        public bool IsReadOnly => false;

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var encodingProperties = new VideoEncodingProperties();
                encodingProperties.Subtype = "ARGB32";
                return new List<VideoEncodingProperties>() { encodingProperties };
            }
        }

        public MediaMemoryTypes SupportedMemoryTypes 
            => MediaMemoryTypes.GpuAndCpu;

        public bool TimeIndependent => true;

        public void SetProperties(IPropertySet configuration)
        {
            foreach (var property in configuration)
            {
                if (property.Key == LightStatusProperty)
                {
                    lights = (Lights)property.Value;
                }
                else if (property.Key == RedLightColorProperty)
                {
                    redLightColor = (Color)property.Value;
                    redLightARGB = ColorToARGBuint(redLightColor);
                }
                else if (property.Key == GreenLightColorProperty)
                {
                    greenLightColor = (Color)property.Value;
                    greenLightARGB = ColorToARGBuint(greenLightColor);
                }
            }
        }
    }
}
