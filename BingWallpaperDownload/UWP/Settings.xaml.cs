using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using UWPLibrary;
using Windows.Storage;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        #region Private Constant Value

        /// <summary>
        /// Name of background task detecting if user is presenting.
        /// </summary>
        private const string UserPresentBackgroundTaskName = "BBBTUserPresent";

        /// <summary>
        /// Name of background task with timer.
        /// </summary>
        private const string TimeBackgroundTaskName = "BBBTTimer";

        /// <summary>
        /// Entry point of background tasks.
        /// </summary>
        private const string BackgroundTaskEntryPoint = "UWPBackgroundTask.BackgroundTask";

        /// <summary>
        /// ID of startup task.
        /// </summary>
        private const string StartupTaskID = "BingBackgroundStartupId";

        /// <summary>
        /// Instance of core.
        /// </summary>
        private Core _core;

        #endregion

        #region Private Properties

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
        private bool IsBackgroundTasksSet()
        {
            if (BackgroundTaskRegistration.AllTasks.Count < 2)
            {
                return false;
            }
            return true;
        }

        #endregion

        public Settings()
        {
            this.InitializeComponent();

            SetToggleSwitches();
        }

        #region Private Set Tasks

        private async Task<bool> RegisterStartupTaskAsync()
        {
            var isOn = false;
            StartupTask startupTask = await StartupTask.GetAsync(StartupTaskID); // Pass the task ID you specified in the appxmanifest file
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    // Task is disabled but can be enabled.
                    StartupTaskState newState = await startupTask.RequestEnableAsync(); // ensure that you are on a UI thread when you call RequestEnableAsync()
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
                    isOn = true;
                    break;
            }
            return isOn;
        }

        /// <summary>
        /// Set background tasks.
        /// </summary>
        private List<IBackgroundTaskRegistration> RegisterBackgroundTasks()
        {
            var result = new List<IBackgroundTaskRegistration>();
            result.Add(RegisterBackgroundTask(UserPresentBackgroundTaskName, BackgroundTaskEntryPoint,
                new SystemTrigger(SystemTriggerType.UserPresent, false)));

            // TODO The time trigger should detect a proper time interval for next day, and register the next trigger.
            //var currentMins = DateTime.Now.Hour * 60 + DateTime.Now.Minute; // current time in mins
            //var restMins = 1440 - currentMins;  // rest mins in a day. 24 * 60 = 1440 mins a day
            //var triggerMins = restMins - restMins % 15 + 15;    // trigger should be set at the beginning of the next day as 15 * n mins
            result.Add(RegisterBackgroundTask(TimeBackgroundTaskName, BackgroundTaskEntryPoint, new TimeTrigger(90, false)));
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
        
        #endregion

        #region Private Set Toggle Switches

        private void SetToggleSwitches()
        {
            SetStartupTaskToggleSwitch();
            SetBackgroundTasksToggleSwitch();
            SetAutoReadToggleSwitch();
        }

        private void SetAutoReadToggleSwitch()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var isAutoRead = localSettings.Values["AutoReadKey"];
            if (isAutoRead == null)
            {
                localSettings.Values["AutoReadKey"] = true;
                isAutoRead = true;
            }
            AutoReadSwitch.IsOn = (bool)isAutoRead;
        }

        private void SetBackgroundTasksToggleSwitch()
        {
            BackgroundTaskSwitch.IsOn = IsBackgroundTasksSet();
        }

        /// <summary>
        /// Set startup task and request permission if necessary.
        /// </summary>
        private async void SetStartupTaskToggleSwitch()
        {
            var isOn = false;
            StartupTask startupTask = await StartupTask.GetAsync(StartupTaskID); // Pass the task ID you specified in the appxmanifest file
            if (startupTask.State == StartupTaskState.Enabled)
            {
                isOn = true;
            }
            StartupTaskSwitch.IsOn = isOn;
        }

        #endregion

        #region Public Listener


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

        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await Windows.System.Launcher.LaunchUriAsync(new Uri(@"https://github.com/BillShiyaoZhang/BingWallpaperDownload/blob/master/BingWallpaperDownload/privacy-policy/en-gb.md"));
        }

        #endregion

        private void AutoReadSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["AutoReadKey"] = AutoReadSwitch.IsOn;
        }

        private async void Feedback_Click(object sender, RoutedEventArgs e)
        {
            var emailMessage = new Windows.ApplicationModel.Email.EmailMessage();

            var emailRecipient = new Windows.ApplicationModel.Email.EmailRecipient("zhangshiyao_ZSY@outlook.com");
            emailMessage.To.Add(emailRecipient);
            emailMessage.Subject = "[BWD] [Feedback]";

            await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }

        private async void Contribute_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://github.com/BillShiyaoZhang/BingWallpaperDownload");
            var success = await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackButton.IsEnabled = Frame.CanGoBack;
        }

    }
}
