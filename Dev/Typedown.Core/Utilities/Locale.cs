using System;
using System.Collections.Generic;
using System.Linq;

namespace Typedown.Core.Utilities
{
    public class LocaleAttribute : Attribute
    {
        public static Func<string, string> StringResolver { get; set; } = key => key;

        public string[] Keys { get; }

        public string Text => Texts.FirstOrDefault() ?? string.Empty;

        public IEnumerable<string> Texts => Keys.Select(StringResolver);

        public LocaleAttribute(params string[] keys)
        {
            Keys = keys;
        }
    }
}
