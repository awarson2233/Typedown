using Microsoft.UI.Xaml;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Controls;

public sealed partial class ParagraphItem : MenuBarItemBase
{
    public ParagraphItem() : base("Paragraph")
    {
        InitializeComponent();
    }

    protected override void ConfigureCommands(AppViewModel? viewModel)
    {
        var paragraph = viewModel?.ParagraphViewModel;
        var settings = viewModel?.SettingsViewModel;

        SetCommand(Heading1Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading2Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading3Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading4Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading5Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading6Item, paragraph?.UpdateParagraphCommand);
        SetCommand(ItemParagraphItem, paragraph?.UpdateParagraphCommand);
        SetCommand(IncreaseHeadingLevelItem, paragraph?.UpdateParagraphCommand);
        SetCommand(DecreaseHeadingLevelItem, paragraph?.UpdateParagraphCommand);
        SetCommand(TableItem, paragraph?.InsertTableCommand);
        SetCommand(CodeFencesItem, paragraph?.UpdateParagraphCommand);
        SetCommand(MathBlockItem, paragraph?.UpdateParagraphCommand);
        SetCommand(QuoteItem, paragraph?.UpdateParagraphCommand);
        SetCommand(OrderedListItem, paragraph?.UpdateParagraphCommand);
        SetCommand(UnorderedListItem, paragraph?.UpdateParagraphCommand);
        SetCommand(TaskListItem, paragraph?.UpdateParagraphCommand);
        SetCommand(InsertParagraphBeforeItem, paragraph?.InsertParagraphCommand);
        SetCommand(InsertParagraphAfterItem, paragraph?.InsertParagraphCommand);
        SetCommand(VegaChartItem, paragraph?.UpdateParagraphCommand);
        SetCommand(FlowChartItem, paragraph?.UpdateParagraphCommand);
        SetCommand(SequenceDiagramItem, paragraph?.UpdateParagraphCommand);
        SetCommand(PlantUMLDiagramItem, paragraph?.UpdateParagraphCommand);
        SetCommand(MermaidItem, paragraph?.UpdateParagraphCommand);
        SetCommand(FootNoteItem, paragraph?.UpdateParagraphCommand);
        SetCommand(HorizontalLineItem, paragraph?.UpdateParagraphCommand);
        SetCommand(YAMLFrontMatterItem, paragraph?.UpdateParagraphCommand);

        SetShortcut(Heading1Item, settings?.ShortcutHeading1);
        SetShortcut(Heading2Item, settings?.ShortcutHeading2);
        SetShortcut(Heading3Item, settings?.ShortcutHeading3);
        SetShortcut(Heading4Item, settings?.ShortcutHeading4);
        SetShortcut(Heading5Item, settings?.ShortcutHeading5);
        SetShortcut(Heading6Item, settings?.ShortcutHeading6);
        SetShortcut(ItemParagraphItem, settings?.ShortcutParagraph);
        SetShortcut(IncreaseHeadingLevelItem, settings?.ShortcutIncreaseHeadingLevel);
        SetShortcut(DecreaseHeadingLevelItem, settings?.ShortcutDecreaseHeadingLevel);
        SetShortcut(TableItem, settings?.ShortcutTable);
        SetShortcut(CodeFencesItem, settings?.ShortcutCodeFences);
        SetShortcut(MathBlockItem, settings?.ShortcutMathBlock);
        SetShortcut(QuoteItem, settings?.ShortcutQuote);
        SetShortcut(OrderedListItem, settings?.ShortcutOrderedList);
        SetShortcut(UnorderedListItem, settings?.ShortcutUnorderedList);
        SetShortcut(TaskListItem, settings?.ShortcutTaskList);
        SetShortcut(InsertParagraphBeforeItem, settings?.ShortcutInsertParagraphBefore);
        SetShortcut(InsertParagraphAfterItem, settings?.ShortcutInsertParagraphAfter);
        SetShortcut(VegaChartItem, settings?.ShortcutVegaChart);
        SetShortcut(FlowChartItem, settings?.ShortcutFlowChart);
        SetShortcut(SequenceDiagramItem, settings?.ShortcutSequenceDiagram);
        SetShortcut(PlantUMLDiagramItem, settings?.ShortcutPlantUMLDiagram);
        SetShortcut(MermaidItem, settings?.ShortcutMermaid);
        SetShortcut(FootNoteItem, settings?.ShortcutFootNote);
        SetShortcut(HorizontalLineItem, settings?.ShortcutHorizontalLine);
        SetShortcut(YAMLFrontMatterItem, settings?.ShortcutYAMLFrontMatter);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ReleaseMenu();
    }
}
