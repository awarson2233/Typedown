/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import { resolve } from 'node:path';

// 生产构建只产出宿主加载的 index.html（输出到 WinUI 的 Resources/Statics，已被 gitignore）；
// bench 模式额外产出不依赖宿主的 dev.html，输出到 dist-bench/，供 Tools/perf-probe 以 file:// 加载测量。
const CORE_MODULES = /node_modules[\\/](@codemirror[\\/](state|view|language|commands|autocomplete|lang-markdown|lang-yaml|lang-html|lang-css|lang-javascript)|@lezer[\\/](common|lr|highlight|markdown|yaml|html|css|javascript)|style-mod|w3c-keyname|crelt|@marijn)[\\/]/;

export default defineConfig(({ mode }) => {
  const bench = mode === 'bench';
  return {
    base: './',
    server: { port: 3000, strictPort: true },
    preview: { port: 3000, strictPort: true },
    build: {
      outDir: bench ? resolve(import.meta.dirname, 'dist-bench') : resolve(import.meta.dirname, '../Typedown.WinUI/Resources/Statics'),
      emptyOutDir: true,
      sourcemap: true,
      target: 'es2022',
      // mermaid 自身按图表类型拆成很多懒块，这里只关心主包大小
      chunkSizeWarningLimit: 700,
      rollupOptions: {
        input: bench
          ? { index: resolve(import.meta.dirname, 'index.html'), dev: resolve(import.meta.dirname, 'dev.html') } as Record<string, string>
          : { index: resolve(import.meta.dirname, 'index.html') },
        output: {
          // 主包只放打开纯文字文档所需的 CM6 核心与自有代码。显式分组的另一个作用：
          // 不分组时 rolldown 把运行时辅助函数放进 mermaid 的公共块（含 d3、dayjs），主包被连带预加载。
          codeSplitting: {
            groups: [
              { name: 'editor-core', test: CORE_MODULES, priority: 10 },
            ],
          },
        },
      },
    },
    test: {
      include: ['test/**/*.test.ts'],
      environment: 'node',
    },
  };
});
