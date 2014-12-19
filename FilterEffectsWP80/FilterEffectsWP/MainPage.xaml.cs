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

using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Phone.Media.Capture;

using FilterEffects.Resources;

namespace FilterEffects
{
    /// <summary>
    /// The main page manages the camera and the main navigation model of the
    /// application.
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        // Members
        private PhotoCaptureDevice _photoCaptureDevice = null;
        private PhotoChooserTask _photoChooserTask = null;
        private ProgressIndicator _progressIndicator = new ProgressIndicator();
        private bool _capturing = false;

        /// <summary>
        /// Contructor.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            ApplicationBarMenuItem menuItem = new ApplicationBarMenuItem
            {
                Text = AppResources.AboutText,
                IsEnabled = false
            };
            menuItem.Click += new EventHandler(AboutMenuItem_Click);
            ApplicationBar.MenuItems.Add(menuItem);

            _photoChooserTask = new PhotoChooserTask();
            _photoChooserTask.Completed += new EventHandler<PhotoResult>(PhotoChooserTask_Completed_Async);

            _progressIndicator.IsIndeterminate = true;
        }

        /// <summary>
        /// If camera has not been initialized when navigating to this page, initialization
        /// will be started asynchronously in this method. Once initialization has been
        /// completed the camera will be set as a source to the VideoBrush element
        /// declared in XAML. On-screen controls are enabled when camera has been initialized.
        /// </summary>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_photoCaptureDevice != null)
            {
                _photoCaptureDevice.Dispose();
                _photoCaptureDevice = null;
            }

            ShowProgress(AppResources.InitializingCameraText);
            await InitializeCamera(CameraSensorLocation.Back);
            HideProgress();
            BackgroundVideoBrush.SetSource(_photoCaptureDevice);

            SetScreenButtonsEnabled(true);
            SetCameraButtonsEnabled(true);
            Storyboard sb = (Storyboard)Resources["CaptureAnimation"];
            sb.Stop();

            SetOrientation(this.Orientation);

            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// On-screen controls are disabled when navigating away from the
        /// viewfinder. This is because we want the controls to default to
        /// disabled when arriving to the page again.
        /// </summary>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_photoCaptureDevice != null)
            {
                _photoCaptureDevice.Dispose();
                _photoCaptureDevice = null;
            }

            SetScreenButtonsEnabled(false);
            SetCameraButtonsEnabled(false);

            base.OnNavigatingFrom(e);
        }

        /// <summary>
        /// Adjusts UI according to device orientation.
        /// </summary>
        /// <param name="e">Orientation event arguments.</param>
        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            base.OnOrientationChanged(e);

            SetOrientation(e.Orientation);
        }

        /// <summary>
        /// Makes adjustments to UI depending on device orientation. Ensures 
        /// that the viewfinder stays fully visible in the middle of the 
        /// screen. This requires dynamic changes to title and video canvas.
        /// </summary>
        /// <param name="orientation">Device orientation.</param>
        private void SetOrientation(PageOrientation orientation)
        {
            // Default values in landscape left orientation.
            int videoBrushTransformRotation = 0;
            int videoCanvasWidth = 640;
            int videoCanvasHeight = 480;
            Thickness videoCanvasMargin = new Thickness(-60, 0, 0, 0);
            Thickness titleTextMargin = new Thickness(0, 0, 0, 0);

            // Orientation.specific changes to default values
            if (orientation == PageOrientation.PortraitUp)
            {
                videoBrushTransformRotation = 90;
                videoCanvasWidth = 480;
                videoCanvasHeight = 640;
                videoCanvasMargin = new Thickness(0, -20, 0, 0);
            }
            else if (orientation == PageOrientation.LandscapeRight)
            {
                videoBrushTransformRotation = 180;
                videoCanvasMargin = new Thickness(60, 0, 0, 0);
                titleTextMargin = new Thickness(60, 0, 0, 0);
            }

            // Set correct values
            VideoBrushTransform.Rotation = videoBrushTransformRotation;
            VideoCanvas.Width = videoCanvasWidth;
            VideoCanvas.Height = videoCanvasHeight;
            VideoCanvas.Margin = videoCanvasMargin;
            TitleText.Margin = titleTextMargin;

            if (_photoCaptureDevice != null)
            {
                _photoCaptureDevice.SetProperty(
                    KnownCameraGeneralProperties.EncodeWithOrientation,
                    VideoBrushTransform.Rotation);
            }
        }

        /// <summary>
        /// Enables or disabled on-screen controls.
        /// </summary>
        /// <param name="enabled">True to enable controls, false to disable controls.</param>
        private void SetScreenButtonsEnabled(bool enabled)
        {
            foreach (ApplicationBarIconButton b in ApplicationBar.Buttons)
            {
                b.IsEnabled = enabled;
            }

            foreach (ApplicationBarMenuItem m in ApplicationBar.MenuItems)
            {
                m.IsEnabled = enabled;
            }
        }

        /// <summary>
        /// Enables or disables listening to hardware shutter release key events.
        /// </summary>
        /// <param name="enabled">True to enable listening, false to disable listening.</param>
        private void SetCameraButtonsEnabled(bool enabled)
        {
            if (enabled)
            {
                Microsoft.Devices.CameraButtons.ShutterKeyHalfPressed += ShutterKeyHalfPressed;
                Microsoft.Devices.CameraButtons.ShutterKeyPressed += ShutterKeyPressed;
            }
            else
            {
                Microsoft.Devices.CameraButtons.ShutterKeyHalfPressed -= ShutterKeyHalfPressed;
                Microsoft.Devices.CameraButtons.ShutterKeyPressed -= ShutterKeyPressed;
            }
        }

        /// <summary>
        /// Clicking on the capture button initiates autofocus and captures a photo.
        /// </summary>
        private async void CaptureButton_Click(object sender, EventArgs e)
        {
            await AutoFocus();
            await Capture();
        }

        /// <summary>
        /// Clicking on the load button begins photo choosing.
        /// </summary>
        private void LoadButton_Click(object sender, EventArgs e)
        {
            try
            {
                _photoChooserTask.Show();
            }
            catch (System.InvalidOperationException /*ex*/)
            {
                MessageBox.Show("An error occurred while choosing an image.");
            }
        }

        /// <summary>
        /// Clicking on the about menu item initiates navigating to the about page.
        /// </summary>
        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Displays the progress indicator with the given message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        private void ShowProgress(String message)
        {
            _progressIndicator.Text = message;
            _progressIndicator.IsVisible = true;
            SystemTray.SetProgressIndicator(this, _progressIndicator);
        }

        /// <summary>
        /// Hides the progress indicator.
        /// </summary>
        private void HideProgress()
        {
            _progressIndicator.IsVisible = false;
            SystemTray.SetProgressIndicator(this, _progressIndicator);
        }

        /// <summary>
        /// Initializes camera.
        /// </summary>
        /// <param name="sensorLocation">Camera sensor to initialize</param>
        private async Task InitializeCamera(CameraSensorLocation sensorLocation)
        {
            Windows.Foundation.Size initialResolution =
                new Windows.Foundation.Size(FilterEffects.DataContext.DefaultPreviewResolutionWidth,
                                            FilterEffects.DataContext.DefaultPreviewResolutionHeight);
            Windows.Foundation.Size previewResolution =
                new Windows.Foundation.Size(FilterEffects.DataContext.DefaultPreviewResolutionWidth,
                                            FilterEffects.DataContext.DefaultPreviewResolutionHeight);

            // Find out the largest 4:3 capture resolution available on device
            IReadOnlyList<Windows.Foundation.Size> availableResolutions =
                PhotoCaptureDevice.GetAvailableCaptureResolutions(sensorLocation);

            Windows.Foundation.Size captureResolution = new Windows.Foundation.Size(0, 0);

            for (int i = 0; i < availableResolutions.Count; i++)
            {
                double ratio = availableResolutions[i].Width / availableResolutions[i].Height;
                if (ratio > 1.32 && ratio < 1.34)
                {
                    if (captureResolution.Width < availableResolutions[i].Width)
                    {
                        captureResolution = availableResolutions[i];
                    }
                }
            }
 
            PhotoCaptureDevice device =
                await PhotoCaptureDevice.OpenAsync(sensorLocation, initialResolution);

            await device.SetPreviewResolutionAsync(previewResolution);
            await device.SetCaptureResolutionAsync(captureResolution);

            _photoCaptureDevice = device;

            SetOrientation(this.Orientation);
        }

        /// <summary>
        /// Starts autofocusing, if supported. Capturing buttons are disabled
        /// while focusing.
        /// </summary>
        private async Task AutoFocus()
        {
            if (!_capturing && PhotoCaptureDevice.IsFocusSupported(_photoCaptureDevice.SensorLocation))
            {
                SetScreenButtonsEnabled(false);
                SetCameraButtonsEnabled(false);

                await _photoCaptureDevice.FocusAsync();

                SetScreenButtonsEnabled(true);
                SetCameraButtonsEnabled(true);

                _capturing = false;
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
                _capturing = true;

                DataContext dataContext = FilterEffects.DataContext.Instance;

                // Reset the streams
                dataContext.ResetStreams();

                CameraCaptureSequence sequence = _photoCaptureDevice.CreateCaptureSequence(1);
                sequence.Frames[0].CaptureStream = dataContext.FullResolutionStream.AsOutputStream();
                sequence.Frames[0].ThumbnailStream = dataContext.PreviewResolutionStream.AsOutputStream();

                await _photoCaptureDevice.PrepareCaptureSequenceAsync(sequence);
                await sequence.StartCaptureAsync();

                // Get the storyboard from application resources
                Storyboard sb = (Storyboard)Resources["CaptureAnimation"];
                sb.Begin(); 

                _capturing = false;
                goToPreview = true;
            }

            _photoCaptureDevice.SetProperty(
                KnownCameraPhotoProperties.LockedAutoFocusParameters,
                AutoFocusParameters.None);

            if (goToPreview)
            {
                NavigationService.Navigate(new Uri("/PreviewPage.xaml", UriKind.Relative));
            }
        }

        /// <summary>
        /// Half-pressing the shutter key initiates autofocus unless tapped to focus.
        /// </summary>
        private async void ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            await AutoFocus();
        }

        /// <summary>
        /// Completely pressing the shutter key initiates capturing a photo.
        /// </summary>
        private async void ShutterKeyPressed(object sender, EventArgs e)
        {
            await Capture();
        }

        /// <summary>
        /// Called when an image has been selected using PhotoChooserTask.
        /// </summary>
        /// <param name="sender">PhotoChooserTask that is completed.</param>
        /// <param name="e">Result of the task, including chosen photo.</param>
        private async void PhotoChooserTask_Completed_Async(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK && e.ChosenPhoto != null)
            {
                DataContext dataContext = FilterEffects.DataContext.Instance;

                // Reset the streams
                dataContext.ResetStreams();

                // Use the largest possible dimensions
                WriteableBitmap bitmap = new WriteableBitmap(3552, 2448);

                BitmapImage image = new BitmapImage();
                image.SetSource(e.ChosenPhoto);

                try
                {
                    // Jpeg images can be used as such
                    bitmap.LoadJpeg(e.ChosenPhoto);
                    e.ChosenPhoto.Position = 0;
                    e.ChosenPhoto.CopyTo(dataContext.FullResolutionStream);
                }
                catch (Exception /*ex*/)
                {
                    // Image format is not jpeg. Can be anything, so first 
                    // load it into a bitmap image and then write as jpeg
                    bitmap = new WriteableBitmap(image);
                    bitmap.SaveJpeg(dataContext.FullResolutionStream, image.PixelWidth, image.PixelHeight, 0, 100);
                }

                dataContext.SetFullResolution(image.PixelWidth, image.PixelHeight);

                dataContext.PreviewResolution = new Windows.Foundation.Size(
                    FilterEffects.DataContext.DefaultPreviewResolutionWidth, 0);
                int previewWidth = (int)FilterEffects.DataContext.DefaultPreviewResolutionWidth;
                int previewHeight = 0;

                AppUtils.CalculatePreviewResolution(
                    (int)dataContext.FullResolution.Width, (int)dataContext.FullResolution.Height,
                    ref previewWidth, ref previewHeight);

                dataContext.SetPreviewResolution(previewWidth, previewHeight);

                await AppUtils.ScaleImageStreamAsync(
                    e.ChosenPhoto, dataContext.PreviewResolutionStream, dataContext.PreviewResolution);

                // Get the storyboard from application resources
                Storyboard sb = (Storyboard)Resources["CaptureAnimation"];
                sb.Begin();

                NavigationService.Navigate(new Uri("/PreviewPage.xaml", UriKind.Relative));
            }
        }
    }
}