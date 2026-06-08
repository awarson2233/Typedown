import { resolveTypingPairing } from './pairingCtrl'

describe('resolveTypingPairing', () => {
  const baseOptions = {
    autoPairBracket: true,
    autoPairQuote: true
  }

  const markdownOptions = {
    ...baseOptions,
    autoPairMarkdownSyntax: true
  }

  it('keeps markdown syntax literal when autoPairMarkdownSyntax is disabled', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'paragraphContent',
        text: '测试*案例*'
      },
      text: '测试*案例*',
      start: { key: 'a', offset: 3 },
      end: { key: 'a', offset: 3 },
      event: {
        type: 'input',
        inputType: 'insertText',
        data: '*'
      },
      options: baseOptions
    })

    expect(result).toBeNull()
  })

  it('auto pairs markdown syntax delimiters', () => {
    for (const delimiter of ['*', '_', '`', '$', '~']) {
      const result = resolveTypingPairing({
        block: {
          functionType: 'paragraphContent',
          text: delimiter
        },
        text: delimiter,
        start: { key: 'a', offset: 1 },
        end: { key: 'a', offset: 1 },
        event: {
          type: 'input',
          inputType: 'insertText',
          data: delimiter
        },
        options: markdownOptions
      })

      expect(result).toEqual({
        text: `${delimiter}${delimiter}`,
        startOffset: 1,
        endOffset: 1,
        needRender: true
      })
    }
  })

  it('skips over paired markdown syntax delimiters', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'paragraphContent',
        text: '****'
      },
      text: '*****',
      start: { key: 'a', offset: 3 },
      end: { key: 'a', offset: 3 },
      event: {
        type: 'input',
        inputType: 'insertText',
        data: '*'
      },
      options: markdownOptions
    })

    expect(result).toEqual({
      text: '****',
      startOffset: 3,
      endOffset: 3,
      needRender: true
    })
  })

  it('keeps literal adjacent markdown delimiters in code blocks', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'codeContent',
        text: '*'
      },
      text: '**',
      start: { key: 'a', offset: 1 },
      end: { key: 'a', offset: 1 },
      event: {
        type: 'input',
        inputType: 'insertText',
        data: '*'
      },
      options: markdownOptions
    })

    expect(result).toBeNull()
  })

  it('keeps literal adjacent markdown delimiters in inline code', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'paragraphContent',
        text: '`*`'
      },
      text: '`**`',
      start: { key: 'a', offset: 2 },
      end: { key: 'a', offset: 2 },
      event: {
        type: 'input',
        inputType: 'insertText',
        data: '*'
      },
      options: markdownOptions
    })

    expect(result).toBeNull()
  })

  it('keeps literal adjacent markdown delimiters in inline math', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'paragraphContent',
        text: '$*$'
      },
      text: '$**$',
      start: { key: 'a', offset: 2 },
      end: { key: 'a', offset: 2 },
      event: {
        type: 'input',
        inputType: 'insertText',
        data: '*'
      },
      options: markdownOptions
    })

    expect(result).toBeNull()
  })

  it('keeps literal emphasis delimiter inserted before an existing delimiter', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'paragraphContent',
        text: 'a*b'
      },
      text: 'a**b',
      start: { key: 'a', offset: 2 },
      end: { key: 'a', offset: 2 },
      event: {
        type: 'input',
        inputType: 'insertText',
        data: '*'
      },
      options: markdownOptions
    })

    expect(result).toBeNull()
  })

  it('deletes the matching markdown delimiter when deleting one side of a pair', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'paragraphContent',
        text: '**'
      },
      text: '*',
      start: { key: 'a', offset: 0 },
      end: { key: 'a', offset: 0 },
      event: {
        type: 'input',
        inputType: 'deleteContentBackward',
        data: null
      },
      options: markdownOptions
    })

    expect(result).toEqual({
      text: '',
      startOffset: 0,
      endOffset: 0,
      needRender: true
    })
  })

  it('does not auto pair markdown delimiters inside code blocks', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'codeContent',
        text: '*'
      },
      text: '*',
      start: { key: 'a', offset: 1 },
      end: { key: 'a', offset: 1 },
      event: {
        type: 'input',
        inputType: 'insertText',
        data: '*'
      },
      options: markdownOptions
    })

    expect(result).toBeNull()
  })

  it('does not keep * * when inserting a space between paired asterisks for a bullet list', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'paragraphContent',
        text: '**'
      },
      text: '* *',
      start: { key: 'a', offset: 2 },
      end: { key: 'a', offset: 2 },
      event: {
        type: 'input',
        inputType: 'insertText',
        data: ' '
      },
      options: markdownOptions
    })

    expect(result).toEqual({
      text: '* ',
      startOffset: 2,
      endOffset: 2,
      needRender: true
    })
  })

  it('still auto pairs brackets', () => {
    const result = resolveTypingPairing({
      block: {
        functionType: 'paragraphContent',
        text: 'test('
      },
      text: 'test(',
      start: { key: 'a', offset: 5 },
      end: { key: 'a', offset: 5 },
      event: {
        type: 'input',
        inputType: 'insertText',
        data: '('
      },
      options: baseOptions
    })

    expect(result).toEqual({
      text: 'test()',
      startOffset: 5,
      endOffset: 5,
      needRender: true
    })
  })
})
