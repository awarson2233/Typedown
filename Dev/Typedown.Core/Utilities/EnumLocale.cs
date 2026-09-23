using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Typedown.Core.Enums;

namespace Typedown.Core.Utilities
{
    /// <summary>
    /// 取枚举值上 <see cref="LocaleAttribute"/> 的本地化文本。
    /// 泛型参数带 <see cref="DynamicallyAccessedMembersAttribute"/>，裁剪与 AOT 会保留具体枚举的字段；
    /// 只拿到装箱值的调用方（XAML 转换器）经 <see cref="GetText(object?)"/> 按已知枚举类型分发。
    /// </summary>
    public static class EnumLocale
    {
        public static string? GetText<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(TEnum value)
            where TEnum : struct, Enum
        {
            return typeof(TEnum).GetField(value.ToString())?.GetCustomAttribute<LocaleAttribute>()?.Text;
        }

        /// <summary>设置界面里用 <see cref="LocaleAttribute"/> 标注的全部枚举；新增这类枚举时要在这里补一行。</summary>
        public static string? GetText(object? value) => value switch
        {
            AppTheme v => GetText(v),
            ExportType v => GetText(v),
            FileStartupAction v => GetText(v),
            FolderStartupAction v => GetText(v),
            ImageUploadMethod v => GetText(v),
            InsertImageAction v => GetText(v),
            PrintOrientation v => GetText(v),
            _ => null,
        };
    }
}
