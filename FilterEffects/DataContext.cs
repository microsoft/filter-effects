/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterEffects
{
    /// <summary>
    /// A singleton class which contains application data that may be accessed
    /// by various classes.
    /// </summary>
    public class DataContext
    {
        private static DataContext _instance = null;
        private MemoryStream _imageStream = null;
        private MemoryStream _thumbStream = null;

        // Properties

        public MemoryStream ImageStream
        {
            get
            {
                return _imageStream;
            }

            set
            {
                _imageStream = value;
            }
        }

        public MemoryStream ThumbStream
        {
            get
            {
                return _thumbStream;
            }

            set
            {
                _thumbStream = value;
            }
        }
        
        /// <summary>
        /// Returns the singleton instance of this class.
        /// </summary>
        public static DataContext Singleton
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DataContext();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Private contructor.
        /// </summary>
        private DataContext()
        {
            CreateStreams();
        }

        /// <summary>
        /// (Re)creates the stream instances.
        /// </summary>
        public void CreateStreams()
        {
            _imageStream = new MemoryStream();
            _thumbStream = new MemoryStream();
        }
    }
}
