using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using Windows.System.UserProfile;
using Windows.Storage;
using System.Net.Http;

namespace BBBackgroundTask
{
    public sealed class BBBackgroundTask : IBackgroundTask
    {
        BBCore.BBCore core;

        const string ImagesSubdirectory = "DownloadedImages";
        BackgroundTaskDeferral _deferral;

        async void IBackgroundTask.Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            if (core == null)
            {
                core = new BBCore.BBCore("_1920x1080.jpg");
            }
            await core.RunFunctionAsync(ImagesSubdirectory);
            //await RunFunctionAsync();
            _deferral.Complete();
        }
    }
}
