/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public abstract class AbstractFilter
    {
        /// <summary>
        /// The states of the state machine.
        /// </summary>
        private enum States
        {
            Wait = 0,
            Apply,
            Schedule
        };

        // Members
        protected IBuffer _buffer;
        protected FilterPropertiesControl _control;
        private WriteableBitmap _previewBitmap;

        // Use a temporary buffer for rendering to remove concurrent access
        // between rendering and the image shown on the screen.
        private WriteableBitmap _tmpBitmap;

        private EditingSession _session;

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

        private States _state = States.Wait;
        private States State
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
        }

        /// <summary>
        /// Define the filter effect(s) to the given session. Each filter must
        /// implement their own version of this method.
        /// </summary>
        /// <param name="session">The session initialized with the desired JPEG.</param>
        public abstract void DefineFilter(EditingSession session);
        
        /// <summary>
        /// Associates the given control with this filter and creates the
        /// control UI components.
        /// </summary>
        /// <param name="control">The control to attach.</param>
        /// <returns>True if the filter supports a control. False otherwise.</returns>
        public abstract bool AttachControl(FilterPropertiesControl control);

        /// <summary>
        /// Sets the buffer for this filter.
        /// </summary>
        /// <param name="buffer">The buffer containing the image data to apply
        /// filter to.</param>
        public void SetBuffer(IBuffer buffer)
        {
            _buffer = buffer;

            if (_session != null)
            {
               _session.Dispose();
               _session = null;
            }

           _session = new EditingSession(_buffer);
        }

        /// <summary>
        /// Creates a filtered preview image from the given buffer.
        /// </summary>
        public void CreatePreviewImage()
        {
            switch (State)
            {
                case States.Wait: // State machine transition: Wait -> Apply
                    State = States.Apply;
                    ApplyFilter(); // Apply the filter
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
        /// Creates the preview buffers or resize them if needed.
        /// </summary>
        /// <param name="session">The session initialized with the desired jpeg.</param>
        public async Task RenderToBitmapAsync(EditingSession session)
        {
            await session.RenderToWriteableBitmapAsync(_tmpBitmap, OutputOption.PreserveAspectRatio);
            _tmpBitmap.Pixels.CopyTo(_previewBitmap.Pixels, 0);
            _previewBitmap.Invalidate(); // Force a redraw
        }

        /// <summary>
        /// Creates a preview bitmap if one does not exist or the size of the
        /// previous bitmap does not match the given dimensions.
        /// </summary>
        /// <param name="width">The width of the output resolution.</param>
        /// <param name="height">The height of the output resolution.</param>
        public void SetOutputResolution(int width, int height)
        {
            if (_previewBitmap != null)
            {
                if ((_previewBitmap.PixelHeight == height)
                    && (_previewBitmap.PixelWidth == width))
                {
                    return; // The current bitmap suits us well
                }
            }

            _previewBitmap = new WriteableBitmap(width, height);
            _tmpBitmap = new WriteableBitmap(width, height);
            PreviewImage.Source = _previewBitmap; // Force a redraw
        }

        /// <summary>
        /// Applies the filter. If another processing request was scheduled
        /// while processing the buffer, the method will recursively call
        /// itself.
        /// </summary>
        protected async void ApplyFilter()
        {
            try
            {
                if (_buffer != null)
                {
                    _session.UndoAll(); // Remove old filters
                    DefineFilter(_session);
                    await RenderToBitmapAsync(_session);
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
                        ApplyFilter(); // Apply the filter
                        break;
                    default:
                        // Do nothing
                        break;
                }
            }
        }
    }
}
