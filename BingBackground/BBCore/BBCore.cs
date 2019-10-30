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
    public enum RunFunctionCode
    {
        SUCCESSFUL, FAILED, NO_INTERNET, UNEXPECTED_EXCEPTION
    }

    public class BBCore
    {
        private bool isResolutionExtensionSet;

        private string resolutionExtension;

        public BBCore()
        {
            isResolutionExtensionSet = false;
        }

        public BBCore(string resolutionExtension)
        {
            this.resolutionExtension = resolutionExtension;
            isResolutionExtensionSet = true;
        }

        public async Task<RunFunctionCode> RunFunctionAsync(string ImagesSubdirectory)
        {
            RunFunctionCode value = RunFunctionCode.UNEXPECTED_EXCEPTION;
            try
            {
                string urlBase = GetBackgroundUrlBase();
                string resolutionExtension;
                if (!isResolutionExtensionSet)
                {
                    resolutionExtension = GetResolutionExtension(urlBase);
                }
                else
                {
                    resolutionExtension = this.resolutionExtension;
                }
                string address = await DownloadWallpaperAsync(urlBase + resolutionExtension, GetFileName(), ImagesSubdirectory);
                var result = await SetWallpaperAsync(address);
                if (result == true)
                {
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values["lastDate"] = GetDateString();
                }
                if (result)
                {
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

        static dynamic DownloadJson()
        {
            using (WebClient webClient = new WebClient())
            {
                Console.WriteLine("Downloading JSON...");
                webClient.Encoding = System.Text.Encoding.UTF8;
                string jsonString = webClient.DownloadString("https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-UK");
                return JsonConvert.DeserializeObject<dynamic>(jsonString);
            }
        }

        static string GetBackgroundUrlBase()
        {
            dynamic jsonObject = DownloadJson();
            return "https://www.bing.com" + jsonObject.images[0].urlbase;
        }

        static bool WebsiteExists(string url)
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

        static string GetResolutionExtension(string url)
        {
            //Rectangle resolution = Screen.PrimaryScreen.Bounds;
            string widthByHeight = DisplayInformation.GetForCurrentView().ScreenWidthInRawPixels + "x" + DisplayInformation.GetForCurrentView().ScreenHeightInRawPixels;
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
                return "_1920x1080.jpg";
            }
        }

        string GetFileName()
        {
            return GetDateString() + ".bmp";
        }

        public string GetDateString()
        {
            return DateTime.Now.ToString("M-d-yyyy");
        }

        public async Task<StorageFolder> GetFolderAsync()
        {
            StorageFolder folder;
            try
            {
                folder = await Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.GetFolderAsync("PickedFolderToken");
            }
            catch (ArgumentException)
            {
                folder = await SetFolderAsync();
            }
            return folder;
        }

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
                FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                //this.textBlock.Text = "Picked folder: " + folder.Name;
            }
            else
            {
                //this.textBlock.Text = "Operation cancelled.";
            }
            return folder;
        }

        public async Task<string> DownloadWallpaperAsync(string url, string fileName, string ImagesSubdirectory)
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

        // Pass in a relative path to a file inside the local appdata folder 
        public async Task<bool> SetWallpaperAsync(string localAppDataFileName)
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

    }
}
