using System.Collections.Generic;

namespace Typedown.Presentation.Interfaces
{
    public interface IEditorSettingsNotifier
    {
        void NotifySettingsChanged(IReadOnlyDictionary<string, object> settings);
    }
}
