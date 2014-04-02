/**
 * Copyright (c) 2013-2014 Nokia Corporation.
 * See the license file delivered with this project for more information.
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
        public const double DefaultPreviewResolutionWidth = 640;
        public const double DefaultPreviewResolutionHeight = 480;

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
