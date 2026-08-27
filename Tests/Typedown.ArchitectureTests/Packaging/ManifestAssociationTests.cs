using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Typedown.ArchitectureTests.Packaging;

[TestClass]
public sealed class ManifestAssociationTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void PackageManifest_DeclaresMarkdownFileAssociations_AndFullTrustCapability()
    {
        var manifestPath = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Package.appxmanifest");
        Assert.IsTrue(File.Exists(manifestPath), $"Package.appxmanifest not found at {manifestPath}");

        var doc = XDocument.Load(manifestPath);
        XNamespace defaultNs = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XNamespace uapNs = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        XNamespace rescapNs = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";

        var package = doc.Element(defaultNs + "Package");
        Assert.IsNotNull(package, "Missing <Package> root element in manifest");

        // Verify Full Trust capability
        var capabilities = package.Element(defaultNs + "Capabilities");
        Assert.IsNotNull(capabilities, "Missing <Capabilities> element");
        var fullTrustCap = capabilities.Elements(rescapNs + "Capability")
            .FirstOrDefault(c => (string?)c.Attribute("Name") == "runFullTrust");
        Assert.IsNotNull(fullTrustCap, "Expected <rescap:Capability Name=\"runFullTrust\" />");

        // Verify FileTypeAssociation extension
        var applications = package.Element(defaultNs + "Applications");
        Assert.IsNotNull(applications, "Missing <Applications> element");
        var application = applications.Element(defaultNs + "Application");
        Assert.IsNotNull(application, "Missing <Application> element");
        var extensions = application.Element(defaultNs + "Extensions");
        Assert.IsNotNull(extensions, "Missing <Extensions> element");

        var fileTypeExtension = extensions.Elements(uapNs + "Extension")
            .FirstOrDefault(e => (string?)e.Attribute("Category") == "windows.fileTypeAssociation");
        Assert.IsNotNull(fileTypeExtension, "Expected <uap:Extension Category=\"windows.fileTypeAssociation\">");

        var fileTypeAssociation = fileTypeExtension.Element(uapNs + "FileTypeAssociation");
        Assert.IsNotNull(fileTypeAssociation, "Expected <uap:FileTypeAssociation>");
        Assert.AreEqual("markdown", (string?)fileTypeAssociation.Attribute("Name"));

        var logo = fileTypeAssociation.Element(uapNs + "Logo");
        Assert.IsNotNull(logo, "Expected <uap:Logo>");
        Assert.AreEqual(@"Assets\Markdown.png", logo.Value.Trim());

        var supportedFileTypes = fileTypeAssociation.Element(uapNs + "SupportedFileTypes");
        Assert.IsNotNull(supportedFileTypes, "Expected <uap:SupportedFileTypes>");

        var fileTypes = supportedFileTypes.Elements(uapNs + "FileType")
            .Select(ft => ft.Value.Trim())
            .ToArray();

        CollectionAssert.Contains(fileTypes, ".md");
        CollectionAssert.Contains(fileTypes, ".markdown");
        CollectionAssert.DoesNotContain(fileTypes, ".txt");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Typedown.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Typedown.sln from test output directory.");
    }
}
