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
using Nokia.Graphics.Utilities;

namespace FilterEffects
{
    /// <summary>
    /// An abstract base class for a filter.
    /// </summary>
    public abstract class AbstractFilter
    {
        protected IBuffer _buffer;
        protected FilterPropertiesControl _control;
        private WriteableBitmap _previewBitmap;
        private Bitmap _filteredBitmap;

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
        }

        /// <summary>
        /// Creates a filtered preview image from the given buffer.
        /// </summary>
        public async void CreatePreviewImage()
        {
            if (_buffer == null)
            {
                return;
            }

            EditingSession session = new EditingSession(_buffer);

            // Perform the filtering
            using (session)
            {
                DefineFilter(session);
                await RenderToBitmapAsync(session);
            }
        }

        /// <summary>
        /// Creates the preview buffers or resize them if needed.
        /// </summary>
        /// <param name="session">The session initialized with the desired jpeg.</param>
        public async Task RenderToBitmapAsync(EditingSession session)
        {
            await session.RenderToBitmapAsync(_filteredBitmap, OutputOption.PreserveAspectRatio);
            _previewBitmap.Invalidate(); // Force a redraw
        }

        /// <summary>
        /// Creates the PreviewBuffers or resize them if needed.
        /// </summary>
        /// <param name="session">The session initialized with the desired jpeg.</param>
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
            _filteredBitmap = BitmapExtensionMethods.AsBitmap(_previewBitmap);
            PreviewImage.Source = _previewBitmap; // Force a redraw
        }
    }
}
