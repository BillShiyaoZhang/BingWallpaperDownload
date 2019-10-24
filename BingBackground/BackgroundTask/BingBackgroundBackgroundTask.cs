using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using Windows.System.UserProfile;
using Windows.Storage;
using System.Net.Http;

namespace BingBackgroundBackgroundTask
{
    public sealed class BingBackgroundBackgroundTask : IBackgroundTask
    {
        const string ImagesSubdirectory = "DownloadedImages";
        BackgroundTaskDeferral _deferral;

        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            await RunFunctionAsync();
            _deferral.Complete();
        }

        async Task RunFunctionAsync()
        {
            //var text = (TextBlock)FindName("Hint");
            try
            {
                string urlBase = GetBackgroundUrlBase();
                var resolutionExtension = GetResolutionExtension(urlBase);
                string address = await DownloadWallpaperAsync(urlBase + resolutionExtension, GetFileName());
                var result = await SetWallpaperAsync(address);
                if (result == true)
                {
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values["lastDate"] = GetDateString();
                }
                if (result)
                {
                    //text.Text = "Wallpaper set successful!";
                }
                else
                {
                    //text.Text = "Wallpaper set failed!";
                }
            }
            //catch (WebException)
            //{
            //    //text.Text = "Find Internet connection problem!";
            //}
            //catch (System.Runtime.InteropServices.COMException)
            //{

            //}

            //catch (Exception)
            //{
            //    text.Text = "Unexpected Exception!";
            //}
            finally
            {
                //text.Visibility = Visibility.Visible;
            }
        }

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

        private static string GetBackgroundUrlBase()
        {
            dynamic jsonObject = DownloadJson();
            return "https://www.bing.com" + jsonObject.images[0].urlbase;
        }

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

        private static string GetResolutionExtension(string url)
        {
            //Rectangle resolution = Screen.PrimaryScreen.Bounds;
            //string widthByHeight = DisplayInformation.GetForCurrentView().ScreenWidthInRawPixels + "x" + DisplayInformation.GetForCurrentView().ScreenHeightInRawPixels;
            //string potentialExtension = "_" + widthByHeight + ".jpg";
            //if (WebsiteExists(url + potentialExtension))
            //{
            //    Console.WriteLine("Background for " + widthByHeight + " found.");
            //    return potentialExtension;
            //}
            //else
            //{
            //    Console.WriteLine("No background for " + widthByHeight + " was found.");
            //    Console.WriteLine("Using 1920x1080 instead.");
            return "_1920x1080.jpg";
            //}
        }

        string GetFileName()
        {
            return GetDateString() + ".bmp";
        }

        string GetDateString()
        {
            return DateTime.Now.ToString("M-d-yyyy");
        }

        async Task<StorageFolder> GetFolderAsync()
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

        async Task<StorageFolder> SetFolderAsync()
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

        async Task<string> DownloadWallpaperAsync(string url, string fileName)
        {
            var rootFolder = await GetFolderAsync();
            StorageFile storageFile;
            string newPath = string.Format("ms-appdata:///local/{0}/{1}", ImagesSubdirectory, fileName);
            var destinationFoler = await ApplicationData.Current.LocalFolder.CreateFolderAsync(ImagesSubdirectory, CreationCollisionOption.OpenIfExists);

            storageFile = (StorageFile)await destinationFoler.TryGetItemAsync(fileName);

            if (storageFile == null || (await storageFile.GetBasicPropertiesAsync()).Size < 10000) // if file size is smaller than 10KB
            {
                try
                {
                    storageFile = await destinationFoler.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                    //storageFile = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                }
                catch (FileLoadException)
                {
                    return newPath;
                    //                storageFile = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                }
                using (HttpClient client = new HttpClient())
                {
                    byte[] buffer = await client.GetByteArrayAsync(url);
                    try
                    {
                        using (Stream stream = await storageFile.OpenStreamForWriteAsync())
                            stream.Write(buffer, 0, buffer.Length);
                    }
                    catch (FileLoadException)
                    {
                        return newPath;
                    }
                }
            }

            // Use this path to load image
            //string newPath = string.Format("ms-appdata:///local/{0}/{1}", ImagesSubdirectory, fileName);
            //var destinationFoler = await ApplicationData.Current.LocalFolder.CreateFolderAsync(ImagesSubdirectory, CreationCollisionOption.OpenIfExists);
            try
            {
                await storageFile.CopyAsync(rootFolder);
            }
            catch (Exception)
            {
                // TODO print file already exist
            }
            return newPath;
        }

        // Pass in a relative path to a file inside the local appdata folder 
        async Task<bool> SetWallpaperAsync(string localAppDataFileName)
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
