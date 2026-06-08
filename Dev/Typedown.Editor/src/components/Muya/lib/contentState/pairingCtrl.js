/* eslint-disable no-useless-escape */
import { tokenizer } from '../parser/'

const BRACKET_HASH = {
  '{': '}',
  '[': ']',
  '(': ')',
  '*': '*',
  _: '_',
  '`': '`',
  '"': '"',
  "'": "'",
  $: '$',
  '~': '~'
}

const BACK_HASH = {
  '}': '{',
  ']': '[',
  ')': '(',
  '*': '*',
  _: '_',
  '`': '`',
  '"': '"',
  "'": "'",
  $: '$',
  '~': '~'
}

const MARKDOWN_PAIR_REG = /[*$`~_]{1}/

const canPairChar = (char, { autoPairBracket, autoPairMarkdownSyntax, autoPairQuote }) => {
  return (autoPairQuote && /['"]{1}/.test(char)) ||
    (autoPairBracket && /[\{\}\[\]\(\)]{1}/.test(char)) ||
    (autoPairMarkdownSyntax && MARKDOWN_PAIR_REG.test(char))
}

const checkCursorInTokenType = (functionType, text, offset, type, options) => {
  if (!/atxLine|paragraphContent|cellContent/.test(functionType)) {
    return false
  }

  const tokens = tokenizer(text, {
    hasBeginRules: false,
    options
  })
  return tokens.filter(t => t.type === type).some(t => offset >= t.range.start && offset <= t.range.end)
}

export const resolveTypingPairing = ({ block, text, start, end, event, options = {} }) => {
  if (!block || !start || !end) {
    return null
  }

  if (start.key !== end.key || start.offset !== end.offset || event.type !== 'input') {
    return null
  }

  const { autoPairBracket, autoPairMarkdownSyntax, autoPairQuote } = options
  const { offset } = start
  const inputChar = text.charAt(+offset - 1)
  const preInputChar = text.charAt(+offset - 2)
  const prePreInputChar = text.charAt(+offset - 3)
  const postInputChar = text.charAt(+offset)
  const deletedChar = block.text[offset]
  let startOffset = start.offset
  let endOffset = end.offset
  let needRender = false

  const isInInlineMath = checkCursorInTokenType(block.functionType, text, offset, 'inline_math', options)
  const isInInlineCode = checkCursorInTokenType(block.functionType, text, offset, 'inline_code', options)
  const canPairMarkdownSyntax = block.functionType !== 'codeContent' && !isInInlineMath && !isInInlineCode

  if (/^delete/.test(event.inputType)) {
    if (canPairChar(deletedChar, options) && event.inputType === 'deleteContentBackward' && postInputChar === BRACKET_HASH[deletedChar]) {
      needRender = true
      text = text.substring(0, offset) + text.substring(offset + 1)
    }
    if (canPairChar(deletedChar, options) && event.inputType === 'deleteContentForward' && inputChar === BACK_HASH[deletedChar]) {
      needRender = true
      startOffset -= 1
      endOffset -= 1
      text = text.substring(0, offset - 1) + text.substring(offset)
    }
  } else if (
    inputChar === postInputChar &&
    (
      (autoPairQuote && /[']{1}/.test(inputChar)) ||
      (autoPairQuote && /["]{1}/.test(inputChar)) ||
      (autoPairBracket && /[\}\]\)]{1}/.test(inputChar)) ||
      (autoPairMarkdownSyntax && canPairMarkdownSyntax && /[$]{1}/.test(inputChar)) ||
      (autoPairMarkdownSyntax && canPairMarkdownSyntax && MARKDOWN_PAIR_REG.test(inputChar) && /[_*~]{1}/.test(prePreInputChar))
    )
  ) {
    needRender = true
    text = text.substring(0, offset) + text.substring(offset + 1)
  } else {
    /* eslint-disable no-useless-escape */
    if (
      // Issue 2566: Do not complete markdown syntax if the previous character is
      // alphanumeric.
      !/\\/.test(preInputChar) &&
      ((autoPairQuote && /[']{1}/.test(inputChar) && !(/[\S]{1}/.test(postInputChar)) && !(/[a-zA-Z\d]{1}/.test(preInputChar))) ||
        (autoPairQuote && /["]{1}/.test(inputChar) && !(/[\S]{1}/.test(postInputChar))) ||
        (autoPairBracket && /[\{\[\(]{1}/.test(inputChar) && !(/[\S]{1}/.test(postInputChar))) ||
        (canPairMarkdownSyntax && autoPairMarkdownSyntax && !/[a-z0-9]{1}/i.test(preInputChar) && MARKDOWN_PAIR_REG.test(inputChar)))
    ) {
      needRender = true
      text = BRACKET_HASH[event.data]
        ? text.substring(0, offset) + BRACKET_HASH[inputChar] + text.substring(offset)
        : text
    }
    /* eslint-enable no-useless-escape */

    // Delete the last `*` of `**` when you insert one space between `**` to create a bullet list.
    if (
      /\s/.test(event.data) &&
      /^\* /.test(text) &&
      preInputChar === '*' &&
      postInputChar === '*'
    ) {
      text = text.substring(0, offset) + text.substring(offset + 1)
      needRender = true
    }
  }

  if (!needRender) {
    return null
  }

  return {
    text,
    startOffset,
    endOffset,
    needRender
  }
}

const pairingCtrl = ContentState => {
  ContentState.prototype.resolveTypingPairing = function (payload) {
    return resolveTypingPairing({
      ...payload,
      options: this.muya.options
    })
  }
}

export default pairingCtrl
