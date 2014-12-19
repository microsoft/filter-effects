/*
 * Copyright (c) 2014 Microsoft Mobile
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;

using Lumia.Imaging;
using Lumia.Imaging.Adjustments;
using FilterEffects.Filters.Controls;

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
                if (_source != null)
                {
                    Debug.WriteLine(DebugTag + Name + ": Rendering...");

                    foreach (var change in Changes)
                    {
                        change();
                    }

                    Changes.Clear();

                    _hdrEffect.Source = _source;

                    using (var renderer = new WriteableBitmapRenderer(_hdrEffect, _tmpBitmap))
                    {
                        await renderer.RenderAsync();
                    }

                    _tmpBitmap.PixelBuffer.CopyTo(_previewBitmap.PixelBuffer);
                    _previewBitmap.Invalidate(); // Force a redraw
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
                    Changes.Add(() =>
                    {
                        _hdrEffect.NoiseSuppression = hdrControl.NoiseSuppression;
                        _hdrEffect.Strength = hdrControl.Strength;
                        _hdrEffect.Saturation = hdrControl.Saturation;
                    });
                }

                Apply();
                NotifyManipulated();
            }
            catch (Exception e)
            {
                Debug.WriteLine(DebugTag + "HdrValueChanged(): " + e.Message);
            }
        }
    }
}

