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

namespace LightDisplayVisualEffect
{
    // These specific values match the Favero FA01 outputs, which makes that specific
    // driver slightly simpler, but it really doesn't matter.
    public enum Lights
    {
        LeftWhite = 1,
        RightWhite = 2,
        Red = 4,            // Red is left on-target
        Green = 8
    }

    public sealed class LightDisplayVisualEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties encodingProperties;
        private IDirect3DDevice device;
        private Lights lights = (Lights)0;
        private Color redLightColor;
        private Color greenLightColor;

        public LightDisplayVisualEffect()
        {
            redLightColor = Colors.Red;
            greenLightColor = Colors.Green;
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            this.encodingProperties = encodingProperties;
            this.device = device;
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

        private void ProcessSoftwareBitmap(ProcessVideoFrameContext context)
        {
        }

        private void ProcessDirect3DSurface(ProcessVideoFrameContext context)
        {

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
                if (property.Key == "lights")
                {
                    lights = (Lights)property.Value;
                }
                else if (property.Key == "redColor")
                {
                    redLightColor = (Color)property.Value;
                }
                else if (property.Key == "greenColor")
                {
                    greenLightColor = (Color)property.Value;
                }
            }
        }
    }
}
