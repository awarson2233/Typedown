using Typedown.Presentation.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUITableDialogService : ITableDialogService
    {
        public Task<TableDialogResult> OpenInsertTableDialogAsync()
        {
            throw new NotSupportedException("WinUI insert-table dialog is not wired in this migration slice.");
        }

        public Task<TableDialogResult> OpenResizeTableDialogAsync()
        {
            throw new NotSupportedException("WinUI resize-table dialog is not wired in this migration slice.");
        }
    }
}
