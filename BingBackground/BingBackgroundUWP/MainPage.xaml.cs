using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Media.Imaging;
using Windows.System.UserProfile;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.Graphics.Display;
using System.Net.Http;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BingBackgroundUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            RunFunction();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RunFunction();
        }

        async void RunFunction()
        {
            string urlBase = GetBackgroundUrlBase();
            var resolutionExtension = GetResolutionExtension(urlBase);
            string address = await DownloadWallpaperAsync(urlBase + resolutionExtension, GetFileName());
            var result = await SetWallpaperAsync(address);
            var text = (TextBlock)FindName("Hint");
            text.Text = result.ToString();
            text.Visibility = Visibility.Visible;
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
            return DateTime.Now.ToString("M-d-yyyy") + ".bmp";
        }

        async Task<string> DownloadWallpaperAsync(string url, string fileName)
        {
            const string imagesSubdirectory = "DownloadedImages";
            var rootFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(imagesSubdirectory, CreationCollisionOption.OpenIfExists);

            var storageFile = await rootFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            using (HttpClient client = new HttpClient())
            {
                byte[] buffer = await client.GetByteArrayAsync(url);
                using (Stream stream = await storageFile.OpenStreamForWriteAsync())
                    stream.Write(buffer, 0, buffer.Length);
            }

            // Use this path to load image
            string newPath = string.Format("ms-appdata:///local/{0}/{1}", imagesSubdirectory, fileName);
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

