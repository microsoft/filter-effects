using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using FilterEffects.Common;

#if WINDOWS_PHONE_APP
using Windows.Phone.UI.Input;
#endif

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace FilterEffects
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
#if WINDOWS_PHONE_APP
        private TransitionCollection _transitions;
        public static ContinuationManager ContinuationManager { get; private set; }
#endif
        public static Frame RootFrame { get; private set; }

        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;

#if WINDOWS_PHONE_APP
            ContinuationManager = new ContinuationManager();
#endif
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Handles the back button press and navigates through the history of the root frame.
        /// </summary>
        /// <param name="sender">The source of the event. <see cref="HardwareButtons"/></param>
        /// <param name="e">Details about the back button press.</param>
        private void OnBackPressed(object sender, BackPressedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;

            if (frame == null)
            {
                return;
            }

            if (frame.CanGoBack && !e.Handled)
            {
                frame.GoBack();
                e.Handled = true;
            }
        }
#endif

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            CreateRootFrame();

            if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                // TODO: Load state from previously suspended application
            }

            if (RootFrame.Content == null)
            {
#if WINDOWS_PHONE_APP
                // TODO: change this value to a cache size that is appropriate for your application
                RootFrame.CacheSize = 1;

                // Removes the turnstile navigation for startup.
                if (RootFrame.ContentTransitions != null)
                {
                    this._transitions = new TransitionCollection();
                    foreach (var c in RootFrame.ContentTransitions)
                    {
                        this._transitions.Add(c);
                    }
                }

                RootFrame.ContentTransitions = null;
                RootFrame.Navigated += this.RootFrame_FirstNavigated;
#endif

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!RootFrame.Navigate(typeof(ViewfinderPage), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        private void CreateRootFrame()
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (RootFrame != null)
                return;

            // Create a Frame to act as the navigation context and navigate to the first page
            RootFrame = new Frame();

            //Associate the frame with a SuspensionManager key                                
            SuspensionManager.RegisterFrame(RootFrame, "AppFrame");

            // Set the default language
            RootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

            RootFrame.NavigationFailed += OnNavigationFailed;

            // Place the frame in the current Window
            Window.Current.Content = RootFrame;
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

#if WINDOWS_PHONE_APP
        protected override void OnActivated(IActivatedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("App.OnActivated(): " + e.PreviousExecutionState.ToString());

            // Check if this is a continuation
            var continuationEventArgs = e as IContinuationActivatedEventArgs;

            if (continuationEventArgs != null)
            {
                ContinuationManager.Continue(continuationEventArgs);
            }

            Window.Current.Activate();
        }

        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            if (rootFrame != null)
            {
                rootFrame.ContentTransitions = this._transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
                rootFrame.Navigated -= this.RootFrame_FirstNavigated;
            }
        }
#endif

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

#if WINDOWS_PHONE_APP
            ContinuationManager.MarkAsStale();
#endif

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}