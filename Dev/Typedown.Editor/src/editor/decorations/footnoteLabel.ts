/**
 * 占位：脚注标签的规范化，规则与 GFM 的 label 匹配相同（大小写折叠、连续空白合并成一个空格、去掉首尾空白）。
 * 正式实现是块组件一侧 syntax/footnote.ts 导出的 normalizeFootnoteLabel；合并时删掉本文件，改为引用那一个。
 */
/** 先小写再大写再小写：与 commonmark.js 的 normalizeReference 相同，近似 Unicode 大小写折叠（ẞ、ς 等） */
export function normalizeFootnoteLabel(label: string): string {
  return label.trim().replace(/\s+/g, ' ').toLowerCase().toUpperCase().toLowerCase();
}
