using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Contracts.EditorRuntime;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13RuntimeStateContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [TestMethod]
    public void FormatState_MapsLegacySelectionFormats()
    {
        var state = EditorFormatState.FromSelectionFormats(
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
    public void ParagraphState_PreservesTaskListListHeadingCodeAndTableSemantics()
    {
        var taskList = EditorParagraphState.FromMenuState(new EditorMenuState
        {
            IsTaskList = true,
            Affiliation = new Dictionary<string, bool> { ["ul"] = true },
        });

        Assert.IsTrue(taskList.TaskList.IsChecked);
        Assert.IsFalse(taskList.BulletList.IsChecked);
        Assert.IsTrue(taskList.TaskList.IsEnable);
        Assert.IsTrue(taskList.ImageIsEnable);

        var bulletList = EditorParagraphState.FromMenuState(new EditorMenuState
        {
            Affiliation = new Dictionary<string, bool> { ["ul"] = true },
        });

        Assert.IsTrue(bulletList.BulletList.IsChecked);
        Assert.IsFalse(bulletList.TaskList.IsChecked);
        Assert.IsTrue(bulletList.OrderList.IsEnable);

        var heading = EditorParagraphState.FromMenuState(new EditorMenuState
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

        var code = EditorParagraphState.FromMenuState(new EditorMenuState
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

        var table = EditorParagraphState.FromMenuState(new EditorMenuState
        {
            IsTable = true,
            Affiliation = new Dictionary<string, bool> { ["p"] = true },
        });

        Assert.IsTrue(table.Table.IsChecked);
        Assert.IsTrue(table.Table.IsEnable);
        Assert.IsTrue(table.Paragraph.IsChecked);
    }

    [TestMethod]
    public void ParagraphState_CoversBlockAndInlineEnableSemantics()
    {
        var blockState = EditorParagraphState.FromMenuState(new EditorMenuState
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

        var multiline = EditorParagraphState.FromMenuState(new EditorMenuState
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
    public void ContentState_AggregatesWordCountTocAndCurrent()
    {
        var current = new EditorTocItem
        {
            Slug = "runtime-state",
            Level = 2,
            Content = "Runtime State",
            IsSelected = true,
        };

        var state = new EditorContentState
        {
            WordCount = new EditorWordCount { Word = 42, Character = 128 },
            Toc = [new EditorTocItem { Slug = "intro", Level = 1, Content = "Intro" }, current],
            Current = current,
        };

        Assert.AreEqual(42, state.WordCount.Word);
        Assert.AreEqual(128, state.WordCount.Character);
        Assert.AreEqual(2, state.Toc.Count);
        Assert.AreSame(current, state.Current);
        Assert.IsTrue(state.Current.IsSelected);
    }

    [TestMethod]
    public void RuntimeContracts_StayPlatformNeutralAndAvoidLegacyDependencies()
    {
        var runtimeFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.Core.Contracts", "EditorRuntime"), "*.cs", SearchOption.TopDirectoryOnly)
            .ToArray();

        Assert.IsTrue(runtimeFiles.Length >= 7, "Expected Phase 13 runtime UI state contracts.");

        foreach (var file in runtimeFiles)
        {
            var source = File.ReadAllText(file);

            AssertNoTypeReference(source, "Microsoft.UI");
            AssertNoTypeReference(source, "Windows.UI.Xaml");
            AssertNoTypeReference(source, "Typedown.WinUI");
            AssertNoTypeReference(source, "Typedown.XamlUI");
            AssertNoTypeReference(source, "Typedown.Core.Models");
            AssertNoTypeReference(source, "PropertyChanged");
            AssertNoTypeReference(source, "ObservableCollection");
        }
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

        throw new InvalidOperationException("Could not locate Typedown.sln from test output directory.");
    }

    private static void AssertNoTypeReference(string source, string text)
    {
        Assert.IsFalse(source.Contains(text, StringComparison.Ordinal), $"Unexpected reference: {text}");
    }
}
