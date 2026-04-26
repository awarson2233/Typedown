using System.Threading.Tasks;

namespace Typedown.Core.Interfaces
{
    public interface IDialogService
    {
        Task<DialogButton> ShowAsync(DialogRequest request);
    }

    public enum DialogButton
    {
        None,
        Primary,
        Secondary
    }

    public class DialogRequest
    {
        public object Title { get; set; }

        public object Content { get; set; }

        public string CloseButtonText { get; set; }

        public string PrimaryButtonText { get; set; }

        public string SecondaryButtonText { get; set; }

        public DialogButton DefaultButton { get; set; } = DialogButton.None;
    }
}
