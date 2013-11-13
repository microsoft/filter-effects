/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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
        private SolidColorBrush _brush = null;
        private bool _hasDarkTheme = false;

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
        /// Provides information on theme background.
        /// </summary>
        public bool PhoneHasDarkTheme
        {
            get
            {
                return _hasDarkTheme;
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

            // Resolve the theme background
            Visibility darkBackgroundVisibility =
                (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            _hasDarkTheme = (darkBackgroundVisibility == Visibility.Visible);
        }

        /// <summary>
        /// (Re)creates the stream instances.
        /// </summary>
        public void CreateStreams()
        {
            _imageStream = new MemoryStream();
            _thumbStream = new MemoryStream();
        }

        /// <returns>A solid color brush instance with color matching the theme
        /// background.</returns>
        public SolidColorBrush ThemeBackgroundBrush()
        {
            if (_brush == null)
            {
                Color color = new Color();

                if (_hasDarkTheme)
                {
                    color.A = 255;
                    color.R = 0;
                    color.G = 0;
                    color.B = 0;
                }
                else
                {
                    color.A = 255;
                    color.R = 255;
                    color.G = 255;
                    color.B = 255;
                }

                _brush = new SolidColorBrush(color);
            }

            return _brush;
        }
    }
}
