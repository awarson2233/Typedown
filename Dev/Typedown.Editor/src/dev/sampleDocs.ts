/**
 * 测试文档生成器，与 docs/artifacts 性能文档（webview-performance.md、wysiwyg-engine-benchmark.md）
 * 使用的 doc-small / mid / rich / large 逐字节相同，便于同口径对照：
 * small = 第 0–7 节；mid = 第 0–249 节；rich = 第 0–99 节且每 5 节带一张 mermaid 与一个公式块；
 * large = 第 0–1299 节重复两遍（约 1 MB）。large-rich 是 rich 重复到约 1 MB，用来测带块组件的大文档。
 */

export function section(i: number, rich: boolean): string {
  let s = `## 第 ${i} 节 Section ${i}\n\n这是一段**加粗**、*斜体*、\`行内代码\`与[链接](https://example.com/${i})混排的正文，用来模拟日常笔记。The quick brown fox jumps over the lazy dog ${i} times.\n\n- 列表项一 item one\n- 列表项二 ~~删除~~\n  - 嵌套项 nested\n- [ ] 任务 task\n\n| 列A | 列B | 列C |\n|---|:-:|--:|\n| ${i} | b${i} | c${i} |\n| x | y | z |\n\n> 引用 quote ${i}\n> 第二行\n\n\`\`\`csharp\nvar x${i} = Enumerable.Range(0, ${i}).Sum();\nConsole.WriteLine(x${i});\n\`\`\`\n\n`;
  if (rich && i % 5 === 0) {
    s += `\`\`\`mermaid\nflowchart TD\n  A${i}[开始] --> B${i}{判断}\n  B${i} -->|是| C${i}[结束]\n  B${i} -->|否| A${i}\n\`\`\`\n\n$$\n\\int_0^{${i}} x^2\\,dx = \\frac{${i}^3}{3}\n$$\n\n`;
  }
  return s;
}

const range = (n: number, rich: boolean) => {
  let s = '';
  for (let i = 0; i < n; i++) s += section(i, rich);
  return s;
};

export const IME_DOC = `# 输入法手测文档

光标先放到下面各行指定的位置，再用输入法打字。标记（\`**\`、反引号、方括号）隐藏时，把光标移到附近它们会变灰显示。

## 行内标记两侧

粗体在中间：前文**粗体文字**后文

行内代码：前文\`code\`后文

链接：前文[链接文字](https://example.com)后文

斜体与删除线：前文*斜体*中间~~删除~~后文

高亮与公式：前文==高亮==中间$E=mc^2$后文

中文标点紧跟粗体结束符：**中文粗体**，后面是逗号；**引号“粗体”**。

## 标题行首

### 在这一行行首输入

## 列表与任务

- 列表项一
- 列表项二
  - 嵌套项
1. 有序项一
2. 有序项二
- [ ] 任务项
- [x] 已完成任务

> 引用块第一行
> 引用块第二行

## 表格

| 姓名 | 城市 | 备注 |
|:---|:---:|---:|
| 张三 | 北京 | 单元格一 |
| 李四 | 上海 |  |

## 公式块与图表

$$
\\sum_{i=1}^{n} i = \\frac{n(n+1)}{2}
$$

\`\`\`mermaid
flowchart LR
  A[开始] --> B[结束]
\`\`\`

结尾段落。
`;

/** 各类块组件各一个，给样式对照截图与手测用（图片用相对路径 images/logo.png，需要 basePath 指向放图片的目录） */
export const BLOCKS_DOC = `---
title: 块组件样例
tags: [typedown, blocks]
---

# 块组件样例

[TOC]

## 代码块

正文段落，下面是带语言的代码块：

\`\`\`javascript
// 计算斐波那契数
function fib(n) {
  return n < 2 ? n : fib(n - 1) + fib(n - 2);
}
const s = \`fib(10) = \${fib(10)}\`;
\`\`\`

\`\`\`python
def greet(name: str) -> str:
    return f"Hello, {name}!"  # 注释
\`\`\`

\`\`\`
没有语言的代码块
\`\`\`

## 公式与图表

$$
\\int_0^1 x^2\\,dx = \\frac{1}{3}
$$

\`\`\`mermaid
flowchart LR
  A[开始] --> B{判断}
  B -->|是| C[结束]
\`\`\`

## HTML 块

<div align="center">
  <b>居中的粗体</b> 与 <i>斜体</i>
</div>

<script>
console.log('不可见');
</script>

## 图片

本地图片：

![logo](images/logo.png "Typedown")

加载失败：![坏图](images/missing.png)

空图片：![]()

## 脚注

这里有两个脚注引用[^1]，第二个[^note]。

[^1]: 第一个脚注的内容，带**粗体**。

[^note]: 第二个脚注。
`;

export function sampleDoc(name: string): string {
  switch (name) {
    case 'small': return range(8, false);
    case 'mid': return range(250, false);
    case 'rich': return range(100, true);
    case 'large': { const half = range(1300, false); return half + half; }
    case 'large-rich': { const r = range(100, true); return r.repeat(25); }
    case 'ime': return IME_DOC;
    case 'blocks': return BLOCKS_DOC;
    default: return range(8, false);
  }
}
