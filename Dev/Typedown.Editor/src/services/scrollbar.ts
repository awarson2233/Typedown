import transport from "./transport"

const getScrollOwner = () => document.querySelector<HTMLElement>('.mu-editor, .CodeMirror-scroll')
    ?? document.scrollingElement as HTMLElement

let postFrame: number | undefined
const postScrollState = () => {
    if (postFrame !== undefined) cancelAnimationFrame(postFrame)
    postFrame = requestAnimationFrame(() => {
        postFrame = undefined
        const owner = getScrollOwner()
        const viewportWidth = owner.clientWidth
        const viewportHeight = owner.clientHeight
        const epsilon = 1
        const maximumXRaw = owner.scrollWidth - viewportWidth
        const maximumYRaw = owner.scrollHeight - viewportHeight
        const maximumX = maximumXRaw <= epsilon ? 0 : maximumXRaw
        const maximumY = maximumYRaw <= epsilon ? 0 : maximumYRaw

        transport.postMessageNoDiff('OnScroll', {
            viewportWidth,
            viewportHeight,
            maximumX,
            maximumY,
            scrollX: owner.scrollLeft,
            scrollY: owner.scrollTop
        })
    })
}

const resizeObserver = new ResizeObserver(postScrollState)
resizeObserver.observe(document.documentElement)
resizeObserver.observe(document.body)
const mutationObserver = new MutationObserver(postScrollState)
mutationObserver.observe(document.body, { childList: true, characterData: true, subtree: true })
addEventListener('scroll', postScrollState, true)
addEventListener('load', postScrollState, true)
addEventListener('resize', postScrollState)
transport.addListener('RefreshScrollState', postScrollState)

transport.addListener<{ scrollX: number, scrollY: number }>('OnScroll', ({ scrollX, scrollY }) => {
    const owner = getScrollOwner()
    const equals = (a: number, b: number) => Math.abs(a - b) < 1
    if (!equals(scrollX, owner.scrollLeft) || !equals(scrollY, owner.scrollTop))
        owner.scrollTo(scrollX, scrollY)
})
