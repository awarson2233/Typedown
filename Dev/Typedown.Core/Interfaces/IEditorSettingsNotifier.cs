using System.Collections.Generic;

namespace Typedown.Core.Interfaces
{
    public interface IEditorSettingsNotifier
    {
        void NotifySettingsChanged(IReadOnlyDictionary<string, object> settings);
    }
}
