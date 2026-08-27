using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Enums;

namespace Typedown.CoreTests.Models;

[TestClass]
public class EnumContractTests
{
    [TestMethod]
    public void CoreEnums_MaintainValueAndMemberInvariance()
    {
        AssertEnumMembers<ImageUploadMethod>(
            ("None", 0),
            ("FTP", 1),
            ("Git", 2),
            ("OSS", 3),
            ("SCP", 4),
            ("PowerShell", 1024));

        AssertEnumMembers<InsertImageAction>(
            ("None", 0),
            ("CopyToPath", 1),
            ("Upload", 2));

        AssertEnumMembers<ExportType>(
            ("None", 0),
            ("PDF", 1),
            ("HTML", 2),
            ("Image", 3));

        AssertEnumMembers<PrintOrientation>(
            ("Portrait", 0),
            ("Landscape", 1));

        AssertEnumMembers<FileStartupAction>(
            ("None", 0),
            ("OpenLast", 1));

        AssertEnumMembers<FolderStartupAction>(
            ("None", 0),
            ("OpenLast", 1),
            ("OpenFolder", 2),
            ("FollowOpenedFileFolder", 3));

        AssertEnumMembers<AppTheme>(
            ("Default", 0),
            ("Light", 1),
            ("Dark", 2));
    }

    private static void AssertEnumMembers<TEnum>(params (string Name, int Value)[] expectedMembers)
        where TEnum : struct, Enum
    {
        var actualMembers = Enum
            .GetValues<TEnum>()
            .Select(value => (value.ToString(), Convert.ToInt32(value)))
            .ToArray();

        CollectionAssert.AreEqual(
            expectedMembers.Select(member => $"{member.Name}:{member.Value}").ToArray(),
            actualMembers.Select(member => $"{member.Item1}:{member.Item2}").ToArray(),
            $"Unexpected enum shape for {typeof(TEnum).Name}.");
    }
}
