import table from './emoji.json';

/**
 * `:别名:` → emoji 字符。emoji.json 由旧编辑器 Muya 的 ui/emojis/emojisJson.json 生成（只留别名到字符的映射），
 * 别名集合与旧编辑器相同；`+1`、`-1`、`t-rex` 等含 `+`、`-` 的别名不在 Lezer 的 Emoji 语法里，写了也不会被识别。
 */
const EMOJI = table as Record<string, string>;

export function emojiFor(name: string): string | undefined {
  return Object.prototype.hasOwnProperty.call(EMOJI, name) ? EMOJI[name] : undefined;
}
