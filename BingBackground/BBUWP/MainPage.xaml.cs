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
using System.Collections.Generic;
using System.Threading.Tasks;

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

            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["launchedWithPrefSize"] == null)
            {
                // first app launch only!!
                ApplicationView.PreferredLaunchViewSize = new Size(510, 320);
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(510, 320));
                localSettings.Values["launchedWithPrefSize"] = true;
            }
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            var text = (TextBlock)FindName(TextID);
            if (!Core.IsUpdated)
            {
                RunAsync(false);
            }
            else
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                text.Text = resourceLoader.GetString("Hint/ImageThere");
            }
            text.Visibility = Visibility.Visible;

            SetStartupTaskToggleSwitch();
            SetBackgroundTasksToggleSwitch();
        }

        #region Public Button Listeners

        /// <summary>
        /// Listener on download button click.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Artuments</param>
        public void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            RunAsync(true);
        }

        /// <summary>
        /// Listener on open folder button click.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">Arguments</param>
        public async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = await Core.GetFolderAsync(true);
            bool success = false;
            if (folder != null)
            {
                success = await Windows.System.Launcher.LaunchFolderAsync(folder);
            }
            var text = (TextBlock)FindName(TextID);
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (!success)
            {
                text.Text = resourceLoader.GetString("Hint/OpenFolderSpace") + resourceLoader.GetString("Hint/FailedExclamation");
            }
            else
            {
                text.Text = resourceLoader.GetString("Hint/OpenFolderSpace") + resourceLoader.GetString("Hint/SuccessedExclamation");
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
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            var text = (TextBlock)FindName(TextID);
            if (folder == null)
            {
                text.Text = resourceLoader.GetString("Hint/SetFolderSpace") + resourceLoader.GetString("Hint/FailedExclamation");
            }
            else
            {
                text.Text = resourceLoader.GetString("Hint/SetFolderSpace") + resourceLoader.GetString("Hint/SuccessedExclamation");
            }
        }

        private async void OpenBingButton_Click(object sender, RoutedEventArgs e)
        {
            var success = await Windows.System.Launcher.LaunchUriAsync(new Uri(@"https://www.bing.co.uk"));
            var text = (TextBlock)FindName(TextID);
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (success)
            {
                text.Text = resourceLoader.GetString("Hint/BrowserLaunchedSpace") + resourceLoader.GetString("Hint/SuccessedExclamation");
            }
            else
            {
                text.Text = resourceLoader.GetString("Hint/BrowserLaunchedSpace") + resourceLoader.GetString("Hint/FailedExclamation");
            }
        }

        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(@"https://github.com/BillShiyaoZhang/BingWallpaperDownload/blob/master/BingBackground/privacy-policy/en-gb.md"));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Set startup task and request permission if necessary.
        /// </summary>
        private async void SetStartupTaskToggleSwitch()
        {
            var isOn = false;
            var toggleSwitch = (ToggleSwitch)FindName("StartupTaskSwitch");
            StartupTask startupTask = await StartupTask.GetAsync(StartupTaskID); // Pass the task ID you specified in the appxmanifest file
            if (startupTask.State == StartupTaskState.Enabled)
            {
                isOn = true;
            }
            toggleSwitch.IsOn = isOn;
        }

        private async Task<bool> RegisterStartupTaskAsync()
        {
            var isOn = false;
            StartupTask startupTask = await StartupTask.GetAsync(StartupTaskID); // Pass the task ID you specified in the appxmanifest file
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    // Task is disabled but can be enabled.
                    StartupTaskState newState = await startupTask.RequestEnableAsync(); // ensure that you are on a UI thread when you call RequestEnableAsync()
                    Debug.WriteLine("Request to enable startup, result = {0}", newState);
                    if (newState == StartupTaskState.Enabled)
                    {
                        isOn = true;
                    }
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
                    MessageDialog dialog1 = new MessageDialog(
                        "Startup disabled by group policy, or not supported on this device.",
                        StartupTaskID);
                    await dialog1.ShowAsync();
                    break;
                case StartupTaskState.Enabled:
                    Debug.WriteLine("Startup is enabled.");
                    isOn = true;
                    break;
            }
            return isOn;
        }

        private void SetBackgroundTasksToggleSwitch()
        {
            var toggleSwitch = (ToggleSwitch)FindName("BackgroundTaskSwitch");
            toggleSwitch.IsOn = IsBackgroundTasksSet();
        }

        /// <summary>
        /// Set background tasks.
        /// </summary>
        private List<IBackgroundTaskRegistration> RegisterBackgroundTasks()
        {
            var result = new List<IBackgroundTaskRegistration>();
            result.Add(RegisterBackgroundTask(UserPresentBTName, BTEntryPoint,
                new SystemTrigger(SystemTriggerType.UserPresent, false)));

            // TODO The time trigger should detect a proper time interval for next day, and register the next trigger.
            //var currentMins = DateTime.Now.Hour * 60 + DateTime.Now.Minute; // current time in mins
            //var restMins = 1440 - currentMins;  // rest mins in a day. 24 * 60 = 1440 mins a day
            //var triggerMins = restMins - restMins % 15 + 15;    // trigger should be set at the beginning of the next day as 15 * n mins
            result.Add(RegisterBackgroundTask(TimeBTName, BTEntryPoint, new TimeTrigger(90, false)));
            return result;
        }

        /// <summary>
        /// Set a background task.
        /// </summary>
        /// <param name="taskName">Name of the task</param>
        /// <param name="taskEntryPoint">Entry point of the task</param>
        /// <param name="trigger">Trigger of the task</param>
        /// <returns>Successfully registered task or null</returns>
        private IBackgroundTaskRegistration RegisterBackgroundTask(string taskName, string taskEntryPoint, IBackgroundTrigger trigger)
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
        private async void RunAsync(bool setFolder)
        {
            var code = await Core.RunAsync(setFolder);
            var text = (TextBlock)FindName(TextID);
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            string msg = "";
            switch (code)
            {
                case RunFunctionCode.SUCCESSFUL:
                    msg = resourceLoader.GetString("Hint/WallpaperSetSpace") + resourceLoader.GetString("Hint/SuccessedExclamation");
                    break;
                case RunFunctionCode.FAILED:
                    msg = resourceLoader.GetString("Hint/WallpaperSetSpace") + resourceLoader.GetString("Hint/FailedExclamation");
                    break;
                case RunFunctionCode.NO_INTERNET:
                    msg = resourceLoader.GetString("Hint/NoInternet");
                    break;
                case RunFunctionCode.FOLDER_NOT_SET:
                    msg = resourceLoader.GetString("Hint/FolderNotSet");
                    break;
                case RunFunctionCode.UNEXPECTED_EXCEPTION:
                    msg = resourceLoader.GetString("Hint/UnexpectedException");
                    break;
                default:
                    break;
            }
            text.Text = msg;
        }


        #endregion
        private async void StartupTaskToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    toggleSwitch.IsOn = await RegisterStartupTaskAsync();
                }
                else
                {
                    StartupTask startupTask = await StartupTask.GetAsync(StartupTaskID); // Pass the task ID you specified in the appxmanifest file
                    startupTask.Disable();
                }
            }
        }


        private void BackgroundTaskToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    RegisterBackgroundTasks();
                    toggleSwitch.IsOn = IsBackgroundTasksSet();
                }
                else
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        task.Value.Unregister(true);
                    }
                }
            }
        }

        private bool IsBackgroundTasksSet()
        {
            if (BackgroundTaskRegistration.AllTasks.Count < 2)
            {
                return false;
            }
            return true;
        }
    }
}

