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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Media.Imaging;

namespace FilterEffects
{
    class AppUtils
    {
        private const string DebugTag = "AppUtils: ";
        
        /// <summary>
        /// Retrieves the available resolutions from the given MediaCapture
        /// instance and places the best values into the given references.
        /// </summary>
        /// <param name="mediaCapture">A MediaCapture instance, e.g. a camera device.</param>
        /// <param name="width">The width of the best resolution.</param>
        /// <param name="height">The height of the best resolution.</param>
        /// <param name="mediaEncodingProperties">The IMediaEncodingProperties for the best resolution.</param>
        public static void GetBestResolution(MediaCapture mediaCapture,
                                             ref uint width,
                                             ref uint height,
                                             ref IMediaEncodingProperties mediaEncodingProperties)
        {
            if (mediaCapture.VideoDeviceController != null)
            {
                IReadOnlyList<IMediaEncodingProperties> mediaEncodingPropertiesList =
                    mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);
                IEnumerator<IMediaEncodingProperties> enumerator = mediaEncodingPropertiesList.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    IMediaEncodingProperties encodingProperties = enumerator.Current;
                    
                    if (encodingProperties == null)
                    {
                        continue;
                    }

                    uint foundWidth = 0;
                    uint foundHeight = 0;

                    try
                    {
                        var imageEncodingProperties = encodingProperties as ImageEncodingProperties;

                        if (imageEncodingProperties != null)
                        {
                            foundWidth = imageEncodingProperties.Width;
                            foundHeight = imageEncodingProperties.Height;
                        }
                        else
                        {
                            var videoEncodingProperties = encodingProperties as VideoEncodingProperties;

                            if (videoEncodingProperties != null)
                            {
                                foundWidth = videoEncodingProperties.Width;
                                foundHeight = videoEncodingProperties.Height;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(DebugTag
                            + "Failed to resolve available resolutions: " + ex.Message);
                    }

                    System.Diagnostics.Debug.WriteLine(DebugTag + "Available resolution: "
                        + foundWidth + "x" + foundHeight);

                    //if (encodingProperties.Width * encodingProperties.Height > width * height) // Use this to get the resolution with the most pixels
                    if (foundWidth > width) // Use this to get the widest resolution
                    {
                        width = foundWidth;
                        height = foundHeight;
                        mediaEncodingProperties = encodingProperties;
                    }
                }
            }
        }

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
        /// Encodes the image data in the given file stream to JPEG and writes
        /// it to the given destination stream. This method should be used if
        /// the image data is not JPEG encoded. Note that the method has no
        /// error handling.
        /// </summary>
        /// <param name="fileStream">The file stream with the image data.</param>
        /// <param name="destinationStream">Where the JPEG data is written to.</param>
        /// <returns></returns>
        public static async Task FileStreamToJpegStreamAsync(IRandomAccessStream fileStream,
                                                             IRandomAccessStream destinationStream)
        {
            var image = new BitmapImage();
            image.SetSource(fileStream);
            int width = image.PixelWidth;
            int height = image.PixelHeight;
            var bitmap = new WriteableBitmap(width, height);
            bitmap.SetSource(fileStream);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, destinationStream);
            Stream outStream = bitmap.PixelBuffer.AsStream();
            var pixels = new byte[outStream.Length];
            await outStream.ReadAsync(pixels, 0, pixels.Length);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 96.0, 96.0, pixels);
            await encoder.FlushAsync();
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

        /// <summary>
        /// Displays a single line toast message. Note that the app needs to be
        /// declared as toast capable to be able to show toast notifications
        /// (See Package.appxmanifest -> Application -> Notifications). Also
        /// note that the toast message will not be shown when the app is run
        /// in Simulator.
        /// </summary>
        /// <param name="message">The message to show.</param>
        public static void ShowToast(string message)
        {
            XmlDocument notificationXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            XmlNodeList toastElement = notificationXml.GetElementsByTagName("text");
            toastElement[0].AppendChild(notificationXml.CreateTextNode(message));
            var toastNotification = new ToastNotification(notificationXml);
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
        }
    }
}
