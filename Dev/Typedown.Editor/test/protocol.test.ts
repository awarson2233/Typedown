import { describe, expect, it } from 'vitest';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import {
  COMMAND_TYPES, ENUMS, EVENT_TYPES, HOST_REQUEST_TYPES, PAGE_REQUEST_TYPES,
  type CommandMap, type CommandType, type EditorInitState, type EnumName, type EventMap, type EventType,
  type HostRequestMap, type HostRequestType, type PageRequestMap, type PageRequestType,
} from '../src/bridge/protocol';
import { decodeEnvelope } from '../src/bridge/channel';
import { normalizeInit } from '../src/app';

/**
 * 契约样例（docs/editor-protocol.md 第 11 节）：仓库根的 Tests/Protocol/ 下
 * samples/<类型名>.json（一条完整信封）、samples/<请求类型名>.res.json（它的成功应答）、enums.json、init-state.json。
 * 样例由集成分支提供；本工作区还没有时可用环境变量 TYPEDOWN_PROTOCOL_DIR 指向另一个工作区的 Tests/Protocol，都没有则整组跳过。
 *
 * 下面每个类型名一份字段表，编译期由 Schema<P> 绑到 protocol.ts 的载荷 interface（字段多写、漏写都编译不过），
 * 运行期拿它逐字段核对样例：必填字段在且类型对、可空字段缺失或为 null 均可、样例里没有表外字段、枚举值在 ENUMS 里。
 */
const root = process.env.TYPEDOWN_PROTOCOL_DIR ?? resolve(import.meta.dirname, '../../../Tests/Protocol');
const samplesDir = resolve(root, 'samples');
const has = existsSync(samplesDir);

type Spec = 'number' | 'string' | 'boolean' | { opt: Spec } | { nul: Spec } | { arr: Spec } | { en: EnumName } | { obj: Record<string, Spec> };
/** 载荷的字段表：键与载荷 interface 的键一一对应 */
type Schema<P> = { [K in keyof Required<P>]-?: Spec };

const opt = (s: Spec): Spec => ({ opt: s });
const nul = (s: Spec): Spec => ({ nul: s });
const arr = (s: Spec): Spec => ({ arr: s });
const en = (e: EnumName): Spec => ({ en: e });
const obj = (o: Record<string, Spec>): Spec => ({ obj: o });

const rect = obj({ x: 'number', y: 'number', width: 'number', height: 'number' });
const color = obj({ r: 'number', g: 'number', b: 'number', a: 'number' });
const theme = obj({ isDark: 'boolean', accent: color, background: color });
const chord = obj({ key: 'number', modifiers: 'number' });
const searchOptions = obj({ caseSensitive: 'boolean', wholeWord: 'boolean', regex: 'boolean' });
const image = obj({ src: 'string', alt: 'string', title: 'string' });
const blockContext = obj({ kinds: arr(en('BlockKind')), multipleBlocks: 'boolean', codeLike: 'boolean', codeLine: 'boolean', blockCommandsDisabled: 'boolean' });
const richSelection = obj({ block: blockContext, selectedImage: opt(image) });
const outlineItem = obj({ id: 'string', level: 'number', text: 'string' });
const settings = obj({
  sourceCode: opt('boolean'), typewriter: opt('boolean'), focusMode: opt('boolean'), searchIsCaseSensitive: opt('boolean'),
  searchIsRegexp: opt('boolean'), searchIsWholeWord: opt('boolean'), fontSize: opt('number'), lineHeight: opt('number'),
  autoPairBracket: opt('boolean'), autoPairQuote: opt('boolean'), trimUnnecessaryCodeBlockEmptyLines: opt('boolean'),
  preferLooseListItem: opt('boolean'), autoPairMarkdownSyntax: opt('boolean'), editorAreaWidth: opt('string'), tabSize: opt('number'),
  spellcheckEnabled: opt('boolean'),
});

const COMMANDS = {
  'doc.load': { version: 'number', text: 'string', basePath: 'string', selection: opt(obj({ anchor: 'number', head: 'number' })), scrollTop: opt('number') },
  'doc.importHtml': { html: 'string' },
  'history.undo': {}, 'history.redo': {}, 'history.clear': {},
  'selection.selectAll': {}, 'selection.delete': {},
  'clipboard.copy': { format: en('CopyFormat') },
  'clipboard.cut': {},
  'clipboard.paste': { format: en('PasteFormat'), text: 'string', html: 'string' },
  'format.toggle': { mark: en('InlineMark') },
  'format.clear': {},
  'block.setKind': { kind: en('BlockKind') },
  'block.promote': {}, 'block.demote': {},
  'block.insert': { position: en('ParagraphPosition') },
  'block.delete': {}, 'block.duplicate': {}, 'block.menuClosed': {},
  'table.insert': { rows: 'number', columns: 'number' },
  'table.edit': { edit: en('TableEdit') },
  'image.insert': { src: 'string', alt: opt('string'), title: opt('string') },
  'image.replace': { target: en('ImageTarget'), src: 'string', alt: opt('string'), title: opt('string') },
  'image.toolbarAction': { action: en('ImageToolbarAction') },
  'image.zoom': { percent: 'number' },
  'search.set': { query: opt('string'), options: searchOptions },
  'search.step': { direction: en('SearchDirection') },
  'search.replace': { query: opt('string'), replacement: 'string', all: 'boolean', options: searchOptions },
  'search.end': {},
  'outline.reveal': { id: 'string' },
  'view.settings': { changes: settings },
  'view.theme': { theme },
  'view.keymap': { chords: arr(chord) },
  'view.scrollTo': { x: 'number', y: 'number' },
  'view.refreshViewport': {},
} satisfies { [T in CommandType]: Schema<CommandMap[T]> };

