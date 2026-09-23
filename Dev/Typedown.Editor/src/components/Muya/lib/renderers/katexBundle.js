import katex from 'katex'
// ESM 入口与上面的 katex 共用 katex.mjs；mhchem.min.js 会 require 到 CJS 的 katex.js，打进第二份且宏注册不到这一份。
import 'katex/contrib/mhchem'

export default katex
