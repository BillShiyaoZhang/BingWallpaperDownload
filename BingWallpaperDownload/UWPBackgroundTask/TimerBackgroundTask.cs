using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBCore;
using Windows.ApplicationModel.Background;

namespace BBBackgroundTask
{
    class TimerBackgroundTask : IBackgroundTask
    {
        /// <summary>
        /// BBCore instance.
        /// </summary>
        private Core _core;

        /// <summary>
        /// Get instance of BBCore.
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
        /// Background task deferral instance.
        /// </summary>
        private BackgroundTaskDeferral _deferral;

        /// <summary>
        /// Name of background task with timer.
        /// </summary>
        private const string TimeBTName = "BBBTTimer";

        /// <summary>
        /// Entry point of background tasks.
        /// </summary>
        private const string BTEntryPoint = "BBBackgroundTask.BackgroundTask";

        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            SetBackgroundTask(TimeBTName, BTEntryPoint, new TimeTrigger(1440, false));
            await Core.RunAsync();
            _deferral.Complete();
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
