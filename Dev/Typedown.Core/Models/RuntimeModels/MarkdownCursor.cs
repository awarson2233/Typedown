namespace Typedown.Core.Models
{
    public record MarkdownCursor
    {
        public record Pos(string Key, int Offset);

        public Pos Anchor = new(string.Empty, 0);

        public Pos Focus = new(string.Empty, 0);
    }
}
