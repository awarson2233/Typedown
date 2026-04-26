namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorPersistenceResult(
        bool Success,
        EditorDocumentState State,
        string? Message = null,
        string? PersistedFilePath = null,
        bool IsCopy = false);
}
