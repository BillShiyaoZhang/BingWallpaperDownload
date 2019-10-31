using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using Windows.System.UserProfile;
using Windows.Storage;
using Windows.Graphics.Display;
using System.Net.Http;

namespace BBCore
{
    /// <summary>
    /// State of function RunFunctionAsync may return.
    /// </summary>
    public enum RunFunctionCode
    {
        SUCCESSFUL, FAILED, NO_INTERNET, UNEXPECTED_EXCEPTION
    }

    /// <summary>
    /// The core of Bing background app.  It would download and set images from Bing to desktop as wallpapers.
    /// </summary>
    public class Core
    {
        #region Properties

        /// <summary>
        /// The key of last date stored in local settings.
        /// </summary>
        public static string LastDateKey { get { return "lastDate"; } }

        /// <summary>
        /// The default resolution extension for downloading image.
        /// </summary>
        public static string DefaultResolutionExtension { get { return "_1920x1080.jpg"; } }
        
        #endregion

        #region Fileds

        /// <summary>
        /// Flag on if or not the resolutionExtension is set.  Use resolutionExtension if yes.
        /// </summary>
        private bool isResolutionExtensionSet;

        /// <summary>
        /// The resolution extension of the image, in the format of "_1920x1080.jpg".
        /// </summary>
        private string resolutionExtension;

        /// <summary>
        /// The token to pick folder which stores images.
        /// </summary>
        private const string PICK_FOLDER_TOKEN = "PickedFolderToken";

        /// <summary>
        /// The defualt subdirectory images stored in local.
        /// </summary>
        private const string DEFAULT_IMAGES_SUBDIRECTORY = "DownloadedImages";
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor with default isResolutionExtensionSet = false;
        /// </summary>
        public Core()
        {
            isResolutionExtensionSet = false;
        }

