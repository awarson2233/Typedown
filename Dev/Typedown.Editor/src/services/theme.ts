import transport from 'services/transport';
import { remote } from 'services/remote';

transport.addListener('ThemeChanged', onThemeChanged)

remote.getCurrentTheme().then(arg => {
    onThemeChanged(arg);
    setTimeout(() => remote.contentLoaded(), 0);
})

function getorCreateStyle(id: string) {
    let style = document.getElementById(id) as HTMLLinkElement;
    if (!style) {
        style = document.createElement("link");
        style.rel = "stylesheet";
        style.id = id;
        document.head.appendChild(style)
    }
    return style;
}

interface IColor { r: number, g: number, b: number, a: number }

// 宿主经 Newtonsoft + CamelCase 序列化 EditorThemePayload，线上是小写键（theme、background.r 等）；
// 这里同时接受大写键，避免载荷写法变动时背景色静默失效（rgba(undefined, …) 会被浏览器丢弃）。
function readColor(color: any): IColor | undefined {
    if (!color) {
        return undefined
    }
    const r = color.r ?? color.R
    const g = color.g ?? color.G
    const b = color.b ?? color.B
    const a = color.a ?? color.A ?? 1
    return [r, g, b, a].every(v => typeof v === 'number') ? { r, g, b, a } : undefined
}

function onThemeChanged(payload: any) {
    const editorStyleDocument = getorCreateStyle("link_style_editor");
    const prismjsStyleDocument = getorCreateStyle("link_style_prismjs");
    const codemirrorStyleDocument = getorCreateStyle("link_style_codemirror");

    const theme = (payload?.theme ?? payload?.Theme)?.toLowerCase()
    editorStyleDocument.href = `theme/editor/${theme}.theme.css`
    prismjsStyleDocument.href = `theme/prismjs/${theme}.theme.css`
    codemirrorStyleDocument.href = `theme/codemirror/${theme}.theme.css`

    const themeColorAlphas = [10, 20, 30, 40, 50, 60, 70, 80, 90]
    const accentColor = readColor(payload?.accentColor ?? payload?.AccentColor)
    const background = readColor(payload?.background ?? payload?.Background)

    if (background) {
        // 宿主的文档创建脚本在首帧前把 html 与 body 都刷成了启动时的主题色，切换主题时两处一起改。
        const color = `rgba(${background.r}, ${background.g}, ${background.b}, ${background.a})`
        document.documentElement.style.backgroundColor = color;
        document.body.style.backgroundColor = color;
    }
    document.documentElement.style.setProperty('--actualTheme', theme)
    if (accentColor) {
        const { r, g, b, a } = accentColor
        document.documentElement.style.setProperty('--themeColor', `rgba(${r}, ${g}, ${b}, ${a})`)
        themeColorAlphas.forEach(e => document.documentElement.style.setProperty(`--themeColor${e}`, `rgba(${r}, ${g}, ${b}, ${a * (e / 100)})`))
    }

    window.actualTheme = theme
}