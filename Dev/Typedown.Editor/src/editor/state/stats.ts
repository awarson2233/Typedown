import type { Text } from '@codemirror/state';

/**
 * 字数统计，口径与旧编辑器（Muya utils.wordCount）相同：
 * - 每个汉字（U+4E00–U+9FA5）算一个词、一个字符；
 * - 其余文字按空白切分成词（汉字不参与切分：`ab中cd` 是一个汉字加一个词 `abcd`），字符数是这些词的长度之和；
 * - 字符数不含空白。长度按 UTF-16 码元计。
 * 直接遍历正文分块，不拼整篇字符串。
 */
export interface Stats { characters: number; words: number }

const isSpace = (c: number) =>
  c === 32 || (c >= 9 && c <= 13) || c === 0xa0 || c === 0x1680 || (c >= 0x2000 && c <= 0x200a) ||
  c === 0x2028 || c === 0x2029 || c === 0x202f || c === 0x205f || c === 0x3000 || c === 0xfeff;

export function countStats(doc: Text): Stats {
  let characters = 0, words = 0, inWord = false;
  for (const it = doc.iter(); !it.next().done;) {
    const s = it.value;
    for (let i = 0; i < s.length; i++) {
      const c = s.charCodeAt(i);
      if (c >= 0x4e00 && c <= 0x9fa5) { characters++; words++; continue; }
      if (isSpace(c)) { inWord = false; continue; }
      characters++;
      if (!inWord) { inWord = true; words++; }
    }
  }
  return { characters, words };
}
