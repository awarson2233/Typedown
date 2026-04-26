namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorSearchRequest
    {
        public string? Value { get; init; }

        public bool SearchIsCaseSensitive { get; init; }

        public bool SearchIsWholeWord { get; init; }

        public bool SearchIsRegexp { get; init; }

        public object? Selection { get; init; }
    }
}
