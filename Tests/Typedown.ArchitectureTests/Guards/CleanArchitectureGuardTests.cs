using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core;

namespace Typedown.ArchitectureTests.Guards;

[TestClass]
public sealed class CleanArchitectureGuardTests
{
    // 锚在 Core 的 DI 组合入口上：它与程序集同生共死，不会因为某个功能模块被移除而连带失效。
    private static readonly Assembly CoreAssembly = typeof(CoreServiceCollectionExtensions).Assembly;

    // Core 与 WinUI 的唯一边界是「有没有平台 UI 类型」：WinUI / Windows App SDK（Microsoft.UI.*、Microsoft.WinUI）、
    // Windows SDK 投影（Microsoft.Windows.SDK.NET、WinRT.Runtime、Windows.* 命名空间）、WebView2 与 SkiaSharp 都只能出现在 WinUI 工程。
    private static readonly string[] ForbiddenPlatformPrefixes =
    [
        "Microsoft.UI",
        "Microsoft.WinUI",
        "Microsoft.Windows.SDK.NET",
        "WinRT",
        "Windows.",
        "Microsoft.Web.WebView2",
        "SkiaSharp"
    ];

    [TestMethod]
    public void CoreAssembly_MustBeCompletelyFreeOfPlatformUiTypes()
    {
        AssertAssemblyHasNoForbiddenReferences(CoreAssembly);
        AssertTypesDoNotExposePlatformTypes(CoreAssembly);
    }

    private static void AssertAssemblyHasNoForbiddenReferences(Assembly assembly)
    {
        var references = assembly.GetReferencedAssemblies();
        foreach (var refName in references.Select(r => r.Name ?? string.Empty))
        {
            Assert.AreNotEqual(
                "Windows",
                refName,
                $"Assembly '{assembly.GetName().Name}' illegally references platform assembly '{refName}'.");

            foreach (var forbidden in ForbiddenPlatformPrefixes)
            {
                Assert.IsFalse(
                    refName.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"Assembly '{assembly.GetName().Name}' illegally references platform assembly '{refName}'.");
            }
        }
    }

    private static void AssertTypesDoNotExposePlatformTypes(Assembly assembly)
    {
        foreach (var type in assembly.GetExportedTypes())
        {
            AssertTypeIsPlatformNeutral(type, type.FullName ?? type.Name);
        }
    }

    private static void AssertTypeIsPlatformNeutral(Type type, string context)
    {
        var typeNamespace = type.Namespace ?? string.Empty;
        foreach (var forbidden in ForbiddenPlatformPrefixes)
        {
            Assert.IsFalse(
                typeNamespace.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase),
                $"Platform-polluted type '{type.FullName}' detected in context: {context}");
        }
    }
}
