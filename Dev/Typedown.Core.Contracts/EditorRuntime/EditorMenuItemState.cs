namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorMenuItemState
    {
        public bool IsEnable { get; set; }

        public bool IsChecked { get; set; }
    }
}
