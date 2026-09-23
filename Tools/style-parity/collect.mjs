// 页面端采集：在旧页面（Muya）与新页面（CM6）里各跑一遍，按源码顺序列出叶子块与行内元素的几何与计算样式。
// 函数体以 toString() 注入页面执行，不能引用模块作用域里的任何东西。
// 采到的元素留在 window.__sp.els 里，由 run.mjs 经 CDP 逐个查实际渲染字体（CSS.getPlatformFontsForNode）。

/** @param {'old' | 'new'} engine */
function collect(engine) {
  const els = [];
  const sy = window.scrollY, sx = window.scrollX;
  const px = v => parseFloat(v) || 0;
  const transparent = c => c === 'transparent' || /rgba\([^)]*,\s*0\)$/.test(c);
  // 旧编辑器里始终不可见的装饰（段落图标、语言输入框、工具条、复制按钮）；新编辑器里的 widget 缓冲与列表符号
  // figure.ag-container-block > pre 是渲染态块里藏起来的源码（0 尺寸、透明，但文字矩形仍有宽度）
  const SKIP = '.ag-hide, figure.ag-container-block:not(.ag-active) > pre, .ag-front-icon, .ag-language-input, .ag-tool-bar, .ag-container-icon, .ag-code-copy, .ag-footnote-input, .cm-widgetBuffer, .cm-td-marker, .cm-td-footnote-label, .cm-td-footnote-back, [aria-hidden="true"], .katex-mathml';

  /** 可见的文字节点（有尺寸、颜色不透明），按文档顺序；limit 个字符后停 */
  function visibleTexts(roots, limit) {
    const out = [];
    let n = 0;
    for (const root of roots) {
      const w = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
      for (let t; (t = w.nextNode());) {
        if (!t.data.trim()) continue;
        const p = t.parentElement;
        if (!p || p.closest(SKIP)) continue;
        const cs = getComputedStyle(p);
        if (transparent(cs.color) || cs.visibility === 'hidden' || px(cs.opacity) === 0 && cs.opacity !== '') continue;
        const r = document.createRange();
        r.selectNodeContents(t);
        const rc = r.getBoundingClientRect();
        if (rc.width < 1 || rc.height < 1) continue;
        out.push({ t, p, rc });
        n += t.data.replace(/\s+/g, '').length;
        if (n >= limit) return out;
      }
    }
    return out;
  }
  const textKey = roots => visibleTexts(roots, 16).map(x => x.t.data).join('').replace(/\s+/g, '').slice(0, 16);

  function textStyle(p) {
    const s = getComputedStyle(p);
    return {
      family: s.fontFamily, size: s.fontSize, weight: s.fontWeight, style: s.fontStyle, lh: s.lineHeight,
      ls: s.letterSpacing, color: s.color,
    };
  }

  /** 块的可见框：文字类取内容框（外边距、以及新编辑器里模拟外边距的内边距都不算）；框类取边框盒，但透明边框（模拟的外边距）不算 */
  function frame(el, text) {
    const r = el.getBoundingClientRect();
    const s = getComputedStyle(el);
    let top = r.top, bottom = r.bottom, left = r.left, right = r.right;
    if (text) {
      top += px(s.borderTopWidth) + px(s.paddingTop); bottom -= px(s.borderBottomWidth) + px(s.paddingBottom);
      left += px(s.borderLeftWidth) + px(s.paddingLeft); right -= px(s.borderRightWidth) + px(s.paddingRight);
    } else {
      if (transparent(s.borderTopColor)) top += px(s.borderTopWidth);
      if (transparent(s.borderBottomColor)) bottom -= px(s.borderBottomWidth);
      if (transparent(s.borderLeftColor)) left += px(s.borderLeftWidth);
      if (transparent(s.borderRightColor)) right -= px(s.borderRightWidth);
    }
    return { top: top + sy, bottom: bottom + sy, left: left + sx, right: right + sx };
  }

  const TEXT_KINDS = new Set(['paragraph', 'list-item', 'task-item', 'hr', 'image', 'link-ref']);
  const isText = type => TEXT_KINDS.has(type.replace(/^(quote>)+/, '')) || /heading-\d$/.test(type);

  const blocks = [];
  /** 语法树里有、DOM 里没渲染出来的块（CM6 视口没覆盖到），调用方据此重试 */
  let missing = 0;
  const missingAt = [];
  /** roots：构成这个块的元素（新编辑器里是若干行与块 widget）；boxEl：量框用的元素（默认 roots） */
  function push(type, roots, extra = {}) {
    const text = isText(type);
    const boxEls = extra.boxEls ?? roots;
    const fs = boxEls.map(e => frame(e, text));
    const f = { top: Math.min(...fs.map(x => x.top)), bottom: Math.max(...fs.map(x => x.bottom)), left: fs[0].left, right: fs[0].right };
    const first = visibleTexts(roots, 1)[0];
    const b = {
      type, key: textKey(roots), y: +f.top.toFixed(2), h: +(f.bottom - f.top).toFixed(2), x: +f.left.toFixed(2), w: +(f.right - f.left).toFixed(2),
      lh: text ? getComputedStyle(boxEls[0]).lineHeight : null,
    };
    // 渲染物本身（mermaid 的 svg、公式的 .katex-display、图片）：两边渲染库版本不同时，框的差 = 高度差 − 渲染物的高度差
    const inner = roots.map(r => r.querySelector('.ag-container-preview > svg, .cm-td-block-body > svg, .katex-display, .ag-image-container img, .cm-td-image img')).find(Boolean);
    if (inner) b.inner = +inner.getBoundingClientRect().height.toFixed(2);
    if (first && type.replace(/^(quote>)+/, '') !== 'hr') {
      b.textX = +(first.rc.left + sx).toFixed(2);
      b.text = textStyle(first.p);
      // 实际渲染字体按文字节点本身查：按元素查会把同一行里其他文字节点（CM6 的行元素直接含多段文字）也算进去
      b.fontEl = els.push(first.t) - 1;
    }
    blocks.push(b);
  }

  if (engine === 'old') {
    const root = document.querySelector('#ag-editor-id');
    const FIG = { TABLE: 'table', MULTIPLEMATH: 'math', MERMAID: 'mermaid', HTML: 'html', FOOTNOTE: 'footnote' };
    const onlyImage = p => !!p.querySelector('.ag-inline-image') && !textKey([p]).length;
    const visit = (el, ctx) => {
      const tag = el.tagName.toLowerCase();
      const q = 'quote>'.repeat(ctx.quote);
      if (/^h[1-6]$/.test(tag)) push(q + 'heading-' + tag[1], [el]);
      else if (tag === 'p') {
        if (el.dataset.role === 'hr') push(q + 'hr', [el]);
        else if (onlyImage(el)) push(q + 'image', [el]);
        else push(q + (ctx.item ?? 'paragraph'), [el]);
      } else if (tag === 'pre') {
        if (el.classList.contains('ag-front-matter')) push(q + 'front-matter', [el]);
        else if (el.classList.contains('ag-html-block')) push(q + 'html', [el]);
        else push(q + 'code', [el]);
      } else if (tag === 'figure') {
        const role = el.dataset.role;
        const type = FIG[role] ?? String(role).toLowerCase();
        if (type === 'table') push(q + type, [el], { boxEls: [el.querySelector('table') ?? el] });
        else push(q + type, [el]);
      } else if (tag === 'ul' || tag === 'ol') {
        for (const c of el.children) if (c.tagName === 'LI') visit(c, ctx);
      } else if (tag === 'li') {
        let item = el.classList.contains('ag-task-list-item') ? 'task-item' : 'list-item';
        for (const c of el.children) {
          if (c.tagName === 'INPUT' || c.matches(SKIP)) continue;
          visit(c, { ...ctx, item });
          item = undefined;
        }
      } else if (tag === 'blockquote') {
        for (const c of el.children) if (!c.matches(SKIP)) visit(c, { quote: ctx.quote + 1 });
      } else if (tag === 'div' && !el.matches(SKIP)) {
        for (const c of el.children) visit(c, ctx);
      }
    };
    for (const c of root.children) visit(c, { quote: 0 });
  } else {
    const view = window.typedown.view;
    const ls = view.state.values.find(v => v && v.context && v.tree && typeof v.tree.resolveInner === 'function');
    const tree = ls.tree;
    const doc = view.state.doc;
    // .cm-content 的直接子元素（行与块 widget）及其起点
    const kids = [...view.contentDOM.children].filter(e => !e.classList.contains('cm-gap')).map(el => ({ el, pos: view.posAtDOM(el, 0) }));
    const within = (from, to) => {
      const a = doc.lineAt(from).from, b = to;
      return kids.filter(k => k.pos >= a && k.pos <= b).map(k => k.el);
    };
    const LEAF = {
      Paragraph: 'paragraph', HorizontalRule: 'hr', CodeBlock: 'code', BlockMath: 'math', Table: 'table', HTMLBlock: 'html',
      CommentBlock: 'html', ProcessingInstructionBlock: 'html', Frontmatter: 'front-matter', TableOfContents: 'toc',
      FootnoteDefinition: 'footnote', LinkReference: 'link-ref', Task: 'task-item',
    };
    const visit = (n, ctx) => {
      const name = n.name;
      const q = 'quote>'.repeat(ctx.quote);
      const h = /^(?:ATX|Setext)Heading(\d)$/.exec(name);
      let type = null;
      if (h) type = 'heading-' + h[1];
      else if (name === 'Blockquote') { for (let c = n.firstChild; c; c = c.nextSibling) visit(c, { quote: ctx.quote + 1 }); return; }
      else if (name === 'BulletList' || name === 'OrderedList') { for (let c = n.firstChild; c; c = c.nextSibling) visit(c, ctx); return; }
      else if (name === 'ListItem') {
        let item = n.getChild('Task') ? 'task-item' : 'list-item';
        for (let c = n.firstChild; c; c = c.nextSibling) {
          if (c.name === 'ListMark') continue;
          visit(c, { ...ctx, item });
          item = undefined;
        }
        return;
      } else if (name === 'FencedCode') {
        const info = n.getChild('CodeInfo');
        type = info && /^mermaid$/i.test(doc.sliceString(info.from, info.to).trim()) ? 'mermaid' : 'code';
      } else if (name in LEAF) type = LEAF[name];
      if (!type) return;
      if (type === 'paragraph') {
        const kidsN = [];
        for (let c = n.firstChild; c; c = c.nextSibling) kidsN.push(c.name);
        if (kidsN.length === 1 && kidsN[0] === 'Image' && n.firstChild.from === n.from && n.firstChild.to === n.to) type = 'image';
        else if (ctx.item) type = ctx.item;
      } else if (type === 'task-item' && !ctx.item) type = 'paragraph';
      // HTML 块的节点终点带着结尾换行
      const to = n.to > n.from && doc.lineAt(n.to).from === n.to ? n.to - 1 : n.to;
      const roots = within(n.from, to);
      if (!roots.length) { missing++; missingAt.push(`${name}@${n.from}`); return; }
      if (type === 'table') {
        const t = roots[0].querySelector('.cm-td-table');
        push(q + type, roots, t ? { boxEls: [t] } : {});
        // 宽表格在外框里横向滚动，滚动条的高度也是这个块占的高度（Muya 的表格直接溢出，没有滚动条）
        const bar = roots[0].offsetHeight - roots[0].clientHeight;
        if (t && bar > 0) blocks[blocks.length - 1].h = +(blocks[blocks.length - 1].h + bar).toFixed(2);
      } else push(q + type, roots);
    };
    for (let c = tree.topNode.firstChild; c; c = c.nextSibling) visit(c, { quote: 0 });
    if (missing) missingAt.unshift(`kids ${kids.length}: ${kids.slice(0, 4).map(k => k.pos + '/' + k.el.className).join(' ')}; doc ${doc.length}; tree ${tree.length}`);
  }

  // ── 行内元素 ──
  const INLINE = engine === 'old' ? {
    strong: 'strong.ag-inline-rule', em: 'em.ag-inline-rule', del: 'del.ag-inline-rule', code: 'span code.ag-inline-rule, p code.ag-inline-rule, h1 code, h2 code, h3 code, h4 code, h5 code, h6 code',
    link: 'a.ag-inline-rule', highlight: 'mark', kbd: 'kbd', u: 'u', sub: 'sub', sup: 'sup:not(.ag-inline-footnote-identifier)',
    math: '.ag-math > .ag-math-render .katex', 'footnote-ref': 'sup.ag-inline-footnote-identifier',
  } : {
    strong: '.cm-td-strong', em: '.cm-td-em', del: '.cm-td-del', code: '.cm-td-code-inline, code.cm-td-html-inline',
    link: '.cm-td-link, a.cm-td-html-inline', highlight: '.cm-td-highlight', kbd: '.cm-content kbd', u: '.cm-content u', sub: '.cm-content sub', sup: '.cm-content sup:not(.cm-td-footnote-ref)',
    math: '.cm-td-math-inline .katex', 'footnote-ref': '.cm-td-footnote-ref',
  };
  const scope = engine === 'old' ? document.querySelector('#ag-editor-id') : window.typedown.view.contentDOM;
  const inlines = [];
  for (const [kind, sel] of Object.entries(INLINE)) {
    const list = [...scope.querySelectorAll(sel)];
    for (const el of list) {
      // CM6 把同一个标记拆成相邻的几段时只取第一段
      if (engine === 'new' && el.parentElement?.closest(sel.split(',')[0])) continue;
      if (el.closest('.ag-hide, .cm-td-fence-line, pre, .cm-td-block-body, .cm-td-footnote-label') && kind !== 'math') continue;
      const r = el.getBoundingClientRect();
      if (r.width < 1 || r.height < 1) continue;
      const s = getComputedStyle(el);
      const firstText = visibleTexts([el], 1)[0];
      inlines.push({
        // 脚注引用：Muya 的 sup 里带着隐藏的 `[^` 与 `]`
        kind, key: (el.textContent || '').replace(/\s+/g, '').replace(kind === 'footnote-ref' ? /^\[\^|\]$/g : /$^/, '').slice(0, 16), y: +(r.top + sy).toFixed(2), h: +r.height.toFixed(2),
        // 颜色取看得见的文字：Muya 的脚注引用把编号放在 sup 里的 <a> 中，sup 本身的颜色不是编号的颜色
        family: s.fontFamily, size: s.fontSize, weight: s.fontWeight, style: s.fontStyle,
        color: kind === 'footnote-ref' && firstText ? getComputedStyle(firstText.p).color : s.color, bg: s.backgroundColor,
        deco: s.textDecorationLine, pad: s.padding, radius: s.borderRadius, ls: s.letterSpacing, va: s.verticalAlign,
        fontEl: els.push(firstText?.t ?? el) - 1,
      });
    }
  }

  window.__sp = { els };
  const rootEl = engine === 'old' ? document.querySelector('#ag-editor-id') : document.getElementById('root');
  const rr = rootEl.getBoundingClientRect();
  return { blocks, inlines, missing, missingAt, root: { x: rr.left + sx, y: rr.top + sy, w: rr.width }, bg: getComputedStyle(document.body).backgroundColor, scrollHeight: document.documentElement.scrollHeight };
}

export const COLLECT_SOURCE = collect.toString();