        /// <summary>
        /// Constructor with resolutionExtension and isResolutionExtensionSet = true;
        /// </summary>
        /// <param name="resolutionExtension"></param>
        public Core(string resolutionExtension)
        {
            this.resolutionExtension = resolutionExtension;
            isResolutionExtensionSet = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Download and set images from Bing as wallpapers.
        /// </summary>
        /// <param name="imagesSubdirectory">Subdirectory of images.  Default as "DownloadedImages".</param>
        /// <returns>RunFunctionCode represents the result of running.</returns>
        public async Task<RunFunctionCode> RunAsync(string imagesSubdirectory = DEFAULT_IMAGES_SUBDIRECTORY)
        {
            RunFunctionCode value;
            try
            {
                string urlBase = GetBackgroundUrlBase();
                if (!isResolutionExtensionSet)
                {
                    resolutionExtension = GetResolutionExtension(urlBase);
                }
                string address = await DownloadWallpaperAsync(urlBase + resolutionExtension, GetFileName(), imagesSubdirectory);
                var result = await SetWallpaperAsync(address);
                if (result)
                {
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values[LastDateKey] = GetDateString();
                    value = RunFunctionCode.SUCCESSFUL; // Wallpaper set successful!
                }
                else
                {
                    value = RunFunctionCode.FAILED; // Wallpaper set failed!
                }
            }
            catch (WebException)
            {
                value = RunFunctionCode.NO_INTERNET;    // Find Internet connection problem!";
            }
            catch (Exception)
            {
                value = RunFunctionCode.UNEXPECTED_EXCEPTION;   // Unexpected Exception!
            }

            return value;
        }

        /// <summary>
        /// Get string of today's DateTime
        /// </summary>
        /// <returns>DateTime of today</returns>
        public string GetDateString()
        {
            return DateTime.Now.ToString("M-d-yyyy");
        }

        /// <summary>
        /// Get the folder which user wants to sotre images.
        /// </summary>
        /// <returns>The folder which user wants to store images.</returns>
        public async Task<StorageFolder> GetFolderAsync()
        {
            StorageFolder folder;
            try
            {
                folder = await Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.GetFolderAsync(PICK_FOLDER_TOKEN);
            }
            catch (ArgumentException)
            {
                folder = await SetFolderAsync();
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
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace(PICK_FOLDER_TOKEN, folder);
                //this.textBlock.Text = "Picked folder: " + folder.Name;
            }
            else
            {
                //this.textBlock.Text = "Operation cancelled.";
            }
            return folder;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Download, from bing, JSON file which includes information of image address.
        /// </summary>
        /// <returns>Deserialized JSON file</returns>
        private static dynamic DownloadJson()
        {
            using (WebClient webClient = new WebClient())
            {
                Console.WriteLine("Downloading JSON...");
                webClient.Encoding = System.Text.Encoding.UTF8;
                string jsonString = webClient.DownloadString("https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-UK");
                return JsonConvert.DeserializeObject<dynamic>(jsonString);
            }
        }

        /// <summary>
        /// Get URL base of the image without resolution extension
        /// </summary>
        /// <returns>the URL base</returns>
        private static string GetBackgroundUrlBase()
        {
            dynamic jsonObject = DownloadJson();
            return "https://www.bing.com" + jsonObject.images[0].urlbase;
        }

        /// <summary>
        /// Test if the website with given URL exists.
        /// </summary>
        /// <param name="url">The URL of the website</param>
        /// <returns>If the website exists.</returns>
        private static bool WebsiteExists(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Method = "HEAD";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
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
        private static string GetResolutionExtension(string url)
        {
            //Rectangle resolution = Screen.PrimaryScreen.Bounds;
            string widthByHeight = DisplayInformation.GetForCurrentView().ScreenWidthInRawPixels
                + "x" + DisplayInformation.GetForCurrentView().ScreenHeightInRawPixels;
            string potentialExtension = "_" + widthByHeight + ".jpg";
            if (WebsiteExists(url + potentialExtension))
            {
                Console.WriteLine("Background for " + widthByHeight + " found.");
                return potentialExtension;
            }
            else
            {
                Console.WriteLine("No background for " + widthByHeight + " was found.");
                Console.WriteLine("Using 1920x1080 instead.");
                return DefaultResolutionExtension;
            }
        }

        /// <summary>
        /// Get the name of file it should be today.
        /// </summary>
        /// <returns>The name of file.</returns>
        private string GetFileName()
        {
            return GetDateString() + ".bmp";
        }

        /// <summary>
        /// Download the image from a given URL and store it with a specified file name under certain subdirectory.
        /// </summary>
        /// <param name="url">URL of the image.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="ImagesSubdirectory">Subdirectory location to store files.</param>
        /// <returns>Path of the image file.</returns>
        private async Task<string> DownloadWallpaperAsync(string url, string fileName, string ImagesSubdirectory)
        {
            var rootFolder = await GetFolderAsync();
            StorageFile storageFile;
            try
            {
                storageFile = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception)
            {
                storageFile = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            }

            using (HttpClient client = new HttpClient())
            {
                byte[] buffer = await client.GetByteArrayAsync(url);
                using (Stream stream = await storageFile.OpenStreamForWriteAsync())
                    stream.Write(buffer, 0, buffer.Length);
            }

            // Use this path to load image
            string newPath = string.Format("ms-appdata:///local/{0}/{1}", ImagesSubdirectory, fileName);
            var file = await ApplicationData.Current.LocalFolder.CreateFolderAsync(ImagesSubdirectory, CreationCollisionOption.OpenIfExists);
            try
            {
                await storageFile.CopyAsync(file);
            }
            catch (Exception)
            {
                // TODO print file already exist
            }
            return newPath;
        }

        /// <summary>
        /// Set the image from given URI as wallpaper.
        /// </summary>
        /// <param name="localAppDataFileName">URI of the image.</param>
        /// <returns>If or not wallpaper is set successed.</returns>
        private async Task<bool> SetWallpaperAsync(string localAppDataFileName)
        {
            bool success = false;
            if (UserProfilePersonalizationSettings.IsSupported())
            {
                var uri = new Uri(localAppDataFileName);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                success = await profileSettings.TrySetWallpaperImageAsync(file);
            }
            return success;
        }

        #endregion
    }
}
