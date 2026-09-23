import './styles/editor.css';
import { createEditor } from './editor/createEditor';
import { applyTheme } from './host/theme';

/**
 * 宿主加载的入口。C1 阶段不接宿主（正文同步等 IEditorSession 完成后再接，见 bridge/protocol.ts），
 * 这里只以空文档构造编辑器并暴露最小的调试接口。
 */
applyTheme(window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
const editor = createEditor({ doc: '', parent: document.getElementById('root')! });
(window as unknown as { typedown: unknown }).typedown = editor;
