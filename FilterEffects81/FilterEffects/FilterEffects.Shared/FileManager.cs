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
using Windows.ApplicationModel.Activation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace FilterEffects
{
    /// <summary>
    /// A utility class for loading and saving image files to the file system.
    /// </summary>
    public class FileManager
    {
        private const string DebugTag = "FileManager: ";
        private const string SelectImageOperationName = "SelectImage";
        private const string SelectDestinationOperationName = "SelectDestination";
        private const string JpegFileTypeDescription = "JPEG file";

        private IBuffer _imageBuffer;
        private readonly string[] _supportedImageFilePostfixes = { ".jpg", ".jpeg", ".png" };
        private readonly List<string> _supportedSaveImageFilePostfixes = new List<string> { ".jpg" };

        public event EventHandler<bool> ImageFileLoadedResult;
        public event EventHandler<bool> ImageFileSavedResult;

        private static FileManager _instance;
        public static FileManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FileManager();
                }

                return _instance;
            }
        }

        public string NameOfSavedFile
        {
            get;
            private set;
        }

        private FileManager()
        {
        }

        public async Task<bool> GetImageFileAsync()
        {
            bool success = false;

            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail
            };

            // Filter to include a sample subset of file types
            picker.FileTypeFilter.Clear();

            foreach (string postfix in _supportedImageFilePostfixes)
            {
                picker.FileTypeFilter.Add(postfix);
            }

#if WINDOWS_PHONE_APP
            picker.ContinuationData["Operation"] = SelectImageOperationName;
            picker.PickSingleFileAndContinue();
            success = true;
#else
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                success = await HandleSelectedImageFileAsync(file);
            }
#endif
            return success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageBuffer"></param>
        /// <returns></returns>
        public async Task<bool> SaveImageFileAsync(IBuffer imageBuffer)
        {
            _imageBuffer = imageBuffer;
            bool success = false;

            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };

            picker.FileTypeChoices.Add(JpegFileTypeDescription, _supportedSaveImageFilePostfixes);
            picker.SuggestedFileName = "FE_" + FormattedDateTime() + _supportedSaveImageFilePostfixes[0];
            System.Diagnostics.Debug.WriteLine(DebugTag + "SaveImageFile(): Suggested filename is " + picker.SuggestedFileName);

#if WINDOWS_PHONE_APP
            picker.ContinuationData["Operation"] = SelectDestinationOperationName;
            picker.PickSaveFileAndContinue();
            success = true;
#else
            StorageFile file = await picker.PickSaveFileAsync();

            if (file != null)
            {
                success = await SaveImageFileAsync(file);
                NameOfSavedFile = file.Name;
            }
#endif
            return success;
        }

#if WINDOWS_PHONE_APP
        public async void ContinueFileOpenPickerAsync(FileOpenPickerContinuationEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(DebugTag + "ContinueFileOpenPicker()");
            bool success = false;

            if (args.Files == null || args.Files.Count == 0 || args.Files[0] == null
                || (args.ContinuationData["Operation"] as string) != SelectImageOperationName)
            {
                System.Diagnostics.Debug.WriteLine(DebugTag + "ContinueFileOpenPicker(): Invalid arguments!");
            }
            else
            {
                StorageFile file = args.Files[0];
                success = await HandleSelectedImageFileAsync(file);
            }

            NotifyLoadedResult(success);
            App.ContinuationManager.MarkAsStale();
        }

        public async void ContinueFileSavePickerAsync(FileSavePickerContinuationEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(DebugTag + "ContinueFileSavePicker()");
            bool success = false;
            StorageFile file = args.File;

            if (file != null && (args.ContinuationData["Operation"] as string) == SelectDestinationOperationName)
            {
                success = await SaveImageFileAsync(file);
                NameOfSavedFile = file.Name;
            }

            NotifySavedResult(success);
            App.ContinuationManager.MarkAsStale();
        }
#endif

        /// <summary>
        /// Reads the given image file and writes it to the buffers while also
        /// scaling a preview image.
        /// 
        /// Note that this method can't handle null argument!
        /// </summary>
        /// <param name="file">The selected image file.</param>
        /// <returns>True if successful, false otherwise.</returns>
        private async Task<bool> HandleSelectedImageFileAsync(StorageFile file)
        {
            System.Diagnostics.Debug.WriteLine(DebugTag + "HandleSelectedImageFile(): " + file.Name);
            var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            DataContext dataContext = DataContext.Instance;

            // Reset the streams
            dataContext.ResetStreams();

            var image = new BitmapImage();
            image.SetSource(fileStream);
            int width = image.PixelWidth;
            int height = image.PixelHeight;
            dataContext.SetFullResolution(width, height);

            int previewWidth = (int)FilterEffects.DataContext.DefaultPreviewResolutionWidth;
            int previewHeight = 0;
            AppUtils.CalculatePreviewResolution(width, height, ref previewWidth, ref previewHeight);
            dataContext.SetPreviewResolution(previewWidth, previewHeight);

            bool success = false;

            try
            {
                // JPEG images can be used as such
                Stream stream = fileStream.AsStream();
                stream.Position = 0;
                stream.CopyTo(dataContext.FullResolutionStream);
                success = true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(DebugTag
                    + "Cannot use stream as such (not probably in JPEG format): " + e.Message);
            }

            if (!success)
            {
                try
                {
                    await AppUtils.FileStreamToJpegStreamAsync(fileStream,
                        (IRandomAccessStream)dataContext.FullResolutionStream.AsInputStream());
                    success = true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(DebugTag
                        + "Failed to convert the file stream content into JPEG format: "
                        + e.ToString());
                }
            }

            if (success)
            {
                await AppUtils.ScaleImageStreamAsync(
                    dataContext.FullResolutionStream,
                    dataContext.FullResolution,
                    dataContext.PreviewResolutionStream,
                    dataContext.PreviewResolution);

                dataContext.WasCaptured = false;
            }

            return success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async Task<bool> SaveImageFileAsync(StorageFile file)
        {
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                await stream.WriteAsync(_imageBuffer);
                await stream.FlushAsync();
            }

            return true;
        }

        /// <summary>
        /// Notifies possible listeners about the result of the image file
        /// loading operation.
        /// </summary>
        /// <param name="wasSuccessful">True if the result was a success, false otherwise.</param>
        private void NotifyLoadedResult(bool wasSuccessful)
        {
            EventHandler<bool> handler = ImageFileLoadedResult;

            if (handler != null)
            {
                handler(this, wasSuccessful);
            }
        }

        /// <summary>
        /// Notifies possible listeners about the result of the image file
        /// saving operation.
        /// </summary>
        /// <param name="wasSuccessful">True if the result was a success, false otherwise.</param>
        private void NotifySavedResult(bool wasSuccessful)
        {
            EventHandler<bool> handler = ImageFileSavedResult;

            if (handler != null)
            {
                handler(this, wasSuccessful);
            }
        }

        private string FormattedDateTime()
        {
            string dateTimeString = DateTime.Now.ToString();
            dateTimeString = dateTimeString.Replace('\\', '-');
            dateTimeString = dateTimeString.Replace('/', '-');
            dateTimeString = dateTimeString.Replace(' ', '_');
            dateTimeString = dateTimeString.Replace('.', '_');
            dateTimeString = dateTimeString.Replace(":", "");
            return dateTimeString;
        }
    }
}
