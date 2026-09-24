import { LOCAL_IMAGE_ORIGIN } from '../src/renderers/imageUrl';
import { INVISIBLE_TAGS, normalizeBlocks, wrapFlow, type Align, type Block, type Inline, type Item, type Node } from './renderTree';

/**
 * 期望侧：规范用例的期望 HTML → 语义树（renderTree.ts）。HTML 由 jsdom 的解析器解析（与浏览器相同的 HTML5 算法），
 * 不另加依赖。编辑器 HTML 块的渲染结果是真实的（消毒后的）HTML，renderActual.ts 也用这里的 htmlNodes 抽取，两侧口径一致。
 *
 * 规范渲染器输出的空白：块之间的 `\n` 不是内容（归一化时去掉）；行内的 `\n` 是软换行，与 `<br />` 一样记成 br
 * （renderTree.ts 顶部的换行约定），`<br />` 后紧跟的 `\n` 属于它自己，不另记。
 */

export function expectedTree(html: string): Block[] {
  const t = document.createElement('template');
  t.innerHTML = html;
  return normalizeBlocks(wrapFlow(htmlNodes(t.content)));
}

/** 本地图片地址（https://typedown.image/…）还原成文档里写的路径：用例装载时的文档目录是 SPEC_BASE_PATH */
export const SPEC_BASE_PATH = 'C:\\spec';
const LOCAL_BASE = `${LOCAL_IMAGE_ORIGIN}/C/spec/`;
const LOCAL_ROOT = `${LOCAL_IMAGE_ORIGIN}/C/`;
export function imageSource(url: string): string {
  const back = (rest: string) => rest.split('/').map(s => { try { return decodeURIComponent(s); } catch { return s; } }).join('/');
  if (url.startsWith(LOCAL_BASE)) return back(url.slice(LOCAL_BASE.length));
  if (url.startsWith(LOCAL_ROOT)) return '/' + back(url.slice(LOCAL_ROOT.length));
  return url;
}

/** DOM 子节点 → 语义节点（块与行内混排，由调用方决定是否 wrapFlow）；skip 是要跳过的节点（任意深度，任务框用） */
export function htmlNodes(parent: ParentNode, skip?: globalThis.Node): Node[] {
  const out: Node[] = [];
  let afterBreak = false;
  for (const child of Array.from(parent.childNodes)) {
    if (child === skip) continue;
    if (child.nodeType === 3) {
      let v = (child as Text).data;
      if (afterBreak) v = v.replace(/^\n/, '');
      afterBreak = false;
      v.split('\n').forEach((part, i) => {
        if (i) out.push({ t: 'br' });
        if (part) out.push({ t: 'text', v: part });
      });
      continue;
    }
    afterBreak = false;
    if (child.nodeType !== 1) continue; // 注释、处理指令
    const el = child as Element;
    const n = elementNode(el, () => htmlNodes(el, skip));
    if (n) out.push(n);
    afterBreak = n?.t === 'br';
  }
  return out;
}

const inlines = (nodes: Node[]) => nodes as Inline[];

/**
 * 一个 HTML 元素 → 语义节点；kids 按需求值（期望侧递归 htmlNodes，编辑器侧的行内 HTML 元素递归它自己的行内抽取）。
 * 两侧共用，标签到语义的映射只在这里。
 */
export function elementNode(el: Element, kids: () => Node[]): Node | null {
  const tag = el.localName;
  if (INVISIBLE_TAGS.has(tag)) return null;
  switch (tag) {
    case 'p': return { t: 'paragraph', c: inlines(kids()) };
    case 'h1': case 'h2': case 'h3': case 'h4': case 'h5': case 'h6': return { t: 'heading', level: Number(tag[1]), c: inlines(kids()) };
    case 'pre': {
      const code = el.firstElementChild;
      if (!code || code.localName !== 'code' || el.childNodes.length !== 1) break;
      const lang = /(?:^|\s)language-(\S+)/.exec(code.getAttribute('class') ?? '')?.[1] ?? '';
      return { t: 'code', lang, v: code.textContent ?? '' };
    }
    case 'ul': case 'ol': {
      const start = tag === 'ol' ? Number(el.getAttribute('start') ?? '1') : 1;
      const items = Array.from(el.children).filter(c => c.localName === 'li').map(listItem);
      return { t: 'list', ordered: tag === 'ol', start: Number.isFinite(start) ? start : 1, c: items };
    }
    case 'blockquote': return { t: 'quote', c: wrapFlow(kids()) };
    case 'hr': return { t: 'hr' };
    case 'table': return table(el);
    case 'em': case 'strong': case 'del': case 'mark': return { t: tag, c: inlines(kids()) };
    case 'code': return { t: 'icode', v: el.textContent ?? '' };
    case 'a': return { t: 'link', href: el.getAttribute('href'), c: inlines(kids()) };
    case 'img': return { t: 'image', src: imageSource(el.getAttribute('src') ?? ''), alt: el.getAttribute('alt') ?? '' };
    case 'br': return { t: 'br' };
  }
  return { t: 'el', tag, c: kids() };
}

/** 任务框：列表项（或其首段）开头的复选框 */
function taskBox(li: Element): HTMLInputElement | null {
  const first = (p: Element) => {
    for (const n of Array.from(p.childNodes)) {
      if (n.nodeType === 3 && !(n as Text).data.trim()) continue;
      return n;
    }
    return null;
  };
  let n = first(li);
  if (n && n.nodeType === 1 && (n as Element).localName === 'p') n = first(n as Element);
  return n && n.nodeType === 1 && (n as Element).localName === 'input' && (n as HTMLInputElement).type === 'checkbox' ? n as HTMLInputElement : null;
}

function listItem(li: Element): Item {
  const box = taskBox(li);
  return { t: 'item', task: box ? box.hasAttribute('checked') : null, c: wrapFlow(htmlNodes(li, box ?? undefined)) };
}

function table(el: Element): Block {
  const rows = Array.from(el.querySelectorAll('tr'));
  const align = (c: Element): Align => {
    const a = (c.getAttribute('align') ?? (c as HTMLElement).style?.textAlign ?? '').toLowerCase();
    return a === 'left' || a === 'center' || a === 'right' ? a : null;
  };
  const head = rows[0] ? Array.from(rows[0].children) : [];
  return {
    t: 'table',
    align: head.map(align),
    rows: rows.map(r => Array.from(r.children).filter(c => c.localName === 'td' || c.localName === 'th').map(c => inlines(htmlNodes(c)))),
  };
}
