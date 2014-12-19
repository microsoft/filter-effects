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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using FilterEffects.Common;
using FilterEffects.Filters;

#if WINDOWS_PHONE_APP
using Windows.UI.Xaml.Media.Animation; // For StoryBoard
#else
using FilterEffects.ViewModel;
#endif

namespace FilterEffects
{
    /// <summary>
    /// Displays the filtered preview images, controls for modifying the filter
    /// properties. The selected image can be saved in full resolution.
    /// </summary>
    public sealed partial class PreviewPage : Page, IFileSavePickerContinuable
    {
        // Constants
        private const string DebugTag = "PreviewPage: ";
#if WINDOWS_PHONE_APP
        private const String PivotItemNamePrefix = "PivotItem_";
        private const int HideControlsDelay = 2; // Seconds
#endif

        // Members
        private readonly NavigationHelper _navigationHelper;
        private ResourceLoader _resourceLoader;
        private List<AbstractFilter> _filters;
#if WINDOWS_PHONE_APP
        private List<Image> _previewImages = null;
        private DispatcherTimer _timer = null;
        private bool _hintTextShown = false;
#else
        private FilterPreviewViewModel _filterPreviewViewModel;
#endif

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

        /// <summary>
        /// Constructor.
        /// </summary>
        public PreviewPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
            _navigationHelper.LoadState += navigationHelper_LoadState;
            _navigationHelper.SaveState += navigationHelper_SaveState;
            _resourceLoader = new ResourceLoader();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO Implement and make async
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO
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
            CreateComponents();
            CreatePreviewImagesAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            NavigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// Constructs the filters and the pivot items.
        /// </summary>
        private void CreateComponents()
        {
            if (_filters == null)
            {
                Debug.WriteLine(DebugTag + "CreateComponents()");
#if !WINDOWS_PHONE_APP
                _filterPreviewViewModel = new FilterPreviewViewModel();
                DataContext = _filterPreviewViewModel;
#endif

                _filters = new List<AbstractFilter>
                {
                    new OriginalImageFilter(), // This is for the original image and has no effects
                    new SixthGearFilter(),
                    new SadHipsterFilter(),
                    new EightiesPopSongFilter(),
                    new MarvelFilter(),
                    new SurroundedFilter()
                };
            }

#if WINDOWS_PHONE_APP
            DataContext dataContext = FilterEffects.DataContext.Instance;
            _previewImages = new List<Image>();
            int i = 0;

            // Create a pivot item with an image for each filter. The image
            // content is added later. In addition, create the preview bitmaps
            // and associate them with the images.
            foreach (AbstractFilter filter in _filters)
            {
                var pivotItem = new PivotItem {Header = filter.Name};

                if (!string.IsNullOrEmpty(filter.ShortDescription))
                {
                    pivotItem.Header += " (" + filter.ShortDescription + ")";
                }

                var grid = new Grid {Name = PivotItemNamePrefix + filter.Name};

                _previewImages.Add(new Image());
                grid.Children.Add(_previewImages[i++]);

                filter.PropertiesManipulated += OnControlManipulated;

                pivotItem.Content = grid;
                FilterPreviewPivot.Items.Add(pivotItem);
                filter.PreviewResolution = FilterEffects.DataContext.Instance.PreviewResolution;
            }

            FilterPreviewPivot.SelectionChanged += FilterPreviewPivot_SelectionChanged;
#endif
        }

        /// <summary>
        /// Initializes the filters and creates the preview images.
        /// </summary>
        private async void CreatePreviewImagesAsync()
        {
            Debug.WriteLine(DebugTag + "CreatePreviewImagesAsync()");
            DataContext dataContext = FilterEffects.DataContext.Instance;

            if (dataContext.PreviewResolutionStream == null)
            {
                Debug.WriteLine(DebugTag + "No image (preview) stream available!");
                NavigationHelper.GoBack();
            }
            else
            {
#if WINDOWS_PHONE_APP
                int i = 0;
#endif
                foreach (AbstractFilter filter in _filters)
                {
                    Debug.WriteLine(DebugTag + "CreatePreviewImagesAsync(): " + filter.Name);
                    filter.PreviewResolution = FilterEffects.DataContext.Instance.PreviewResolution;
                    filter.Buffer = dataContext.PreviewResolutionStream.GetWindowsRuntimeBuffer();
#if WINDOWS_PHONE_APP
                    _previewImages[i++].Source = filter.PreviewImageSource;
#endif
                    filter.Apply();

#if !WINDOWS_PHONE_APP
                    if (filter is OriginalImageFilter)
                    {
                        AbstractFilter filter1 = filter;
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                            Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                PreviewImage.Source = filter1.PreviewImageSource;
                            });
                    }

                    _filterPreviewViewModel.Add(filter);
#endif
                }
            }

#if !WINDOWS_PHONE_APP
            FilterPreviewListView.SelectedIndex = 0;
#endif
        }

        /// <summary>
        /// Clicking on the save button opens a file picker, which prompts for
        /// the location where to save the image.
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ProgressIndicator.IsActive = true;
#if WINDOWS_PHONE_APP
            int selectedIndex = FilterPreviewPivot.SelectedIndex;
#else
            int selectedIndex = FilterPreviewListView.SelectedIndex;
