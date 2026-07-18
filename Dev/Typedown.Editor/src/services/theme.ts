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

function getColorComponent(color: any, upperKey: string, lowerKey: string) {
    return color?.[lowerKey] ?? color?.[upperKey]
}

function normalizeTheme(theme: any) {
    const value = typeof theme === 'string' ? theme.toLowerCase() : '';
    return value === 'dark' || value === 'light' ? value : 'light';
}

function clampNumber(value: any, fallback: number, min: number, max: number) {
    const numberValue = Number(value);
    if (!Number.isFinite(numberValue)) {
        return fallback;
    }

    return Math.min(Math.max(numberValue, min), max);
}

function normalizeColor(color: any, fallback: { r: number, g: number, b: number, a: number }) {
    return {
        r: clampNumber(getColorComponent(color, 'R', 'r'), fallback.r, 0, 255),
        g: clampNumber(getColorComponent(color, 'G', 'g'), fallback.g, 0, 255),
        b: clampNumber(getColorComponent(color, 'B', 'b'), fallback.b, 0, 255),
        a: clampNumber(getColorComponent(color, 'A', 'a'), fallback.a, 0, 1)
    };
}

function formatRgba(color: { r: number, g: number, b: number, a: number }) {
    return `rgba(${color.r}, ${color.g}, ${color.b}, ${color.a})`;
}

function onThemeChanged(payload: any) {
    const editorStyleDocument = getorCreateStyle("link_style_editor");
    const prismjsStyleDocument = getorCreateStyle("link_style_prismjs");
    const codemirrorStyleDocument = getorCreateStyle("link_style_codemirror");

    const theme = normalizeTheme(payload?.theme)
    editorStyleDocument.href = `theme/v2/editor/${theme}.theme.css`
    prismjsStyleDocument.href = `theme/v2/prism/${theme}.theme.css`
    codemirrorStyleDocument.href = `theme/v2/codemirror/${theme}.theme.css`

    const themeColorSteps = [10, 20, 30, 40, 50, 60, 70, 80, 90]
    const accent = normalizeColor(payload?.accentColor, { r: 27, g: 102, b: 107, a: 1 })
    const defaultBackground = theme === 'dark'
        ? { r: 40, g: 40, b: 40, a: 1 }
        : { r: 249, g: 249, b: 249, a: 1 };
    const bg = normalizeColor(payload?.background, defaultBackground)

    const foreground = theme === 'dark'
        ? { r: 242, g: 242, b: 242, a: 1 }
        : { r: 31, g: 31, b: 31, a: 1 }

    document.body.style.backgroundColor = formatRgba(bg);
    document.documentElement.style.setProperty('--actual-theme', theme)
    document.documentElement.style.setProperty('--editor-background', formatRgba(bg))
    document.documentElement.style.setProperty('--theme-color', formatRgba(accent))
    document.documentElement.style.setProperty('--editor-color', formatRgba(foreground))
    document.documentElement.style.setProperty('--editor-color-80', formatRgba({ ...foreground, a: 0.8 }))
    document.documentElement.style.setProperty('--editor-color-50', formatRgba({ ...foreground, a: 0.5 }))
    document.documentElement.style.setProperty('--editor-color-30', formatRgba({ ...foreground, a: 0.3 }))
    document.documentElement.style.setProperty('--editor-color-10', formatRgba({ ...foreground, a: 0.1 }))
    document.documentElement.style.setProperty('--editor-color-04', formatRgba({ ...foreground, a: 0.04 }))
    document.documentElement.style.setProperty('--editor-bg-color', formatRgba(bg))
    document.documentElement.style.setProperty('--code-block-bg-color', formatRgba({ ...foreground, a: 0.04 }))
    document.documentElement.style.setProperty('--table-border-color', formatRgba({ ...foreground, a: 0.1 }))
    document.documentElement.style.setProperty('--input-bg-color', formatRgba({ ...foreground, a: 0.06 }))
    document.documentElement.style.setProperty('--button-font-color', formatRgba(foreground))
    document.documentElement.style.setProperty('--button-bg-color', formatRgba(bg))
    document.documentElement.style.setProperty('--button-bg-color-hover', formatRgba({ ...foreground, a: 0.04 }))
    document.documentElement.style.setProperty('--button-border', `1px solid ${formatRgba({ ...foreground, a: 0.15 })}`)
    document.documentElement.style.setProperty('--float-bg-color', formatRgba(bg))
    document.documentElement.style.setProperty('--float-hover-color', formatRgba({ ...foreground, a: 0.06 }))
    document.documentElement.style.setProperty('--float-border-color', formatRgba({ ...foreground, a: 0.15 }))
    document.documentElement.style.setProperty('--icon-color', formatRgba({ ...foreground, a: 0.7 }))
    document.documentElement.style.setProperty('--selection-color', formatRgba({ ...accent, a: accent.a * 0.3 }))
    themeColorSteps.forEach(e => document.documentElement.style.setProperty(`--theme-color-${e}`, formatRgba({ ...accent, a: accent.a * (e / 100) })))

    window.actualTheme = theme
}