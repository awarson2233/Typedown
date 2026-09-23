/*eslint-disable*/
var path = require("path")
var webpack = require("webpack")

const paths = require('react-scripts/config/paths')
// 宿主工程在 WinUI3 迁移时由 Typedown 更名为 Typedown.WinUI，输出路径随之调整；
// TYPEDOWN_EDITOR_BUILD_OUTPUT 供 MSBuild 在非默认暂存目录下构建时覆盖。
paths.appBuild = process.env.TYPEDOWN_EDITOR_BUILD_OUTPUT
    ? path.resolve(__dirname, process.env.TYPEDOWN_EDITOR_BUILD_OUTPUT)
    : path.resolve(__dirname, '../Typedown.WinUI/Resources/Statics')

module.exports = function override(config, env) {
    const overrideConfig = {
        ...config,
        module: {
            ...config.module,
            rules: [
                ...config.module.rules,
                {
                    test: require.resolve(path.join(__dirname, './src/assets/libs/snap.svg-min.js')),
                    use: 'imports-loader?this=>window,fix=>module.exports=0'
                }
            ]
        },
        resolve: {
            ...config.resolve,
            alias: {
                ...config.resolve.alias,
                snapsvg: path.join(__dirname, './src/assets/libs/snap.svg-min.js')
            },
        },
        // 不再用 LimitChunkCountPlugin({ maxChunks: 1 }) 把 import() 并回主包：图表引擎、prism 语言、
        // 源码模式的 CodeMirror 各自成块，按需加载。publicPath 是 CRA 按 homepage "." 给的 "./"，
        // 分块相对 index.html 解析，file:// 与开发服务器下都能加载。
        plugins: [
            ...config.plugins,
            new webpack.ProvidePlugin({
                process: 'process/browser',
            }),
        ]
    }

    return overrideConfig;
}