using System;
using System.Linq;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorParagraphState
    {
        public EditorMenuState MenuState { get; init; } = new();

        public EditorMenuItemState Heading1 { get; init; } = new();

        public EditorMenuItemState Heading2 { get; init; } = new();

        public EditorMenuItemState Heading3 { get; init; } = new();

        public EditorMenuItemState Heading4 { get; init; } = new();

        public EditorMenuItemState Heading5 { get; init; } = new();

        public EditorMenuItemState Heading6 { get; init; } = new();

        public EditorMenuItemState Paragraph { get; init; } = new();

        public EditorMenuItemState UpgradeHeading { get; init; } = new();

        public EditorMenuItemState DegradeHeading { get; init; } = new();

        public EditorMenuItemState Table { get; init; } = new();

        public EditorMenuItemState CodeFences { get; init; } = new();

        public EditorMenuItemState HtmlBlock { get; init; } = new();

        public EditorMenuItemState MathBlock { get; init; } = new();

        public EditorMenuItemState QuoteBlock { get; init; } = new();

        public EditorMenuItemState OrderList { get; init; } = new();

        public EditorMenuItemState BulletList { get; init; } = new();

        public EditorMenuItemState TaskList { get; init; } = new();

        public EditorMenuItemState Chart { get; init; } = new();

        public EditorMenuItemState LinkReference { get; init; } = new();

        public EditorMenuItemState Footnote { get; init; } = new();

        public EditorMenuItemState HorizontalLine { get; init; } = new();

        public EditorMenuItemState Toc { get; init; } = new();

        public EditorMenuItemState FrontMatter { get; init; } = new();

        public bool FormatIsEnable { get; set; }

        public bool HyperlinkIsEnable { get; set; }

        public bool ImageIsEnable { get; set; }

        public static EditorParagraphState FromMenuState(EditorMenuState menuState)
        {
            var state = new EditorParagraphState { MenuState = menuState };
            state.UpdateCheckedMenuItem();
            state.UpdateEnableMenuItem();
            return state;
        }

        private void UpdateCheckedMenuItem()
        {
            var affiliation = MenuState.Affiliation;
            TaskList.IsChecked = affiliation.ContainsKey("ul") && MenuState.IsTaskList;
            Table.IsChecked = MenuState.IsTable;
            CodeFences.IsChecked = MenuState.IsCodeFences && affiliation.Keys.Any(key => key.Contains("code", StringComparison.Ordinal));
            Heading1.IsChecked = affiliation.ContainsKey("h1");
            Heading2.IsChecked = affiliation.ContainsKey("h2");
            Heading3.IsChecked = affiliation.ContainsKey("h3");
            Heading4.IsChecked = affiliation.ContainsKey("h4");
            Heading5.IsChecked = affiliation.ContainsKey("h5");
            Heading6.IsChecked = affiliation.ContainsKey("h6");
            HtmlBlock.IsChecked = affiliation.ContainsKey("html");
            MathBlock.IsChecked = affiliation.ContainsKey("multiplemath");
            QuoteBlock.IsChecked = affiliation.ContainsKey("blockquote");
            OrderList.IsChecked = affiliation.ContainsKey("ol");
            BulletList.IsChecked = affiliation.ContainsKey("ul") && !MenuState.IsTaskList;
            Paragraph.IsChecked = affiliation.ContainsKey("p");
            HorizontalLine.IsChecked = affiliation.ContainsKey("hr");
            FrontMatter.IsChecked = affiliation.ContainsKey("frontmatter");
            Footnote.IsChecked = MenuState.IsFootnote;
            Chart.IsChecked = (MenuState.IsCodeContent || MenuState.IsCodeFences) && !CodeFences.IsChecked && !MathBlock.IsChecked && !HtmlBlock.IsChecked;
            Toc.IsChecked = false;
            LinkReference.IsChecked = false;
        }

        private void UpdateEnableMenuItem()
        {
            if (MenuState.IsDisabled)
            {
                ResetEnableMenuItem(false);
                FormatIsEnable = true;
                HyperlinkIsEnable = true;
                ImageIsEnable = true;
            }
            else if (MenuState.IsCodeContent || MenuState.IsCodeFences)
            {
                ResetEnableMenuItem(false);
                CodeFences.IsEnable = CodeFences.IsChecked;
            }
            else if (MenuState.IsMultiline)
            {
                ResetEnableMenuItem(true);
                Heading1.IsEnable = false;
                Heading2.IsEnable = false;
                Heading3.IsEnable = false;
                Heading4.IsEnable = false;
                Heading5.IsEnable = false;
                Heading6.IsEnable = false;
                UpgradeHeading.IsEnable = false;
                DegradeHeading.IsEnable = false;
                Table.IsEnable = false;
                HyperlinkIsEnable = false;
                ImageIsEnable = false;
            }
            else if (Heading1.IsChecked || Heading2.IsChecked || Heading3.IsChecked || Heading4.IsChecked || Heading5.IsChecked || Heading6.IsChecked)
            {
                ResetEnableMenuItem(false);
                Heading1.IsEnable = true;
                Heading2.IsEnable = true;
                Heading3.IsEnable = true;
                Heading4.IsEnable = true;
                Heading5.IsEnable = true;
                Heading6.IsEnable = true;
                Paragraph.IsEnable = true;
                UpgradeHeading.IsEnable = !Heading1.IsChecked;
                DegradeHeading.IsEnable = true;
                FormatIsEnable = true;
                HyperlinkIsEnable = true;
            }
            else
            {
                ResetEnableMenuItem(true);
                DegradeHeading.IsEnable = !Paragraph.IsChecked;
                FormatIsEnable = true;
                HyperlinkIsEnable = true;
                ImageIsEnable = true;
            }
        }

        private void ResetEnableMenuItem(bool isEnable)
        {
            Heading1.IsEnable = isEnable;
            Heading2.IsEnable = isEnable;
            Heading3.IsEnable = isEnable;
            Heading4.IsEnable = isEnable;
            Heading5.IsEnable = isEnable;
            Heading6.IsEnable = isEnable;
            Paragraph.IsEnable = isEnable;
            UpgradeHeading.IsEnable = isEnable;
            DegradeHeading.IsEnable = isEnable;
            Table.IsEnable = isEnable;
            CodeFences.IsEnable = isEnable;
            HtmlBlock.IsEnable = isEnable;
            MathBlock.IsEnable = isEnable;
            QuoteBlock.IsEnable = isEnable;
            OrderList.IsEnable = isEnable;
            BulletList.IsEnable = isEnable;
            TaskList.IsEnable = isEnable;
            Chart.IsEnable = isEnable;
            LinkReference.IsEnable = isEnable;
            Footnote.IsEnable = isEnable;
            HorizontalLine.IsEnable = isEnable;
            Toc.IsEnable = isEnable;
            FrontMatter.IsEnable = isEnable;
            FormatIsEnable = isEnable;
            HyperlinkIsEnable = isEnable;
            ImageIsEnable = isEnable;
        }
    }
}
