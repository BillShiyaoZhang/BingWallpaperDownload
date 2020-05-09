using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using BBCore;

namespace BBUWP
{
    /// <summary>
    /// The main page includes three buttons and one text block.
    /// Three buttons are: Set Folder, Download, and Open Folder.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Private Constant

        /// <summary>
        /// Name of background task detecting if user is presenting.
        /// </summary>
        private const string UserPresentBTName = "BBBTUserPresent";

        /// <summary>
        /// Name of background task with timer.
        /// </summary>
        private const string TimeBTName = "BBBTTimer";

        /// <summary>
        /// Entry point of background tasks.
        /// </summary>
        private const string BTEntryPoint = "BBBackgroundTask.BackgroundTask";

        /// <summary>
        /// ID of startup task.
        /// </summary>
        private const string StartupTaskID = "BingBackgroundStartupId";

        /// <summary>
        /// ID of text block in the frame.
        /// </summary>
        private const string TextID = "Hint";

        #endregion

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
            ApplicationView.PreferredLaunchViewSize = new Size(300, 200);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            //ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            //var lastDate = (string)localSettings.Values[Core.LastDateKey];
            var text = (TextBlock)FindName(TextID);
            if (!Core.IsUpdated)
            {
                RunAsync();
            }
            else
            {
                text.Text = "The image has already been there!";
            }
            text.Visibility = Visibility.Visible;

            SetStartupTask();
            SetBackgroundTasks();
        }

        #region Public Button Listeners

        /// <summary>
        /// Listener on download button click.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Artuments</param>
        public void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            RunAsync();
        }

        /// <summary>
        /// Listener on open folder button click.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Arguments</param>
        public async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = await Core.GetFolderAsync();
            var success = await Windows.System.Launcher.LaunchFolderAsync(folder);
            var text = (TextBlock)FindName(TextID);
            if (!success)
            {
                text.Text = "Open folder failed";
            }
            else
            {
                text.Text = "Open folder successed!";
            }
        }

        /// <summary>
        /// Listener on set folder button click.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Artuments</param>
        public async void SetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = await Core.SetFolderAsync();
            var text = (TextBlock)FindName(TextID);
            if (folder == null)
            {
                text.Text = "Set folder failed!";
            }
            else
            {
                text.Text = "Set folder successed!";
            }
        }

        private async void OpenBingButton_Click(object sender, RoutedEventArgs e)
        {
            var success = await Windows.System.Launcher.LaunchUriAsync(new Uri(@"https://www.bing.co.uk"));
            var text = (TextBlock)FindName(TextID);
            if (success)
            {
                text.Text = "Browser lanuched successed!";
            }
            else
            {
                text.Text = "Browser lanuched failed!";
            }
        }

        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Set startup task and request permission if necessary.
        /// </summary>
        private async void SetStartupTask()
        {
            StartupTask startupTask = await StartupTask.GetAsync(StartupTaskID); // Pass the task ID you specified in the appxmanifest file
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
                        StartupTaskID);
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

        /// <summary>
        /// Set background tasks.
        /// </summary>
        private void SetBackgroundTasks()
        {

            SetBackgroundTask(UserPresentBTName, BTEntryPoint,
                new SystemTrigger(SystemTriggerType.UserPresent, false));

            // TODO The time trigger should detect a proper time interval for next day, and register the next trigger.
            //var currentMins = DateTime.Now.Hour * 60 + DateTime.Now.Minute; // current time in mins
            //var restMins = 1440 - currentMins;  // rest mins in a day. 24 * 60 = 1440 mins a day
            //var triggerMins = restMins - restMins % 15 + 15;    // trigger should be set at the beginning of the next day as 15 * n mins
            SetBackgroundTask(TimeBTName, BTEntryPoint, new TimeTrigger(90, false));

        }

        /// <summary>
        /// Set a background task.
        /// </summary>
        /// <param name="taskName">Name of the task</param>
        /// <param name="taskEntryPoint">Entry point of the task</param>
        /// <param name="trigger">Trigger of the task</param>
        /// <returns>Successfully registered task or null</returns>
        private IBackgroundTaskRegistration SetBackgroundTask(string taskName, string taskEntryPoint, IBackgroundTrigger trigger)
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
                builder.AddCondition(new SystemCondition(SystemConditionType.FreeNetworkAvailable));
                builder.IsNetworkRequested = true;
                var task = builder.Register();
                return task;
            }
            return null;
        }

        /// <summary>
        /// Download and set image from Bing as wallpaper and get result.
        /// </summary>
        private async void RunAsync()
        {
            var code = await Core.RunAsync();
            var text = (TextBlock)FindName(TextID);
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


        #endregion

    }
}

