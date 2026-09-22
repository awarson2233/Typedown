using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core;
using Typedown.Presentation.ViewModels;

namespace Typedown.ArchitectureTests.Guards;

[TestClass]
public sealed class LayerDependencyGuardTests
{
    // 锚在 Core 的 DI 组合入口上：它与程序集同生共死，不会因为某个功能模块被移除而连带失效。
    private static readonly Assembly CoreAssembly = typeof(CoreServiceCollectionExtensions).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(EditorViewModel).Assembly;

    [TestMethod]
    public void Core_MustNotReference_PresentationOrWinUI()
    {
        var referencedAssemblies = CoreAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.Presentation");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.WinUI");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.Editor");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.XamlUI");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.UI");
    }

    [TestMethod]
    public void Presentation_MustReferenceCore_AndNotReferenceWinUI()
    {
        var referencedAssemblies = PresentationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        CollectionAssert.Contains(referencedAssemblies, "Typedown.Core");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.WinUI");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.Editor");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.XamlUI");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.UI");
    }
}
