using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Utilities;

namespace Typedown.CoreTests.Utilities;

[TestClass]
public class HtmlUtilityTests
{
    [TestMethod]
    public void ExtractHtmlFragment_HandlesCommentMarkers()
    {
        var html = "header<!--StartFragment--><b>fragment</b><!--EndFragment-->tail";
        var result = Common.ExtractHtmlFragment(html);
        Assert.AreEqual("<b>fragment</b>", result);
    }

    [TestMethod]
    public void ExtractHtmlFragment_HandlesBodyTagWithAttributes()
    {
        var html = "<html><body class=\"office\"><i>body</i></body></html>";
        var result = Common.ExtractHtmlFragment(html);
        Assert.AreEqual("<i>body</i>", result);
    }

    [TestMethod]
    public void ExtractHtmlFragment_HandlesCfHtmlPrefixWithByteOffsets()
    {
        const string fragment = "<strong>offset</strong>";
        var prefix = "Version:1.0\r\nStartFragment:0000000000\r\nEndFragment:0000000000\r\n";
        var start = Encoding.UTF8.GetByteCount(prefix);
        var end = start + Encoding.UTF8.GetByteCount(fragment);
        var cfHtml = prefix.Replace("StartFragment:0000000000", $"StartFragment:{start:D10}", StringComparison.Ordinal)
            .Replace("EndFragment:0000000000", $"EndFragment:{end:D10}", StringComparison.Ordinal) + fragment;

        var result = Common.ExtractHtmlFragment(cfHtml);
        Assert.AreEqual(fragment, result);
    }

    [TestMethod]
    public void ExtractHtmlFragment_ReturnsEmpty_WhenInputIsNullOrEmpty()
    {
        Assert.AreEqual(string.Empty, Common.ExtractHtmlFragment(null!));
        Assert.AreEqual(string.Empty, Common.ExtractHtmlFragment(string.Empty));
    }

    [TestMethod]
    public void ExtractHtmlFragment_ReturnsOriginalString_WhenNoMarkersPresent()
    {
        const string plainText = "<span>plain text without fragments</span>";
        var result = Common.ExtractHtmlFragment(plainText);
        Assert.AreEqual(plainText, result);
    }
}
