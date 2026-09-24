import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

/**
 * 官方规范用例作为语料：
 * - CommonMark 0.31.2：devDependency commonmark-spec（CC-BY-SA-4.0，只作测试数据，不进产物）
 * - GFM 0.29：test/fixtures/gfm-spec.txt，取自 github/cmark-gfm 的 test/spec.txt（CC-BY-SA-4.0），
 *   包含 CommonMark 0.29 的全部用例与表格、任务列表、删除线、扩展自动链接、tagfilter 等扩展用例
 */
export interface SpecExample {
  spec: 'commonmark' | 'gfm';
  number: number;
  section: string;
  /** GFM 扩展用例的标记（table / strikethrough / tasklist / autolink / tagfilter），普通用例为空 */
  extension: string;
  markdown: string;
  /** 参照渲染器给出的期望 HTML（渲染对照测试用，见 render.test.ts） */
  html: string;
}

function extract(text: string, spec: SpecExample['spec']): SpecExample[] {
  const out: SpecExample[] = [];
  let section = '';
  let n = 0;
  const body = text.replace(/\r\n?/g, '\n').replace(/^<!-- END TESTS -->(.|[\n])*/m, '');
  const re = /^`{32} example([^\n]*)\n([\s\S]*?)^\.\n([\s\S]*?)^`{32}$|^#{1,6} *(.*)$/gm;
  for (let m; (m = re.exec(body));) {
    if (m[4] !== undefined) { section = m[4]; continue; }
    out.push({ spec, number: ++n, section, extension: m[1].trim(), markdown: m[2].replace(/→/g, '\t'), html: m[3].replace(/→/g, '\t') });
  }
  return out;
}

export const commonmarkExamples = extract(readFileSync(resolve(import.meta.dirname, '../node_modules/commonmark-spec/spec.txt'), 'utf8'), 'commonmark');
export const gfmExamples = extract(readFileSync(resolve(import.meta.dirname, 'fixtures/gfm-spec.txt'), 'utf8'), 'gfm');
export const allExamples = [...commonmarkExamples, ...gfmExamples];
export const label = (e: SpecExample) => `${e.spec} #${e.number}${e.extension ? ` (${e.extension})` : ''} ${e.section}`;
