using System.Linq;
using Typedown.Core.Editor;

namespace Typedown.Core.Models
{
    /// <summary>段落菜单各项的勾选与可用状态，由光标所在块的 <see cref="BlockContext"/> 投影而来。</summary>
    public class ParagraphState
    {
        public BlockContext Block { get; }

        public ParagraphState() : this(BlockContext.Empty)
        {
        }

        public ParagraphState(BlockContext block)
        {
            Block = block;
            UpdateCheckedMenuItem();
            UpdateEnableMenuItem();
        }

        private bool Is(BlockKind kind) => Block.Kinds.Contains(kind);

        private void UpdateCheckedMenuItem()
        {
            TaskList.IsChecked = Is(BlockKind.TaskList);
            Table.IsChecked = Is(BlockKind.Table);
            CodeFences.IsChecked = Is(BlockKind.CodeBlock);
            Heading1.IsChecked = Is(BlockKind.Heading1);
            Heading2.IsChecked = Is(BlockKind.Heading2);
            Heading3.IsChecked = Is(BlockKind.Heading3);
            Heading4.IsChecked = Is(BlockKind.Heading4);
            Heading5.IsChecked = Is(BlockKind.Heading5);
            Heading6.IsChecked = Is(BlockKind.Heading6);
            HtmlBlock.IsChecked = Is(BlockKind.HtmlBlock);
            MathBlock.IsChecked = Is(BlockKind.MathBlock);
            QuoteBlock.IsChecked = Is(BlockKind.Quote);
            OrderList.IsChecked = Is(BlockKind.OrderedList);
            BulletList.IsChecked = Is(BlockKind.BulletList);
            Paragraph.IsChecked = Is(BlockKind.Paragraph);
            HorizontalLine.IsChecked = Is(BlockKind.HorizontalRule);
            FrontMatter.IsChecked = Is(BlockKind.FrontMatter);
            Footnote.IsChecked = Is(BlockKind.Footnote);
            Chart.IsChecked = (Block.CodeLine || Block.CodeLike) && !CodeFences.IsChecked && !MathBlock.IsChecked && !HtmlBlock.IsChecked;
            Toc.IsChecked = false;
            LinkReference.IsChecked = false;
        }

        private void UpdateEnableMenuItem()
        {
            if (Block.BlockCommandsDisabled)
            {
                ResetEnableMenuItem(false);
                FormatIsEnable = true;
                HyperlinkIsEnable = true;
                ImageIsEnable = true;
            }
            else if (Block.CodeLine || Block.CodeLike)
            {
                ResetEnableMenuItem(false);
                CodeFences.IsEnable = CodeFences.IsChecked;
            }
            else if (Block.MultipleBlocks)
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

        private void ResetEnableMenuItem(bool IsEnable)
        {
            Heading1.IsEnable = IsEnable;
            Heading2.IsEnable = IsEnable;
            Heading3.IsEnable = IsEnable;
            Heading4.IsEnable = IsEnable;
            Heading5.IsEnable = IsEnable;
            Heading6.IsEnable = IsEnable;
            Paragraph.IsEnable = IsEnable;
            UpgradeHeading.IsEnable = IsEnable;
            DegradeHeading.IsEnable = IsEnable;
            Table.IsEnable = IsEnable;
            CodeFences.IsEnable = IsEnable;
            HtmlBlock.IsEnable = IsEnable;
            MathBlock.IsEnable = IsEnable;
            QuoteBlock.IsEnable = IsEnable;
            OrderList.IsEnable = IsEnable;
            BulletList.IsEnable = IsEnable;
            TaskList.IsEnable = IsEnable;
            Chart.IsEnable = IsEnable;
            LinkReference.IsEnable = IsEnable;
            Footnote.IsEnable = IsEnable;
            HorizontalLine.IsEnable = IsEnable;
            Toc.IsEnable = IsEnable;
            FrontMatter.IsEnable = IsEnable;
            FormatIsEnable = IsEnable;
            HyperlinkIsEnable = IsEnable;
            ImageIsEnable = IsEnable;
        }

        public MenuItemState Heading1 { get; set; } = new();
        public MenuItemState Heading2 { get; set; } = new();
        public MenuItemState Heading3 { get; set; } = new();
        public MenuItemState Heading4 { get; set; } = new();
        public MenuItemState Heading5 { get; set; } = new();
        public MenuItemState Heading6 { get; set; } = new();
        public MenuItemState Paragraph { get; set; } = new();
        public MenuItemState UpgradeHeading { get; set; } = new();
        public MenuItemState DegradeHeading { get; set; } = new();
        public MenuItemState Table { get; set; } = new();
        public MenuItemState CodeFences { get; set; } = new();
        public MenuItemState HtmlBlock { get; set; } = new();
        public MenuItemState MathBlock { get; set; } = new();
        public MenuItemState QuoteBlock { get; set; } = new();
        public MenuItemState OrderList { get; set; } = new();
        public MenuItemState BulletList { get; set; } = new();
        public MenuItemState TaskList { get; set; } = new();
        public MenuItemState Chart { get; set; } = new();
        public MenuItemState LinkReference { get; set; } = new();
        public MenuItemState Footnote { get; set; } = new();
        public MenuItemState HorizontalLine { get; set; } = new();
        public MenuItemState Toc { get; set; } = new();
        public MenuItemState FrontMatter { get; set; } = new();

        public bool FormatIsEnable { get; set; }
        public bool HyperlinkIsEnable { get; set; }
        public bool ImageIsEnable { get; set; }
    }
}
