namespace Typedown.Core.Interfaces
{
    /// <summary>
    /// Factory for creating or resolving IEditorSurface instances based on engine kind or feature toggle.
    /// </summary>
    public interface IEditorSurfaceFactory
    {
        /// <summary>
        /// Creates an IEditorSurface instance for the requested engine kind.
        /// </summary>
        /// <param name="engineKind">The requested editor engine kind (WebView2Muya or Native).</param>
        /// <returns>An IEditorSurface implementation.</returns>
        IEditorSurface CreateEditorSurface(EditorEngineKind engineKind);
    }
}
