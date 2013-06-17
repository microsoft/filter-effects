/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;
using Windows.Storage.Streams;

using Nokia.Graphics;
using Nokia.Graphics.Imaging;

using FilterEffects.Resources;
using System.Windows.Media.Animation;

namespace FilterEffects
{
    /// <summary>
    /// The filter preview page is Pivot based, and displays the preview images
    /// of all the created filters. This page also manages saving the image
    /// into the media library.
    /// </summary>
    public partial class FilterPreviewPage : PhoneApplicationPage
    {
        // Constants
        private const int DefaultOutputResolutionWidth = 480;
        private const int DefaultOutputResolutionHeight = 640;
        private const String FileNamePrefix = "FilterEffects_";
        private const String TombstoneImageDir = "TempData";
        private const String TombstoneImageFile = "TempData\\TempImage.jpg";
        private const String StateIndexKey = "PivotIndex";
        private const int HideControlsDelay = 2; // Seconds
        private const String PivotItemNamePrefix = "PivotItem_";
        private const String FilterPropertyControlNamePrefix = "FilterPropertyControl_";

        // Members
        private List<AbstractFilter> _filters = null;
        private ProgressIndicator _progressIndicator = new ProgressIndicator();
        private DispatcherTimer _timer = null;
        private FilterPropertiesControl _controlToHide = null;
        private bool _isNewPageInstance = false;
        private bool _hintTextShown = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FilterPreviewPage()
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
                    DataContext dataContext = FilterEffects.DataContext.Singleton;
                    dataContext.ImageStream.Position = 0;
                    dataContext.ImageStream.CopyTo(isoFileStream);
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
                        DataContext dataContext = FilterEffects.DataContext.Singleton;

                        // Load image asynchronously at application launch
                        await isoFileStream.CopyToAsync(dataContext.ImageStream);
                        Dispatcher.BeginInvoke(() =>
                        {
                            CreatePreviewImages();
                        });
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

            DataContext dataContext = FilterEffects.DataContext.Singleton;

            // Create a pivot item with an image for each filter. The image
            // content is added later. In addition, create the preview bitmaps
            // and associate them with the images.
            foreach (AbstractFilter filter in _filters)
            {
                PivotItem pivotItem = new PivotItem();
                pivotItem.Header = filter.Name;

                FilterPropertiesControl control = new FilterPropertiesControl();

                String name = FilterPropertyControlNamePrefix + filter.Name;
                control.Name = name;

                Grid grid = new Grid();

                name = PivotItemNamePrefix + filter.Name;
                grid.Name = name;

                grid.Children.Add(filter.PreviewImage);

                if (filter.AttachControl(control))
                {
                    control.VerticalAlignment = VerticalAlignment.Bottom;
                    control.Opacity = 0;
                    control.Visibility = Visibility.Collapsed;
                    grid.Children.Add(control);

                    grid.Tap += ShowPropertiesControls;
                    control.Manipulated += OnControlManipulated;
                }

                pivotItem.Content = grid;
                FilterPreviewPivot.Items.Add(pivotItem);
                filter.SetOutputResolution(DefaultOutputResolutionWidth,
                                           DefaultOutputResolutionHeight);
            }

            FilterPreviewPivot.SelectionChanged += FilterPreviewPivot_SelectionChanged;
        }

        /// <summary>
        /// Constructs the filters.
        /// </summary>
        private void CreateFilters()
        {
            _filters = new List<AbstractFilter>();
            _filters.Add(new OriginalImageFilter()); // This is for the original image and has no effects
            _filters.Add(new CarShowFilter());
            _filters.Add(new SadHipsterFilter());
            _filters.Add(new EightiesPopSongFilter());
            _filters.Add(new CartoonFilter());
        }

        /// <summary>
        /// Takes the captured image and applies filters to it.
        /// </summary>
        private async void CreatePreviewImages()
        {
            DataContext dataContext = FilterEffects.DataContext.Singleton;

            if (dataContext.ThumbStream == null)
            {
                // No captured image available!
                NavigationService.GoBack();
                return;
            }

            // Create a session that we will use with all the filters
            IBuffer buffer = dataContext.ImageStream.GetWindowsRuntimeBuffer();
            EditingSession session = new EditingSession(buffer);

            // Perform the filtering
            using (session)
            {
                foreach (AbstractFilter filter in _filters)
                {
                    filter.SetBuffer(buffer);
                    filter.DefineFilter(session);

                    await filter.RenderToBitmapAsync(session);
                    session.UndoAll();
                }
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

            MediaLibrary library = new MediaLibrary();
            int selectedIndex = FilterPreviewPivot.SelectedIndex;

            DataContext dataContext = FilterEffects.DataContext.Singleton;

            try
            {
                if (selectedIndex == 0)
                {
                    library.SavePictureToCameraRoll(FileNamePrefix
                        + DateTime.Now.ToString() + ".jpg",
                        dataContext.ImageStream);
                }
                else
                {
                    AbstractFilter filter = _filters[selectedIndex];
                    EditingSession session = new EditingSession(dataContext.ImageStream.GetWindowsRuntimeBuffer());

                    using (session)
                    {
                        filter.DefineFilter(session);
                        IBuffer data = await session.RenderToJpegAsync();

                        // Save the rendered image into a file
                        library.SavePictureToCameraRoll(FileNamePrefix
                            + DateTime.Now.ToString() + ".jpg", data.AsStream());
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("FilterPreviewPage::SaveButton_Click(): Failed to save the image to camera roll: "
                    + ex.ToString());
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
            Debug.WriteLine("FilterPreviewPage.FilterPreviewPivot_SelectionChanged()");

            if (!_hintTextShown && FilterPreviewPivot.SelectedIndex != 0)
            {
                HintText.Visibility = Visibility.Visible;
                _hintTextShown = true;
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
                            Debug.WriteLine("FilterPreviewPage.ShowPropertiesControls()");

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

                            _timer = new DispatcherTimer();
                            _timer.Interval = new TimeSpan(0, 0, 0, HideControlsDelay);
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
        /// 
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
                Debug.WriteLine("FilterPreviewPage.HidePropertiesControls()");
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
            Debug.WriteLine("FilterPreviewPage.OnControlManipulated(): " + sender);

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Start();
            }
        }
    }
}