#endif
            AbstractFilter filter = _filters[selectedIndex];
            DataContext dataContext = FilterEffects.DataContext.Instance;
            IBuffer buffer = await filter.RenderJpegAsync(
                dataContext.FullResolutionStream.GetWindowsRuntimeBuffer());
            FileManager fileManager = FileManager.Instance;
            bool success = await fileManager.SaveImageFileAsync(buffer);

            if (success)
            {
#if WINDOWS_PHONE_APP
                fileManager.ImageFileSavedResult += OnImageFileSavedResult;
#else
                // On Windows the image is already saved
                OnImageFileSavedResult(null, success);
#endif
            }
            else
            {
                ProgressIndicator.IsActive = false;
            }
        }

#if WINDOWS_PHONE_APP
        public void ContinueFileSavePicker(FileSavePickerContinuationEventArgs args)
        {
            FileManager.Instance.ContinueFileSavePickerAsync(args);
        }
#endif

        private void OnImageFileSavedResult(object sender, bool wasSuccessful)
        {
            FileManager fileManager = FileManager.Instance;
#if WINDOWS_PHONE_APP
            fileManager.ImageFileSavedResult -= OnImageFileSavedResult;
#endif
            ProgressIndicator.IsActive = false;

            if (wasSuccessful)
            {
                AppUtils.ShowToast(_resourceLoader.GetString("ImageSavedAs/Text")
                    + " " + fileManager.NameOfSavedFile);
            }
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Severes the connections related to showing and hiding the filter
        /// property controls and hides the controls if visible.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FilterPreviewPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = FilterPreviewPivot.SelectedIndex;
            Debug.WriteLine(DebugTag + "FilterPreviewPivot_SelectionChanged(): Index is " + index);

            if (!_hintTextShown && index != 0)
            {
                HintText.Visibility = Visibility.Visible;
                _hintTextShown = true;
            }
            else if (_hintTextShown
                     && HintText.Visibility == Visibility.Visible
                     && index == 0)
            {
                HintText.Visibility = Visibility.Collapsed;
                _hintTextShown = false;
            }

            ShowControlsAnimationStoryBoard.Completed -= ShowControlsAnimationStoryBoard_Completed;
            HideControlsAnimation.Completed -= HideControlsAnimation_Completed;
            ShowControlsAnimationStoryBoard.Stop();
            HideControlsAnimationStoryBoard.Stop();

            FilterControlsContainer.Visibility = Visibility.Collapsed;
            FilterControlsContainer.Opacity = 0;
            FilterControlsContainer.Children.Clear();

            AbstractFilter filter = _filters[index];

            if (filter.Control != null)
            {
                FilterControlsContainer.Children.Add(filter.Control);
            }
        }
#else
        /// <summary>
        /// Invoked when an item within the list is selected.
        /// </summary>
        /// <param name="sender">The GridView displaying the selected item.</param>
        /// <param name="e">Event data that describes how the selection was changed.</param>
        private void FilterPreviewListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterEffects.DataContext.Instance != null)
            {
                DataContext dataContext = FilterEffects.DataContext.Instance;

                foreach (object item in e.AddedItems)
                {
                    string filterName = ((AbstractFilter)item).Name;
                    Debug.WriteLine(DebugTag + "FilterPreviewListView_SelectionChanged(): " + filterName);

                    foreach (AbstractFilter filter in _filters)
                    {
                        if (filter.Name.Equals(filterName))
                        {
                            PreviewImage.Source = filter.PreviewImageSource;
                            FilterControlsContainer.Children.Clear();

                            if (filter.Control != null)
                            {
                                FilterControlsContainer.Children.Add(filter.Control);
                            }

                            if (filterName.Equals("Original") && !dataContext.WasCaptured)
                            {
                                /* It does not make sense to allow saving the
                                 * original image if it was taken from the file
                                 * system.
                                 */
                                SaveButton.IsEnabled = false;
                            }
                            else
                            {
                                SaveButton.IsEnabled = true;
                            }
                        }
                    }
                }
            }
        }
#endif

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Shows the filter property controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShowPropertiesControls(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Debug.WriteLine(DebugTag + "ShowPropertiesControls()");

            if (FilterControlsContainer.Visibility == Visibility.Collapsed
                || FilterControlsContainer.Opacity < 1)
            {
                Debug.WriteLine(DebugTag + "ShowPropertiesControls(): Showing");

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

                FilterControlsContainer.Visibility = Visibility.Visible;

                try
                {
                    Storyboard.SetTargetName(ShowControlsAnimation, FilterControlsContainer.Name);
                    ShowControlsAnimation.From = FilterControlsContainer.Opacity;
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

        /// <summary>
        /// Makes sure that the controls stay visible after the animation is
        /// completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShowControlsAnimationStoryBoard_Completed(object sender, object e)
        {
            FilterControlsContainer.Opacity = 1;
            ShowControlsAnimationStoryBoard.Completed -= ShowControlsAnimationStoryBoard_Completed;
        }

        /// <summary>
        /// Hides the filter property controls.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void HidePropertiesControls(object sender, object e)
        {
            Debug.WriteLine(DebugTag + "HidePropertiesControls()");
            ShowControlsAnimationStoryBoard.Stop();

            Storyboard.SetTargetName(HideControlsAnimation, FilterControlsContainer.Name);
            HideControlsAnimation.From = FilterControlsContainer.Opacity;
            HideControlsAnimationStoryBoard.Begin();
            HideControlsAnimation.Completed += HideControlsAnimation_Completed;

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
        void HideControlsAnimation_Completed(object sender, object e)
        {
            HideControlsAnimation.Completed -= HideControlsAnimation_Completed;
            FilterControlsContainer.Visibility = Visibility.Collapsed;
            FilterControlsContainer.Opacity = 0;
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
#endif
    }
}
