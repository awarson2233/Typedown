import transport from "./transport"

// 分数缩放（如 175%）下 CSS 视口宽度是小数，innerWidth 与 scrollWidth 各自取整后可能差出 1px，
// 宿主就会据此画出一条根本滚不动的滚动条。不超过 1px 的"溢出"一律视为没有。
const EPSILON = 1

const overflow = (content: number, viewport: number) => {
    const delta = content - viewport
    return delta <= EPSILON ? 0 : delta
}

let pendingFrame: number | undefined

// 尺寸、滚动、宿主请求可能在同一帧里接连触发；合并到下一帧，读的是布局稳定后的尺寸。
const postScrollState = () => {
    if (pendingFrame !== undefined) return
    pendingFrame = requestAnimationFrame(() => {
        pendingFrame = undefined
        transport.postMessage('OnScroll', {
            viewportWidth: window.innerWidth,
            viewportHeight: window.innerHeight,
            maximumX: overflow(document.body.scrollWidth, window.innerWidth),
            maximumY: overflow(document.body.scrollHeight, window.innerHeight),
            scrollX: window.scrollX,
            scrollY: window.scrollY
        })
    })
}

const resizeObserver = new ResizeObserver(postScrollState)
// body 带 min-width，视口窄于它之后 body 不再变化，只有根元素还跟着视口变
resizeObserver.observe(document.documentElement)
resizeObserver.observe(document.body)
addEventListener('scroll', postScrollState)
addEventListener('resize', postScrollState)
// WinUI3 的 WebView2 在 XAML 布局之后才异步更新内核视口，宿主布局定型后会请求补报一次
transport.addListener('RefreshScrollState', postScrollState)

transport.addListener<{ scrollX: number, scrollY: number }>('OnScroll', ({ scrollX, scrollY }) => {
    const equals = (a: number, b: number) => Math.abs(a - b) < 1
    if (!equals(scrollX, window.scrollX) || !equals(scrollY, window.scrollY))
        window.scrollTo(scrollX, scrollY)
})
