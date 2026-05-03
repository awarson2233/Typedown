using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Models;

namespace Typedown.ArchitectureTests;

[TestClass]
public class Phase13RuntimeStateContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

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
    public void RuntimeContracts_StayPlatformNeutralAndAvoidLegacyDependencies()
    {
        var runtimeFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Models", "RuntimeModels"), "*.cs", SearchOption.TopDirectoryOnly)
            .ToArray();

        var runtimeFileNames = runtimeFiles.Select(Path.GetFileName).ToArray();
        CollectionAssert.IsSubsetOf(
            new[]
            {
                "ContentState.cs",
                "FormatState.cs",
                "MenuItemState.cs",
                "MenuState.cs",
                "ParagraphState.cs",
                "TocItem.cs",
                "WordCount.cs",
            },
            runtimeFileNames);

        foreach (var file in runtimeFiles)
        {
            var source = File.ReadAllText(file);

            AssertNoTypeReference(source, "Microsoft.UI");
            AssertNoTypeReference(source, "Microsoft.UI.Xaml");
            AssertNoTypeReference(source, "Windows.UI.Xaml");
            AssertNoTypeReference(source, "Microsoft.Web.WebView2");
            AssertNoTypeReference(source, "Windows.Storage.Pickers");
            AssertNoTypeReference(source, "Typedown.WinUI");
            AssertNoTypeReference(source, "Typedown.XamlUI");
            AssertNoTypeReference(source, "Typedown.Core.Legacy");
        }
    }

    [TestMethod]
    public void Presentation_DoesNotCarryDuplicateRuntimeStateModels()
    {
        var presentationRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation");

        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "Models", "EditorRuntimeState.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "ViewModels", "EditorRuntimeViewModel.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(presentationRoot, "ViewModels", "EditorTocNodeViewModel.cs")));
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
