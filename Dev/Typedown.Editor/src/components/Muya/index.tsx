import {
    CodeBlockLanguageSelector,
    EmojiSelector,
    FootnoteTool,
    ImageEditTool,
    ImageResizeBar,
    ImageToolBar,
    InlineFormatToolbar,
    LinkTools,
    Muya,
    ParagraphFrontButton,
    ParagraphFrontMenu,
    ParagraphQuickInsertMenu,
    PreviewToolBar,
    TableChessboard,
    TableColumnToolbar,
    TableDragBar,
    TableRowColumMenu,
    wordCount
} from '@muyajs/core';
import React, { useCallback, useEffect, useRef, useState } from "react";
import { createApplicationMenuState } from "services/menuState";
import transport from "services/transport";
import { remote } from "services/remote";

interface IMuyaEditor {
    markdown: string
    documentId: string
    pendingDocument?: { text: string, id: string }
    replacement?: { documentId: string, revision: string, text: string, cursor: any, origin: 'import' | 'undo' | 'redo' }
    onReplacementConsumed: (documentId: string, revision: string) => void
    cursor: any
    options: any
    searchOpen: number
    searchArg: { value: string, opt: any } | undefined
    scrollTopRef: React.MutableRefObject<number>
    onMarkdownChange: (markdown: string, documentId: string) => void
    onCursorChange: (cursor: any, documentId: string) => void
    onSearchArgChange: (arg: { value: string, opt: any } | undefined) => void
}

const register = (name: string, plugin: any, options: Record<string, unknown> = {}) => {
    if (typeof plugin !== 'function') {
        throw new TypeError(`Muya plugin "${name}" must be a constructor, received ${typeof plugin}.`)
    }

    Muya.use(plugin, options)
}

register('EmojiSelector', EmojiSelector)
register('FootnoteTool', FootnoteTool)
// register('InlineFormatToolbar', InlineFormatToolbar)
register('ImageEditTool', ImageEditTool, {
    imagePathPicker: () => remote.pickImage(),
    imageAction: ({ src, alt, title }: { src: string, alt: string, title: string }) =>
        remote.processImage({ src, alt, title })
})
register('ImageToolBar', ImageToolBar)
register('ImageResizeBar', ImageResizeBar)
register('CodeBlockLanguageSelector', CodeBlockLanguageSelector)
register('LinkTools', LinkTools, { jumpClick: (linkInfo: { href?: string } | null) => {
    if (linkInfo?.href) transport.postMessage('OpenURI', { uri: linkInfo.href })
} });
(ParagraphFrontButton as any).pluginName = 'paragraphFrontButton'
register('ParagraphFrontButton', ParagraphFrontButton)
// register('ParagraphFrontMenu', ParagraphFrontMenu)
register('TableChessboard', TableChessboard)
register('TableColumnToolbar', TableColumnToolbar)
register('ParagraphQuickInsertMenu', ParagraphQuickInsertMenu)
register('TableDragBar', TableDragBar)
register('TableRowColumMenu', TableRowColumMenu)
register('PreviewToolBar', PreviewToolBar)

const STANDARD_Y = 320

const plainSelection = (selection: any) => ({
    start: selection?.anchor ? { offset: selection.anchor.offset } : undefined,
    end: selection?.focus ? { offset: selection.focus.offset } : undefined,
    anchor: selection?.anchor ? { offset: selection.anchor.offset } : undefined,
    focus: selection?.focus ? { offset: selection.focus.offset } : undefined,
    anchorPath: [...(selection?.anchorPath ?? selection?.anchor?.path ?? [])],
    focusPath: [...(selection?.focusPath ?? selection?.focus?.path ?? [])],
    isCollapsed: Boolean(selection?.isCollapsed),
    isSelectionInSameBlock: Boolean(selection?.isSelectionInSameBlock),
    direction: selection?.direction,
    type: selection?.type,
    kind: selection?.kind,
    cursorCoords: selection?.cursorCoords ? {
        x: selection.cursorCoords.x,
        y: selection.cursorCoords.y,
        top: selection.cursorCoords.top,
        bottom: selection.cursorCoords.bottom,
        left: selection.cursorCoords.left,
        right: selection.cursorCoords.right,
        width: selection.cursorCoords.width,
        height: selection.cursorCoords.height
    } : undefined,
    formats: (selection?.formats ?? []).map((format: any) => ({ type: format.type, tag: format.tag })),
    affiliation: (selection?.affiliation ?? []).map((item: any) => ({
        type: item.type,
        blockName: item.blockName,
        listType: item.listType,
        listItemType: item.listItemType,
        isLooseListItem: item.isLooseListItem
    })),
    anchorBlockInfo: selection?.anchorBlockInfo ? { ...selection.anchorBlockInfo } : undefined,
    focusBlockInfo: selection?.focusBlockInfo ? { ...selection.focusBlockInfo } : undefined
})

