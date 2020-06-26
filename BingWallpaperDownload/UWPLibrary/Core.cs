using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.System.UserProfile;
using Windows.Storage;
using Windows.Graphics.Display;
using System.Diagnostics;

namespace UWPLibrary
{

    /// <summary>
    /// The core of Bing background app.  It would download and set images from Bing to desktop as wallpapers.
    /// </summary>
    public class Core
    {
        #region Properties

        private static string GetImageUrl(string code)
        {
            return "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=" + code;
        }

        public static string EnGbGeographicRegion { get { return "en-GB"; } }

        public static string GeographicRegion
        {
            get
            {
                return GlobalizationPreferences.Languages[0];
            }
        }

        public static string AutoReadKey { get { return "AutoReadKey"; } }

        /// <summary>
        /// The key of last date stored in local settings.
        /// </summary>
        public static string LastDateKey { get { return "lastDate"; } }

        public static string ImageTodayTitleKey { get { return "imageTodayTitle"; } }

        public static string ImageTodayDescriptionKey { get { return "imageTodayDescription"; } }

        public static string ImageTodayLearnMoreHrefKey { get { return "learnMoreHref"; } }

        /// <summary>
        /// The default resolution extension for downloading image.
        /// </summary>
        public static string DefaultResolutionExtension { get { return "_" + DefaultWidthByHeight + ".jpg"; } }

        /// <summary>
        /// Get string of today's DateTime
        /// </summary>
        public static string DefaultDateString { get { return DateTime.Now.ToString("M-d-yyyy"); } }

        /// <summary>
        /// Flag on if or not the resolutionExtension is set.  Use resolutionExtension if yes.
        /// </summary>
        private bool IsResolutionExtensionSet { get; set; }

        /// <summary>
        /// The resolution extension of the image, in the format of "_1920x1080.jpg".
        /// </summary>
        private string ResolutionExtension { get; set; }

        /// <summary>
        /// The token to pick folder which stores images.
        /// </summary>
        private string PickFolderToken { get { return "PickedFolderToken"; } }
        private static string DefaultWidthByHeight { get { return "1920x1080"; } }

