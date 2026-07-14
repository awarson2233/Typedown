import { MarkdownToHtml } from '@muyajs/core'
import DOMPurify from 'dompurify'
import footerHeaderCss from '!!raw-loader!../assets/styles/headerFooterStyle.css'

class ExportHtml {
  constructor(markdown, options) {
    this.markdown = markdown
    this.options = options
  }

  async generate(options) {
    const { title, extraCss, extraHead, extraBody } = options
    const renderer = new MarkdownToHtml(this.markdown)
    const article = await renderer.renderHtml()
    const body = this._prepareHtml(article, options)
    const shell = await renderer.generate({
      title,
      inlineStyles: true,
      extraCSS: `${extraCss || ''}${options.header || options.footer ? footerHeaderCss : ''}`
    })
    const parsed = new DOMParser().parseFromString(shell, 'text/html')
    parsed.body.innerHTML = `${body}${extraBody || ''}`
    if (extraHead) parsed.head.insertAdjacentHTML('beforeend', DOMPurify.sanitize(extraHead))
    return '<!DOCTYPE html>\n' + parsed.documentElement.outerHTML
  }

  _prepareHtml(html, options) {
    const { header, footer } = options
    if (!header && !footer) return html

    let output = '<table class="page-container">'
    if (header) output += createTableHeader(options)
    if (footer) {
      output += HF_TABLE_FOOTER
      output = createRealFooter(options) + output
    }
    output += `<tbody><tr><td><div class="main-container">${html}</div></td></tr></tbody></table>`
    return DOMPurify.sanitize(output)
  }
}

const safe = value => DOMPurify.sanitize(String(value || ''))
const styledClass = value => value === undefined ? '' : (!value ? ' simple' : ' styled')
const createTableHeader = options => {
  const { type, left, center, right } = options.header
  return `<thead class="page-header ${type === 1 ? 'single' : ''}${styledClass(options.headerFooterStyled)}"><tr><th><div class="hf-container"><div class="header-content-left">${safe(left)}</div><div class="header-content">${safe(center)}</div><div class="header-content-right">${safe(right)}</div></div></th></tr></thead>`
}
const HF_TABLE_FOOTER = '<tfoot class="page-footer-fake"><tr><td><div class="hf-container">&nbsp;</div></td></tr></tfoot>'
const createRealFooter = options => {
  const { type, left, center, right } = options.footer
  return `<div class="page-footer ${type === 1 ? 'single' : ''}${styledClass(options.headerFooterStyled)}"><div class="hf-container"><div class="footer-content-left">${safe(left)}</div><div class="footer-content">${safe(center)}</div><div class="footer-content-right">${safe(right)}</div></div></div>`
}

export default ExportHtml
