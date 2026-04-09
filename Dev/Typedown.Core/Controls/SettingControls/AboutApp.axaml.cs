using PropertyChanged;
﻿using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Typedown.Core.Controls
{
[DoNotNotify]
        public sealed partial class AboutApp : UserControl
    {
        public AboutApp()
        {
            InitializeComponent();
        }

        public static string GetAppVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        private async void FeedBackButton_Click(object sender, RoutedEventArgs e)
        {
            await FeedbackDialog.OpenFeedbackDialog(this);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
