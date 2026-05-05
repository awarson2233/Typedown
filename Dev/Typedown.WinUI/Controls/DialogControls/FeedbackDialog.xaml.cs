using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Core.Utilities;
using Typedown.Presentation.Utilities;

namespace Typedown.WinUI.Controls
{
    public sealed partial class FeedbackDialog : UserControl
    {
        public static readonly DependencyProperty RantingProperty = DependencyProperty.Register(nameof(Ranting), typeof(int), typeof(FeedbackDialog), new PropertyMetadata(-1));
        public int Ranting { get => (int)GetValue(RantingProperty); set => SetValue(RantingProperty, value); }

        public static readonly DependencyProperty FeedbackProperty = DependencyProperty.Register(nameof(Feedback), typeof(string), typeof(FeedbackDialog), new PropertyMetadata(""));
        public string Feedback { get => (string)GetValue(FeedbackProperty); set => SetValue(FeedbackProperty, value); }

        public static readonly DependencyProperty ContactProperty = DependencyProperty.Register(nameof(Contact), typeof(string), typeof(FeedbackDialog), new PropertyMetadata(""));
        public string Contact { get => (string)GetValue(ContactProperty); set => SetValue(ContactProperty, value); }

        public FeedbackDialog()
        {
            InitializeComponent();
        }

        public static async Task OpenFeedbackDialog(XamlRoot xamlRoot)
        {
            var content = new FeedbackDialog();
            var result = await new ContentDialog
            {
                Title = Locale.GetDialogString("FeedbackTitle"),
                Content = content,
                CloseButtonText = Locale.GetDialogString("Cancel"),
                PrimaryButtonText = Locale.GetDialogString("Submit"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = xamlRoot
            }.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            string msg;
            if (string.IsNullOrEmpty(content.Feedback))
            {
                msg = Locale.GetDialogString("ContentCanNotBeBlank");
            }
            else
            {
                try
                {
                    var res = await Common.Post("https://typedown.ownbox.cn/feedback", new
                    {
                        rating = content.Ranting,
                        feedback = content.Feedback,
                        contact = content.Contact,
                    });

                    msg = res["code"]?.ToObject<int>() == 0
                        ? Locale.GetDialogString("SubmittedSuccessfully")
                        : res["msg"]?.ToString() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                }
            }

            await new ContentDialog
            {
                Title = Locale.GetDialogString("FeedbackTitle"),
                Content = msg,
                CloseButtonText = Locale.GetDialogString("Ok"),
                XamlRoot = xamlRoot
            }.ShowAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Bindings?.StopTracking();
        }
    }
}
