const rendererCache = new Map()
/**
 *
 * @param {string} name the renderer name: katex, sequence, plantuml, flowchart, mermaid, vega-lite
 */
const loadRenderer = async (name) => {
  if (!rendererCache.has(name)) {
    let m
    switch (name) {
      case 'sequence':
        m = await import(/* webpackChunkName: "sequence" */ '../parser/render/sequence')
        rendererCache.set(name, m.default)
        break
      case 'plantuml':
        m = await import(/* webpackChunkName: "plantuml" */ '../parser/render/plantuml')
        rendererCache.set(name, m.default)
        break
      case 'flowchart':
        m = await import(/* webpackChunkName: "flowchart" */ 'flowchart.js')
        rendererCache.set(name, m.default)
        break
      case 'mermaid':
        m = await import(/* webpackChunkName: "mermaid" */ 'mermaid/dist/mermaid.core.js')
        rendererCache.set(name, m.default)
        break
      case 'vega-lite':
        m = await import(/* webpackChunkName: "vega" */ 'vega-embed')
        rendererCache.set(name, m.default)
        break
      default:
        throw new Error(`Unknown diagram name ${name}`)
    }
  }

  return rendererCache.get(name)
}

export default loadRenderer
