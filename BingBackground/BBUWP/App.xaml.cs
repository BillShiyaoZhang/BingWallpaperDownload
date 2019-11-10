using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using BBCore;

namespace BBUWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
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
        private const string StartupTaskID = "MyStartupId";

        /// <summary>
        /// ID of text block in the frame.
        /// </summary>
        private const string TextID = "Hint";

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
                    _core = new Core(Core.DefaultResolutionExtension);
                }
                return _core;
            }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
            }

            if (args.Kind == ActivationKind.StartupTask)
            {
                string payload = string.Empty;
                var startupArgs = args as StartupTaskActivatedEventArgs;
                payload = ActivationKind.StartupTask.ToString();
                rootFrame.Navigate(typeof(MainPage), payload);
                Window.Current.Activate();
                //SetBackgroundTasks();
                //await Core.RunAsync();
                //Window.Current.Close();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
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
            //var timerOneShot = SetBackgroundTask(TimeBTName, BTEntryPoint, new TimeTrigger((uint)triggerMins, false));
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
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                builder.IsNetworkRequested = true;
                var task = builder.Register();
                return task;
            }
            return null;
        }

    }
}
