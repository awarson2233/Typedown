using Microsoft.Extensions.DependencyInjection;
using System;
using Typedown.Core.Interfaces;
using Typedown.Interfaces;
using Typedown.Presentation.Interfaces;

namespace Typedown.Services
{
    public sealed class EditorCommandSink : IEditorCommandSink
    {
        private readonly IServiceProvider serviceProvider;

        public EditorCommandSink(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public bool Send(string name, object args)
        {
            return serviceProvider.GetService<IMarkdownEditor>()?.PostMessage(name, args) == true;
        }
    }
}
