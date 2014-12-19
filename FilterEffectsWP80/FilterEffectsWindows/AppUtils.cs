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
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace FilterEffects
{
    class AppUtils
    {
        private const string DebugTag = "AppUtils: ";
        
        /// <summary>
        /// Scales the image in the given memory stream.
        /// </summary>
        /// <param name="originalStream">The original image stream to scale.</param>
        /// <param name="originalResolutionWidth">The original width.</param>
        /// <param name="originalResolutionHeight">The original height.</param>
        /// <param name="scaledStream">Stream where the scaled image is stored.</param>
        /// <param name="scaleWidth">The target width.</param>
        /// <param name="scaleHeight">The target height.</param>
        /// <returns></returns>
        public static async Task ScaleImageStreamAsync(MemoryStream originalStream,
                                                       int originalResolutionWidth,
                                                       int originalResolutionHeight,
                                                       MemoryStream scaledStream,
                                                       int scaleWidth,
                                                       int scaleHeight)
        {
            System.Diagnostics.Debug.WriteLine(DebugTag + "ScaleImageStreamAsync() ->");

            // Create a bitmap containing the full resolution image
            var bitmap = new WriteableBitmap(originalResolutionWidth, originalResolutionHeight);
            originalStream.Seek(0, SeekOrigin.Begin);
            await bitmap.SetSourceAsync(originalStream.AsRandomAccessStream());

            /* Construct a JPEG encoder with the newly created
             * InMemoryRandomAccessStream as target
             */
            IRandomAccessStream previewResolutionStream = new InMemoryRandomAccessStream();
            previewResolutionStream.Size = 0;
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(
                BitmapEncoder.JpegEncoderId, previewResolutionStream);

            // Copy the full resolution image data into a byte array
            Stream pixelStream = bitmap.PixelBuffer.AsStream();
            var pixelArray = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixelArray, 0, pixelArray.Length);

            // Set the scaling properties
            encoder.BitmapTransform.ScaledWidth = (uint)scaleWidth;
            encoder.BitmapTransform.ScaledHeight = (uint)scaleHeight;
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
            encoder.IsThumbnailGenerated = true;

            // Set the image data and the image format setttings to the encoder
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                (uint)originalResolutionWidth, (uint)originalResolutionHeight,
                96.0, 96.0, pixelArray);

            await encoder.FlushAsync();
            previewResolutionStream.Seek(0);
            await previewResolutionStream.AsStream().CopyToAsync(scaledStream);

            System.Diagnostics.Debug.WriteLine(DebugTag + "<- ScaleImageStreamAsync()");
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        /// <param name="originalStream"></param>
        /// <param name="originalSize"></param>
        /// <param name="scaledStream"></param>
        /// <param name="scaleSize"></param>
        /// <returns></returns>
        public static async Task ScaleImageStreamAsync(MemoryStream originalStream,
                                                       Size originalSize,
                                                       MemoryStream scaledStream,
                                                       Size scaleSize)
        {
            await ScaleImageStreamAsync(
                originalStream, (int)originalSize.Width, (int)originalSize.Height,
                scaledStream, (int)scaleSize.Width, (int)scaleSize.Height);
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
