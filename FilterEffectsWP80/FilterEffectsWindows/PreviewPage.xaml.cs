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
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using FilterEffects.Common;
using FilterEffects.Filters;
using FilterEffects.ViewModel;

namespace FilterEffects
{
    /// <summary>
    /// A page that displays a group title, a list of items within the group, and details for
    /// the currently selected item.
    /// </summary>
    public sealed partial class PreviewPage : Page
    {
        // Constants
        private const string DebugTag = "FilterPreviewPage: ";
//        private const string TombstoneImageDir = "TempData";
//        private const string TombstoneImageFile = "TempData\\TempImage.jpg";

        // Members
        private readonly NavigationHelper _navigationHelper;
        private List<AbstractFilter> _filters;
        private FilterPreviewViewModel _filterPreviewViewModel;

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return _navigationHelper; }
        }


        public PreviewPage()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
            _navigationHelper.LoadState += navigationHelper_LoadState;
            _navigationHelper.SaveState += navigationHelper_SaveState;

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
            _navigationHelper.OnNavigatedTo(e);
            CreateComponents();
            CreatePreviewImagesAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

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

        /// <summary>
        /// Constructs the filters and the pivot items.
        /// </summary>
        private void CreateComponents()
        {
            if (_filters == null)
            {
                Debug.WriteLine(DebugTag + "CreateComponents()");
                _filterPreviewViewModel = new FilterPreviewViewModel();
                DataContext = _filterPreviewViewModel;

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
        }

        /// <summary>
        /// Initializes the filters and creates the preview images.
        /// </summary>
        private async void CreatePreviewImagesAsync()
        {
            Debug.WriteLine(DebugTag + "CreatePreviewImages()");
            DataContext dataContext = FilterEffects.DataContext.Instance;

            if (dataContext.PreviewResolutionStream == null)
            {
                Debug.WriteLine(DebugTag + "No image (preview) stream available!");
                _navigationHelper.GoBack();
            }
            else
            {
                foreach (AbstractFilter filter in _filters)
                {
                    Debug.WriteLine(DebugTag + "CreatePreviewImages(): " + filter.Name);
                    filter.PreviewResolution = FilterEffects.DataContext.Instance.PreviewResolution;
                    filter.Buffer = dataContext.PreviewResolutionStream.GetWindowsRuntimeBuffer();
                    filter.Apply();

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
                }
            }

            FilterPreviewListView.SelectedIndex = 0;
        }

        /// <summary>
        /// Clicking on the save button saves the photo in MainPage.ImageStream
        /// to media library camera roll. Once image has been saved, the
        /// application will navigate back to the main page.
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = FilterPreviewListView.SelectedIndex;

            DataContext dataContext = FilterEffects.DataContext.Instance;

            // Create the File Picker control
            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add("JPG File", new List<string> { ".jpg" });
            StorageFile file = await picker.PickSaveFileAsync();

            if (file != null)
            {
                // If the file path and name is entered properly, and user has not tapped 'cancel'..

                AbstractFilter filter = _filters[selectedIndex];
                IBuffer buffer = await filter.RenderJpegAsync(
                    dataContext.FullResolutionStream.GetWindowsRuntimeBuffer());

                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await stream.WriteAsync(buffer);
                    await stream.FlushAsync();
                }

                ShowToast(Strings.ImageSavedAs + file.Name);
            }            
        }

        /// <summary>
        /// Displays a single line toast message. Note that the app needs to be
        /// declared as toast capable to be able to show toast notifications
        /// (See Package.appxmanifest -> Application -> Notifications). Also
        /// note that the toast message will not be shown when the app is run
        /// in Simulator.
        /// </summary>
        /// <param name="message">The message to show.</param>
        private void ShowToast(string message)
        {
            XmlDocument notificationXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            XmlNodeList toastElement = notificationXml.GetElementsByTagName("text");
            toastElement[0].AppendChild(notificationXml.CreateTextNode(message));
            var toastNotification = new ToastNotification(notificationXml);
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);
        }
    }
}
