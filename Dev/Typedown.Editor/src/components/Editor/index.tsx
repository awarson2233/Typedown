import CodeMirror from "components/CodeMirror";
import MuyaEditor from "components/Muya";
import React, { useCallback, useEffect, useRef, useState } from "react";
import { remote } from "services/remote";
import transport from "services/transport";
import './index.scss'
import ExportHtml from "services/exportHtml";
import { htmlToMarkdown } from "services/importHtml";
import { DEFAULT_TURNDOWN_CONFIG } from "services/importHtml";
import { getHtmlToc, getTOC } from "services/common";

type ReplacementOrigin = 'import' | 'undo' | 'redo'
type DocumentReplacement = { documentId: string, revision: number, text: string, cursor: any, origin: ReplacementOrigin }

const Editor: React.FC = () => {
    const [markdown, setMarkdown] = useState<string>();
    const markdownRef = useRef<string>();
    const [documentId, setDocumentId] = useState<string>('');
    const documentIdRef = useRef<string>('');
    const [pendingDocument, setPendingDocument] = useState<{ text: string, id: string }>();
    const [cursor, setCursor] = useState<any>();
    const [replacement, setReplacement] = useState<DocumentReplacement>();
    const [options, setOptions] = useState<any>();
    const optionsRef = useRef<any>();
    const [searchOpen, setSearchOpen] = useState(0);
    const [searchArg, setSearchArg] = useState<{ value: string, opt: any }>();
    const muyaScrollTopRef = useRef(0);
    const codeMirrorScrollRef = useRef(0);

    const activateDocument = useCallback((text: string, id: string) => {
        documentIdRef.current = id
        markdownRef.current = text
        setDocumentId(id)
        setPendingDocument(undefined)
        setReplacement(undefined)
        setCursor(undefined)
        setMarkdown(text)
    }, [])

    const loadDocument = useCallback((text: string, id?: string) => {
        const nextId = id || `${Date.now()}-${Math.random()}`
        if (documentIdRef.current) {
            setPendingDocument({ text, id: nextId })
        } else {
            activateDocument(text, nextId)
        }
    }, [activateDocument])

    useEffect(() => {
        remote.getSettings().then(({ markdown, basePath, documentId, ...opt }: any) => {
            window.basePath = basePath
            setOptions(opt)
            loadDocument(markdown, documentId)
        })
    }, [loadDocument]);

    useEffect(() => {
        optionsRef.current = options
    }, [options])

    const onMuyaMarkdownChange = useCallback((text: string, id: string) => {
        if (documentIdRef.current !== id) return
        markdownRef.current = text
        setMarkdown(text)
        transport.postMessage('MarkdownChange', { text, documentId: id })
    }, [])

    const onMuyaCursorChange = useCallback((nextCursor: any, id: string) => {
        if (documentIdRef.current !== id) return
        setCursor(nextCursor)
        transport.postMessage('CursorChange', { cursor: nextCursor, documentId: id })
    }, [])

    useEffect(() => {
        if (markdown != undefined && markdownRef.current != markdown) {
            transport.postMessage('MarkdownChange', { text: markdown, documentId: documentIdRef.current });
            markdownRef.current = markdown
        }
    }, [markdown])

    useEffect(() => {
        if (options?.sourceCode) transport.postMessage('CursorChange', { cursor, documentId: documentIdRef.current })
    }, [cursor, options?.sourceCode])

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

    const replaceCurrentDocument = useCallback((text: string, nextCursor: any, origin: ReplacementOrigin) => {
        const currentDocumentId = documentIdRef.current
        if (!currentDocumentId) return
        const revision = Date.now() + Math.random()
        markdownRef.current = text
        setCursor(nextCursor)
        setMarkdown(text)
        setReplacement({ documentId: currentDocumentId, text, cursor: nextCursor, origin, revision })
        if (origin === 'import') transport.postMessage('MarkdownChange', { text, documentId: currentDocumentId, revision, origin })
    }, [])

    const consumeReplacement = useCallback((documentId: string, revision: number) => {
        setReplacement(current => current?.documentId === documentId && current.revision === revision ? undefined : current)
    }, [])

    useEffect(() => transport.addListener<{ type: string, text: string }>('ImportFile', ({ text }) => {
        replaceCurrentDocument(htmlToMarkdown(text, [], DEFAULT_TURNDOWN_CONFIG), undefined, 'import')
    }), [replaceCurrentDocument]);

    useEffect(() => transport.addListener<{ text: string, basePath: string, documentId?: string }>('LoadFile', ({ text, basePath, documentId }) => {
        window.basePath = basePath
        loadDocument(text, documentId)
    }), [loadDocument]);

    useEffect(() => transport.addListener<{ text: string, basePath: string, documentId: string }>('ActivateDocument', ({ text, basePath, documentId }) => {
        const pending = pendingDocument
        if (!pending || pending.id !== documentId) return
        window.basePath = basePath
        activateDocument(text, documentId)
    }), [activateDocument, pendingDocument]);

    useEffect(() => transport.addListener<{ text: string, cursor: any, basePath: string, origin?: 'undo' | 'redo' }>('SetMarkdown', ({ text, cursor, basePath, origin }) => {
        window.basePath = basePath
        replaceCurrentDocument(text, cursor, origin ?? 'undo')
    }), [replaceCurrentDocument]);

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
            <CodeMirror
                options={options}
                cursor={cursor}
                replacement={replacement}
                onReplacementConsumed={consumeReplacement}
                documentId={documentId}
                pendingDocument={pendingDocument}
                markdown={markdown ?? ''}
                searchOpen={searchOpen}
                searchArg={searchArg}
                scrollTopRef={codeMirrorScrollRef}
                onMarkdownChange={setMarkdown}
                onCursorChange={setCursor}
                onSearchArgChange={setSearchArg}
            />
        )
    } else {
        return (
            <MuyaEditor
                options={options}
                cursor={cursor}
                replacement={replacement}
                onReplacementConsumed={consumeReplacement}
                documentId={documentId}
                pendingDocument={pendingDocument}
                markdown={markdown ?? ''}
                searchOpen={searchOpen}
                searchArg={searchArg}
                scrollTopRef={muyaScrollTopRef}
                onMarkdownChange={onMuyaMarkdownChange}
                onCursorChange={onMuyaCursorChange}
                onSearchArgChange={setSearchArg}
            />
        )
    }
}

export default Editor;