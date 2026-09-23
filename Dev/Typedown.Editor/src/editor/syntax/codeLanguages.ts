import { LanguageDescription, LanguageSupport, StreamLanguage, type StreamParser } from '@codemirror/language';
import { javascript } from '@codemirror/lang-javascript';
import { css } from '@codemirror/lang-css';
import { html } from '@codemirror/lang-html';
import { yaml } from '@codemirror/lang-yaml';

/**
 * 围栏代码的嵌套语言清单。没有用 @codemirror/language-data：它对 lang-css / lang-javascript / lang-html 等
 * 做动态 import，而这几个包已被 lang-markdown 静态引入主包，rolldown 因此要在主包里生成模块命名空间对象，
 * 连带把它的运行时辅助放进 mermaid 的公共块（含 d3、dayjs），使主包预加载该块。
 * 这里改为：主包里已有的语言直接用，其余用 legacy-modes 按需 import()。
 * 语言按需加载时 lang-markdown 返回「跳过解析器」，加载完成后 CM6 自动对视口重解析，无需重配。
 */

const done = (s: LanguageSupport) => () => Promise.resolve(s);
const legacy = (load: () => Promise<StreamParser<unknown>>) => async () => new LanguageSupport(StreamLanguage.define(await load()));

const L = (name: string, alias: string[], load: () => Promise<LanguageSupport>) => LanguageDescription.of({ name, alias, load });

export const codeLanguages: readonly LanguageDescription[] = [
  L('JavaScript', ['js', 'javascript', 'mjs', 'cjs', 'node'], done(javascript())),
  L('TypeScript', ['ts', 'typescript'], done(javascript({ typescript: true }))),
  L('JSX', ['jsx'], done(javascript({ jsx: true }))),
  L('TSX', ['tsx'], done(javascript({ jsx: true, typescript: true }))),
  L('JSON', ['json', 'jsonc', 'json5'], done(javascript())),
  L('CSS', ['css'], done(css())),
  L('HTML', ['html', 'htm', 'xhtml', 'vue', 'svelte'], done(html())),
  L('YAML', ['yaml', 'yml'], done(yaml())),
  L('C', ['c', 'h'], legacy(() => import('@codemirror/legacy-modes/mode/clike').then(m => m.c))),
  L('C++', ['cpp', 'c++', 'cc', 'cxx', 'hpp'], legacy(() => import('@codemirror/legacy-modes/mode/clike').then(m => m.cpp))),
  L('C#', ['cs', 'csharp', 'c#'], legacy(() => import('@codemirror/legacy-modes/mode/clike').then(m => m.csharp))),
  L('Java', ['java'], legacy(() => import('@codemirror/legacy-modes/mode/clike').then(m => m.java))),
  L('Kotlin', ['kotlin', 'kt'], legacy(() => import('@codemirror/legacy-modes/mode/clike').then(m => m.kotlin))),
  L('Python', ['python', 'py'], legacy(() => import('@codemirror/legacy-modes/mode/python').then(m => m.python))),
  L('Go', ['go', 'golang'], legacy(() => import('@codemirror/legacy-modes/mode/go').then(m => m.go))),
  L('Rust', ['rust', 'rs'], legacy(() => import('@codemirror/legacy-modes/mode/rust').then(m => m.rust))),
  L('Shell', ['sh', 'bash', 'shell', 'zsh'], legacy(() => import('@codemirror/legacy-modes/mode/shell').then(m => m.shell))),
  L('PowerShell', ['powershell', 'ps1', 'pwsh'], legacy(() => import('@codemirror/legacy-modes/mode/powershell').then(m => m.powerShell))),
  L('SQL', ['sql'], legacy(() => import('@codemirror/legacy-modes/mode/sql').then(m => m.standardSQL))),
  L('XML', ['xml', 'xaml', 'svg', 'csproj'], legacy(() => import('@codemirror/legacy-modes/mode/xml').then(m => m.xml))),
  L('Diff', ['diff', 'patch'], legacy(() => import('@codemirror/legacy-modes/mode/diff').then(m => m.diff))),
  L('TOML', ['toml'], legacy(() => import('@codemirror/legacy-modes/mode/toml').then(m => m.toml))),
  L('Lua', ['lua'], legacy(() => import('@codemirror/legacy-modes/mode/lua').then(m => m.lua))),
  L('Ruby', ['ruby', 'rb'], legacy(() => import('@codemirror/legacy-modes/mode/ruby').then(m => m.ruby))),
  L('Swift', ['swift'], legacy(() => import('@codemirror/legacy-modes/mode/swift').then(m => m.swift))),
  L('Dockerfile', ['dockerfile', 'docker'], legacy(() => import('@codemirror/legacy-modes/mode/dockerfile').then(m => m.dockerFile))),
];
