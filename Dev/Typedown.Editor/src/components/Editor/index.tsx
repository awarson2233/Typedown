// codemirror.css 留在主包且排在最前，保持它在样式表里的原有位置：其后的 Muya 样式与运行时插入的
// public/theme/codemirror 主题都有与它同优先级的规则，靠先后顺序生效。JS 部分只在源码模式才加载。
import 'codemirror/lib/codemirror.css';
import MuyaEditor from "components/Muya";
import React, { Suspense, useCallback, useEffect, useRef, useState } from "react";
import { remote } from "services/remote";
import transport from "services/transport";
import './index.scss'
import { htmlToMarkdown } from "services/importHtml";
import { DEFAULT_TURNDOWN_CONFIG } from "components/Muya/lib/config";
import { getHtmlToc, getTOC } from "services/common";

const CodeMirror = React.lazy(() => import(/* webpackChunkName: "codemirror" */ "components/CodeMirror"));

const Editor: React.FC = () => {
    // 正文与光标只放在 ref 里：每次按键都会变，放进 state 会让外壳与编辑器包装组件整棵重渲染。
    // 只有从宿主装载新正文（GetSettings、LoadFile、SetMarkdown、ImportFile）时递增 markdownVersion，
    // 让当前编辑器组件把 ref 里的正文与光标应用进去。
    const markdownRef = useRef<string>();
    const cursorRef = useRef<any>();
    const [markdownVersion, setMarkdownVersion] = useState(0);
    const [options, setOptions] = useState<any>();
    const optionsRef = useRef<any>();
    const [searchOpen, setSearchOpen] = useState(0);
    const [searchArg, setSearchArg] = useState<{ value: string, opt: any }>();
    const muyaScrollTopRef = useRef(0);
    const codeMirrorScrollRef = useRef(0);

    const OnFileLoaded = useCallback(() => setTimeout(() => transport.postMessage('FileLoaded', { text: markdownRef.current }), 100), [])

    const loadMarkdown = useCallback((text: string) => {
        markdownRef.current = text
        setMarkdownVersion(version => version + 1)
    }, [])

    // 编辑器回报的正文变更：与已知正文不同才上报宿主（装载时编辑器回显的同一份正文不算变更）。
    const onMarkdownChange = useCallback((text: string) => {
        if (markdownRef.current != text) {
            markdownRef.current = text
            transport.postMessage('MarkdownChange', { text })
        }
    }, [])

    const onCursorChange = useCallback((cursor: any) => {
        cursorRef.current = cursor
        transport.postMessage('CursorChange', { cursor })
    }, [])

    useEffect(() => {
        remote.getSettings().then(({ markdown, basePath, ...opt }: any) => {
            window.basePath = basePath
            markdownRef.current = markdown
            setOptions(opt)
            loadMarkdown(markdown)
            OnFileLoaded();
        })
    }, [OnFileLoaded, loadMarkdown]);

    useEffect(() => {
        optionsRef.current = options
    }, [options])

    useEffect(() => transport.addListener<IExportArgs>('Export', async ({ type, context, basePath, title, options }) => {
        const generateOption = { printOptimization: false, title, toc: getHtmlToc(getTOC(markdownRef.current ?? '').toc), ...options }
        const baseUrl = basePath ? `file:///${basePath.replaceAll('\\', '/')}/` : undefined
        const { default: ExportHtml } = await import(/* webpackChunkName: "export" */ "services/exportHtml")
        const html = await new ExportHtml(markdownRef.current, { ...optionsRef.current, baseUrl }).generate(generateOption)
        if (type == 'print') {
            remote.printHTML({ html, context })
        } else {
            remote.exportCallback({ html, context })
        }
    }), []);

    useEffect(() => transport.addListener<{ type: string, text: string }>('ImportFile', ({ text }) => {
        const markdown = htmlToMarkdown(text, [], DEFAULT_TURNDOWN_CONFIG)
        if (markdownRef.current != markdown) {
            transport.postMessage('MarkdownChange', { text: markdown })
            loadMarkdown(markdown)
        }
    }), [loadMarkdown]);

    useEffect(() => transport.addListener<{ text: string, basePath: string }>('LoadFile', ({ text, basePath }) => {
        window.basePath = basePath
        cursorRef.current = undefined
        loadMarkdown(text)
        OnFileLoaded();
    }), [OnFileLoaded, loadMarkdown]);

    useEffect(() => transport.addListener<{ text: string, cursor: string, basePath: string }>('SetMarkdown', ({ text, cursor, basePath }) => {
        window.basePath = basePath
        cursorRef.current = cursor
        markdownRef.current = text
        setTimeout(() => loadMarkdown(text))
    }), [loadMarkdown]);

    useEffect(() => transport.addListener<Record<string, unknown>>('SettingsChanged', (newOptions) => {
        for (const name in newOptions) {
            const value = newOptions[name];
            if (name.startsWith('search'))
                setSearchArg(old => old ? { ...old, opt: { ...old.opt, [name]: value } } : old)
        }
        setOptions((oldOptions: any) => ({ ...oldOptions, ...newOptions }))
    }), []);

    useEffect(() => transport.addListener<{ open: number }>('SearchOpenChange', ({ open }) => {
        setSearchOpen(open)
    }), []);

    if (!options) {
        return <></>
    }

    if (options.sourceCode) {
        return (
            <Suspense fallback={<></>}>
                <CodeMirror
                    options={options}
                    cursor={cursorRef.current}
                    markdown={markdownRef.current ?? ''}
                    markdownVersion={markdownVersion}
                    searchOpen={searchOpen}
                    searchArg={searchArg}
                    scrollTopRef={codeMirrorScrollRef}
                    onMarkdownChange={onMarkdownChange}
                    onCursorChange={onCursorChange}
                    onSearchArgChange={setSearchArg}
                />
            </Suspense>
        )
    } else {
        return (
            <MuyaEditor
                options={options}
                cursor={cursorRef.current}
                markdown={markdownRef.current ?? ''}
                markdownVersion={markdownVersion}
                searchOpen={searchOpen}
                searchArg={searchArg}
                scrollTopRef={muyaScrollTopRef}
                onMarkdownChange={onMarkdownChange}
                onCursorChange={onCursorChange}
                onSearchArgChange={setSearchArg}
            />
        )
    }
}

export default Editor;