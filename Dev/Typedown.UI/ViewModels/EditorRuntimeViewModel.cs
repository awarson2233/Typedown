using Typedown.Core.Contracts.EditorRuntime;
using Typedown.UI.Mvvm;

namespace Typedown.UI.ViewModels;

public sealed class EditorRuntimeViewModel : ObservableObject
{
    private EditorContentState contentState = new();
    private EditorFormatState formatState = new();
    private EditorMenuState menuState = new();
    private EditorParagraphState paragraphState = EditorParagraphState.FromMenuState(new EditorMenuState());
    private EditorSelectionState selectionState = new();
    private IReadOnlyList<EditorTocNodeViewModel> tocNodes = Array.Empty<EditorTocNodeViewModel>();

    public EditorContentState ContentState
    {
        get => contentState;
        private set => SetProperty(ref contentState, value);
    }

    public EditorFormatState FormatState
    {
        get => formatState;
        private set
        {
            if (SetProperty(ref formatState, value))
            {
                OnPropertyChanged(nameof(IsSelectionActive));
                OnPropertyChanged(nameof(IsImageContextMenuVisible));
            }
        }
    }

    public EditorMenuState MenuState
    {
        get => menuState;
        private set => SetProperty(ref menuState, value);
    }

    public EditorParagraphState ParagraphState
    {
        get => paragraphState;
        private set => SetProperty(ref paragraphState, value);
    }

    public EditorSelectionState SelectionState
    {
        get => selectionState;
        private set
        {
            if (SetProperty(ref selectionState, value))
            {
                OnPropertyChanged(nameof(IsSelectionActive));
                OnPropertyChanged(nameof(IsImageContextMenuVisible));
            }
        }
    }

    public IReadOnlyList<EditorTocNodeViewModel> TocNodes
    {
        get => tocNodes;
        private set => SetProperty(ref tocNodes, value);
    }

    public bool IsSelectionActive => SelectionState.IsTextSelected || FormatState.Image;

    public bool IsImageContextMenuVisible => FormatState.Image && SelectionState.HasSelectedImage;

    public void ApplyContentState(EditorContentState? state)
    {
        ContentState = state ?? new EditorContentState();
        TocNodes = EditorTocNodeViewModel.CreateTree(ContentState.Toc);
    }

    public void ApplyFormatState(EditorFormatState? state)
    {
        FormatState = state ?? new EditorFormatState();
    }

    public void ApplyMenuState(EditorMenuState? state)
    {
        MenuState = state ?? new EditorMenuState();
        ParagraphState = EditorParagraphState.FromMenuState(MenuState);
    }

    public void ApplySelectionState(EditorSelectionState? state)
    {
        SelectionState = state ?? new EditorSelectionState();
    }
}
