/**
 * Copyright (c) 2013-2014 Microsoft Mobile.
 * See the license file delivered with this project for more information.
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

namespace FilterEffects
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class ViewfinderPage : Page, IFileOpenPickerContinuable
    {
        // Constants
        private const string DebugTag = "ViewfinderPage: ";

        // Members
        private readonly NavigationHelper _navigationHelper;
        
        // MediaCapture class requires both Webcam and Microphone capabilities
        private MediaCapture _mediaCapture;

        private readonly DataContext _dataContext;
        private bool _capturing;
        private bool _pickingFile;
        private bool _resumingFromFile = false;

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get
            {
                return _navigationHelper;
            }
        }

        public ViewfinderPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
            _mediaCapture = new MediaCapture();
            _dataContext = FilterEffects.DataContext.Instance;
            _capturing = false;
            _pickingFile = false;
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
            NavigationHelper.OnNavigatedTo(e);

            if (ProgressIndicator.IsActive)
            {
                ProgressIndicator.IsActive = false;
            }

            if (!_pickingFile)
            {
                InitializeCameraAsync();
            }
            
        }
        
        private void DisplayInfo_OrientationChanged(DisplayInformation sender, object args)
        {
            if (_mediaCapture == null)
            {
                return;
            }
            var rotation = VideoRotationLookup(sender.CurrentOrientation, false);
            _mediaCapture.SetPreviewRotation(rotation);
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
        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (!_resumingFromFile)
            {
                try
                {
                    if (_mediaCapture != null)
                    {
                        await _mediaCapture.StopPreviewAsync();
                        _mediaCapture.Dispose();
                        _mediaCapture = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(DebugTag + "OnNavigatedFrom(): " + ex.ToString());
                }
            }
            else
            {
                _resumingFromFile = false; 
            }
            NavigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// Initialises the camera and resolves the resolution for both the
        /// full resolution and preview images.
        /// </summary>
        private async void InitializeCameraAsync()
        {
            Debug.WriteLine(DebugTag + "InitializeCameraAsync() ->");
            ProgressIndicator.IsActive = true;
            MessageDialog messageDialog = null;

#if WINDOWS_PHONE_APP
            DeviceInformationCollection devices;
            devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
#endif

            try
            {
#if WINDOWS_PHONE_APP
                await _mediaCapture.InitializeAsync(
                    new MediaCaptureInitializationSettings
                    {
                        StreamingCaptureMode = StreamingCaptureMode.Video,
                        PhotoCaptureSource = PhotoCaptureSource.VideoPreview,
                        AudioDeviceId = string.Empty,
                        VideoDeviceId = devices[0].Id
                    });
#else
                await _mediaCapture.InitializeAsync();
#endif
                MyCaptureElement.Source = _mediaCapture;
            }
            catch (Exception ex)
            {
                messageDialog = new MessageDialog(ex.ToString(), LocalizedStrings.GetText("CameraInitializationFailed"));  
            }

            if (messageDialog != null)
            {
                /* Add commands and set their callbacks; both buttons use the
                 * same callback function instead of inline event handlers
                 */
                if (messageDialog.Commands != null)
                {
                    messageDialog.Commands.Add(new UICommand(LocalizedStrings.GetText("Retry"), CommandInvokedHandler));
                    messageDialog.Commands.Add(new UICommand(LocalizedStrings.GetText("Cancel"), CommandInvokedHandler));
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

                _mediaCapture.SetPreviewMirroring(false);
                
                // Start the camera
                await _mediaCapture.StartPreviewAsync();
                
                ProgressIndicator.IsActive = false;
                Debug.WriteLine(DebugTag + "InitializeCameraAsync() <-");
            }

            DisplayInformation displayInfo = DisplayInformation.GetForCurrentView();
            displayInfo.OrientationChanged += DisplayInfo_OrientationChanged;
            
            DisplayInfo_OrientationChanged(displayInfo, null);
            Debug.WriteLine(MyCaptureElement.ActualHeight.ToString());
            
        }

        private void CommandInvokedHandler(IUICommand command)
        {
            if (command.Label.Equals(LocalizedStrings.GetText("Retry")))
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
                MyCaptureElement.Source = null;
                ProgressIndicator.IsActive = true;
                _capturing = true;

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
            await _mediaCapture.StopPreviewAsync();
            
            _mediaCapture.Dispose();
            _mediaCapture = null;

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
