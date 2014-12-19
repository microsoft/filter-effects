using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Streams;

using Lumia.Imaging;
using Lumia.Imaging.Adjustments;
using Lumia.InteropServices.WindowsRuntime;
using FilterEffects.Filters.FilterControls;

namespace FilterEffects.Filters
{
    public class SurroundedFilter : AbstractFilter
    {
        private const string DebugTag = "SurroundedFilter: ";
        private readonly HdrEffect _hdrEffect;
        private HdrControl _hdrControl;
    
        public SurroundedFilter()
        {
            Name = "Surrounded";
            ShortDescription = "HDR";
            _hdrEffect = new HdrEffect();
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

                    TmpBitmap.Pixels.CopyTo(PreviewBitmap.Pixels, 0);
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

        public override bool AttachControl(FilterPropertiesControl control)
        {
            Control = control;

            _hdrControl = new HdrControl
            {
                NoiseSuppression = _hdrEffect.NoiseSuppression,
                Strength = _hdrEffect.Strength,
                Saturation = _hdrEffect.Saturation
            };

            control.ControlsContainer.Children.Add(_hdrControl);
            _hdrControl.ValueChanged += HdrValueChanged;

            return true;
        }

        private void HdrValueChanged(object sender, EventArgs a)
        {
            try
            {
                if (_hdrControl != null)
                {
                    Changes.Add(() =>
                    {
                        _hdrEffect.NoiseSuppression = _hdrControl.NoiseSuppression;
                        _hdrEffect.Strength = _hdrControl.Strength;
                        _hdrEffect.Saturation = _hdrControl.Saturation;
                    });
                }

                Apply();
                Control.NotifyManipulated();
            }
            catch (Exception e)
            {
                Debug.WriteLine(DebugTag + "HdrValueChanged(): " + e.Message);
            }
        }
    }
}

