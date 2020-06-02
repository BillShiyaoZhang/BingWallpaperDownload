using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using UWPLibrary;

namespace UWP
{
    /// <summary>
    /// The main page includes three buttons and one text block.
    /// Three buttons are: Set Folder, Download, and Open Folder.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        string ImageTodayAddress
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Core.LastDate))
                {
                    return "";
                }
                return $"{Core.ImageAddressPrefix}/{Core.LastDate}.bmp";
            }
        }

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
        /// Initialize the main page.  Download and set today's image 
        /// as wallpaper if it didn't do so early today.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            MyInit();
        }

        private async void MyInit()
        {
            SetWindowSize();

            if (!Core.IsUpdated)
            {
                await RunAsync(false);
            }
            else
            {
                var resourceLoader = Windows.ApplicationModel.Resources
                    .ResourceLoader.GetForCurrentView();
                MainHint.Text = resourceLoader.GetString("Hint/ImageThere");
            }
            MainHint.Visibility = Visibility.Visible;

            if (!string.IsNullOrWhiteSpace(ImageTodayAddress))
                await UpdateImageToday(ImageTodayAddress);
        }

        private async Task UpdateImageToday(string imageLocation)
        {
            // Set image today on UI
            var uri = new Uri(imageLocation);
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            using (var randomAccessStream = await file.OpenAsync(FileAccessMode.Read))
            {
                var image = new BitmapImage();
                await image.SetSourceAsync(randomAccessStream);
                ImageToday.Source = image;
                ImageTodayGrid.Visibility = Visibility.Visible;
            }

            // Set title and description of image today
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            var imageTodayTitleText = (string)localSettings.Values[Core.ImageTodayTitleKey];
            if (!string.IsNullOrWhiteSpace(imageTodayTitleText))
                ImageTodayTitle.Text = imageTodayTitleText;

            var imageTodayDescriptionText = (string)localSettings
                .Values[Core.ImageTodayDescriptionKey];
            if (!string.IsNullOrWhiteSpace(imageTodayDescriptionText))
                ImageTodayDescription.Text = imageTodayDescriptionText;

            var imageTodayHrefText = (string)localSettings
                .Values[Core.ImageTodayLearnMoreHrefKey];
            if (!string.IsNullOrWhiteSpace(imageTodayHrefText))
                ImageTodayHrefButton.NavigateUri
                    = new Uri("https://www.bing.com" + imageTodayHrefText);
        }

        #region Private Methods

        private void SetWindowSize()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            ApplicationView.PreferredLaunchViewSize = new Size(767, 500);
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(510, 320));
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        /// <summary>
        /// Download and set image from Bing as wallpaper and get result.
        /// </summary>
        private async Task RunAsync(bool setFolder)
        {
            var code = await Core.DownloadAndSetWallpaperAsync(setFolder);
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            string msg = "";

            switch (code)
            {
                case DownloadAndSetWallpaperCode.SUCCESSFUL:
                    msg = resourceLoader.GetString("Hint/WallpaperSetSpace") +
                        resourceLoader.GetString("Hint/SuccessedExclamation");
                    break;
                case DownloadAndSetWallpaperCode.FAILED:
                    msg = resourceLoader.GetString("Hint/WallpaperSetSpace") +
                        resourceLoader.GetString("Hint/FailedExclamation");
                    break;
                case DownloadAndSetWallpaperCode.NO_INTERNET:
                    msg = resourceLoader.GetString("Hint/NoInternet");
                    break;
                case DownloadAndSetWallpaperCode.FOLDER_NOT_SET:
                    msg = resourceLoader.GetString("Hint/FolderNotSet");
                    break;
                case DownloadAndSetWallpaperCode.UNEXPECTED_EXCEPTION:
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
        public async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            await RunAsync(true);

            if (!string.IsNullOrWhiteSpace(ImageTodayAddress))
                await UpdateImageToday(ImageTodayAddress);
        }

        private async void OpenBingButton_Click(object sender, RoutedEventArgs e)
        {
            var success = await Windows.System.Launcher.LaunchUriAsync(new Uri(@"https://www.bing.co.uk"));
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (success)
            {
                MainHint.Text = resourceLoader.GetString("Hint/BrowserLaunchedSpace")
                    + resourceLoader.GetString("Hint/SuccessedExclamation");
            }
            else
            {
                MainHint.Text = resourceLoader.GetString("Hint/BrowserLaunchedSpace")
                    + resourceLoader.GetString("Hint/FailedExclamation");
            }
        }

        #endregion

        private void ImageTodayTextPanel_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ImageTodayTitle.SetValue(RelativePanel.AlignBottomWithPanelProperty,
                !(bool)ImageTodayTitle.GetValue(RelativePanel.AlignBottomWithPanelProperty));
            ImageTodayDescription.SetValue(RelativePanel.AlignBottomWithPanelProperty,
               !(bool)ImageTodayDescription.GetValue(RelativePanel.AlignBottomWithPanelProperty));
        }

        private void ImageTodayTextPanel_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var view = (RelativePanel)sender;
            view.Background = Application.Current.Resources["ListViewItemRevealBackgroundSelectedPointerOver"] as Brush;
        }

        private void ImageTodayTextPanel_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var view = (RelativePanel)sender;
            view.Background = Application.Current.Resources["SystemControlAcrylicElementMediumHighBrush"] as Brush;
        }
    }
}

