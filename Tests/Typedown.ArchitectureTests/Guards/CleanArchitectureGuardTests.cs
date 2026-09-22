using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Services;
using Typedown.Presentation.ViewModels;

namespace Typedown.ArchitectureTests.Guards;

[TestClass]
public sealed class CleanArchitectureGuardTests
{
    private static readonly Assembly CoreAssembly = typeof(EditorBridge).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(EditorViewModel).Assembly;

    private static readonly string[] ForbiddenPlatformPrefixes =
    [
        "Microsoft.UI",
        "Windows.UI",
        "Microsoft.Web.WebView2",
        "Windows.Storage.Pickers"
    ];

    [TestMethod]
    public void CoreAssembly_MustBeCompletelyFreeOfPlatformUiTypes()
    {
        AssertAssemblyHasNoForbiddenReferences(CoreAssembly);
        AssertTypesDoNotExposePlatformTypes(CoreAssembly);
    }

    [TestMethod]
    public void PresentationAssembly_MustBeCompletelyFreeOfPlatformUiTypes()
    {
        AssertAssemblyHasNoForbiddenReferences(PresentationAssembly);
        AssertTypesDoNotExposePlatformTypes(PresentationAssembly);
    }

    private static void AssertAssemblyHasNoForbiddenReferences(Assembly assembly)
    {
        var references = assembly.GetReferencedAssemblies();
        foreach (var refName in references.Select(r => r.Name ?? string.Empty))
        {
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
