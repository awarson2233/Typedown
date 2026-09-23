// codemirror.css 留在主包且排在最前，保持它在样式表里的原有位置：其后的 Muya 样式与运行时插入的
// public/theme/codemirror 主题都有与它同优先级的规则，靠先后顺序生效。JS 部分只在源码模式才加载。
import 'codemirror/lib/codemirror.css';
import MuyaEditor from "components/Muya";
import React, { Suspense, useCallback, useEffect, useRef, useState } from "react";
import { remote } from "services/remote";
import transport from "services/transport";
import './index.scss'
import ExportHtml from "services/exportHtml";
import { htmlToMarkdown } from "services/importHtml";
import { DEFAULT_TURNDOWN_CONFIG } from "components/Muya/lib/config";
import { getHtmlToc, getTOC } from "services/common";

const CodeMirror = React.lazy(() => import(/* webpackChunkName: "codemirror" */ "components/CodeMirror"));

const Editor: React.FC = () => {
    const [markdown, setMarkdown] = useState<string>();
    const markdownRef = useRef<string>();
    const [cursor, setCursor] = useState<any>();
    const [options, setOptions] = useState<any>();
    const optionsRef = useRef<any>();
    const [searchOpen, setSearchOpen] = useState(0);
    const [searchArg, setSearchArg] = useState<{ value: string, opt: any }>();
    const muyaScrollTopRef = useRef(0);
    const codeMirrorScrollRef = useRef(0);

    const OnFileLoaded = useCallback(() => setTimeout(() => transport.postMessage('FileLoaded', { text: markdownRef.current }), 100), [])

    useEffect(() => {
        remote.getSettings().then(({ markdown, basePath, ...opt }: any) => {
            window.basePath = basePath
            setOptions(opt)
            setMarkdown(markdown)
            markdownRef.current = markdown
            OnFileLoaded();
        })
    }, [OnFileLoaded]);

    useEffect(() => {
        optionsRef.current = options
    }, [options])

    useEffect(() => {
        if (markdown != undefined && markdownRef.current != markdown) {
            transport.postMessage('MarkdownChange', { text: markdown });
            markdownRef.current = markdown
        }
    }, [markdown])

    useEffect(() => {
        transport.postMessage('CursorChange', { cursor })
    }, [cursor])

    useEffect(() => transport.addListener<IExportArgs>('Export', async ({ type, context, basePath, title, options }) => {
        const generateOption = { printOptimization: false, title, toc: getHtmlToc(getTOC(markdownRef.current ?? '').toc), ...options }
        const baseUrl = basePath ? `file:///${basePath.replaceAll('\\', '/')}/` : undefined
        const html = await new ExportHtml(markdownRef.current, { ...optionsRef.current, baseUrl }).generate(generateOption)
        if (type == 'print') {
            remote.printHTML({ html, context })
        } else {
            remote.exportCallback({ html, context })
        }
    }), []);

    useEffect(() => transport.addListener<{ type: string, text: string }>('ImportFile', ({ text }) => {
        setMarkdown(htmlToMarkdown(text, [], DEFAULT_TURNDOWN_CONFIG))
    }), [options]);

    useEffect(() => transport.addListener<{ text: string, basePath: string }>('LoadFile', ({ text, basePath }) => {
        window.basePath = basePath
        setCursor(undefined)
        setMarkdown(text)
        markdownRef.current = text
        OnFileLoaded();
    }), [OnFileLoaded]);

    useEffect(() => transport.addListener<{ text: string, cursor: string, basePath: string }>('SetMarkdown', ({ text, cursor, basePath }) => {
        window.basePath = basePath
        setCursor(cursor)
        setTimeout(() => setMarkdown(text))
        markdownRef.current = text
    }), []);

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
                    cursor={cursor}
                    markdown={markdown ?? ''}
                    searchOpen={searchOpen}
                    searchArg={searchArg}
                    scrollTopRef={codeMirrorScrollRef}
                    onMarkdownChange={setMarkdown}
                    onCursorChange={setCursor}
                    onSearchArgChange={setSearchArg}
                />
            </Suspense>
        )
    } else {
        return (
            <MuyaEditor
                options={options}
                cursor={cursor}
                markdown={markdown ?? ''}
                searchOpen={searchOpen}
                searchArg={searchArg}
                scrollTopRef={muyaScrollTopRef}
                onMarkdownChange={setMarkdown}
                onCursorChange={setCursor}
                onSearchArgChange={setSearchArg}
            />
        )
    }
}

export default Editor;