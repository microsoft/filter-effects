using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;

using Nokia.Graphics.Imaging;
using FilterEffects.Filters.FilterControls;

namespace FilterEffects.Filters
{
    public class SurroundedFilter : AbstractFilter
    {
        private const string DebugTag = "SurroundedFilter: ";
        private readonly HdrEffect _hdrEffect;
    
        public SurroundedFilter()
        {
            Name = "Surrounded";
            ShortDescription = "HDR";
            _hdrEffect = new HdrEffect();
            CreateControl();      
        }

        protected override void SetFilters(FilterEffect effect)
        {
        }

        public async override Task<IBuffer> RenderJpegAsync(IBuffer buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                Debug.WriteLine(DebugTag + Name + ": RenderJpegAsync(): The given buffer is null or empty!");
                return null;
            }

            IBuffer outputBuffer;

            using (var source = new BufferImageSource(buffer))
            {
                _hdrEffect.Source = source;
       
                using (var renderer = new JpegRenderer(_hdrEffect))
                {
                    outputBuffer = await renderer.RenderAsync();
                }

                _hdrEffect.Dispose();
            }

            return outputBuffer;
        }

        protected override async void Render()
        {
            try
            {
                if (Source != null)
                {
                    Debug.WriteLine(DebugTag + Name + ": Rendering...");

                    foreach (var change in Changes)
                    {
                        change();
                    }

                    Changes.Clear();

                    _hdrEffect.Source = Source;

                    using (var renderer = new WriteableBitmapRenderer(_hdrEffect, TmpBitmap))
                    {
                        await renderer.RenderAsync();
                    }

                    TmpBitmap.PixelBuffer.CopyTo(PreviewBitmap.PixelBuffer);
                    PreviewBitmap.Invalidate(); // Force a redraw
                }
                else
                {
                    Debug.WriteLine(DebugTag + Name + ": Render(): No buffer set!");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(DebugTag + Name + ": Render(): " + e.Message);
            }
            finally
            {
                switch (State)
                {
                    case States.Apply:
                        State = States.Wait;
                        break;
                    case States.Schedule:
                        State = States.Apply;
                        Render();
                        break;
                    default:
                        break;
                }
            }
        }

        protected void CreateControl()
        {
            var hdrControl = new HdrControl
                {
                    NoiseSuppression = _hdrEffect.NoiseSuppression,
                    Strength = _hdrEffect.Strength,
                    Saturation = _hdrEffect.Saturation
                };

            Control = hdrControl;
            hdrControl.ValueChanged += HdrValueChanged;
        }

        private void HdrValueChanged(object sender, EventArgs a)
        {
            try
            {
                var hdrControl = Control as HdrControl;
                if (hdrControl != null)
                {
                    _hdrEffect.NoiseSuppression = hdrControl.NoiseSuppression;
                    _hdrEffect.Strength = hdrControl.Strength;
                    _hdrEffect.Saturation = hdrControl.Saturation;
                }
                Apply();
            }
            catch (Exception e)
            {
                Debug.WriteLine(DebugTag + "HdrValueChanged(): " + e.Message);
            }
        }
    }
}

