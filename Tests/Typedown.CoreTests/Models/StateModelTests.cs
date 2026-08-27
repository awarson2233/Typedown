using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Models;

namespace Typedown.CoreTests.Models;

[TestClass]
public class StateModelTests
{
    [TestMethod]
    public void FormatState_MapsLegacySelectionFormatsFromCore()
    {
        var state = new FormatState(
        [
            new("strong", string.Empty),
            new("em", string.Empty),
            new("html_tag", "u"),
            new("inline_code", string.Empty),
            new("inline_math", string.Empty),
            new("html_tag", "mark"),
            new("del", string.Empty),
            new("link", string.Empty),
            new("span", "img"),
        ]);

        Assert.IsTrue(state.Bold);
        Assert.IsTrue(state.Italic);
        Assert.IsTrue(state.Underline);
        Assert.IsTrue(state.InlineCode);
        Assert.IsTrue(state.InlineMath);
        Assert.IsTrue(state.Highlight);
        Assert.IsTrue(state.Strikethrough);
        Assert.IsTrue(state.Hyperlink);
        Assert.IsTrue(state.Image);
    }

    [TestMethod]
    public void FormatState_DefaultInitialization_AllPropertiesFalse()
    {
        var state = new FormatState();

        Assert.IsFalse(state.Bold);
        Assert.IsFalse(state.Italic);
        Assert.IsFalse(state.Underline);
        Assert.IsFalse(state.InlineCode);
        Assert.IsFalse(state.InlineMath);
        Assert.IsFalse(state.Highlight);
        Assert.IsFalse(state.Strikethrough);
        Assert.IsFalse(state.Hyperlink);
        Assert.IsFalse(state.Image);
    }

    [TestMethod]
    public void ParagraphState_PreservesTaskListListHeadingCodeAndTableSemanticsFromCore()
    {
        var taskList = new ParagraphState(new MenuState
        {
            IsTaskList = true,
            Affiliation = new Dictionary<string, bool> { ["ul"] = true },
        });

        Assert.IsTrue(taskList.TaskList.IsChecked);
        Assert.IsFalse(taskList.BulletList.IsChecked);
        Assert.IsTrue(taskList.TaskList.IsEnable);
        Assert.IsTrue(taskList.ImageIsEnable);

        var bulletList = new ParagraphState(new MenuState
        {
            Affiliation = new Dictionary<string, bool> { ["ul"] = true },
        });

        Assert.IsTrue(bulletList.BulletList.IsChecked);
        Assert.IsFalse(bulletList.TaskList.IsChecked);
        Assert.IsTrue(bulletList.OrderList.IsEnable);

        var heading = new ParagraphState(new MenuState
        {
            Affiliation = new Dictionary<string, bool> { ["h2"] = true },
        });

        Assert.IsTrue(heading.Heading2.IsChecked);
        Assert.IsTrue(heading.Heading1.IsEnable);
        Assert.IsTrue(heading.Heading6.IsEnable);
        Assert.IsTrue(heading.Paragraph.IsEnable);
        Assert.IsTrue(heading.UpgradeHeading.IsEnable);
        Assert.IsTrue(heading.DegradeHeading.IsEnable);
        Assert.IsFalse(heading.Table.IsEnable);
        Assert.IsTrue(heading.FormatIsEnable);
        Assert.IsTrue(heading.HyperlinkIsEnable);
        Assert.IsFalse(heading.ImageIsEnable);

        var code = new ParagraphState(new MenuState
        {
            IsCodeFences = true,
            Affiliation = new Dictionary<string, bool> { ["code"] = true },
        });

        Assert.IsTrue(code.CodeFences.IsChecked);
        Assert.IsTrue(code.CodeFences.IsEnable);
        Assert.IsFalse(code.Heading1.IsEnable);
        Assert.IsFalse(code.FormatIsEnable);
        Assert.IsFalse(code.HyperlinkIsEnable);
        Assert.IsFalse(code.ImageIsEnable);

        var table = new ParagraphState(new MenuState
        {
            IsTable = true,
            Affiliation = new Dictionary<string, bool> { ["p"] = true },
        });

        Assert.IsTrue(table.Table.IsChecked);
        Assert.IsTrue(table.Table.IsEnable);
        Assert.IsTrue(table.Paragraph.IsChecked);
    }

    [TestMethod]
    public void ParagraphState_CoversBlockAndInlineEnableSemanticsFromCore()
    {
        var blockState = new ParagraphState(new MenuState
        {
            IsFootnote = true,
            Affiliation = new Dictionary<string, bool>
            {
                ["blockquote"] = true,
                ["html"] = true,
                ["multiplemath"] = true,
                ["frontmatter"] = true,
                ["hr"] = true,
            },
        });

        Assert.IsTrue(blockState.QuoteBlock.IsChecked);
        Assert.IsTrue(blockState.HtmlBlock.IsChecked);
        Assert.IsTrue(blockState.MathBlock.IsChecked);
        Assert.IsTrue(blockState.Footnote.IsChecked);
        Assert.IsTrue(blockState.FrontMatter.IsChecked);
        Assert.IsTrue(blockState.HorizontalLine.IsChecked);
        Assert.IsTrue(blockState.FormatIsEnable);
        Assert.IsTrue(blockState.HyperlinkIsEnable);
        Assert.IsTrue(blockState.ImageIsEnable);

        var multiline = new ParagraphState(new MenuState
        {
            IsMultiline = true,
            Affiliation = new Dictionary<string, bool> { ["p"] = true },
        });

        Assert.IsFalse(multiline.Heading1.IsEnable);
        Assert.IsFalse(multiline.Table.IsEnable);
        Assert.IsFalse(multiline.HyperlinkIsEnable);
        Assert.IsFalse(multiline.ImageIsEnable);
        Assert.IsTrue(multiline.FormatIsEnable);
        Assert.IsTrue(multiline.CodeFences.IsEnable);
    }

    [TestMethod]
    public void ContentState_AggregatesWordCountTocAndCurrentFromCore()
    {
        var current = new TocItem
        {
            Slug = "runtime-state",
            Lvl = 2,
            Content = "Runtime State",
            IsSelected = true,
        };

        var state = new ContentState
        {
            WordCount = new WordCount { Word = 42, Character = 128 },
            Toc = [new TocItem { Slug = "intro", Lvl = 1, Content = "Intro" }, current],
            Cur = current,
        };

        Assert.AreEqual(42, state.WordCount.Word);
        Assert.AreEqual(128, state.WordCount.Character);
        Assert.AreEqual(2, state.Toc.Count);
        Assert.AreSame(current, state.Cur);
        Assert.IsTrue(state.Cur.IsSelected);
    }

    [TestMethod]
    public void ContentState_DefaultInitialization_HasEmptyCollections()
    {
        var state = new ContentState();

        Assert.AreEqual(0, state.WordCount.Word);
        Assert.AreEqual(0, state.WordCount.Character);
        Assert.IsNotNull(state.Toc);
        Assert.AreEqual(0, state.Toc.Count);
        Assert.IsNull(state.Cur);
    }
}
