import { generateGithubSlug } from '@muyajs/core'
import DOMPurify from 'dompurify'
import execAll from 'execall'

export const getTOC = (markdown: string) => {
    const toc: any[] = []
    const seen = new Map<string, number>()
    const lines = markdown.split('\n')
    for (let lineNumber = 0; lineNumber < lines.length; lineNumber++) {
        const match = /^ {0,3}(#{1,6})(?:\s+(.*)|\s*)$/.exec(lines[lineNumber])
        if (!match) continue
        const content = (match[2] ?? '').trim()
        const base = generateGithubSlug(content) || 'heading'
        const index = seen.get(base) ?? 0
        seen.set(base, index + 1)
        toc.push({ content, lvl: match[1].length, slug: index ? `${base}-${index}` : base, line: lineNumber })
    }
    return { toc }
}

export const matchString = (text: string, value: string, options: any) => {
    const { searchIsCaseSensitive, searchIsWholeWord, searchIsRegexp } = options
    /* eslint-disable no-useless-escape */
    const SPECIAL_CHAR_REG = /[\[\]\\^$.\|\?\*\+\(\)\/]{1}/g
    /* eslint-enable no-useless-escape */
    let regStr = value
    let flag = 'g'
    if (!searchIsCaseSensitive) flag += 'i'
    if (!searchIsRegexp) regStr = value.replace(SPECIAL_CHAR_REG, p => p === '\\' ? '\\\\' : `\\${p}`)
    if (searchIsWholeWord) regStr = `\\b${regStr}\\b`
    try { return execAll(new RegExp(regStr, flag), text) } catch { return [] }
}

export const generateHtmlToc = (tocList: any[], currentLevel: number, options: any): string => {
    if (!tocList?.length) return ''
    const topLevel = tocList[0].lvl
    if (!options.tocIncludeTopHeading && topLevel <= 1) {
        tocList.shift()
        return generateHtmlToc(tocList, currentLevel, options)
    }
    if (topLevel <= currentLevel) return ''
    const { content, lvl, slug } = tocList.shift()
    let html = `<li><span><a class="toc-h${lvl}" href="#${slug}">${content}</a><span class="dots"></span></span>`
    if (tocList.length && tocList[0].lvl > lvl) html += '<ul>' + generateHtmlToc(tocList, lvl, options) + '</ul>'
    return html + '</li>' + generateHtmlToc(tocList, currentLevel, options)
}

export const getHtmlToc = (toc: any, options: any = {}) => {
    const tocList = generateHtmlToc(toc.map((item: any) => ({ ...item })), 0, options)
    if (!tocList) return ''
    const title = options.tocTitle || 'Table of Contents'
    return DOMPurify.sanitize(`<div class="toc-container"><p class="toc-title">${title}</p><ul class="toc-list">${tocList}</ul></div>`)
}
