using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Typedown.Core.Interfaces;
using Typedown.Interfaces;
using Typedown.Presentation.Interfaces;

namespace Typedown.Services
{
    public sealed class EditorSettingsNotifier : IEditorSettingsNotifier
    {
        private readonly IServiceProvider serviceProvider;

        public EditorSettingsNotifier(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void NotifySettingsChanged(IReadOnlyDictionary<string, object> settings)
        {
            serviceProvider.GetService<IMarkdownEditor>()?.PostMessage("SettingsChanged", settings);
        }
    }
}
