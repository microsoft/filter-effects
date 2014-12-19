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
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;
using Windows.Storage.Streams;

using FilterEffects.Filters;
using FilterEffects.Filters.FilterControls;
using FilterEffects.Resources;

namespace FilterEffects
{
    /// <summary>
    /// The filter preview page is Pivot based, and displays the preview images
    /// of all the created filters. This page also manages saving the image
    /// into the media library.
    /// </summary>
    public partial class PreviewPage : PhoneApplicationPage
    {
        // Constants
        private const String DebugTag = "PreviewPage: ";
        private const double DefaultOutputResolutionWidth = 480;
        private const double DefaultOutputResolutionHeight = 640;
        private const String FileNamePrefix = "FilterEffects_";
        private const String TombstoneImageDir = "TempData";
        private const String TombstoneImageFile = "TempData\\TempImage.jpg";
        private const String StateIndexKey = "PivotIndex";
        private const int HideControlsDelay = 2; // Seconds
        private const String PivotItemNamePrefix = "PivotItem_";
        private const String FilterPropertyControlNamePrefix = "FilterPropertyControl_";

        // Members
        private List<AbstractFilter> _filters = null;
        private List<Image> _previewImages = null;
        private ProgressIndicator _progressIndicator = new ProgressIndicator();
        private DispatcherTimer _timer = null;
        private FilterPropertiesControl _controlToHide = null;
        private bool _isNewPageInstance = false;
        private bool _hintTextShown = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PreviewPage()
        {
            InitializeComponent();
            _isNewPageInstance = true;
            _progressIndicator.IsIndeterminate = true;
            CreateComponents();
        }

        /// <summary>
        /// Creates the preview images and restores page state if needed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // If _isNewPageInstance is true, the state may need to be restored.
            if (_isNewPageInstance)
            {
                RestoreState();
            }

            // If the user navigates back to this page and it has remained in 
            // memory, this value will continue to be false.
            _isNewPageInstance = false;

            CreatePreviewImages();
        }

        /// <summary>
        /// Stores the page state when navigating away from the application.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // On back navigation there is no need to save state.
            if (e.NavigationMode != System.Windows.Navigation.NavigationMode.Back)
            {
                StoreState();
            }
            else
            {
                // Navigating back
                // Dispose the filters
                foreach (AbstractFilter filter in _filters)
                {
                    filter.Dispose();
                }
            }

            FilterPreviewPivot.SelectionChanged -= FilterPreviewPivot_SelectionChanged;
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Store the page state in case application gets tombstoned.
        /// </summary>
        private void StoreState()
        {
            // Save the currently filtered image into isolated app storage.
            IsolatedStorageFile myStore = IsolatedStorageFile.GetUserStoreForApplication();
            myStore.CreateDirectory(TombstoneImageDir);

            try
            {
                using (var isoFileStream = new IsolatedStorageFileStream(
                    TombstoneImageFile,
                    FileMode.OpenOrCreate,
                    myStore))
                {
                    DataContext dataContext = FilterEffects.DataContext.Instance;
                    dataContext.FullResolutionStream.Position = 0;
                    dataContext.FullResolutionStream.CopyTo(isoFileStream);
                    isoFileStream.Flush();
                }
            }
            catch
            {
                MessageBox.Show("Error while trying to store temporary image.");
            }

            // Save also the current preview index 
            State[StateIndexKey] = FilterPreviewPivot.SelectedIndex;
        }