        private string WidthByHeight
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_widthByHeight))
                {
                    _widthByHeight = DefaultWidthByHeight;
                }
                return _widthByHeight;
            }
        }

        /// <summary>
        /// Get the relevant path of file it should be today.
        /// </summary>
        private string DefaultFilePath { get { return DateTime.Now.Year.ToString() + "\\" + DefaultFileName; } }

        /// <summary>
        /// Get the name of file it should be today.
        /// </summary>
        private string DefaultFileName { get { return DefaultDateString + ".bmp"; } }

        public static string LastDate
        {
            get
            {
                return (string)ApplicationData.Current.
                    LocalSettings.Values[Core.LastDateKey];
            }
        }

        public bool IsUpdated
        {
            get
            {
                if (LastDate != DefaultDateString)
                {
                    return false;
                }
                return true;
            }
        }

        #endregion

        /// <summary>
        /// The defualt subdirectory images stored in local.
        /// </summary>
        private const string DEFAULT_IMAGES_SUBDIRECTORY = "DownloadedImages";

        private string _widthByHeight;

        public static T GetLocalSettingsOrDefault<T>(string key, T defaultValue)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var result = localSettings.Values[key];
            if (result == null)
            {
                result = (T)defaultValue;
                localSettings.Values[key] = result;
            }
            return (T)result;
        }

        #region Constructors

        /// <summary>
        /// Constructor with default isResolutionExtensionSet = false;
        /// </summary>
        public Core()
        {
            IsResolutionExtensionSet = false;
        }

        /// <summary>
        /// Constructor with resolutionExtension and isResolutionExtensionSet = true;
        /// </summary>
        /// <param name="resolutionExtension"></param>
        public Core(string resolutionExtension)
        {
            this.ResolutionExtension = resolutionExtension;
            IsResolutionExtensionSet = true;
        }

        #endregion

        #region Public Methods

        public static string ImageAddressPrefix
        {
            get
            {
                return $"ms-appdata:///local/{DEFAULT_IMAGES_SUBDIRECTORY}";
            }
        }

        private string ImageAddress
        {
            get
            {
                return $"{ImageAddressPrefix}/{DefaultFileName}";
            }
        }

        public string RootFolderName { get { return "BWD images"; } }

        /// <summary>
        /// Download and set images from Bing as wallpapers.
        /// </summary>
        /// <param name="imagesSubdirectory">Subdirectory of images.  Default as "DownloadedImages".</param>
        /// <returns>RunFunctionCode represents the result of running.</returns>
        public async Task<DownloadAndSetWallpaperCode> RunAsync(bool setFolder, string imagesSubdirectory = DEFAULT_IMAGES_SUBDIRECTORY)
        {
            DownloadLearnMoreInformation();

            DownloadAndSetWallpaperCode value;

            if (!IsResolutionExtensionSet)
            {
                SetWidthByHeight(); // Set
            }

            // exit UI context
            var folder = await GetFolderAsync(setFolder).ConfigureAwait(false);
            if (folder == null)
            {
                value = DownloadAndSetWallpaperCode.FOLDER_NOT_SET;
                return value;
            }

            try
            {
                string urlBase = await GetBackgroundUrlBaseAsync().ConfigureAwait(false);
                if (!IsResolutionExtensionSet)
                {
                    ResolutionExtension
                        = await GetResolutionExtensionAsync(urlBase).ConfigureAwait(false);
                }
                await DownloadWallpaperAsync(urlBase + ResolutionExtension,
                    folder, DefaultFilePath, imagesSubdirectory).ConfigureAwait(false);
                var wallpaperResult = await SetWallpaperAsync(ImageAddress).ConfigureAwait(false);
                if (wallpaperResult)
                {
                    ApplicationDataContainer localSettings
                        = ApplicationData.Current.LocalSettings;
                    localSettings.Values[LastDateKey] = DefaultDateString;
                    value = DownloadAndSetWallpaperCode.SUCCESSFUL; // Wallpaper set successful!
                }
                else
                {
                    value = DownloadAndSetWallpaperCode.FAILED; // Wallpaper set failed!
                }
            }
            catch (WebException)
            {
                value = DownloadAndSetWallpaperCode.NO_INTERNET;    // Find Internet connection problem!";
            }
            //catch (Exception)
            //{
            //    value = RunFunctionCode.UNEXPECTED_EXCEPTION;   // Unexpected Exception!
            //}

            return value;
        }

        public static async void DownloadLearnMoreInformation()
        {

            try
            {
                await DownloadLearnMoreInformationWithCountryCode(
                    GlobalizationPreferences.HomeGeographicRegion);
            }
            catch (ArgumentNullException)
            {
                try
                {
                    await DownloadLearnMoreInformationWithCountryCode("gb");

                }
                catch (Exception) { }
            }
        }

        private static async Task DownloadLearnMoreInformationWithCountryCode(string countryCode)
        {
            ApplicationDataContainer localSettings
                = ApplicationData.Current.LocalSettings;
            string learnMoreHref = new HtmlWeb().Load(@"https://www.bing.com/?cc=" + countryCode)
                   .DocumentNode
                   .SelectNodes("//a[@class='learn_more']")
                   .FirstOrDefault()
                   .Attributes["href"]
                   .Value;
            learnMoreHref = learnMoreHref
                .Replace("&amp;", "&")
                .Replace("&quot;", "\"");
            if (string.IsNullOrWhiteSpace(learnMoreHref))
                throw new ArgumentNullException();
            localSettings.Values[ImageTodayLearnMoreHrefKey] = learnMoreHref;

            var uri = new Uri("https://www.bing.com" + learnMoreHref);
            using (var httpClient = new Windows.Web.Http.HttpClient())
            {
                string result = await httpClient.GetStringAsync(uri);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(result);

                string imageTitle = htmlDoc.DocumentNode
                    .SelectNodes("//h2[@class=' ency_imgTitle']")
                    .FirstOrDefault()
                    .InnerText;
                if (string.IsNullOrWhiteSpace(imageTitle))
                    throw new ArgumentNullException();
                localSettings.Values[ImageTodayTitleKey] = imageTitle;

                string imageDescription = htmlDoc.DocumentNode
                    .SelectNodes("//div[@class='ency_desc']")
                    .FirstOrDefault()
                    .InnerText;
                if (string.IsNullOrWhiteSpace(imageDescription))
                    throw new ArgumentNullException();
                localSettings.Values[ImageTodayDescriptionKey] = imageDescription;
            }
        }

        /// <summary>
        /// Get the folder which user wants to sotre images.
        /// </summary>
        /// <returns>The folder which user wants to store images.</returns>
        public async Task<StorageFolder> GetFolderAsync(bool setFolder)
        {
            StorageFolder folder;
            try
            {
                folder = await Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.GetFolderAsync(PickFolderToken);
            }
            catch (ArgumentException)
            {
                if (setFolder)
                {
                    folder = await SetFolderAsync().ConfigureAwait(false);
                }
                else
                {
                    folder = null;
                }
            }
            return folder;
        }

        /// <summary>
        /// Set the folder to store images with FolderPicker.
        /// </summary>
        /// <returns>The folder user choose.</returns>
        public async Task<StorageFolder> SetFolderAsync()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                if (folder.Name != RootFolderName)
                {
                    var isEmpty = await IsFolderEmpty(folder).ConfigureAwait(false);
                    if (!isEmpty)
                        folder = await folder.CreateFolderAsync(RootFolderName, CreationCollisionOption.OpenIfExists);
                }
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.AddOrReplace(PickFolderToken, folder);
            }

            return folder;
        }

        private async Task<bool> IsFolderEmpty(StorageFolder folder)
        {
            var items = await folder.GetItemsAsync(0, 1);
            return items.Count == 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Download, from bing, JSON file which includes information of image address.
        /// </summary>
        /// <returns>Deserialized JSON file</returns>
        private async Task<dynamic> DownloadJsonAsync(string geographicRegionCode)
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri(GetImageUrl(geographicRegionCode));
                var jsonString = await client.GetStringAsync(uri);
                return JsonConvert.DeserializeObject<dynamic>(jsonString);
            }
        }

        /// <summary>
        /// Get URL base of the image without resolution extension
        /// </summary>
        /// <returns>the URL base</returns>
        private async Task<string> GetBackgroundUrlBaseAsync()
        {
            dynamic jsonObject = await DownloadJsonAsync(GeographicRegion).ConfigureAwait(false);
            if (jsonObject == null ||
                string.IsNullOrWhiteSpace((string)jsonObject.images[0].urlbase))
            {
                jsonObject = await DownloadJsonAsync(EnGbGeographicRegion).ConfigureAwait(false);
            }
            return "https://www.bing.com" + jsonObject.images[0].urlbase;
        }

        /// <summary>
        /// Test if the website with given URL exists.
        /// </summary>
        /// <param name="url">The URL of the website</param>
        /// <returns>If the website exists.</returns>
        private async Task<bool> WebsiteExistsAsync(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Method = "HEAD";

                HttpWebResponse response
                    = (HttpWebResponse)await request.GetResponseAsync()
                    .ConfigureAwait(false);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get the resolution extension, given URL, of images.
        /// </summary>
        /// <param name="url">URL of images.</param>
        /// <returns>The resolution extension.</returns>
        private async Task<string> GetResolutionExtensionAsync(string url)
        {
            string potentialExtension = "_" + WidthByHeight + ".jpg";
            if (await WebsiteExistsAsync(url + potentialExtension).ConfigureAwait(false))
                return potentialExtension;
            return DefaultResolutionExtension;
        }

        /// <summary>
        /// Download the image from a given URL and store it with a specified file name under certain subdirectory.
        /// </summary>
        /// <param name="url">URL of the image.</param>
        /// <param name="filePath">The reletive path of the file.</param>
        /// <param name="ImagesSubdirectory">Subdirectory location to store files.</param>
        /// <returns>Path of the image file.</returns>
        private async Task DownloadWallpaperAsync(string url, StorageFolder rootFolder, string filePath, string ImagesSubdirectory)
        {
            StorageFile storageFile;
            try
            {
                storageFile = await rootFolder
                    .CreateFileAsync(filePath, CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception)
            {
                storageFile = await rootFolder
                    .CreateFileAsync(filePath, CreationCollisionOption.OpenIfExists);
            }

            using (HttpClient client = new HttpClient())
            {
                byte[] buffer = await client.GetByteArrayAsync(url);
                using (Stream stream = await storageFile.OpenStreamForWriteAsync())
                    stream.Write(buffer, 0, buffer.Length);
            }

            // Use this path to load image
            var localFolder = await ApplicationData.Current.LocalFolder
                .CreateFolderAsync(ImagesSubdirectory, CreationCollisionOption.OpenIfExists);
            //string newPath = string.Format("ms-appdata:///local/{0}", GetFileName());
            //var localFolder = ApplicationData.Current.LocalFolder;
            await storageFile
                .CopyAsync(localFolder, DefaultFileName, NameCollisionOption.ReplaceExisting);
        }

        /// <summary>
        /// Set the image from given URI as wallpaper.
        /// </summary>
        /// <param name="localAppDataFileName">URI of the image.</param>
        /// <returns>If or not wallpaper is set successed.</returns>
        private async Task<bool> SetWallpaperAsync(string localAppDataFileName)
        {
            if (UserProfilePersonalizationSettings.IsSupported())
            {
                var uri = new Uri(localAppDataFileName);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                UserProfilePersonalizationSettings profileSettings
                    = UserProfilePersonalizationSettings.Current;
                return await profileSettings.TrySetWallpaperImageAsync(file);
            }
            return false;
        }

        private void SetWidthByHeight()
        {
            _widthByHeight = DisplayInformation.GetForCurrentView().ScreenWidthInRawPixels
                + "x" + DisplayInformation.GetForCurrentView().ScreenHeightInRawPixels;
        }

        #endregion
    }
}
