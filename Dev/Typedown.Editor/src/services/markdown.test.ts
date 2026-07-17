import { normalizeMarkdownLineEndings } from './markdown'

describe('normalizeMarkdownLineEndings', () => {
    it.each([
        ['CRLF', 'first\r\nsecond\r\n', 'first\nsecond\n'],
        ['CR', 'first\rsecond\r', 'first\nsecond\n'],
        ['LF', 'first\nsecond\n', 'first\nsecond\n'],
        ['mixed', 'first\r\nsecond\rthird\nfourth', 'first\nsecond\nthird\nfourth']
    ])('normalizes %s line endings', (_name, markdown, expected) => {
        expect(normalizeMarkdownLineEndings(markdown)).toBe(expected)
    })
})
