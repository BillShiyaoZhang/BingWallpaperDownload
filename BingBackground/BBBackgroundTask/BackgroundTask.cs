using Windows.ApplicationModel.Background;
using BBCore;

namespace BBBackgroundTask
{
    /// <summary>
    /// Background task for Bing background UWP program.
    /// </summary>
    public sealed class BackgroundTask : IBackgroundTask
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
        /// Download and set images from Bing as wallpaper.
        /// </summary>
        /// <param name="taskInstance">task instance maintained by runtime</param>
        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            await Core.RunAsync();
            _deferral.Complete();
        }
    }
}
