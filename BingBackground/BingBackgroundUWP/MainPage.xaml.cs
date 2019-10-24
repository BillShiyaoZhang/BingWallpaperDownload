using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using Windows.System.UserProfile;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Graphics.Display;
using System.Net.Http;
using BingBackgroundBackgroundTask;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel;
using System.Diagnostics;
using Windows.UI.Popups;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BingBackgroundUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string ImagesSubdirectory = "DownloadedImages";

        public MainPage()
        {
            this.InitializeComponent();

            SetStartupTask();

            var taskRegistered = false;
            var exampleTaskName = "BingBackgroundBackgroundTask";

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == exampleTaskName)
                {
                    taskRegistered = true;
                    break;
                }
            }

            if (!taskRegistered)
            {
                var builder = new BackgroundTaskBuilder();

                builder.Name = exampleTaskName;
                builder.TaskEntryPoint = "BingBackgroundBackgroundTask.BingBackgroundBackgroundTask";
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.UserPresent, false));
                //builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                builder.IsNetworkRequested = true;
                BackgroundTaskRegistration task = builder.Register();
            }



            // Check if need to download today's wallpaper
            // if have done today    
            // if file exist
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var lastDate = (string)localSettings.Values["lastDate"];
            if (lastDate != GetDateString())
            {
                RunFunctionAsync();
            }
            else
            {
                var text = (TextBlock)FindName("Hint");
                text.Text = "The image has already been there!";
                text.Visibility = Visibility.Visible;
            }
        }

        async void SetStartupTask()
        {
            StartupTask startupTask = await StartupTask.GetAsync("MyStartupId"); // Pass the task ID you specified in the appxmanifest file
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    // Task is disabled but can be enabled.
                    StartupTaskState newState = await startupTask.RequestEnableAsync(); // ensure that you are on a UI thread when you call RequestEnableAsync()
                    Debug.WriteLine("Request to enable startup, result = {0}", newState);
                    break;
                case StartupTaskState.DisabledByUser:
                    // Task is disabled and user must enable it manually.
                    MessageDialog dialog = new MessageDialog(
                        "You have disabled this app's ability to run " +
                        "as soon as you sign in, but if you change your mind, " +
                        "you can enable this in the Startup tab in Task Manager.",
                        "TestStartup");
                    await dialog.ShowAsync();
                    break;
                case StartupTaskState.DisabledByPolicy:
                    Debug.WriteLine("Startup disabled by group policy, or not supported on this device");
                    break;
                case StartupTaskState.Enabled:
                    Debug.WriteLine("Startup is enabled.");
                    break;
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            RunFunctionAsync();
        }

        private async void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = await GetFolderAsync();
            await Windows.System.Launcher.LaunchFolderAsync(folder);
        }

        private async void SetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            _ = await SetFolderAsync();
        }

        async void RunFunctionAsync()
        {
            var text = (TextBlock)FindName("Hint");
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
                    text.Text = "Wallpaper set successful!";
                }
                else
                {
                    text.Text = "Wallpaper set failed!";
                }
            }
            catch (WebException)
            {
                text.Text = "Find Internet connection problem!";
            }
            //catch (Exception)
            //{
            //    text.Text = "Unexpected Exception!";
            //}
            finally
            {
                text.Visibility = Visibility.Visible;
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

