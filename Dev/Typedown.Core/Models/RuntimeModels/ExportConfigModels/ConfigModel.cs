using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace Typedown.Core.Models.ExportConfigModels
{
    public partial class ConfigModel : INotifyPropertyChanged
    {
        public Dictionary<string, JsonElement> Addition { get; } = new();

        public string ScriptAfter { get; } = string.Empty;

        public virtual Task Export(IServiceProvider serviceProvider, string html, string filePath)
        {
            return Task.FromException(new NotSupportedException($"{GetType().Name} does not implement export."));
        }
    }
}
