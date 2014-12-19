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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Foundation;
using Windows.Storage.Streams;

using Lumia.Imaging;

using FilterEffects.Filters.FilterControls;

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

        // Members
        protected BufferImageSource Source;
        protected List<Action> Changes;
        protected FilterEffect Effect;
        protected WriteableBitmap PreviewBitmap;

        // Use a temporary buffer for rendering to remove concurrent access
        // between rendering and the image shown on the screen.
        protected WriteableBitmap TmpBitmap;

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
                    if (Source != null)
                    {
                        Source.Dispose();
                        Source = null;
                    }

                    Source = new BufferImageSource(value);
                    Debug.WriteLine(DebugTag + Name + ": Buffer.set: Buffer length is " + Source.Buffer.Length + " bytes");

                    if (Effect != null)
                    {
                        Effect.Dispose();
                        Effect = null;
                    }

                    // Construct the FilterEffect instance and set the
                    // filters.
                    Effect = new FilterEffect(Source);
                    SetFilters(Effect);
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

                if (PreviewBitmap != null)
                {
                    width = PreviewBitmap.PixelWidth;
                    height = PreviewBitmap.PixelHeight;
                }

                return new Size(width, height);
            }
            set
            {
                int width = (int)value.Width;
                int height = (int)value.Height;

                if (PreviewBitmap == null ||
                    (PreviewBitmap.PixelWidth != width
                     && PreviewBitmap.PixelHeight == height))
                {
                    PreviewBitmap = new WriteableBitmap(width, height);
                    TmpBitmap = new WriteableBitmap(width, height);
                    PreviewImageSource = PreviewBitmap; // Redraw

                    Debug.WriteLine(DebugTag + ": Resolution.set: "
                        + PreviewBitmap.PixelWidth + "x" + PreviewBitmap.PixelHeight);
                }
            }
        }

        public FilterPropertiesControl Control
        {
            get;
            protected set;
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


            if (Effect != null)
            {
                Effect.Dispose();
                Effect = null;
            }

            // Construct the FilterEffect instance and set the
            // filters.
            Effect = new FilterEffect(Source);
            SetFilters(Effect);
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
        /// Attaches a UI controls for adjusting filter properties.
        /// </summary>
        /// <param name="control"></param>
        /// <returns>True if the control was populated, false otherwise.</returns>
        public virtual bool AttachControl(FilterPropertiesControl control)
        {
            return false;
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
                if (Source != null)
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
                    using (var renderer = new WriteableBitmapRenderer(Effect, TmpBitmap))
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
        /// From IDisposable.
        /// </summary>
        public virtual void Dispose()
        {
            Debug.WriteLine("Disposing effect.");

            if (Source != null)
            {
                Source.Dispose();
                Source = null;
            }

            if (Effect != null)
            {
                Effect.Dispose();
                Effect = null;
            }
        }
    }
}
