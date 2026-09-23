/**
 * 页面自带的少量界面文字（占位、提示），按初始态的 locale 取（docs/editor-protocol.md 第 3 节「初始态」）。
 * 只分中文与英文两套：locale 以 zh 开头用中文，其余用英文。
 */

export enum UiText {
  ImageLoadFailed,
  ImageEmpty,
  LanguagePlaceholder,
  EmptyMath,
  InvalidMath,
  EmptyMermaid,
  InvalidMermaid,
  EmptyHtml,
  TocEmpty,
  FootnoteBack,
}

const ZH: Record<UiText, string> = {
  [UiText.ImageLoadFailed]: '图片加载失败',
  [UiText.ImageEmpty]: '空图片',
  [UiText.LanguagePlaceholder]: '输入语言',
  [UiText.EmptyMath]: '< 空公式 >',
  [UiText.InvalidMath]: '< 无效的公式 >',
  [UiText.EmptyMermaid]: '< 空的 Mermaid 图表 >',
  [UiText.InvalidMermaid]: '< 无效的 Mermaid 代码 >',
  [UiText.EmptyHtml]: '< 空的 HTML 块 >',
  [UiText.TocEmpty]: '目录（文档中还没有标题）',
  [UiText.FootnoteBack]: '回到引用处',
};

const EN: Record<UiText, string> = {
  [UiText.ImageLoadFailed]: 'Failed to load the image',
  [UiText.ImageEmpty]: 'Empty image',
  [UiText.LanguagePlaceholder]: 'Language',
  [UiText.EmptyMath]: '< Empty Mathematical Formula >',
  [UiText.InvalidMath]: '< Invalid Mathematical Formula >',
  [UiText.EmptyMermaid]: '< Empty Mermaid Block >',
  [UiText.InvalidMermaid]: '< Invalid Mermaid Codes >',
  [UiText.EmptyHtml]: '< Empty HTML Block >',
  [UiText.TocEmpty]: 'Table of contents (no headings yet)',
  [UiText.FootnoteBack]: 'Back to reference',
};

let table = EN;

/** 按 locale 选文字表（app.ts 读完初始态后调用一次） */
export function setUiLocale(locale: string) {
  table = /^zh/i.test(locale) ? ZH : EN;
}

export const uiText = (key: UiText) => table[key];
