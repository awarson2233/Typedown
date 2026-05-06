using System;

namespace Typedown.WinUI.Controls
{
    internal sealed class WinUIEditorHostController
    {
        private readonly IEditorDocumentSession documentSession;
        private readonly IEditorHostSink hostSink;
        private bool editorReady;
        private bool pendingLoadFile;

        public WinUIEditorHostController(IEditorDocumentSession documentSession, IEditorHostSink hostSink)
        {
            this.documentSession = documentSession ?? throw new ArgumentNullException(nameof(documentSession));
            this.hostSink = hostSink ?? throw new ArgumentNullException(nameof(hostSink));
            pendingLoadFile = true;
        }

        public string? InitialFilePath { get; set; }

        public IEditorDocumentSession DocumentSession => documentSession;

        // Controller entrypoints: LoadFile(), Save(), SaveAs().

        public void ResetForNavigation()
        {
            editorReady = false;
            pendingLoadFile = true;
        }

        public void MarkEditorReady()
        {
            editorReady = true;
        }

        public EditorPersistenceResult LoadFile(string filePath)
        {
            var result = documentSession.LoadFile(filePath);
            pendingLoadFile = result.Success;
            if (result.Success)
            {
                _ = TrySendLoadFile();
            }

            return result;
        }

        public EditorPersistenceResult ReplaceFileText(string text, string? filePath = null, string? basePath = null)
        {
            var result = documentSession.ReplaceFileText(text, filePath, basePath);
            pendingLoadFile = result.Success;
            if (result.Success)
            {
                _ = TrySendLoadFile();
            }

            return result;
        }

        public EditorPersistenceResult Save()
        {
            return documentSession.Save();
        }

        public EditorPersistenceResult SaveAs(string filePath, bool saveCopy = false)
        {
            return documentSession.SaveAs(filePath, saveCopy);
        }

        public bool TryLoadInitialFile()
        {
            if (string.IsNullOrWhiteSpace(InitialFilePath))
            {
                return false;
            }

            var result = LoadFile(InitialFilePath);
            return result.Success;
        }

        public bool TrySendLoadFile()
        {
            if (!editorReady || !pendingLoadFile)
            {
                return false;
            }

            pendingLoadFile = !hostSink.Send(EditorHostCommands.CreateLoadFile(documentSession.State));
            return !pendingLoadFile;
        }
    }
}
