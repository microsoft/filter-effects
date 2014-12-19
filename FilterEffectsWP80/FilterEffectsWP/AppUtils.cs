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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Lumia.InteropServices.WindowsRuntime;

namespace FilterEffects
{
    public class AppUtils
    {
        private static int _hasDarkTheme = -1;
        public static bool PhoneHasDarkTheme
        {
            get
            {
                if (_hasDarkTheme == -1)
                {
                    // Resolve the theme background
                    Visibility darkBackgroundVisibility =
                        (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
                    _hasDarkTheme = (darkBackgroundVisibility == Visibility.Visible) ? 1 : 0;
                }

                return (_hasDarkTheme == 1);
            }
            private set
            {
            }
        }

        private static SolidColorBrush _brush = null;
        public static SolidColorBrush ThemeBackgroundBrush
        {
            get
            {
                if (_brush == null)
                {
                    Color color = new Color();

                    if (PhoneHasDarkTheme)
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
            private set
            {
            }
        }

        private const string DebugTag = "AppUtils: ";

        /// <summary>
        /// Scales the image in the given memory stream.
        /// </summary>
        /// <param name="originalStream">The original image stream to scale.</param>
        /// <param name="scaledStream">Stream where the scaled image is stored.</param>
        /// <param name="scaleWidth">The target width.</param>
        /// <param name="scaleHeight">The target height.</param>
        /// <returns></returns>
        public static async Task ScaleImageStreamAsync(Stream originalStream,
                                                       MemoryStream scaledStream,
                                                       int scaleWidth,
                                                       int scaleHeight)
        {
            System.Diagnostics.Debug.WriteLine(DebugTag + "ScaleImageStreamAsync() -> " + scaleWidth + "x" + scaleHeight);

            BitmapImage image = new BitmapImage();
            originalStream.Seek(0, SeekOrigin.Begin);
            image.SetSource(originalStream);

            WriteableBitmap bitmap = new WriteableBitmap(image);
            Stream tempStream = new MemoryStream();

            try
            {
                bitmap.SaveJpeg(tempStream, scaleWidth, scaleHeight, 0, 100);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(DebugTag + "SaveJpeg() failed: " + e.ToString());
            }

            tempStream.Seek(0, SeekOrigin.Begin);
            await tempStream.CopyToAsync(scaledStream);

            System.Diagnostics.Debug.WriteLine(DebugTag + "<- ScaleImageStreamAsync()");
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        /// <param name="originalStream"></param>
        /// <param name="scaledStream"></param>
        /// <param name="scaleSize"></param>
        /// <returns></returns>
        public static async Task ScaleImageStreamAsync(Stream originalStream,
                                                       MemoryStream scaledStream,
                                                       Windows.Foundation.Size scaleSize)
        {
            await ScaleImageStreamAsync(
                originalStream, scaledStream, (int)scaleSize.Width, (int)scaleSize.Height);
        }

        /// <summary>
        /// Calculates preview resolution based on the original resolution and
        /// either width or height of the preview resolution, i.e. either
        /// preview width or height needs to be given. The aspect ratio is
        /// kept the same. The calculated values are stored in the given
        /// preview arguments.
        /// </summary>
        /// <param name="originalWidth">The original width.</param>
        /// <param name="originalHeight">The original height.</param>
        /// <param name="previewWidth">The preview width.</param>
        /// <param name="previewHeight">The preview height.</param>
        public static void CalculatePreviewResolution(int originalWidth, int originalHeight,
                                                      ref int previewWidth, ref int previewHeight)
        {
            if (previewWidth > 0)
            {
                double heightToWidthRatio = (double)originalHeight / (double)originalWidth;
                previewHeight = (int)Math.Round(previewWidth * heightToWidthRatio, 0);
            }
            else if (previewHeight > 0)
            {
                double widthToHeightRatio = (double)originalWidth / (double)originalHeight;
                previewWidth = (int)Math.Round(previewHeight * widthToHeightRatio, 0);
            }

            System.Diagnostics.Debug.WriteLine(DebugTag
                + "CalculatePreviewResolution(): " + originalWidth + "x" + originalHeight
                + " -> " + previewWidth + "x" + previewHeight);
        }
    }
}
