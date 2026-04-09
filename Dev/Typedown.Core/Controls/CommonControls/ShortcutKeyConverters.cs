using Avalonia.Data.Converters;
using System;
using System.Globalization;
using Typedown.Core.Models;
using Typedown.Core.Utilities;

namespace Typedown.Core.Controls
{
    public static class ShortcutKeyConverters
    {
        public static readonly IValueConverter HasShortcutKey = new FuncValueConverter<ShortcutKey, bool>(
            key => ShortcutPickerButton.HasShortcutKey(key));

        public static readonly IValueConverter HasShortcutKeyReverse = new FuncValueConverter<ShortcutKey, bool>(
            key => ShortcutPickerButton.HasShortcutKeyReverse(key));

        public static readonly IValueConverter GetShortcutKeyTextList = new FuncValueConverter<ShortcutKey, System.Collections.Generic.IEnumerable<string>>(
            key => Common.GetShortcutKeyTextList(key));
    }
}