        /// <summary>
        /// Restores the page state if application was tombstoned.
        /// </summary>
        private async void RestoreState()
        {
            // Load the image which was filtered from isolated app storage.
            IsolatedStorageFile myStore = IsolatedStorageFile.GetUserStoreForApplication();

            try
            {
                if (myStore.FileExists(TombstoneImageFile))
                {
                    using (var isoFileStream = new IsolatedStorageFileStream(
                        TombstoneImageFile, 
                        FileMode.Open, 
                        myStore))
                    {
                        DataContext dataContext = FilterEffects.DataContext.Instance;

                        // Load image asynchronously at application launch
                        await isoFileStream.CopyToAsync(dataContext.FullResolutionStream);
                        Dispatcher.BeginInvoke(CreatePreviewImages);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error while trying to restore temporary image.");
            }

            // Remove temporary file from isolated storage
            try
            {
                if (myStore.FileExists(TombstoneImageFile))
                {
                    myStore.DeleteFile(TombstoneImageFile);
                }
            }
            catch (IsolatedStorageException /*ex*/)
            {
                MessageBox.Show("Error while trying to delete temporary image.");
            }

            // Load also the preview index which was last used
            if (State.ContainsKey("PivotIndex"))
            {
                FilterPreviewPivot.SelectedIndex = (int)State[StateIndexKey];
            }
        }

        /// <summary>
        /// Constructs the filters and the pivot items.
        /// </summary>
        private void CreateComponents()
        {
            CreateFilters();

            DataContext dataContext = FilterEffects.DataContext.Instance;
            _previewImages = new List<Image>();
            int i = 0;

            // Create a pivot item with an image for each filter. The image
            // content is added later. In addition, create the preview bitmaps
            // and associate them with the images.
            foreach (AbstractFilter filter in _filters)
            {
                PivotItem pivotItem = new PivotItem {Header = filter.Name};

                if (filter.ShortDescription != null && filter.ShortDescription.Length > 0)
                {
                    pivotItem.Header += " (" + filter.ShortDescription + ")";
                }

                FilterPropertiesControl control = new FilterPropertiesControl();

                String name = FilterPropertyControlNamePrefix + filter.Name;
                control.Name = name;

                Grid grid = new Grid();

                name = PivotItemNamePrefix + filter.Name;
                grid.Name = name;

                _previewImages.Add(new Image());
                grid.Children.Add(_previewImages[i++]);

                if (filter.AttachControl(control))
                {
                    control.VerticalAlignment = VerticalAlignment.Bottom;
                    control.Opacity = 0;
                    control.Visibility = Visibility.Collapsed;
                    control.ControlBackground.Fill = AppUtils.ThemeBackgroundBrush;
                    grid.Children.Add(control);

                    grid.Tap += ShowPropertiesControls;
                    control.Manipulated += OnControlManipulated;
                }

                pivotItem.Content = grid;
                FilterPreviewPivot.Items.Add(pivotItem);
                filter.PreviewResolution = new Windows.Foundation.Size(
                    DefaultOutputResolutionWidth, DefaultOutputResolutionHeight);
            }

            HintTextBackground.Fill = AppUtils.ThemeBackgroundBrush;

            FilterPreviewPivot.SelectionChanged += FilterPreviewPivot_SelectionChanged;
        }

        /// <summary>
        /// Constructs the filters.
        /// </summary>
        private void CreateFilters()
        {
            _filters = new List<AbstractFilter>
            {
                new OriginalImageFilter(),
                new SixthGearFilter(),
                new SadHipsterFilter(),
                new EightiesPopSongFilter(),
                new MarvelFilter(),
                new SurroundedFilter()
            };
        }

        /// <summary>
        /// Takes the captured image and applies filters to it.
        /// </summary>
        private void CreatePreviewImages()
        {
            DataContext dataContext = FilterEffects.DataContext.Instance;

            if (dataContext.PreviewResolutionStream == null)
            {
                // No captured image available!
                NavigationService.GoBack();
                return;
            }

            int i = 0;

            foreach (AbstractFilter filter in _filters)
            {
                filter.Buffer = dataContext.PreviewResolutionStream.GetWindowsRuntimeBuffer();
                _previewImages[i++].Source = filter.PreviewImageSource;
                filter.Apply();
            }
        }

        /// <summary>
        /// Clicking on the save button saves the photo in MainPage.ImageStream
        /// to media library camera roll. Once image has been saved, the
        /// application will navigate back to the main page.
        /// </summary>
        private async void SaveButton_Click(object sender, EventArgs e)
        {
            _progressIndicator.Text = AppResources.SavingText;
            _progressIndicator.IsVisible = true;
            SystemTray.SetProgressIndicator(this, _progressIndicator);
            int selectedIndex = FilterPreviewPivot.SelectedIndex;

            DataContext dataContext = FilterEffects.DataContext.Instance;
            
            GC.Collect();
            
            try
            {
                if (selectedIndex == 0)
                {
                    using (MediaLibrary library = new MediaLibrary())
                    {
                        dataContext.FullResolutionStream.Position = 0;
                        library.SavePictureToCameraRoll(FileNamePrefix
                            + DateTime.Now.ToString() + ".jpg",
                            dataContext.FullResolutionStream);
                    }
                }
                else
                {
                    AbstractFilter filter = _filters[selectedIndex];

                    IBuffer buffer = await filter.RenderJpegAsync(
                        dataContext.FullResolutionStream.GetWindowsRuntimeBuffer());

                    using (MediaLibrary library = new MediaLibrary())
                    {
                        library.SavePictureToCameraRoll(FileNamePrefix
                            + DateTime.Now.ToString() + ".jpg", buffer.AsStream());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save the image: " + ex.ToString());
            }

            _progressIndicator.IsVisible = false;
            SystemTray.SetProgressIndicator(this, _progressIndicator);

            NavigationService.GoBack();
        }

        /// <summary>
        /// Severes the connections related to showing and hiding the filter
        /// property controls and hides the controls if visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FilterPreviewPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine(DebugTag + "FilterPreviewPivot_SelectionChanged()");

            if (!_hintTextShown && FilterPreviewPivot.SelectedIndex != 0)
            {
                HintText.Visibility = Visibility.Visible;
                _hintTextShown = true;
            }
            else if (_hintTextShown
                     && HintText.Visibility == Visibility.Visible
                     && FilterPreviewPivot.SelectedIndex == 0)
            {
                HintText.Visibility = Visibility.Collapsed;
                _hintTextShown = false;
            }

            ShowControlsAnimationStoryBoard.Completed -= ShowControlsAnimationStoryBoard_Completed;
            HideControlsAnimation.Completed -= HideControlsAnimation_Completed;
            ShowControlsAnimationStoryBoard.Stop();
            HideControlsAnimationStoryBoard.Stop();

            if (_controlToHide != null)
            {
                _controlToHide.Visibility = Visibility.Collapsed;
                _controlToHide.Opacity = 0;
                _controlToHide = null;
            }
        }

        /// <summary>
        /// Shows the filter property controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShowPropertiesControls(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (sender is Grid)
            {
                Grid grid = (Grid)sender;

                foreach (UIElement element in grid.Children)
                {
                    if (element is FilterPropertiesControl)
                    {
                        if (element.Visibility == Visibility.Collapsed
                            || element.Opacity < 1)
                        {
                            Debug.WriteLine(DebugTag + "ShowPropertiesControls()");

                            if (HintText.Visibility == Visibility.Visible)
                            {
                                HintText.Visibility = Visibility.Collapsed;
                            }

                            HideControlsAnimation.Completed -= HideControlsAnimation_Completed;
                            HideControlsAnimationStoryBoard.Stop();

                            if (_timer != null)
                            {
                                _timer.Tick -= HidePropertiesControls;
                                _timer.Stop();
                                _timer = null;
                            }

                            _controlToHide = (FilterPropertiesControl)element;
                            _controlToHide.Visibility = Visibility.Visible;

                            try
                            {
                                Storyboard.SetTargetName(ShowControlsAnimation, _controlToHide.Name);
                                ShowControlsAnimation.From = _controlToHide.Opacity;
                                ShowControlsAnimationStoryBoard.Completed += ShowControlsAnimationStoryBoard_Completed;
                                ShowControlsAnimationStoryBoard.Begin();
                            }
                            catch (InvalidOperationException ex)
                            {
                                Debug.WriteLine(ex.ToString());
                            }

                            _timer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 0, HideControlsDelay)};
                            _timer.Tick += HidePropertiesControls;
                            _timer.Start();
                        }
                        else if (e.OriginalSource is Image)
                        {
                            HidePropertiesControls(null, null);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Makes sure that the controls stay visible after the animation is
        /// completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShowControlsAnimationStoryBoard_Completed(object sender, EventArgs e)
        {
            _controlToHide.Opacity = 1;
            ShowControlsAnimationStoryBoard.Completed -= ShowControlsAnimationStoryBoard_Completed;
        }

        /// <summary>
        /// Hides the filter property controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HidePropertiesControls(object sender, EventArgs e)
        {
            ShowControlsAnimationStoryBoard.Stop();

            if (_controlToHide != null)
            {
                Debug.WriteLine(DebugTag + "HidePropertiesControls()");
                Storyboard.SetTargetName(HideControlsAnimation, _controlToHide.Name);
                HideControlsAnimation.From = _controlToHide.Opacity;
                HideControlsAnimationStoryBoard.Begin();
                HideControlsAnimation.Completed += HideControlsAnimation_Completed;
            }

            if (_timer != null)
            {
                _timer.Tick -= HidePropertiesControls;
                _timer.Stop();
                _timer = null;
            }
        }

        /// <summary>
        /// Completes the actions when HideControlsAnimation has finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HideControlsAnimation_Completed(object sender, EventArgs e)
        {
            HideControlsAnimation.Completed -= HideControlsAnimation_Completed;
            _controlToHide.Visibility = Visibility.Collapsed;
            _controlToHide.Opacity = 0;
            _controlToHide = null;
        }

        /// <summary>
        /// Restarts the timer responsible for hiding the filter property
        /// controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnControlManipulated(object sender, EventArgs e)
        {
            Debug.WriteLine(DebugTag + "OnControlManipulated(): " + sender);

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Start();
            }
        }
    }
}
