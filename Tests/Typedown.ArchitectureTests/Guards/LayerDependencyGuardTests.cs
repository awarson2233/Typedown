using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core;

namespace Typedown.ArchitectureTests.Guards;

[TestClass]
public sealed class LayerDependencyGuardTests
{
    // 锚在 Core 的 DI 组合入口上：它与程序集同生共死，不会因为某个功能模块被移除而连带失效。
    private static readonly Assembly CoreAssembly = typeof(CoreServiceCollectionExtensions).Assembly;

    [TestMethod]
    public void Core_MustNotReference_WinUIOrEditor()
    {
        var referencedAssemblies = CoreAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.WinUI");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.Editor");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.XamlUI");
        CollectionAssert.DoesNotContain(referencedAssemblies, "Typedown.UI");
    }
}
