using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Interfaces;
using Typedown.Presentation.ViewModels;

namespace Typedown.ArchitectureTests.Guards;

[TestClass]
public sealed class LayerDependencyGuardTests
{
    private static readonly Assembly CoreAssembly = typeof(IEditorSurface).Assembly;
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
