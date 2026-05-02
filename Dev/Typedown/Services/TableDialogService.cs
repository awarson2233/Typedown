using System.Threading.Tasks;
using Typedown.Controls;
using Typedown.Core.Interfaces;
using Typedown.Presentation.Interfaces;
using Windows.UI.Xaml;

namespace Typedown.Services
{
    public sealed class TableDialogService : ITableDialogService
    {
        private readonly IWindowContext windowContext;

        public TableDialogService(IWindowContext windowContext)
        {
            this.windowContext = windowContext;
        }

        public async Task<TableDialogResult> OpenInsertTableDialogAsync()
        {
            var result = await InsertTableDialog.OpenInsertTableDialog((XamlRoot)windowContext.ViewRoot);
            return result == null ? null : new TableDialogResult { Rows = result.Rows, Columns = result.Columns };
        }

        public async Task<TableDialogResult> OpenResizeTableDialogAsync()
        {
            var result = await InsertTableDialog.OpenResizeTableDialog((XamlRoot)windowContext.ViewRoot);
            return result == null ? null : new TableDialogResult { Rows = result.Rows, Columns = result.Columns };
        }
    }
}
