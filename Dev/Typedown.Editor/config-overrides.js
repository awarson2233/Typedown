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
        plugins: [
            ...config.plugins,
            new webpack.optimize.LimitChunkCountPlugin({
                maxChunks: 1
            }),
            new webpack.ProvidePlugin({
                process: 'process/browser',
            }),
        ]
    }

    return overrideConfig;
}