import transport from "./transport"

const postScrollState = () => {
    const viewportWidth = document.documentElement.clientWidth || window.innerWidth
    const viewportHeight = document.documentElement.clientHeight || window.innerHeight
    const maximumXRaw = Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - viewportWidth
    const maximumYRaw = Math.max(document.documentElement.scrollHeight, document.body.scrollHeight) - viewportHeight
    const epsilon = 1
    const maximumX = maximumXRaw <= epsilon ? 0 : maximumXRaw
    const maximumY = maximumYRaw <= epsilon ? 0 : maximumYRaw

    transport.postMessage('OnScroll', {
        viewportWidth,
        viewportHeight,
        maximumX,
        maximumY,
        scrollX: window.scrollX,
        scrollY: window.scrollY
    })
}

const resizeObserver = new ResizeObserver(postScrollState)
resizeObserver.observe(document.body)
addEventListener('scroll', postScrollState)
addEventListener('resize', postScrollState)
transport.addListener('RefreshScrollState', postScrollState)

transport.addListener<{ scrollX: number, scrollY: number }>('OnScroll', ({ scrollX, scrollY }) => {
    const equals = (a: number, b: number) => Math.abs(a - b) < 1
    if (!equals(scrollX, window.scrollX) || !equals(scrollY, window.scrollY))
        window.scrollTo(scrollX, scrollY)
})