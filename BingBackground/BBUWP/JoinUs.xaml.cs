using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BBUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class JoinUs : Page
    {
        public JoinUs()
        {
            this.InitializeComponent();
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
    }
}
