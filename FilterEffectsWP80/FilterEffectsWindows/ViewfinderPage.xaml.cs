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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

using FilterEffects.Common;

namespace FilterEffects
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class ViewfinderPage : Page
    {
        // Constants
        private const string DebugTag = "ViewfinderPage: ";
        private readonly string[] _supportedImageFilePostfixes = { ".jpg", ".jpeg", ".png" };

        // Members
        private readonly NavigationHelper _navigationHelper;

        // MediaCapture class requires both Webcam and Microphone capabilities
        private readonly MediaCapture _photoCaptureManager;
        readonly DataContext _dataContext;
        private bool _capturing;

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return _navigationHelper; }
        }

        public ViewfinderPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
            _photoCaptureManager = new MediaCapture();
            _dataContext = FilterEffects.DataContext.Instance;
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            InitializeCameraAsync();
            _navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        private async void InitializeCameraAsync()
        {
            MessageDialog messageDialog = null;

            try
            {
                await _photoCaptureManager.InitializeAsync();
                CapturePreview.Source = _photoCaptureManager;
            }
            catch (Exception ex)
            {
                messageDialog = new MessageDialog(ex.ToString(), Strings.CameraInitializationFailed);
                
            }

            if (messageDialog != null)
            {
                /* Add commands and set their callbacks; both buttons use the
                 * same callback function instead of inline event handlers
                 */
                if (messageDialog.Commands != null)
                {
                    messageDialog.Commands.Add(new UICommand(Strings.Retry, CommandInvokedHandler));
                    messageDialog.Commands.Add(new UICommand(Strings.Cancel, CommandInvokedHandler));
                }

                // Set the command that will be invoked by default
                messageDialog.DefaultCommandIndex = 0;

                // Set the command to be invoked when escape is pressed
                messageDialog.CancelCommandIndex = 1;

                await messageDialog.ShowAsync();
            }
            else
            {
                // Get the resolution
                if (_photoCaptureManager.VideoDeviceController != null)
                {
                    var mediaEncodingPropertiesList = _photoCaptureManager.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);
                    uint width = 0;
                    uint height = 0;
                    IMediaEncodingProperties propertiesToSet = null;

                    for (int i = 0; i < mediaEncodingPropertiesList.Count; ++i)
                    {
                        IMediaEncodingProperties mediaEncodingProperties =
                            mediaEncodingPropertiesList.ElementAt(i);
                        uint foundWidth = 0;
                        uint foundHeight = 0;

                        try
                        {
                            var properties = mediaEncodingProperties as ImageEncodingProperties;
                            if (properties != null)
                            {
                                foundWidth = properties.Width;
                                foundHeight = properties.Height;
                            }
                            else
                            {
                                var encodingProperties = mediaEncodingProperties as VideoEncodingProperties;
                                if (encodingProperties != null)
                                {
                                    foundWidth = encodingProperties.Width;
                                    foundHeight = encodingProperties.Height;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(DebugTag + "Failed to resolve available resolutions: " + ex.Message);
                        }

                        Debug.WriteLine(DebugTag + "Available resolution: "
                                        + foundWidth + "x" + foundHeight);

                        //if (encodingProperties.Width * encodingProperties.Height > width * height) // Use this to get the resolution with the most pixels
                        if (foundWidth > width) // Use this to get the widest resolution
                        {
                            width = foundWidth;
                            height = foundHeight;
                            propertiesToSet = mediaEncodingProperties;
                        }
                    }

                    if (width > 0 && height > 0)
                    {
                        _dataContext.SetFullResolution((int)width, (int)height);
                        int previewWidth = (int)FilterEffects.DataContext.DefaultPreviewResolutionWidth;
                        int previewHeight = 0;
                        AppUtils.CalculatePreviewResolution((int)width, (int)height, ref previewWidth, ref previewHeight);
                        _dataContext.SetPreviewResolution(previewWidth, previewHeight);
                    }

                    if (propertiesToSet != null)
                    {
                        await _photoCaptureManager.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, propertiesToSet);
                        Debug.WriteLine(DebugTag + "Capture resolution set to "
                                        + width + "x" + height + "!");
                    }
                    else
                    {
                        Debug.WriteLine(DebugTag + "Failed to set capture resolution! Using fallback resolution.");
                        var fallbackResolution = new Size(
                            FilterEffects.DataContext.DefaultPreviewResolutionWidth,
                            FilterEffects.DataContext.DefaultPreviewResolutionHeight);
                        _dataContext.PreviewResolution = fallbackResolution;
                        _dataContext.FullResolution = fallbackResolution;
                    }
                }

                // Start the camera
                await _photoCaptureManager.StartPreviewAsync();
            }
        }

        private void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label.Equals(Strings.Retry))
            {
                InitializeCameraAsync();
            }
        }

        /// <summary>
        /// Captures a photo. Photo data is stored to ImageStream, and
        /// application is navigated to the preview page after capturing.
        /// </summary>
        private async Task Capture()
        {
            bool goToPreview = false;

            if (!_capturing)
            {
                CapturePreview.Source = null;
                Progress.IsActive = true;
                _capturing = true;

                _dataContext.ResetStreams();

                IRandomAccessStream stream = _dataContext.FullResolutionStream.AsRandomAccessStream();
                await _photoCaptureManager.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

                await _photoCaptureManager.StopPreviewAsync();

                await AppUtils.ScaleImageStreamAsync(
                    _dataContext.FullResolutionStream,
                    _dataContext.FullResolution,
                    _dataContext.PreviewResolutionStream,
                    _dataContext.PreviewResolution);

                _capturing = false;
                goToPreview = true;
            }

            if (goToPreview)
            {
                _dataContext.WasCaptured = true;
                Frame.Navigate(typeof(PreviewPage));
            }
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            await Capture();
        }

        private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            await _photoCaptureManager.StopPreviewAsync();

            var openPicker = new Windows.Storage.Pickers.FileOpenPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary,
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail
            };

            // Filter to include a sample subset of file types
            openPicker.FileTypeFilter.Clear();

            foreach (string postfix in _supportedImageFilePostfixes)
            {
                openPicker.FileTypeFilter.Add(postfix);
            }

            // Open the file picker
            Windows.Storage.StorageFile file = await openPicker.PickSingleFileAsync();

            // File is null if user cancels the file picker
            if (file != null)
            {
                var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);

                // Reset the streams
                _dataContext.ResetStreams();

                var image = new BitmapImage();
                image.SetSource(fileStream);
                int width = image.PixelWidth;
                int height = image.PixelHeight;
                var bitmap = new WriteableBitmap(width, height);
                _dataContext.SetFullResolution(width, height);

                int previewWidth = (int)FilterEffects.DataContext.DefaultPreviewResolutionWidth;
                int previewHeight = 0;
                AppUtils.CalculatePreviewResolution(width, height, ref previewWidth, ref previewHeight);
                _dataContext.SetPreviewResolution(previewWidth, previewHeight);

                bool success = false;

                try
                {
                    // Jpeg images can be used as such.
                    Stream stream = fileStream.AsStream();
                    stream.Position = 0;
                    stream.CopyTo(_dataContext.FullResolutionStream);
                    success = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(DebugTag
                        + "Cannot use stream as such (not probably jpeg): " + ex.Message);
                }

                if (!success)
                {
                    // TODO: Test this part! It may not work.
                    //
                    // Image format is not jpeg. Can be anything, so first
                    // load it into a bitmap image and then write as jpeg.
                    bitmap.SetSource(fileStream);
                    var inStream = (IRandomAccessStream)_dataContext.FullResolutionStream.AsInputStream();
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, inStream);
                    Stream outStream = bitmap.PixelBuffer.AsStream();
                    var pixels = new byte[outStream.Length];
                    await outStream.ReadAsync(pixels, 0, pixels.Length);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 96.0, 96.0, pixels);
                    await encoder.FlushAsync();
                }

                await AppUtils.ScaleImageStreamAsync(
                    _dataContext.FullResolutionStream,
                    _dataContext.FullResolution,
                    _dataContext.PreviewResolutionStream,
                    _dataContext.PreviewResolution);

                _dataContext.WasCaptured = false;
                Frame.Navigate(typeof(PreviewPage));
            } // if (file != null)
        }
    }
}