const normalizeSearchOptions = (opt: any = {}) => ({
    ...opt,
    isCaseSensitive: opt.isCaseSensitive ?? opt.searchIsCaseSensitive,
    isWholeWord: opt.isWholeWord ?? opt.searchIsWholeWord,
    isRegexp: opt.isRegexp ?? opt.searchIsRegexp
})

const parseTableSize = (value: any) => {
    if (value && typeof value === 'object') return value
    const match = String(value ?? '').match(/(\d+)\D+(\d+)/)
    return { rows: Number(match?.[1] ?? 2), columns: Number(match?.[2] ?? 2) }
}

const MuyaEditor: React.FC<IMuyaEditor> = (props) => {
    const [editor, setEditor] = useState<Muya>()
    const [marginTop, setMarginTop] = useState(0)
    const [documentEmpty, setDocumentEmpty] = useState(() => !props.markdown.trim())
    const mountRef = useRef<HTMLDivElement>(null)
    const markdownRef = useRef('')
    const cursorRef = useRef<any>()
    const searchArgRef = useRef<any>()
    const optionsRef = useRef<any>(props.options)
    const documentIdRef = useRef(props.documentId)
    const loadingRef = useRef(false)
    const loadedDocumentIdRef = useRef<string>()

    const scrollOwner = useCallback(() => editor?.domNode, [editor])
    const relativeScroll = useCallback((delta: number) => scrollOwner()?.scrollBy(0, delta), [scrollOwner])
    const scrollToElement = useCallback((selector: string) => {
        const owner = scrollOwner()
        const anchor = owner?.querySelector(selector)
        if (owner && anchor) relativeScroll(anchor.getBoundingClientRect().y - owner.getBoundingClientRect().y - STANDARD_Y)
    }, [relativeScroll, scrollOwner])
    const scrollToElementIfInvisible = useCallback((selector: string) => {
        const owner = scrollOwner()
        const anchor = owner?.querySelector(selector)
        if (!owner || !anchor) return
        const ownerRect = owner.getBoundingClientRect()
        const y = anchor.getBoundingClientRect().y
        if (y < ownerRect.top || y > ownerRect.bottom) scrollToElement(selector)
    }, [scrollOwner, scrollToElement])
    const scrollToCursor = useCallback(() => {
        const y = (editor?.getSelection() as any)?.cursorCoords?.y
        if (typeof y === 'number') relativeScroll(y - STANDARD_Y)
    }, [editor, relativeScroll])
    const runSearch = useCallback((arg: any) => {
        if (arg?.value && editor) editor.search(arg.value, { ...normalizeSearchOptions(arg.opt), selection: arg.opt?.selection?.start ? arg.opt.selection : undefined })
    }, [editor])
    const getActiveHeading = useCallback(() => {
        if (!editor) return undefined
        const y = (editor.getSelection() as any)?.cursorCoords?.y ?? 0
        const headings = Array.from(editor.domNode.querySelectorAll<HTMLElement>('h1[id], h2[id], h3[id], h4[id], h5[id], h6[id]'))
        const active = [...headings].reverse().find(heading => heading.getBoundingClientRect().top <= y + 1) ?? headings[0]
        return active ? { slug: active.id } : undefined
    }, [editor])

    useEffect(() => { cursorRef.current = props.cursor }, [props.cursor])
    useEffect(() => { searchArgRef.current = props.searchArg }, [props.searchArg])

    useEffect(() => {
        const mount = mountRef.current
        if (!mount) return
        const muya = new Muya(mount, { markdown: props.markdown, ...optionsRef.current })
        muya.init()
        markdownRef.current = muya.getMarkdown()
        setDocumentEmpty(!markdownRef.current.trim())
        setEditor(muya)

        // 💡 订阅行头 ¶ 的点击，向 C# 发送 OpenFrontMenu 消息并携带坐标
        muya.eventCenter.on('muya-front-menu', ({ reference }) => {
            const rect = reference.getBoundingClientRect();
            transport.postMessage('OpenFrontMenu', {
                boundingClientRect: {
                    x: rect.x,
                    y: rect.y,
                    width: rect.width,
                    height: rect.height
                }
            });
        });

        // 💡 劫持隐藏方法以支持“按钮常驻当前光标编辑行”
        const frontButton = (muya as any)._uiPlugins['paragraphFrontButton'];
        if (frontButton) {
            const originalHide = frontButton.hide.bind(frontButton);
            (frontButton as any).originalHide = originalHide;
            frontButton.hide = () => {
                // 阻止其它地方（如 mousemove）自动隐藏
            };
        }

        // 💡 监听全局右键点击，向 C# 发送 OpenContextMenu 并携带当前鼠标在窗口内的绝对坐标
        const handleContextMenu = (e: MouseEvent) => {
            e.preventDefault();
            transport.postMessage('OpenContextMenu', {
                x: e.clientX,
                y: e.clientY
            });
        };
        document.addEventListener('contextmenu', handleContextMenu);

        return () => {
            setEditor(undefined);
            muya.destroy();
            document.removeEventListener('contextmenu', handleContextMenu);
        }
        // Initial content is supplied to the constructor; later documents use setContent.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [])

    useEffect(() => {
        if (!editor || !props.pendingDocument) return
        const outgoingId = documentIdRef.current
        editor.flush()
        const outgoingMarkdown = editor.getMarkdown()
        markdownRef.current = outgoingMarkdown
        props.onMarkdownChange(outgoingMarkdown, outgoingId)
        props.onCursorChange(editor.getCursorOffset(), outgoingId)
        transport.postMessageNoDiff('DocumentFlushed', { documentId: outgoingId, nextDocumentId: props.pendingDocument.id })
    // The parent callbacks are stable useCallback instances.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [editor, props.pendingDocument, props.onCursorChange, props.onMarkdownChange])

    useEffect(() => {
        if (!editor || loadedDocumentIdRef.current === props.documentId) return
        loadingRef.current = true
        documentIdRef.current = props.documentId
        if (loadedDocumentIdRef.current !== undefined && markdownRef.current !== props.markdown) {
            editor.setContent(props.markdown)
        }
        editor.clearHistory()
        markdownRef.current = editor.getMarkdown()
        setDocumentEmpty(!markdownRef.current.trim())
        if (cursorRef.current) editor.setCursorByOffset(cursorRef.current)
        else editor.setCursorByOffset({ anchor: { line: 0, ch: 0 }, focus: { line: 0, ch: 0 } })
        loadingRef.current = false
        loadedDocumentIdRef.current = props.documentId
        transport.postMessageNoDiff('FileLoaded', { text: markdownRef.current, documentId: props.documentId })
        const toc = editor.getTOC().map(item => ({ ...item }))
        const active = getActiveHeading()
        const cur = active ? toc.find(item => item.slug === active.slug) : undefined
        transport.postMessage('StateChange', { state: { wordCount: wordCount(markdownRef.current), toc, cur }, muya: true, documentId: props.documentId })
        const live = plainSelection(editor.getSelection())
        const menuState = createApplicationMenuState({
            ...live,
            start: { key: live.anchorPath.join('/'), block: live.anchorBlockInfo ?? {} },
            end: { key: live.focusPath.join('/'), block: live.focusBlockInfo ?? {} }
        })
        transport.postMessage('SelectionChange', { selection: live, menuState, selectionText: '', documentId: props.documentId })
        transport.postMessage('SelectionFormats', { formats: live.formats, documentId: props.documentId })
        const owner = editor.domNode
        owner.scrollTop = props.scrollTopRef.current
        requestAnimationFrame(() => {
            if (documentIdRef.current !== props.documentId) return
            owner.scrollTop = props.scrollTopRef.current
            runSearch(searchArgRef.current)
            transport.postMessage('DocumentRendered', { documentId: props.documentId })
        })
    }, [editor, getActiveHeading, props.documentId, props.markdown, props.scrollTopRef, runSearch])

    useEffect(() => {
        const replacement = props.replacement
        if (!editor || !replacement || replacement.documentId !== props.documentId) return
        loadingRef.current = true
        editor.setContent(replacement.text)
        markdownRef.current = editor.getMarkdown()
        setDocumentEmpty(!markdownRef.current.trim())
        if (replacement.cursor) editor.setCursorByOffset(replacement.cursor)
        else editor.setCursorByOffset({ anchor: { line: 0, ch: 0 }, focus: { line: 0, ch: 0 } })
        loadingRef.current = false
        const currentCursor = editor.getCursorOffset()
        const toc = editor.getTOC().map(item => ({ ...item }))
        const active = getActiveHeading()
        const cur = active ? toc.find(item => item.slug === active.slug) : undefined
        const selection = plainSelection(editor.getSelection())
        const menuState = createApplicationMenuState({ ...selection, start: { key: selection.anchorPath.join('/'), block: selection.anchorBlockInfo ?? {} }, end: { key: selection.focusPath.join('/'), block: selection.focusBlockInfo ?? {} } })
        let retryCount = 0
        let retryTimer: number | undefined
        const sendFinal = () => {
            if (replacement.origin !== 'import' || retryCount >= 5) return
            retryCount++
            transport.postMessage('MarkdownChange', { text: markdownRef.current, documentId: replacement.documentId, revision: replacement.revision, origin: replacement.origin, phase: 'final' })
            retryTimer = window.setTimeout(sendFinal, 750)
        }
        sendFinal()
        transport.postMessage('CursorChange', { cursor: currentCursor, documentId: replacement.documentId })
        transport.postMessage('StateChange', { state: { wordCount: wordCount(markdownRef.current), toc, cur }, muya: true, documentId: replacement.documentId })
        transport.postMessage('SelectionChange', { selection, menuState, selectionText: '', documentId: replacement.documentId })
        transport.postMessage('SelectionFormats', { formats: selection.formats, documentId: replacement.documentId })
        return () => { if (retryTimer !== undefined) window.clearTimeout(retryTimer) }
    // All accessed data props are listed explicitly.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [editor, getActiveHeading, props.documentId, props.replacement])

    useEffect(() => { editor?.setOptions(props.options, true) }, [editor, props.options])
    useEffect(() => { runSearch(props.searchArg) }, [props.searchArg, runSearch])

    useEffect(() => transport.addListener<{ slug: string }>('ScrollTo', ({ slug }) => scrollToElement(`#${CSS.escape(slug)}`)), [scrollToElement])
    useEffect(() => transport.addListener<string>('UpdateParagraph', type => editor?.updateParagraph(type)), [editor])
    useEffect(() => transport.addListener<any>('InsertParagraph', pos => editor?.insertParagraph(pos?.location ?? pos ?? 'after', '', true)), [editor])
    useEffect(() => transport.addListener('DeleteParagraph', () => editor?.deleteParagraph()), [editor])
    useEffect(() => transport.addListener('Duplicate', () => editor?.duplicate()), [editor])
    useEffect(() => transport.addListener<string>('Format', type => editor?.format(type)), [editor])
    useEffect(() => transport.addListener('DeleteSelection', () => document.execCommand('delete')), [editor])
    useEffect(() => transport.addListener('SelectAll', () => editor?.selectAll()), [editor])
    useEffect(() => transport.addListener<{ type?: string }>('Copy', ({ type } = {}) => {
        if (!editor) return
        if (type === 'copyAsMarkdown') editor.copyAsMarkdown()
        else if (type === 'copyAsHtml') editor.copyAsHtml()
        else if (type === 'copyAsRich') editor.copyAsRich()
        else document.execCommand('copy')
    }), [editor])
    useEffect(() => transport.addListener('Cut', () => document.execCommand('cut')), [editor])
    useEffect(() => transport.addListener<any>('Paste', arg => {
        if (!editor) return
        if (arg?.src) void editor.pasteImage(arg.src)
        else if (arg?.type === 'pasteAsPlainText') void editor.pastePlainText(arg?.text ?? '')
        else {
            const data = new DataTransfer()
            data.setData('text/plain', arg?.text ?? '')
            data.setData('text/html', arg?.html ?? '')
            editor.domNode.dispatchEvent(new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData: data }))
        }
    }), [editor])
    useEffect(() => transport.addListener<any>('InsertTable', arg => editor?.createTable(parseTableSize(arg))), [editor])
    useEffect(() => transport.addListener<any>('InsertImage', arg => editor?.insertImage(typeof arg === 'string' ? { src: arg } : arg)), [editor])
    useEffect(() => transport.addListener<any>('Search', arg => {
        props.onSearchArgChange(arg)
        runSearch(arg)
        requestAnimationFrame(() => scrollToElementIfInvisible('.mu-search-match'))
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }), [props.onSearchArgChange, runSearch, scrollToElementIfInvisible])
    useEffect(() => transport.addListener<{ action: 'previous' | 'next' }>('Find', ({ action }) => {
        editor?.find(action)
        requestAnimationFrame(() => scrollToElementIfInvisible('.mu-search-match'))
    }), [editor, scrollToElementIfInvisible])
    useEffect(() => transport.addListener<any>('Replace', ({ value, opt }) => editor?.replace(value, normalizeSearchOptions(opt))), [editor])
    useEffect(() => transport.addListener<{ open: number }>('SearchOpenChange', ({ open }) => {
        if (open === 0 && editor) {
            editor.search('')
            props.onSearchArgChange(undefined)
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }), [editor, props.onSearchArgChange])
    useEffect(() => transport.addListener<Record<string, unknown>>('SettingsChanged', newOptions => {
        editor?.setOptions(newOptions, true)
        if (newOptions.typewriter) scrollToCursor()
    }), [editor, scrollToCursor])

    useEffect(() => {
        if (!editor) return
        const listener = (live: any) => {
            const selection = plainSelection(live)
            const menuInput = {
                ...selection,
                start: { key: selection.anchorPath.join('/'), block: selection.anchorBlockInfo ?? {} },
                end: { key: selection.focusPath.join('/'), block: selection.focusBlockInfo ?? {} }
            }
            const menuState = createApplicationMenuState(menuInput)
            const selectionText = window.getSelection()?.toString() ?? ''
            transport.postMessage('SelectionChange', { selection, menuState, selectionText, documentId: documentIdRef.current })
            transport.postMessage('SelectionFormats', { formats: selection.formats, documentId: documentIdRef.current })
            transport.postMessage('ActiveHeadingChange', { cur: getActiveHeading(), documentId: documentIdRef.current })



            // 💡 2. 动态更新行头 P 按钮，使其贴在当前编辑行的左侧
            try {
                const frontButton = (editor as any)._uiPlugins['paragraphFrontButton'];
                if (frontButton) {
                    let activeBlock: any = null;
                    const winSel = window.getSelection();
                    if (winSel && winSel.rangeCount > 0) {
                        const node: Node | null = winSel.getRangeAt(0).startContainer;
                        let element: HTMLElement | null = node.nodeType === Node.ELEMENT_NODE ? (node as HTMLElement) : node.parentElement;
                        while (element) {
                            if ((element as any).__MUYA_BLOCK__) {
                                activeBlock = (element as any).__MUYA_BLOCK__;
                                break;
                            }
                            element = element.parentElement;
                        }
                    }
                    if (activeBlock) {
                        while (activeBlock && !activeBlock.isOutMostBlock) {
                            activeBlock = activeBlock.parent;
                        }
                    }
                    // 确保 activeBlock.domNode 依然挂载在当前文档中，避免 Floating UI 进行已销毁节点的测量异常
                    if (activeBlock && document.body.contains(activeBlock.domNode) && activeBlock.blockName !== 'frontmatter') {
                        frontButton.show(activeBlock);
                    } else {
                        if ((frontButton as any).originalHide) {
                            (frontButton as any).originalHide();
                        } else {
                            frontButton.hide();
                        }
                    }
                }
            } catch (err) {
                // 💡 彻底捕获 DOM 重置或 range 临时失效时的异常（如 IndexSizeError），保证文件加载不受任何干扰
                console.warn("Repositioning front button caught error:", err);
            }

            const y = selection.cursorCoords?.y
            if (typeof y === 'number') {
                if (props.options?.typewriter) relativeScroll(y - window.innerHeight / 2 + 136)
                else if (window.innerHeight - y < 100) relativeScroll(y - window.innerHeight + 100)
            }
        }
        editor.on('selection-change', listener)
        return () => editor.off('selection-change', listener)
    }, [editor, getActiveHeading, props.options?.typewriter, relativeScroll])

    useEffect(() => {
        if (!editor) return
        const listener = () => {
            if (loadingRef.current) return
            const markdown = editor.getMarkdown()
            const cursor = editor.getCursorOffset()
            const toc = editor.getTOC().map(item => ({ ...item }))
            const active = getActiveHeading()
            const cur = active ? toc.find(item => item.slug === active.slug) : undefined
            markdownRef.current = markdown
            setDocumentEmpty(!markdown.trim())
            props.onMarkdownChange(markdown, documentIdRef.current)
            props.onCursorChange(cursor, documentIdRef.current)
            transport.postMessage('StateChange', { state: { wordCount: wordCount(markdown), toc, cur }, muya: true, documentId: documentIdRef.current })
        }
        editor.on('json-change', listener)
        return () => editor.off('json-change', listener)
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [editor, getActiveHeading, props.onCursorChange, props.onMarkdownChange])

    useEffect(() => {
        setMarginTop(current => {
            const next = ({ 0: 0, 1: 50, 2: 90 } as Record<number, number>)[props.searchOpen] ?? 0
            relativeScroll(next - current)
            return next
        })
    }, [props.searchOpen, relativeScroll])

    useEffect(() => {
        if (!editor) return
        const owner = editor.domNode
        owner.style.boxSizing = 'border-box'
        owner.style.width = '100%'
        owner.style.height = '100%'
        owner.style.overflow = 'auto'
        owner.style.paddingTop = props.options?.typewriter ? `calc(50vh - ${136 - marginTop}px)` : `${marginTop}px`
        owner.style.paddingBottom = props.options?.typewriter ? 'calc(50vh - 54px)' : '0'
    }, [editor, marginTop, props.options?.typewriter])

    useEffect(() => { document.documentElement.style.setProperty('--editor-area-width', props.options?.editorAreaWidth ?? '750px') }, [props.options?.editorAreaWidth])
    useEffect(() => { try { editor?.focus() } catch (err) { console.error(err) } }, [editor])
    useEffect(() => {
        const owner = editor?.domNode
        if (!owner) return
        const onScroll = () => { props.scrollTopRef.current = owner.scrollTop }
        owner.addEventListener('scroll', onScroll)
        return () => owner.removeEventListener('scroll', onScroll)
    }, [editor, props.scrollTopRef])

    return <div className={`muya-host${documentEmpty ? ' muya-document-empty' : ''}`}><div ref={mountRef} /></div>
}

export default MuyaEditor