const EVENTS = {
  'lifecycle.ready': { protocol: 'number', engine: 'string' },
  'lifecycle.fault': { message: 'string', stack: opt('string'), fatal: 'boolean' },
  'doc.changed': { baseVersion: 'number', version: 'number', changes: arr(obj({ from: 'number', to: 'number', insert: 'string' })) },
  'doc.rendered': { version: 'number' },
  'history.changed': { canUndo: 'boolean', canRedo: 'boolean' },
  'selection.changed': { hasText: 'boolean', text: 'string', rich: opt(richSelection), anchor: 'number', head: 'number' },
  'selection.marks': { marks: arr(en('InlineMark')) },
  'outline.changed': { items: arr(outlineItem), current: opt(outlineItem) },
  'stats.changed': { characters: 'number', words: 'number' },
  'search.result': { count: 'number', current: 'number' },
  'view.viewport': { viewportWidth: 'number', viewportHeight: 'number', maximumX: 'number', maximumY: 'number', scrollX: 'number', scrollY: 'number' },
  'view.shortcut': { key: 'number', modifiers: 'number' },
  'view.openLink': { uri: 'string' },
  'float.blockMenu': { anchor: opt(rect) },
  'float.formatPicker': { anchor: opt(rect) },
  'float.imageToolbar': { anchor: opt(rect) },
  'float.imageEditor': { anchor: opt(rect), image },
  'float.tableTools': { anchor: opt(rect), axis: en('TableAxis') },
  'float.tooltip': { kind: en('TooltipKind'), anchor: opt(rect) },
  'float.tooltipDismissed': {},
} satisfies { [T in EventType]: Schema<EventMap[T]> };

const exportOptions = obj({ extraHead: opt('string'), extraBody: opt('string'), header: opt('string'), footer: opt('string') });
const REQUESTS = {
  'doc.flush': [{}, obj({ version: 'number' })],
  'doc.getText': [{}, obj({ version: 'number', text: 'string' })],
  'export.renderHtml': [{ purpose: en('ExportPurpose'), title: 'string', basePath: opt('string'), options: opt(exportOptions) }, obj({ html: 'string' })],
  'selection.contextAt': [{ x: 'number', y: 'number' }, nul(richSelection)],
  'table.pickSize': [{}, nul(obj({ rows: 'number', columns: 'number' }))],
  'clipboard.write': [{ plainText: opt('string'), html: opt('string') }, obj({})],
  'image.resolve': [{ source: obj({ kind: en('ImageSourceKind'), value: 'string' }) }, nul(obj({ src: 'string' }))],
} satisfies { [T in HostRequestType]: [Schema<HostRequestMap[T][0]>, Spec] } & { [T in PageRequestType]: [Schema<PageRequestMap[T][0]>, Spec] };

const INIT = obj({ protocol: 'number', settings, theme, keymap: arr(chord), locale: 'string' });
// 初始态的字段表同样绑到 EditorInitState
const _initKeys: Schema<EditorInitState> = { protocol: 'number', settings, theme, keymap: arr(chord), locale: 'string' };
void _initKeys;

function check(v: unknown, s: Spec, path: string, errs: string[]) {
  if (typeof s === 'string') {
    if (typeof v !== s || (s === 'number' && !Number.isFinite(v))) errs.push(`${path}: 应为 ${s}，实为 ${JSON.stringify(v)}`);
  } else if ('opt' in s) {
    if (v !== undefined && v !== null) check(v, s.opt, path, errs);
  } else if ('nul' in s) {
    if (v !== null) check(v, s.nul, path, errs);
  } else if ('arr' in s) {
    if (!Array.isArray(v)) errs.push(`${path}: 应为数组`);
    else v.forEach((x, i) => check(x, s.arr, `${path}[${i}]`, errs));
  } else if ('en' in s) {
    if (!(ENUMS[s.en] as readonly unknown[]).includes(v)) errs.push(`${path}: ${JSON.stringify(v)} 不是 ${s.en} 的值`);
  } else {
    if (typeof v !== 'object' || v === null || Array.isArray(v)) { errs.push(`${path}: 应为对象`); return; }
    const o = v as Record<string, unknown>;
    for (const [k, sub] of Object.entries(s.obj)) {
      const required = typeof sub === 'string' || !('opt' in sub);
      if (!(k in o)) { if (required) errs.push(`${path}.${k}: 缺少必填字段`); continue; }
      check(o[k], sub, `${path}.${k}`, errs);
    }
    for (const k of Object.keys(o)) if (!(k in s.obj)) errs.push(`${path}.${k}: protocol.ts 里没有这个字段`);
  }
}
const errorsOf = (v: unknown, s: Spec) => { const errs: string[] = []; check(v, s, 'p', errs); return errs; };

