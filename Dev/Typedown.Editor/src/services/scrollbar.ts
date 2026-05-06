import transport from "./transport"

const epsilon = 1

const normalizeOverflow = (value: number) => value <= epsilon ? 0 : value

const postScrollState = () => {
    const root = document.documentElement
    const body = document.body
    const viewportWidth = root?.clientWidth || window.innerWidth
    const viewportHeight = root?.clientHeight || window.innerHeight
    const contentWidth = Math.max(root?.scrollWidth || 0, body?.scrollWidth || 0)
    const contentHeight = Math.max(root?.scrollHeight || 0, body?.scrollHeight || 0)
    const maximumXRaw = Math.max(0, contentWidth - viewportWidth)
    const maximumYRaw = Math.max(0, contentHeight - viewportHeight)
    const maximumX = maximumXRaw <= epsilon ? 0 : maximumXRaw
    const maximumY = maximumYRaw <= epsilon ? 0 : maximumYRaw

    transport.postMessage('OnScroll', {
        viewportWidth,
        viewportHeight,
        maximumX,
        maximumY,
        scrollX: normalizeOverflow(maximumXRaw) > 0 ? window.scrollX : 0,
        scrollY: normalizeOverflow(maximumYRaw) > 0 ? window.scrollY : 0
    })
}

const resizeObserver = new ResizeObserver(postScrollState)
resizeObserver.observe(document.body)
addEventListener('scroll', postScrollState)
addEventListener('resize', postScrollState)

transport.addListener<{ scrollX: number, scrollY: number }>('OnScroll', ({ scrollX, scrollY }) => {
    const equals = (a: number, b: number) => Math.abs(a - b) < 1
    if (!equals(scrollX, window.scrollX) || !equals(scrollY, window.scrollY))
        window.scrollTo(scrollX, scrollY)
})
