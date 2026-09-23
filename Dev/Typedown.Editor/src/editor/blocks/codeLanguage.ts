import { LanguageDescription } from '@codemirror/language';
import { codeLanguages } from '../syntax/codeLanguages';

/**
 * 代码块信息串 → 语言。语言表沿用 syntax/codeLanguages.ts 的精简清单（不用 @codemirror/language-data）。
 * id 是 prism 的语言名（`language-<id>` 行类名让 prism 主题里 `.language-css .token.selector` 这类规则照常生效）。
 */

const PRISM_ID: Record<string, string> = {
  'JavaScript': 'javascript', 'TypeScript': 'typescript', 'JSX': 'jsx', 'TSX': 'tsx', 'JSON': 'json', 'CSS': 'css',
  'HTML': 'html', 'YAML': 'yaml', 'C': 'c', 'C++': 'cpp', 'C#': 'csharp', 'Java': 'java', 'Kotlin': 'kotlin',
  'Python': 'python', 'Go': 'go', 'Rust': 'rust', 'Shell': 'bash', 'PowerShell': 'powershell', 'SQL': 'sql',
  'XML': 'xml', 'Diff': 'diff', 'TOML': 'toml', 'Lua': 'lua', 'Ruby': 'ruby', 'Swift': 'swift', 'Dockerfile': 'docker',
};

/** 信息串的第一个词（``` js {1,3} 取 js） */
export const infoLanguageName = (info: string) => info.trim().split(/[\s{]/, 1)[0] ?? '';

export function findCodeLanguage(info: string): LanguageDescription | null {
  const name = infoLanguageName(info);
  return name ? LanguageDescription.matchLanguageName(codeLanguages, name, false) : null;
}

/** 行类名用的语言 id；不认识的语言为 null */
export function codeLanguageId(info: string): string | null {
  const d = findCodeLanguage(info);
  return d ? PRISM_ID[d.name] ?? null : null;
}

export interface LanguageOption {
  /** 写进信息串的名字 */
  readonly label: string;
  /** 补全列表里显示的全名 */
  readonly name: string;
  /** 可匹配的别名 */
  readonly aliases: readonly string[];
}

/** 写进信息串的名字：优先 prism 的语言名（javascript、csharp、cpp、bash），其次全名的小写，再次第一个别名 */
function preferredLabel(d: LanguageDescription): string {
  const prism = PRISM_ID[d.name];
  if (prism && d.alias.includes(prism)) return prism;
  const lower = d.name.toLowerCase();
  return d.alias.includes(lower) ? lower : d.alias[0] ?? lower;
}

/** 补全列表：清单里的语言再加 mermaid（首版图表只做 mermaid），按全名排序 */
export const languageOptions: readonly LanguageOption[] = [
  ...codeLanguages.map(d => ({ label: preferredLabel(d), name: d.name, aliases: d.alias })),
  { label: 'mermaid', name: 'Mermaid', aliases: ['mermaid'] },
].sort((a, b) => a.name.localeCompare(b.name));
