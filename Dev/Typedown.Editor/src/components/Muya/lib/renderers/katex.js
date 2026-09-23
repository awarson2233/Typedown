// katex 与 mhchem 在模块求值时就要建大量符号表与宏，放在主包里会拖慢每次启动；
// 改为第一个公式出现（或导出）时才加载，加载完成前渲染器先放占位，见 StateRender.addPendingMath。
let katex = null
let loading = null

export const getKatex = () => katex

export const loadKatex = () => {
  if (!loading) {
    loading = import(/* webpackChunkName: "katex" */ './katexBundle').then(m => {
      katex = m.default
      return katex
    }, err => {
      loading = null
      throw err
    })
  }
  return loading
}
