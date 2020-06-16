using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Resources;
using Windows.Media.SpeechSynthesis;
using Windows.ApplicationModel;
using UWPLibrary;

namespace UWP
{
    /// <summary>
    /// The main page includes three buttons and one text block.
    /// Three buttons are: Set Folder, Download, and Open Folder.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private bool isMediaPlaying = false;

        private static string ImageTodayDescriptionVisibleKey { get { return "ImageTodayDescriptionVisibleKey"; } }

        private static string FirstLaunchKey { get { return "FirstLaunchKey"; } }
        private static bool IsFirstLaunch { get { return Core.GetLocalSettingsOrDefault(FirstLaunchKey, true); } }

        static string ImageTodayAddress
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
        private Core Model
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
            try
            {
                MyInit();
            }
            catch (Exception)
            {
                MainHint.Text = ResourceLoader.GetForCurrentView()
                    .GetString("Hint/UnexpectedException");
            }
        }

        private async void MyInit()
        {
            if (IsFirstLaunch)
                FirstLaunch();

            SetWindowSize();

            if (!Model.IsUpdated)
            {
                var code = await RunAsync();
                SetHint(code);
            }
            else
            {
                MainHint.Text = ResourceLoader.GetForCurrentView()
                    .GetString("Hint/ImageThere");
            }
            MainHint.Visibility = Visibility.Visible;

            if (!string.IsNullOrWhiteSpace(ImageTodayAddress))
                //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                //{
                //UI code here
                await UpdateImageToday(ImageTodayAddress);
            //});

            await SetAutoRead().ConfigureAwait(false);
        }

        private void FirstLaunch()
        {
            // show teaching tips
            MainTeachingTip.IsOpen = true;
            ApplicationData.Current.LocalSettings.Values[FirstLaunchKey] = false;

            UWP.Settings.RegisterBackgroundTasks();
        }

        private void SetHint(DownloadAndSetWallpaperCode code)
        {
            var resourceLoader = ResourceLoader.GetForCurrentView();
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

        private async Task SetAutoRead()
        {
            // Set auto read
            if (Core.GetLocalSettingsOrDefault(Core.AutoReadKey, false))
                await AutoReadImageTodayAsync().ConfigureAwait(false);
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

            //var imageTodayHrefText = (string)localSettings
            //    .Values[Core.ImageTodayLearnMoreHrefKey];
            //if (!string.IsNullOrWhiteSpace(imageTodayHrefText))
            //    ImageTodayHrefButton.NavigateUri
            //        = new Uri("https://www.bing.com" + imageTodayHrefText);

            // Set description visibility
            if (Core.GetLocalSettingsOrDefault(ImageTodayDescriptionVisibleKey, true))
                ImageTodayDescription.Visibility = Visibility.Visible;
            else
                ImageTodayDescription.Visibility = Visibility.Collapsed;

        }

        private async Task AutoReadImageTodayAsync()
        {
            // The object for controlling the speech synthesis engine (voice).
            using (var synth = new SpeechSynthesizer())
            {
                var autoReadText = "";
                var title = ImageTodayTitle.Text;
                if (!string.IsNullOrWhiteSpace(title))
                    autoReadText += $"Title: {title}\n";
                var description = ImageTodayDescription.Text;
                if (!string.IsNullOrWhiteSpace(description))
                    autoReadText += $"Description: {description}";

                ReadAloud.Label = ResourceLoader.GetForCurrentView().GetString("ReadAloudMute/Label");
                ReadAloud.Icon = new SymbolIcon(Symbol.Mute);

                var srs = new Windows.UI.Accessibility.ScreenReaderService();
                // if screen reader is open, wait it until AutomationProperties.Name 
                // of ReadAloud button finished read
                if (srs.CurrentScreenReaderPosition.ScreenPositionInRawPixels != default(Rect))
                {
                    await Task.Delay(TimeSpan.FromSeconds(3.5)).ConfigureAwait(true);
                }

                // Generate the audio stream from plain text.
                SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(autoReadText);
                // Send the stream to the media object.
                mediaElement.SetSource(stream, stream.ContentType);
                isMediaPlaying = true;
            }
        }

        #region Private Methods

        private static void SetWindowSize()
        {
            ApplicationView.PreferredLaunchViewSize = new Size(900, 525);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
        }

        /// <summary>
        /// Download and set image from Bing as wallpaper and get result.
        /// </summary>
        private async Task<DownloadAndSetWallpaperCode> RunAsync(bool setFolder = false)
        {
            return await Model.RunAsync(setFolder);
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
            var resourceLoader = ResourceLoader.GetForCurrentView();
            MainHint.Text = resourceLoader.GetString("Hint/Downloading");

            var code = await RunAsync(true);
            SetHint(code);

            if (!string.IsNullOrWhiteSpace(ImageTodayAddress))
                await UpdateImageToday(ImageTodayAddress);
        }

        private async void OpenBingButton_Click(object sender, RoutedEventArgs e)
        {
            var success = await Windows.System.Launcher
                .LaunchUriAsync(new Uri(@"http://bing.com/"));

            var resourceLoader = ResourceLoader.GetForCurrentView();
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
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            switch (ImageTodayDescription.Visibility)
            {
                case Visibility.Visible:
                    ImageTodayDescription.Visibility = Visibility.Collapsed;
                    localSettings.Values[ImageTodayDescriptionVisibleKey] = false;
                    break;
                case Visibility.Collapsed:
                default:
                    ImageTodayDescription.Visibility = Visibility.Visible;
                    localSettings.Values[ImageTodayDescriptionVisibleKey] = true;
                    break;
            }
        }

        private void ImageTodayTextPanel_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var view = (StackPanel)sender;
            view.Opacity = 0.95;
        }

        private void ImageTodayTextPanel_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var view = (StackPanel)sender;
            view.Opacity = 0.7;
        }

        private async void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            if (isMediaPlaying)
                ResetMediaElement();
            else
                await AutoReadImageTodayAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Listener on set folder button click.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Artuments</param>
        public async void SetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = await Model.SetFolderAsync();
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (folder == null)
            {
                MainHint.Text = resourceLoader.GetString("Hint/SetFolderSpace") + resourceLoader.GetString("Hint/FailedExclamation");
            }
            else
            {
                MainHint.Text = resourceLoader.GetString("Hint/SetFolderSpace") + resourceLoader.GetString("Hint/SuccessedExclamation");
            }
            MainHint.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Listener on open folder button click.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Arguments</param>
        public async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = await Model.GetFolderAsync(true);
            bool success = false;
            if (folder != null)
            {
                success = await Windows.System.Launcher.LaunchFolderAsync(folder);
            }
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (!success)
            {
                MainHint.Text = resourceLoader.GetString("Hint/OpenFolderSpace") + resourceLoader.GetString("Hint/FailedExclamation");
            }
            else
            {
                MainHint.Text = resourceLoader.GetString("Hint/OpenFolderSpace") + resourceLoader.GetString("Hint/SuccessedExclamation");
            }
            MainHint.Visibility = Visibility.Visible;
        }
        private async void Feedback_Click(object sender, RoutedEventArgs e)
        {
            var emailMessage = new Windows.ApplicationModel.Email.EmailMessage();

            var emailRecipient = new Windows.ApplicationModel.Email.EmailRecipient("zhangshiyao_ZSY@outlook.com");
            emailMessage.To.Add(emailRecipient);
            emailMessage.Subject = $"[BWD] [Feedback] [{GetAppVersion()}]";

            await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }

        public static string GetAppVersion()
        {
            var version = Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        private async void Contribute_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("http://github.com/BillShiyaoZhang/BingWallpaperDownload");
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings));
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            ResetMediaElement();
        }

        private void mediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ResetMediaElement();
        }

        private void ResetMediaElement()
        {
            isMediaPlaying = false;
            mediaElement.Stop();
            var resourceLoader = ResourceLoader.GetForCurrentView();
            ReadAloud.Label = resourceLoader.GetString("ReadAloud/Label");
            ReadAloud.Icon = new SymbolIcon(Symbol.Volume);
        }

        private void MainTeachingTip_ActionButtonClick(Microsoft.UI.Xaml.Controls.TeachingTip sender, object args)
        {
            DownloadButton_Click(null, null);
        }
    }
}

