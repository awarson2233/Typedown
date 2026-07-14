import TurndownService from 'turndown'
import { gfm } from 'turndown-plugin-gfm'

const DEFAULT_TURNDOWN_CONFIG = {
    bulletListMarker: '-',
    codeBlockStyle: 'fenced',
    emDelimiter: '*',
    fence: '```',
    headingStyle: 'atx',
    keepReplacement: content => content,
    strongDelimiter: '**'
}

const turnSoftBreakToBr = html => {
    const parser = new DOMParser()
    const doc = parser.parseFromString(`<x-mt id="turn-root">${html}</x-mt>`, 'text/html')
    const root = doc.querySelector('#turn-root')
    if (!root) return html
    const walker = doc.createTreeWalker(root, NodeFilter.SHOW_TEXT)
    const nodes = []
    while (walker.nextNode()) nodes.push(walker.currentNode)
    for (const node of nodes) {
        if (node.parentElement?.tagName === 'CODE' || !node.nodeValue?.includes('\n')) continue
        const fragments = node.nodeValue.split('\n').flatMap((text, index, all) => index === all.length - 1
            ? [doc.createTextNode(text)]
            : [doc.createTextNode(text), doc.createElement('br')])
        node.replaceWith(...fragments)
    }
    return root.innerHTML.trim()
}

export const htmlToMarkdown = (html, keeps = [], turndownConfig = DEFAULT_TURNDOWN_CONFIG) => {
    const service = new TurndownService(turndownConfig)
    service.use(gfm)
    service.addRule('multiplemath', {
        filter: node => node.nodeName === 'PRE' && node.classList.contains('multiple-math'),
        replacement: content => `\n\n$$\n${content}\n$$\n\n`
    })
    if (keeps.length) service.keep(keeps)
    return service.turndown(turnSoftBreakToBr(html.replace(/<span>&nbsp;<\/span>/g, String.fromCharCode(160))))
}

export { DEFAULT_TURNDOWN_CONFIG }
