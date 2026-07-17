/*eslint-disable*/
var path = require("path")
var webpack = require("webpack")

const paths = require('react-scripts/config/paths')
paths.appBuild = process.env.TYPEDOWN_EDITOR_BUILD_OUTPUT
    ? path.resolve(__dirname, process.env.TYPEDOWN_EDITOR_BUILD_OUTPUT)
    : path.resolve(__dirname, '../Typedown.WinUI/Resources/Statics')

module.exports = function override(config, env) {
    const muyaCoreCjs = path.join(__dirname, './vendor/muya-core/lib/cjs/index.js')
    const moduleScopePlugin = config.resolve.plugins.find(plugin => plugin.constructor.name === 'ModuleScopePlugin')

    if (moduleScopePlugin) {
        moduleScopePlugin.allowedFiles.add(muyaCoreCjs)
        moduleScopePlugin.allowedPaths.push(path.dirname(muyaCoreCjs))
        moduleScopePlugin.allowedPaths.push(path.resolve(__dirname, './vendor/muya-core'))
    }

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
                '@muyajs/core$': muyaCoreCjs,
                snapsvg: path.join(__dirname, './src/assets/libs/snap.svg-min.js')
            },
        },
        plugins: [
            ...config.plugins,
            new webpack.optimize.LimitChunkCountPlugin({
                maxChunks: 1
            }),
            new webpack.ProvidePlugin({
                process: 'process/browser.js',
            }),
        ]
    }

    return overrideConfig;
}