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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

using Lumia.Imaging;

namespace FilterEffects.Filters
{
    /// <summary>
    /// An abstract base class for a filter.
    /// </summary>
    public abstract class AbstractFilter : IDisposable
    {
        protected const double FilterControlTitleFontSize = 16;
        protected const double GridRowMinimumHeight = 30;
        protected const double GridRowMaxHeight = 75;
        private const string DebugTag = "AbstractFilter: ";

        /// <summary>
        /// The states of the state machine.
        /// </summary>
        protected enum States
        {
            Wait = 0,
            Apply,
            Schedule
        };

        public event EventHandler PropertiesManipulated;

        // Members
        protected ResourceLoader _resourceLoader;
        protected BufferImageSource _source;
        protected FilterEffect _effect;
        protected WriteableBitmap _previewBitmap;

        // Use a temporary buffer for rendering to remove concurrent access
        // between rendering and the image shown on the screen.
        protected WriteableBitmap _tmpBitmap;

        /// <summary>
        /// Name of the filter.
        /// </summary>
        public string Name
        {
            get;
            protected set;
        }

        public string ShortDescription
        {
            get;
            protected set;
        }

        public ImageSource PreviewImageSource
        {
            get;
            private set;
        }

        public IBuffer Buffer
        {
            set
            {
                if (value != null)
                {
                    if (_source != null)
                    {
                        _source.Dispose();
                        _source = null;
                    }

                    _source = new BufferImageSource(value);
                    Debug.WriteLine(DebugTag + Name + ": Buffer.set: Buffer length is " + _source.Buffer.Length + " bytes");

                    if (_effect != null)
                    {
                        _effect.Dispose();
                        _effect = null;
                    }

                    // Construct the FilterEffect instance and set the
                    // filters.
                    _effect = new FilterEffect(_source);
                    SetFilters(_effect);
                }
                else
                {
                    Debug.WriteLine(DebugTag + Name + ": The given buffer is null!");
                }
            }
        }

        /// <summary>
        /// Resolution of the bitmaps.
        /// </summary>
        public Size PreviewResolution
        {
            get
            {
                double width = 0;
                double height = 0;

                if (_previewBitmap != null)
                {
                    width = _previewBitmap.PixelWidth;
                    height = _previewBitmap.PixelHeight;
                }

                return new Size(width, height);
            }
            set
            {
                int width = (int)value.Width;
                int height = (int)value.Height;

                if (_previewBitmap == null ||
                    (_previewBitmap.PixelWidth != width
                     && _previewBitmap.PixelHeight == height))
                {
                    _previewBitmap = new WriteableBitmap(width, height);
                    _tmpBitmap = new WriteableBitmap(width, height);
                    PreviewImageSource = _previewBitmap; // Redraw

                    Debug.WriteLine(DebugTag + ": Resolution.set: "
                        + _previewBitmap.PixelWidth + "x" + _previewBitmap.PixelHeight);
                }
            }
        }

        public UIElement Control
        {
            get;
            protected set;
        }

        protected List<Action> Changes
        {
            get;
            set;
        }

        private States _state = States.Wait;
        protected States State
        {
            get
            {
                return _state;
            }
            set
            {
                if (_state != value)
                {
                    Debug.WriteLine(DebugTag
                        + "State.set: " + _state + " -> " + value);
                    _state = value;
                }
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected AbstractFilter()
        {
            _resourceLoader = new ResourceLoader();
            Name = "n/a";
            Changes = new List<Action>();
        }

        /// <summary>
        /// Creates a filtered preview image from the given buffer.
        /// </summary>
        public void Apply()
        {
            switch (State)
            {
                case States.Wait: // State machine transition: Wait -> Apply
                    State = States.Apply;
                    Render(); // Apply the filter
                    break;
                case States.Apply: // State machine transition: Apply -> Schedule
                    State = States.Schedule;
                    break;
                default:
                    // Do nothing
                    break;
            }
        }

        /// <summary>
        /// Renders the given buffer with the applied filters to an output
        /// buffer and returns it. Meant to be used where the filtered image
        /// is, for example, going to be saved to a file.
        /// </summary>
        /// <param name="buffer">The buffer containing the original image data.</param>
        /// <returns>Buffer containing the filtered image data.</returns>
        public virtual async Task<IBuffer> RenderJpegAsync(IBuffer buffer)
        {
            if (buffer == null || buffer.Length == 0)
            {
                Debug.WriteLine(DebugTag + Name + ": RenderJpegAsync(): The given buffer is null or empty!");
                return null;
            }


            if (_effect != null)
            {
                _effect.Dispose();
                _effect = null;
            }

            // Construct the FilterEffect instance and set the
            // filters.
            _effect = new FilterEffect(_source);
            SetFilters(_effect);
            IBuffer outputBuffer;

            using (var source = new BufferImageSource(buffer))
            {
                var effect = new FilterEffect(source);
                SetFilters(effect);

                using (var renderer = new JpegRenderer(effect))
                {
                    outputBuffer = await renderer.RenderAsync();
                }

                effect.Dispose();
            }

            return outputBuffer;
        }

        /// <summary>
        /// Sets the filters for the given effect instance.
        /// </summary>
        /// <param name="effect">The effect instance to set filters to.</param>
        protected abstract void SetFilters(FilterEffect effect);

        /// <summary>
        /// Applies the filter. If another processing request was scheduled
        /// while processing the buffer, the method will recursively call
        /// itself.
        /// </summary>
        protected virtual async void Render()
        {
            try
            {
                if (_source != null)
                {
                    Debug.WriteLine(DebugTag + Name + ": Rendering...");

                    // Apply the pending changes to the filter(s)
                    foreach (var change in Changes)
                    {
                        change();
                    }

                    Changes.Clear();

                    // Render the filters first to the temporary bitmap and
                    // copy the changes then to the preview bitmap
                    using (var renderer = new WriteableBitmapRenderer(_effect, _tmpBitmap))
                    {
                        await renderer.RenderAsync();
                    }

                    /* "using System.Runtime.InteropServices.WindowsRuntime" is
                     * required for WriteableBitmap.PixelBuffer.CopyTo() and
                     * WriteableBitmap.PixelBuffer.AsStream().
                     */
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
                    case States.Apply: // State machine transition : Apply -> Wait
                        State = States.Wait;
                        break;
                    case States.Schedule: // State machine transition: Schedule -> Apply
                        State = States.Apply;
                        Render(); // Apply the filter
                        break;
                    default:
                        // Do nothing
                        break;
                }
            }
        }

        /// <summary>
        /// Notifies possible listeners that the properties of this filter were
        /// manipulated. Note that it is up to the derived classes to implement
        /// the logic for calling this method.
        /// </summary>
        protected void NotifyManipulated()
        {
            EventHandler handler = PropertiesManipulated;

            if (handler != null)
            {
                handler(this, null);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(DebugTag +
                    "NotifyManipulated(): No handler for PropertiesManipulated event!");
            }
        }

        /// <summary>
        /// From IDisposable.
        /// </summary>
        public virtual void Dispose()
        {
            Debug.WriteLine("Disposing effect.");

            if (_source != null)
            {
                _source.Dispose();
                _source = null;
            }

            if (_effect != null)
            {
                _effect.Dispose();
                _effect = null;
            }
        }
    }
}
