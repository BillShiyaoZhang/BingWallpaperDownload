using System;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using UWPLibrary;

namespace UWP
{
    /// <summary>
    /// The main page includes three buttons and one text block.
    /// Three buttons are: Set Folder, Download, and Open Folder.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Instance of core.
        /// </summary>
        private Core _core;

        /// <summary>
        /// Porperty to access the instance of core.
        /// </summary>
        private Core Core
        {
            get
            {
                if (_core == null)
                {
                    _core = new Core();
                }
                return _core;
            }
        }

        /// <summary>
        /// Initialize the main page.  Download and set today's image as wallpaper if it didn't do so early today.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            SetWindowSize();

            if (!Core.IsUpdated)
            {
                RunAsync(false);
            }
            else
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                MainHint.Text = resourceLoader.GetString("Hint/ImageThere");
            }
            MainHint.Visibility = Visibility.Visible;
        }

        #region Private Methods

        private void SetWindowSize()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["launchedWithPrefSize"] == null)
            {
                // first app launch only!!
                ApplicationView.PreferredLaunchViewSize = new Size(510, 320);
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(510, 320));
                localSettings.Values["launchedWithPrefSize"] = true;
            }
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        /// <summary>
        /// Download and set image from Bing as wallpaper and get result.
        /// </summary>
        private async void RunAsync(bool setFolder)
        {
            var code = await Core.RunAsync(setFolder);
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            string msg = "";
            switch (code)
            {
                case RunFunctionCode.SUCCESSFUL:
                    msg = resourceLoader.GetString("Hint/WallpaperSetSpace") + resourceLoader.GetString("Hint/SuccessedExclamation");
                    break;
                case RunFunctionCode.FAILED:
                    msg = resourceLoader.GetString("Hint/WallpaperSetSpace") + resourceLoader.GetString("Hint/FailedExclamation");
                    break;
                case RunFunctionCode.NO_INTERNET:
                    msg = resourceLoader.GetString("Hint/NoInternet");
                    break;
                case RunFunctionCode.FOLDER_NOT_SET:
                    msg = resourceLoader.GetString("Hint/FolderNotSet");
                    break;
                case RunFunctionCode.UNEXPECTED_EXCEPTION:
                    msg = resourceLoader.GetString("Hint/UnexpectedException");
                    break;
                default:
                    break;
            }
            MainHint.Text = msg;
        }
        
        #endregion

        #region Public Listeners

        /// <summary>
        /// Listener on download button click.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Artuments</param>
        public void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            RunAsync(true);
        }

        private async void OpenBingButton_Click(object sender, RoutedEventArgs e)
        {
            var success = await Windows.System.Launcher.LaunchUriAsync(new Uri(@"https://www.bing.co.uk"));
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (success)
            {
                MainHint.Text = resourceLoader.GetString("Hint/BrowserLaunchedSpace") + resourceLoader.GetString("Hint/SuccessedExclamation");
            }
            else
            {
                MainHint.Text = resourceLoader.GetString("Hint/BrowserLaunchedSpace") + resourceLoader.GetString("Hint/FailedExclamation");
            }
        }

        #endregion

    }
}