describe.skipIf(!has)('契约样例 ↔ protocol.ts', () => {
  const read = (f: string) => JSON.parse(readFileSync(f, 'utf8')) as Record<string, unknown>;
  const files = has ? readdirSync(samplesDir).filter(f => f.endsWith('.json')) : [];
  const samples = new Map(files.map(f => [f.slice(0, -'.json'.length), read(resolve(samplesDir, f))]));
  const commands = COMMANDS as Record<string, Record<string, Spec>>;
  const events = EVENTS as Record<string, Record<string, Spec>>;
  const requests = REQUESTS as Record<string, [Record<string, Spec>, Spec]>;

  it('每个类型名都有样例，每个请求都有应答样例，没有多余的样例', () => {
    const all = [...COMMAND_TYPES, ...EVENT_TYPES, ...HOST_REQUEST_TYPES, ...PAGE_REQUEST_TYPES];
    const expected = [...all, ...HOST_REQUEST_TYPES.map(t => `${t}.res`), ...PAGE_REQUEST_TYPES.map(t => `${t}.res`)].sort();
    expect([...samples.keys()].sort()).toEqual(expected);
  });

  it('命令：信封种类、字段与枚举值', () => {
    for (const t of COMMAND_TYPES) {
      const m = samples.get(t)!;
      expect([m.k, m.t], t).toEqual(['cmd', t]);
      expect(errorsOf(m.p, obj(commands[t])), t).toEqual([]);
      expect(decodeEnvelope(JSON.stringify(m)), t).toEqual({ k: 'cmd', t, p: m.p });
    }
  });

  it('事件：信封种类、字段与枚举值', () => {
    for (const t of EVENT_TYPES) {
      const m = samples.get(t)!;
      expect([m.k, m.t], t).toEqual(['evt', t]);
      expect(errorsOf(m.p, obj(events[t])), t).toEqual([]);
    }
  });

  it('请求与应答：两个方向的请求载荷、应答载荷；应答按 id 配对', () => {
    for (const t of [...HOST_REQUEST_TYPES, ...PAGE_REQUEST_TYPES]) {
      const req = samples.get(t)!, res = samples.get(`${t}.res`)!;
      expect([req.k, req.t, typeof req.id], t).toEqual(['req', t, 'number']);
      expect(errorsOf(req.p, obj(requests[t][0])), t).toEqual([]);
      expect([res.k, res.id, res.ok], `${t}.res`).toEqual(['res', req.id, true]);
      expect(errorsOf(res.p, requests[t][1]), `${t}.res`).toEqual([]);
    }
    // 宿主发给页面的请求与宿主给页面请求的应答，都能被页面的信封解码
    for (const t of HOST_REQUEST_TYPES) expect(decodeEnvelope(JSON.stringify(samples.get(t))), t).toMatchObject({ k: 'req', t });
    for (const t of PAGE_REQUEST_TYPES) expect(decodeEnvelope(JSON.stringify(samples.get(`${t}.res`))), t).toMatchObject({ k: 'res', ok: true, p: samples.get(`${t}.res`)!.p });
  });

  it('枚举：ENUMS 与 enums.json 逐项相同（含顺序）', () => {
    expect(read(resolve(root, 'enums.json'))).toEqual(ENUMS);
  });

  it('初始态：init-state.json 符合 EditorInitState，页面原样读入', () => {
    const init = read(resolve(root, 'init-state.json'));
    expect(errorsOf(init, INIT)).toEqual([]);
    expect(normalizeInit(init as unknown as EditorInitState)).toEqual(init);
  });
});

describe('字段表自检', () => {
  it('畸形载荷能被查出来', () => {
    expect(errorsOf({ version: 1 }, obj(COMMANDS['doc.load']))).toEqual(['p.text: 缺少必填字段', 'p.basePath: 缺少必填字段']);
    expect(errorsOf({ mark: 'bold' }, obj(COMMANDS['format.toggle']))).toEqual(['p.mark: "bold" 不是 InlineMark 的值']);
    expect(errorsOf({ version: 1, text: '', basePath: '', extra: 1, scrollTop: null }, obj(COMMANDS['doc.load']))).toEqual(['p.extra: protocol.ts 里没有这个字段']);
  });
});
