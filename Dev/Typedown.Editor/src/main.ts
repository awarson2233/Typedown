import './styles/editor.css';
import { startEditorApp } from './app';
import { FakeHost, webviewTransport } from './bridge/channel';
import type { EditorInitState } from './bridge/protocol';

/**
 * 宿主加载的入口（https://typedown.editor/index.html，开发时 http://localhost:3000）。
 * 在 WebView2 里经 chrome.webview 与 WebViewEditorSession 对话；普通浏览器里没有 chrome.webview，
 * 换成假宿主并装一篇空文档，页面照样能打开调试（假宿主挂在 window.__typedownFakeHost 上，可在控制台发命令）。
 */
declare global {
  interface Window { __typedownInit?: Partial<EditorInitState> }
}

function browserFallback(): FakeHost {
  const host = new FakeHost();
  host.onMessage(m => { if (m.k === 'evt' && m.t === 'lifecycle.ready') host.command('doc.load', { version: 1, text: '', basePath: '' }); });
  (window as unknown as { __typedownFakeHost: FakeHost }).__typedownFakeHost = host;
  return host;
}

const real = webviewTransport();
// XAML 滚动条是唯一的滚动条，宿主里隐藏页面的原生滚动条
if (real) document.documentElement.classList.add('td-host');
const app = startEditorApp({ parent: document.getElementById('root')!, transport: real ?? browserFallback(), init: window.__typedownInit });
(window as unknown as { typedown: unknown }).typedown = app;
