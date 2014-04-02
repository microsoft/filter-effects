using System;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FilterEffects
{
    /// <summary>
    /// ContinuationManager is used to detect if the most recent activation was due
    /// to a continuation such as the FileOpenPicker
    /// </summary>
    public class ContinuationManager
    {
#if WINDOWS_PHONE_APP
        IContinuationActivatedEventArgs args = null;
        bool handled = false;
        Guid id = Guid.Empty;

        /// <summary>
        /// Sets the ContinuationArgs for this instance. Should be called by the main actication
        /// handling code in App.xaml.cs
        /// </summary>
        /// <param name="args">The activation args</param>
        internal void Continue(IContinuationActivatedEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            if (this.args != null && !handled)
                throw new InvalidOperationException("Can't set args more than once");

            this.args = args;
            this.handled = false;
            id = Guid.NewGuid();

            var rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;

            switch (args.Kind)
            {
                case ActivationKind.PickFileContinuation:
                    var fileOpenPickerPage = rootFrame.Content as IFileOpenPickerContinuable;
                    if (fileOpenPickerPage != null)
                    {
                        fileOpenPickerPage.ContinueFileOpenPicker(args as FileOpenPickerContinuationEventArgs);
                    }
                    break;

                case ActivationKind.PickSaveFileContinuation:
                    var fileSavePickerPage = rootFrame.Content as IFileSavePickerContinuable;
                    if (fileSavePickerPage != null)
                    {
                        fileSavePickerPage.ContinueFileSavePicker(args as FileSavePickerContinuationEventArgs);
                    }
                    break;
            }
        }

        /// <summary>
        /// Marks the contination data as 'stale', meaning that it is probably no longer of
        /// any use. Called when the app is suspended (to ensure future activations don't appear
        /// to be for the same continiation) and whenever the continuation data is retrieved 
        /// (so that it isn't retrieved on subsequent navigations)
        /// </summary>
        internal void MarkAsStale()
        {
            this.handled = true;
        }

        /// <summary>
        /// Retrieves the continuation args, if they have not already been retrieved, and 
        /// prevents further retrieval via this property (to avoid accidentla double-usage)
        /// </summary>
        public IContinuationActivatedEventArgs ContinuationArgs
        {
            get
            {
                if (handled)
                {
                    return null;
                }

                MarkAsStale();
                return args;
            }
        }

        /// <summary>
        /// Unique identifier for this particular continuation. Most useful for components that 
        /// retrieve the continuation data via <see cref="GetContinuationArgs"/> and need
        /// to perform their own replay check
        /// </summary>
        public Guid Id
        {
            get
            {
                return id;
            }
        }

        /// <summary>
        /// Retrieves the continuation args, optionally retrieving them even if they have already
        /// been retrieved
        /// </summary>
        /// <param name="includeStaleArgs">Set to true to return args even if they have previously been returned</param>
        /// <returns>The continuation args, or null if there aren't any</returns>
        public IContinuationActivatedEventArgs GetContinuationArgs(bool includeStaleArgs)
        {
            if (!includeStaleArgs && handled)
            {
                return null;
            }

            MarkAsStale();
            return args;
        }
#endif
    }

    /// <summary>
    /// Implement this interface if your page invokes the file open picker
    /// API.
    /// </summary>
    interface IFileOpenPickerContinuable
    {
#if WINDOWS_PHONE_APP
            /// <summary>
            /// This method is invoked when the file open picker returns picked
            /// files
            /// </summary>
            /// <param name="args"></param>
            void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args);
#endif
    }

    /// <summary>
    /// Implement this interface if your page invokes the file save picker
    /// API
    /// </summary>
    interface IFileSavePickerContinuable
    {
#if WINDOWS_PHONE_APP
            /// <summary>
            /// This method is invoked when the file save picker returns saved
            /// files
            /// </summary>
            /// <param name="args"></param>
            void ContinueFileSavePicker(FileSavePickerContinuationEventArgs args);
#endif
    }
}
