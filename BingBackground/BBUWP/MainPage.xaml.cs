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
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel;
using System.Diagnostics;
using Windows.UI.Popups;
using BBCore;
using Windows.UI.ViewManagement;
using Windows.Foundation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BBUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string ImagesSubdirectory = "DownloadedImages";
        const string UserPresentBTName = "BBBTUserPresent";
        const string TimeBTName = "BBBTTimer";
        const string BTEntryPoint = "BBBackgroundTask.BBBackgroundTask";

        BBCore.BBCore core;

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(300, 200);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            if (core == null)
            {
                core = new BBCore.BBCore();
            }

            SetStartupTask();
            SetBackgroundTasks();

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var lastDate = (string)localSettings.Values["lastDate"];
            var text = (TextBlock)FindName("Hint");
            if (lastDate != core.GetDateString())
            {
                RunFunction();
            }
            else
            {
                text.Text = "The image has already been there!";
            }
            text.Visibility = Visibility.Visible;
        }

        public async void SetStartupTask()
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

        public void SetBackgroundTasks()
        {

            SetBackgroundTask(UserPresentBTName, BTEntryPoint,
                new SystemTrigger(SystemTriggerType.UserPresent, false));

            // TODO The time trigger should detect a proper time interval for next day, and register the next trigger.
            SetBackgroundTask(TimeBTName, BTEntryPoint, new TimeTrigger(90, false));
        }

        public IBackgroundTaskRegistration SetBackgroundTask(string taskName, string taskEntryPoint, IBackgroundTrigger trigger)
        {
            var taskRegistered = false;

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    taskRegistered = true;
                    return task.Value;
                }
            }

            if (!taskRegistered)
            {
                var builder = new BackgroundTaskBuilder();

                builder.Name = taskName;
                builder.TaskEntryPoint = taskEntryPoint;
                builder.SetTrigger(trigger);
                //builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                builder.IsNetworkRequested = true;
                BackgroundTaskRegistration task = builder.Register();
                return task;
            }
            return null;
        }


        public void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            RunFunction();
        }

        public async void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            //var folder = await GetFolderAsync();
            if (core == null)
            {
                core = new BBCore.BBCore();
            }
            var folder = await core.GetFolderAsync();
            var success = await Windows.System.Launcher.LaunchFolderAsync(folder);
            if (!success)
            {
                var text = (TextBlock)FindName("Hint");
                text.Text = "Open folder failed";
            }
        }

        public async void SetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (core == null)
            {
                core = new BBCore.BBCore();
            }
            var folder = await core.SetFolderAsync();
            var text = (TextBlock)FindName("Hint");
            if (folder == null)
            {
                text.Text = "Set folder failed!";
            }
            else
            {
                text.Text = "Set folder successed!";
            }
        }

        async void RunFunction()
        {
            var code = await core.RunFunctionAsync(ImagesSubdirectory);
            var text = (TextBlock)FindName("Hint");
            string msg = "";
            switch (code)
            {
                case RunFunctionCode.SUCCESSFUL:
                    msg = "Wallpaper set successful!";
                    break;
                case RunFunctionCode.FAILED:
                    msg = "Wallpaper set failed!";
                    break;
                case RunFunctionCode.NO_INTERNET:
                    msg = "Find Internet connection problem!";
                    break;
                case RunFunctionCode.UNEXPECTED_EXCEPTION:
                    msg = "Unexpected Exception!";
                    break;
                default:
                    break;
            }
            text.Text = msg;
        }
    }
}

