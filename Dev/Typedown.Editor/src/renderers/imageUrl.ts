/**
 * 图片地址解析：正文里的图片源码 → 页面可加载的地址（docs/editor-protocol.md 第 3 节「本地图片」）。
 *
 * 页面由 https://typedown.editor 虚拟主机加载，不能直接读 file://。本地图片一律改写成宿主拦截的专用主机：
 *   https://typedown.image/<盘符>/<逐段 encodeURIComponent 的路径段>
 * 例如 C:\Users\me\图 1.png → https://typedown.image/C/Users/me/%E5%9B%BE%201.png。
 * 相对路径在页面内按文档目录（doc.load 的 basePath）解析成绝对路径，`.`、`..` 在这里消解，
 * 所以宿主收到的路径段里不会有 `.`、`..`，出现即拒绝（Typedown.Core 的 Editor/Wire/LocalImageRequest.cs）。
 * 网络图片（http/https）、data:、blob: 原样使用；UNC 路径（\\server\share）与其他协议不加载。
 */

export const LOCAL_IMAGE_ORIGIN = 'https://typedown.image';

export enum ImageSourceKind {
  /** 源码为空 */
  Empty,
  /** 浏览器直接加载的地址（网络、data:、blob:） */
  Remote,
  /** 本地文件，经宿主的专用主机加载 */
  Local,
  /** 无法加载（UNC、不支持的协议、相对路径却没有文档目录、越过盘符根目录） */
  Unsupported,
}

export type ResolvedImage =
  | { readonly kind: ImageSourceKind.Empty }
  | { readonly kind: ImageSourceKind.Remote; readonly url: string }
  | { readonly kind: ImageSourceKind.Local; readonly url: string; readonly path: string }
  | { readonly kind: ImageSourceKind.Unsupported };

const EMPTY: ResolvedImage = { kind: ImageSourceKind.Empty };
const UNSUPPORTED: ResolvedImage = { kind: ImageSourceKind.Unsupported };

/** 图片链接目标的源码形态 → 目标文本：去掉尖括号、反斜杠转义 */
export function unescapeDestination(raw: string): string {
  let s = raw.trim();
  if (s.startsWith('<') && s.endsWith('>')) s = s.slice(1, -1);
  return s.replace(/\\([!-/:-@[-`{-~])/g, '$1');
}

const decode = (s: string) => {
  try { return decodeURIComponent(s); } catch { return s; }
};

/** 绝对 Windows 路径（`C:\a\b.png` 或 `C:/a/b.png`）→ 盘符 + 规范化的路径段；越过根目录时返回 null */
function splitAbsolute(path: string): { drive: string; segments: string[] } | null {
  const m = /^([a-zA-Z]):[\\/]*(.*)$/s.exec(path);
  if (!m) return null;
  const segments: string[] = [];
  for (const seg of m[2].split(/[\\/]+/)) {
    if (!seg || seg === '.') continue;
    if (seg === '..') { if (!segments.length) return null; segments.pop(); continue; }
    segments.push(seg);
  }
  return { drive: m[1].toUpperCase(), segments };
}

export function localImageUrl(drive: string, segments: readonly string[]): string {
  return `${LOCAL_IMAGE_ORIGIN}/${drive}/${segments.map(encodeURIComponent).join('/')}`;
}

/**
 * 解析图片源码。`basePath` 是文档目录（Windows 绝对路径），为空时相对路径无法解析。
 * 本地路径里的百分号转义按 URL 解码（`my%20image.png` 指 `my image.png`，与旧编辑器经 file:// 加载时一致）。
 */
export function resolveImageSource(raw: string, basePath: string): ResolvedImage {
  const src = unescapeDestination(raw);
  if (!src) return EMPTY;
  // UNC 与长路径前缀按源码原文判断：Windows 路径通常不写转义，`\\server` 按 CommonMark 反转义会变成 `\server`（当前盘的根目录）
  const literal = raw.trim().replace(/^<([\s\S]*)>$/, '$1');
  if (/^\\\\\?\\[a-zA-Z]:\\/.test(literal)) return fromAbsolute(literal.slice(4));
  if (/^(?:\\\\|\/\/)/.test(literal)) return UNSUPPORTED;
  if (/^(?:https?:|data:image\/|blob:)/i.test(src)) return { kind: ImageSourceKind.Remote, url: src };
  let path: string;
  if (/^file:/i.test(src)) {
    // file:///C:/a.png、file://C:/a.png；file://server/share 是 UNC
    const m = /^file:\/*([a-zA-Z](?::|%3A)[\\/].*)$/is.exec(src);
    if (!m) return UNSUPPORTED;
    path = decode(m[1].split(/[?#]/)[0]);
  } else if (/^\\\\\?\\[a-zA-Z]:\\/.test(src)) {
    path = src.slice(4); // \\?\C:\… 长路径前缀
  } else if (/^(?:\\\\|\/\/)/.test(src)) {
    return UNSUPPORTED; // UNC：宿主不代为访问网络共享（避免把凭据发给文档指定的服务器）
  } else if (/^[a-zA-Z]:[\\/]/.test(src)) {
    path = decode(src);
  } else if (/^[a-zA-Z][a-zA-Z0-9+.-]+:/.test(src)) {
    return UNSUPPORTED; // 其他协议（mailto:、javascript: 等）
  } else {
    const base = splitAbsolute(basePath);
    if (!base) return UNSUPPORTED;
    const rel = decode(src.split(/[?#]/)[0]);
    // `/a.png` 按文档所在盘的根目录解析
    path = /^[\\/]/.test(rel) ? `${base.drive}:${rel}` : `${base.drive}:\\${[...base.segments, rel].join('\\')}`;
  }
  return fromAbsolute(path);
}

/** Windows 文件名里不能出现的字符（宿主同样拒绝），页面先判掉，直接显示失败占位 */
const BAD_SEGMENT = /[<>:"|?*\u0000-\u001f]/;

function fromAbsolute(path: string): ResolvedImage {
  const abs = splitAbsolute(path);
  if (!abs || !abs.segments.length || abs.segments.some(s => BAD_SEGMENT.test(s))) return UNSUPPORTED;
  return { kind: ImageSourceKind.Local, url: localImageUrl(abs.drive, abs.segments), path: `${abs.drive}:\\${abs.segments.join('\\')}` };
}

/** 直接给 `<img src>` 用的地址；不能加载时返回 null */
export function imageUrlOf(raw: string, basePath: string): string | null {
  const r = resolveImageSource(raw, basePath);
  return r.kind === ImageSourceKind.Remote || r.kind === ImageSourceKind.Local ? r.url : null;
}
