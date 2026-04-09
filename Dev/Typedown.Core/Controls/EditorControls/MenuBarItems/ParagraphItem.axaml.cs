using PropertyChanged;
﻿using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;

namespace Typedown.Core.Controls.EditorControls.MenuBarItems
{
[DoNotNotify]
        public sealed partial class ParagraphItem : MenuBarItemBase
    {
        public ParagraphViewModel Paragraph => ViewModel?.ParagraphViewModel;

        public EditorViewModel Editor => ViewModel?.EditorViewModel;

        public ParagraphItem()
        {
            InitializeComponent();
            Unloaded += OnUnloaded;
        }

        protected override void OnRegisterShortcut()
        {
            RegisterEditorShortcut(Settings.ShortcutHeading1, this.FindControl<MenuItem>("Heading1Item"));
            RegisterEditorShortcut(Settings.ShortcutHeading2, this.FindControl<MenuItem>("Heading2Item"));
            RegisterEditorShortcut(Settings.ShortcutHeading3, this.FindControl<MenuItem>("Heading3Item"));
            RegisterEditorShortcut(Settings.ShortcutHeading4, this.FindControl<MenuItem>("Heading4Item"));
            RegisterEditorShortcut(Settings.ShortcutHeading5, this.FindControl<MenuItem>("Heading5Item"));
            RegisterEditorShortcut(Settings.ShortcutHeading6, this.FindControl<MenuItem>("Heading6Item"));
            RegisterEditorShortcut(Settings.ShortcutParagraph, this.FindControl<MenuItem>("ItemParagraphItem"));
            RegisterEditorShortcut(Settings.ShortcutIncreaseHeadingLevel, this.FindControl<MenuItem>("IncreaseHeadingLevelItem"));
            RegisterEditorShortcut(Settings.ShortcutDecreaseHeadingLevel, this.FindControl<MenuItem>("DecreaseHeadingLevelItem"));
            RegisterEditorShortcut(Settings.ShortcutTable, this.FindControl<MenuItem>("TableItem"));
            RegisterEditorShortcut(Settings.ShortcutCodeFences, this.FindControl<MenuItem>("CodeFencesItem"));
            RegisterEditorShortcut(Settings.ShortcutMathBlock, this.FindControl<MenuItem>("MathBlockItem"));
            RegisterEditorShortcut(Settings.ShortcutQuote, this.FindControl<MenuItem>("QuoteItem"));
            RegisterEditorShortcut(Settings.ShortcutOrderedList, this.FindControl<MenuItem>("OrderedListItem"));
            RegisterEditorShortcut(Settings.ShortcutUnorderedList, this.FindControl<MenuItem>("UnorderedListItem"));
            RegisterEditorShortcut(Settings.ShortcutTaskList, this.FindControl<MenuItem>("TaskListItem"));
            RegisterEditorShortcut(Settings.ShortcutInsertParagraphBefore, this.FindControl<MenuItem>("InsertParagraphBeforeItem"));
            RegisterEditorShortcut(Settings.ShortcutInsertParagraphAfter, this.FindControl<MenuItem>("InsertParagraphAfterItem"));
            RegisterEditorShortcut(Settings.ShortcutVegaChart, this.FindControl<MenuItem>("VegaChartItem"));
            RegisterEditorShortcut(Settings.ShortcutFlowChart, this.FindControl<MenuItem>("FlowChartItem"));
            RegisterEditorShortcut(Settings.ShortcutSequenceDiagram, this.FindControl<MenuItem>("SequenceDiagramItem"));
            RegisterEditorShortcut(Settings.ShortcutPlantUMLDiagram, this.FindControl<MenuItem>("PlantUMLDiagramItem"));
            RegisterEditorShortcut(Settings.ShortcutMermaid, this.FindControl<MenuItem>("MermaidItem"));
            //RegisterEditorShortcut(Settings.ShortcutLinkReferences, this.FindControl<MenuItem>("LinkReferencesItem"));
            RegisterEditorShortcut(Settings.ShortcutFootNote, this.FindControl<MenuItem>("FootNoteItem"));
            RegisterEditorShortcut(Settings.ShortcutHorizontalLine, this.FindControl<MenuItem>("HorizontalLineItem"));
            //RegisterEditorShortcut(Settings.ShortcutToc, this.FindControl<MenuItem>("TocItem"));
            RegisterEditorShortcut(Settings.ShortcutYAMLFrontMatter, this.FindControl<MenuItem>("YAMLFrontMatterItem"));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
