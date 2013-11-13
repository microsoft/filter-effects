/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Windows.Storage.Streams;

using Nokia.Graphics;
using Nokia.Graphics.Imaging;
using Nokia.InteropServices.WindowsRuntime;

namespace FilterEffects
{
    /// <summary>
    /// An abstract base class for a filter.
    /// </summary>
    public abstract class AbstractFilter : IDisposable
    {
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
        protected BufferImageSource _source;
        protected FilterPropertiesControl _control;
        protected List<Action> _changes;

        private FilterEffect _effect;
        private WriteableBitmap _previewBitmap;

        // Use a temporary buffer for rendering to remove concurrent access
        // between rendering and the image shown on the screen.
        private WriteableBitmap _tmpBitmap;

        /// <summary>
        /// Name of the filter.
        /// </summary>
        public String Name
        {
            get;
            protected set;
        }

        /// <summary>
        /// The Image used for displaying the preview.
        /// </summary>
        public Image PreviewImage
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
                    _source = new BufferImageSource(value);

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
            }
        }

        /// <summary>
        /// Resolution of the bitmaps.
        /// </summary>
        public Size Resolution
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
                    PreviewImage.Source = _previewBitmap; // Force a redraw
                }
            }
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
                    System.Diagnostics.Debug.WriteLine(
                        "AbstractFilter.State.set: " + _state + " -> " + value);
                    _state = value;
                }
            }
        }            

        /// <summary>
        /// Constructor.
        /// </summary>
        public AbstractFilter()
        {
            PreviewImage = new Image();
            _changes = new List<Action>();
        }

        /// <summary>
        /// Associates the given control with this filter and creates the
        /// control UI components.
        /// </summary>
        /// <param name="control">The control to attach.</param>
        /// <returns>True if the filter supports a control. False otherwise.</returns>
        public abstract bool AttachControl(FilterPropertiesControl control);

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
        /// Renders current image with applied filters to a buffer and returns it.
        /// Meant to be used where the filtered image is for example going to be
        /// saved to a file.
        /// </summary>
        /// <param name="buffer">The buffer containing the original image data.</param>
        /// <returns>Buffer containing the filtered image data.</returns>
        public async Task<IBuffer> RenderJpegAsync(IBuffer buffer)
        {
            using (BufferImageSource source = new BufferImageSource(buffer))
            using (JpegRenderer renderer = new JpegRenderer(_effect))
            {
                return await renderer.RenderAsync();
            }
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
        protected async void Render()
        {
            try
            {
                if (_source != null)
                {
                    // Apply the pending changes to the filter(s)
                    foreach (var change in _changes)
                    {
                        change();
                    }

                    _changes.Clear();

                    // Render the filters first to the temporary bitmap and
                    // copy the changes then to the preview bitmap
                    WriteableBitmapRenderer renderer = new WriteableBitmapRenderer(_effect, _tmpBitmap);
                    await renderer.RenderAsync();
                    _tmpBitmap.Pixels.CopyTo(_previewBitmap.Pixels, 0);
                    _previewBitmap.Invalidate(); // Force a redraw
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "AbstractFilter.ApplyFilter(): No buffer set!");
                }
            }
            catch (Exception)
            {
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
        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("Disposing effect.");

            if (_effect != null)
            {
                _effect.Dispose();
                _effect = null;
            }
        }
    }
}
