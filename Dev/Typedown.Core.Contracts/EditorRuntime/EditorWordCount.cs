namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorWordCount
    {
        public int Word { get; init; }

        public int Character { get; init; }
    }
}
