using Typedown.Core.Enums;

namespace Typedown.Core.Models
{
    public class PdfPrintSettings
    {
        public PrintOrientation Orientation { get; set; }

        public double? ScaleFactor { get; set; }

        public UiSize? PageSize { get; set; }

        public UiThickness? Margin { get; set; }

        public bool ShouldPrintHeaderAndFooter { get; set; }

        public string Header { get; set; } = string.Empty;

        public string Footer { get; set; } = string.Empty;
    }
}
