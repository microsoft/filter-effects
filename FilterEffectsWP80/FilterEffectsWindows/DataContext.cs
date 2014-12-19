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

using System.IO;
using Windows.Foundation;

namespace FilterEffects
{
    /// <summary>
    /// A singleton class which contains application data that may be accessed
    /// by various classes.
    /// </summary>
    public class DataContext
    {
        public const double DefaultPreviewResolutionWidth = 800;
        public const double DefaultPreviewResolutionHeight = 600;

        // Properties

        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        private static DataContext _instance;
        public static DataContext Instance
        {
            get { return _instance ?? (_instance = new DataContext()); }
        }

        public MemoryStream PreviewResolutionStream
        {
            get;
            set;
        }

        public MemoryStream FullResolutionStream
        {
            get;
            set;
        }

        public Size PreviewResolution
        {
            get;
            set;
        }

        public Size FullResolution
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the image data was just captured with camera or
        /// is an existing image from the file system.
        /// </summary>
        public bool WasCaptured
        {
            get;
            set;
        }

        /// <summary>
        /// Private contructor.
        /// </summary>
        private DataContext()
        {
            PreviewResolutionStream = new MemoryStream();
            FullResolutionStream = new MemoryStream();
            FullResolution = new Size(DefaultPreviewResolutionWidth, DefaultPreviewResolutionHeight);
            PreviewResolution = new Size(DefaultPreviewResolutionWidth, DefaultPreviewResolutionHeight);
        }

        public void ResetStreams()
        {
            PreviewResolutionStream.Seek(0, SeekOrigin.Begin);
            FullResolutionStream.Seek(0, SeekOrigin.Begin);
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetPreviewResolution(int width, int height)
        {
            PreviewResolution = new Size(width, height);
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetFullResolution(int width, int height)
        {
            FullResolution = new Size(width, height);
        }
    }
}
