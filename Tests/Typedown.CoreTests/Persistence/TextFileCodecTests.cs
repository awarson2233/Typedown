using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Services;

namespace Typedown.CoreTests.Persistence;

/// <summary>
/// 打开与保存的字节保真：编辑器只见 <c>\n</c>，写盘时还原原来的编码、BOM 与换行符；
/// 打开后不编辑直接保存，文件字节不变（混合换行的文件统一成占多数的换行符）。
/// </summary>
[TestClass]
public sealed class TextFileCodecTests
{
    private const string Body = "# 标题\r\n\r\n正文 😀 <b>&</b>\r\n- 列表\r\n";

    private string tempDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "Typedown_TextFileCodec_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory, true);
    }

    public static IEnumerable<object[]> UnchangedFiles()
    {
        yield return Case("utf8-lf", new UTF8Encoding(false), false, Body.Replace("\r\n", "\n"), TextFileEncoding.Utf8, LineEnding.Lf);
        yield return Case("utf8-crlf", new UTF8Encoding(false), false, Body, TextFileEncoding.Utf8, LineEnding.CrLf);
        yield return Case("utf8-bom-lf", new UTF8Encoding(true), true, Body.Replace("\r\n", "\n"), TextFileEncoding.Utf8, LineEnding.Lf);
        yield return Case("utf8-bom-crlf", new UTF8Encoding(true), true, Body, TextFileEncoding.Utf8, LineEnding.CrLf);
        yield return Case("utf8-cr", new UTF8Encoding(false), false, Body.Replace("\r\n", "\r"), TextFileEncoding.Utf8, LineEnding.Cr);
        yield return Case("utf8-no-newline", new UTF8Encoding(false), false, "单行", TextFileEncoding.Utf8, LineEnding.Lf);
        yield return Case("utf8-empty", new UTF8Encoding(false), false, string.Empty, TextFileEncoding.Utf8, LineEnding.Lf);
        yield return Case("utf8-bom-empty", new UTF8Encoding(true), true, string.Empty, TextFileEncoding.Utf8, LineEnding.Lf);
        yield return Case("utf16le-crlf", new UnicodeEncoding(false, true), true, Body, TextFileEncoding.Utf16LittleEndian, LineEnding.CrLf);
        yield return Case("utf16be-lf", new UnicodeEncoding(true, true), true, Body.Replace("\r\n", "\n"), TextFileEncoding.Utf16BigEndian, LineEnding.Lf);
        yield return Case("utf32le-crlf", new UTF32Encoding(false, true), true, Body, TextFileEncoding.Utf32LittleEndian, LineEnding.CrLf);
        yield return Case("utf32be-lf", new UTF32Encoding(true, true), true, Body.Replace("\r\n", "\n"), TextFileEncoding.Utf32BigEndian, LineEnding.Lf);
    }

    private static object[] Case(string name, Encoding encoding, bool bom, string text, TextFileEncoding expectedEncoding, LineEnding expectedLineEnding)
    {
        var bytes = (bom ? encoding.GetPreamble() : []).Concat(encoding.GetBytes(text)).ToArray();
        return [name, bytes, new TextFileFormat(expectedEncoding, bom, expectedLineEnding)];
    }

    [DataTestMethod]
    [DynamicData(nameof(UnchangedFiles), DynamicDataSourceType.Method)]
    public async Task OpenThenSaveWithoutEditing_KeepsTheBytes(string name, byte[] original, TextFileFormat expectedFormat)
    {
        var path = Path.Combine(tempDirectory, name + ".md");
        await File.WriteAllBytesAsync(path, original);

        var file = await TextFileCodec.ReadAsync(path);
        Assert.AreEqual(expectedFormat, file.Format, name);
        Assert.IsFalse(file.Text.Contains('\r'), $"{name}: the editor only sees \\n");

        await new AtomicFileWriter().WriteAllBytesAsync(path, TextFileCodec.Encode(file.Text, file.Format));
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path), name);
    }

    [TestMethod]
    public void MixedLineEndings_TakeTheMajority()
    {
        var crlfMajority = TextFileCodec.Decode(Encoding.UTF8.GetBytes("a\r\nb\r\nc\nd"));
        Assert.AreEqual(LineEnding.CrLf, crlfMajority.Format.LineEnding);
        Assert.AreEqual("a\nb\nc\nd", crlfMajority.Text);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("a\r\nb\r\nc\r\nd"), TextFileCodec.Encode(crlfMajority.Text, crlfMajority.Format));

        var lfMajority = TextFileCodec.Decode(Encoding.UTF8.GetBytes("a\nb\nc\r\nd\re"));
        Assert.AreEqual(LineEnding.Lf, lfMajority.Format.LineEnding);
        Assert.AreEqual("a\nb\nc\nd\ne", lfMajority.Text);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("a\nb\nc\nd\ne"), TextFileCodec.Encode(lfMajority.Text, lfMajority.Format));

        Assert.AreEqual(LineEnding.CrLf, TextFileCodec.DetectLineEnding("a\r\nb\nc"), "a tie prefers CRLF");
    }

    [TestMethod]
    public void EditedText_IsWrittenWithTheOriginalLineEndingAndBom()
    {
        var file = TextFileCodec.Decode(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes("a\r\nb")).ToArray());
        var saved = TextFileCodec.Encode(file.Text + "\n新行\n", file.Format);
        CollectionAssert.AreEqual(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes("a\r\nb\r\n新行\r\n")).ToArray(), saved);
    }

    [TestMethod]
    public void NewDocuments_AreUtf8WithoutBomAndLf()
    {
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("# 新\n"), TextFileCodec.Encode("# 新\n", TextFileFormat.Default));
    }

    [TestMethod]
    public void Utf32LittleEndianBom_IsNotMistakenForUtf16()
    {
        var bytes = new UTF32Encoding(false, true).GetPreamble().Concat(new UTF32Encoding(false, false).GetBytes("x")).ToArray();
        Assert.AreEqual(TextFileEncoding.Utf32LittleEndian, TextFileCodec.Decode(bytes).Format.Encoding);
        Assert.AreEqual("x", TextFileCodec.Decode(bytes).Text);
    }
}
