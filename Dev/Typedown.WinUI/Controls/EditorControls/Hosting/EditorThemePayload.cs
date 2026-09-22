namespace Typedown.WinUI.Controls
{
    /// <summary>
    /// ThemeChanged 命令与 GetCurrentTheme invoke 的载荷。
    /// 线上形状由 <c>Typedown.Core.Config.EditorJsonSerializerSettings</c>（Newtonsoft + CamelCase）决定：
    /// 属性名会被自动降为 <c>theme</c> / <c>accentColor</c> / <c>background</c> 与 <c>r</c> / <c>g</c> / <c>b</c> / <c>a</c>，
    /// 与 1.2.19 前端 services/theme.ts 的读取方式一致。不要在这里挂 System.Text.Json 特性。
    /// </summary>
    public sealed record EditorThemePayload
    {
        public string Theme { get; init; } = "Light";

        public EditorColorPayload AccentColor { get; init; } = new(27, 102, 107, 1);

        public EditorColorPayload Background { get; init; } = new(249, 249, 249, 1);
    }

    public sealed record EditorColorPayload(int R, int G, int B, double A);
}
