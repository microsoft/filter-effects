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
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

using FilterEffects.Common;
using Windows.ApplicationModel.Resources;

namespace FilterEffects
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class ViewfinderPage : Page, IFileOpenPickerContinuable
    {
        // Constants
        private const string DebugTag = "ViewfinderPage: ";

        // Data types
        public enum CameraStates
        {
            NotInitialized,
            Initializing,
            Initialized,
            Capturing,
            Stopping
        }

        // Members and properties
        private MediaCapture _mediaCapture; // MediaCapture class requires both Webcam and Microphone capabilities
        private readonly DataContext _dataContext;
        private ResourceLoader _resourceLoader;
        private CameraStates _cameraState = CameraStates.NotInitialized;
        private bool _pickingFile;
        private bool _resumingFromFile;
        private bool _cam;

        public CameraStates CameraState
        {
            get
            {
                return _cameraState;
            }
            private set
            {
                Debug.WriteLine("CameraState changed from " + _cameraState + " to " + value);
                _cameraState = value;
            }
        }

        public ViewfinderPage()
        {
            InitializeComponent();
            _dataContext = FilterEffects.DataContext.Instance;
            _resourceLoader = new ResourceLoader();
        }

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
            Debug.WriteLine(DebugTag + "OnNavigatedTo(): Picking file == " + _pickingFile);
            Window.Current.VisibilityChanged += OnVisibilityChanged;

            if (ProgressIndicator.IsActive)
            {
                ProgressIndicator.IsActive = false;
            }

            if (!_pickingFile)
            {
                InitializeCameraAsync();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Window.Current.VisibilityChanged -= OnVisibilityChanged;

            if (!_resumingFromFile)
            {
                StopCameraAsync();
            }
            else
            {
                _resumingFromFile = false;
            }
        }

        void OnVisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            Debug.WriteLine(DebugTag + "OnVisibilityChanged(): " + e.Visible);

            if (e.Visible && !_pickingFile)
            {
                InitializeCameraAsync();
            }
            else
            {
                StopCameraAsync();
            }
        }

        private void DisplayInfo_OrientationChanged(DisplayInformation sender, object args)
        {
            if (_mediaCapture == null)
            {
                return;
            }

            _mediaCapture.SetPreviewRotation(_cam
                    ? VideoRotationLookup(sender.CurrentOrientation, true)
                    : VideoRotationLookup(sender.CurrentOrientation, false));
            var rotation = VideoRotationLookup(sender.CurrentOrientation, false);
            
            _mediaCapture.SetRecordRotation(rotation);
        }

        private VideoRotation VideoRotationLookup(DisplayOrientations displayOrientation, bool counterclockwise)
        {
            switch (displayOrientation)
            {
                case DisplayOrientations.Landscape:
                    return VideoRotation.None;

                case DisplayOrientations.Portrait:
                    return (counterclockwise) ? VideoRotation.Clockwise270Degrees : VideoRotation.Clockwise90Degrees;

                case DisplayOrientations.LandscapeFlipped:
                    return VideoRotation.Clockwise180Degrees;

                case DisplayOrientations.PortraitFlipped:
                    return (counterclockwise) ? VideoRotation.Clockwise90Degrees :
                        VideoRotation.Clockwise270Degrees;

                default:
                    return VideoRotation.None;
            }
        }

        /// <summary>
        /// Initialises the camera and resolves the resolution for both the
        /// full resolution and preview images.
        /// </summary>
        private async void InitializeCameraAsync()
        {
            if (CameraState != CameraStates.NotInitialized)
            {
                Debug.WriteLine(DebugTag + "InitializeCameraAsync(): Invalid state");
                return;
            }

            Debug.WriteLine(DebugTag + "InitializeCameraAsync() ->");
            CameraState = CameraStates.Initializing;
            ProgressIndicator.IsActive = true;
            MessageDialog messageDialog = null;

#if WINDOWS_PHONE_APP
            DeviceInformationCollection devices;
            devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            if (devices.Count == 0)
            {
                ProgressIndicator.IsActive = false;
                CameraState = CameraStates.NotInitialized;
                messageDialog = new MessageDialog(
                    _resourceLoader.GetString("FailedToFindCameraDevice/Text"),
                    _resourceLoader.GetString("CameraInitializationFailed/Text"));
                await messageDialog.ShowAsync();
                return;
            }

            // Use the back camera 
            DeviceInformation info = devices[0];
            _cam = true;

            foreach (var devInfo in devices)
            {
                if (devInfo.Name.ToLowerInvariant().Contains("back"))
                {
                    info = devInfo;
                    _cam = false; // Set this to true if you use front camera 
                    break;
                }
            }

            MyCaptureElement.FlowDirection = _cam ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
#endif
            _mediaCapture = new MediaCapture();

            try
            {
#if WINDOWS_PHONE_APP
                await _mediaCapture.InitializeAsync(
                    new MediaCaptureInitializationSettings
                    {
                        StreamingCaptureMode = StreamingCaptureMode.Video,
                        PhotoCaptureSource = PhotoCaptureSource.VideoPreview,
                        AudioDeviceId = string.Empty,
                        VideoDeviceId = info.Id
                    });
#else
                await _mediaCapture.InitializeAsync();
#endif
            }
            catch (Exception ex)
            {
                messageDialog = new MessageDialog(ex.ToString(), _resourceLoader.GetString("CameraInitializationFailed/Text"));  
            }

            MyCaptureElement.Source = _mediaCapture;

            if (messageDialog != null)
            {
                /* Add commands and set their callbacks; both buttons use the
                 * same callback function instead of inline event handlers
                 */
                if (messageDialog.Commands != null)
                {
                    messageDialog.Commands.Add(new UICommand(_resourceLoader.GetString("Retry/Text"), CommandInvokedHandler));
                    messageDialog.Commands.Add(new UICommand(_resourceLoader.GetString("Cancel/Text"), CommandInvokedHandler));
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
                uint width = 0;
                uint height = 0;
                IMediaEncodingProperties propertiesToSet = null;
                AppUtils.GetBestResolution(_mediaCapture, ref width, ref height, ref propertiesToSet);

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
                    await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(
                        MediaStreamType.Photo, propertiesToSet);
                    Debug.WriteLine(DebugTag + "Capture resolution set to " + width + "x" + height + "!");
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

                _mediaCapture.SetPreviewMirroring(false);
                await _mediaCapture.StartPreviewAsync();
            }
            
#if WINDOWS_PHONE_APP
            // In case front camera is used, we need to handle the rotations
            DisplayInformation displayInfo = DisplayInformation.GetForCurrentView();
            displayInfo.OrientationChanged += DisplayInfo_OrientationChanged;      
            DisplayInfo_OrientationChanged(displayInfo, null);
#endif
            CameraState = CameraStates.Initialized;
            ProgressIndicator.IsActive = false;
            CaptureButton.IsEnabled = true;
            Debug.WriteLine(DebugTag + "InitializeCameraAsync() <-");
        }

        /// <summary>
        /// Stops camera.
        /// </summary>
        public async void StopCameraAsync()
        {
            if (CameraState != CameraStates.Initialized || _mediaCapture == null)
            {
                Debug.WriteLine(DebugTag + "StopCameraAsync(): Camera is not initialised");
                return;
            }

            CameraState = CameraStates.Stopping;
            CaptureButton.IsEnabled = false;

            try
            {
                Debug.WriteLine(DebugTag + "StopCameraAsync(): Stopping camera...");
                await _mediaCapture.StopPreviewAsync();
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(DebugTag + "StopCameraAsync(): Failed to stop camera: " + ex.ToString());
            }

            CameraState = CameraStates.NotInitialized;
        }

        private void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label.Equals(_resourceLoader.GetString("Retry/Text")))
            {
                InitializeCameraAsync();
            }
        }

        /// <summary>
        /// Captures a photo. Photo data is stored to ImageStream, and
        /// application is navigated to the preview page after capturing.
        /// </summary>
        private async Task CaptureAsync()
        {
            bool goToPreview = false;

            if (CameraState == CameraStates.Initialized)
            {
                CameraState = CameraStates.Capturing;
                CaptureButton.IsEnabled = false;
                MyCaptureElement.Source = null;
                ProgressIndicator.IsActive = true;

                _dataContext.ResetStreams();

                IRandomAccessStream stream = _dataContext.FullResolutionStream.AsRandomAccessStream();
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);

                await _mediaCapture.StopPreviewAsync();
                _mediaCapture.Dispose();
                _mediaCapture = null;

                await AppUtils.ScaleImageStreamAsync(
                    _dataContext.FullResolutionStream,
                    _dataContext.FullResolution,
                    _dataContext.PreviewResolutionStream,
                    _dataContext.PreviewResolution);

                CameraState = CameraStates.Initialized;
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
            await CaptureAsync();
        }

        private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            StopCameraAsync();

            _pickingFile = true;
            FileManager fileManager = FileManager.Instance;
            bool success = await fileManager.GetImageFileAsync();

            if (success)
            {
#if WINDOWS_PHONE_APP
                fileManager.ImageFileLoadedResult += OnImageFileLoadedResult;
                ProgressIndicator.IsActive = true;
#else
                // On Windows the image is already loaded and we are ready to
                // move on to the preview page
                _pickingFile = false;
                ProgressIndicator.IsActive = true;
                Frame.Navigate(typeof(PreviewPage));
#endif
            }
            else
            {
                _pickingFile = false;
                InitializeCameraAsync();
            }
        }

#if WINDOWS_PHONE_APP
        public void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {
            FileManager.Instance.ContinueFileOpenPickerAsync(args);
            _resumingFromFile = true;
        }
#endif

        private void OnImageFileLoadedResult(object sender, bool wasSuccessful)
        {
            Debug.WriteLine(DebugTag + "OnImageFileLoadedResult(): " + wasSuccessful);
            _pickingFile = false;
            FileManager.Instance.ImageFileLoadedResult -= OnImageFileLoadedResult;

            if (wasSuccessful)
            {
                Frame.Navigate(typeof(PreviewPage));
            }
            else
            {
                _mediaCapture = new MediaCapture();
                InitializeCameraAsync();
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
#if WINDOWS_PHONE_APP
            Frame.Navigate(typeof(AboutPage));
#endif
        }
    }
}
