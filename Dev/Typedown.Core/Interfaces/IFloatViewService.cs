using Typedown.Core.Editor;

namespace Typedown.Core.Interfaces
{
    /// <summary>宿主画的编辑器浮层（段落菜单、格式条、图片编辑、图片工具条、表格工具、提示）。</summary>
    public interface IFloatViewService
    {
        void OpenImageToolbar(ImageToolbarRequested request);

        void OpenFrontMenu(BlockMenuRequested request);

        void OpenFormatPicker(FormatPickerRequested request);

        void OpenImageSelector(ImageEditorRequested request);

        void OpenTableTools(TableToolsRequested request);

        void OpenToolTip(TooltipRequested request);

        void CloseToolTip();
    }
}
