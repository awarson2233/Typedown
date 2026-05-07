using System.Threading.Tasks;

namespace Typedown.Presentation.Interfaces
{
    public interface ITableDialogService
    {
        Task<TableDialogResult?> OpenInsertTableDialogAsync();

        Task<TableDialogResult?> OpenResizeTableDialogAsync();
    }

    public sealed class TableDialogResult
    {
        public int Rows { get; set; }

        public int Columns { get; set; }
    }
}
