import components from 'prismjs/components.js'
import getLoader from 'prismjs/dependencies'
import { getDefer } from '../utils'
/**
 * The set of all languages which have been loaded using the below function.
 *
 * @type {Set<string>}
 */
export const loadedLanguages = new Set(['markup', 'css', 'clike', 'javascript'])
export const languages = components.languages
export const alias = { 'c++': 'cpp' }

// 补充别名
Object.keys(alias).forEach(name => Object.assign(languages[alias[name]], { alias: [...languages[alias[name]].alias ?? [], name] }))

// Look for the origin languge by alias
export const transformAliasToOrigin = langs => {
  const result = []
  for (const lang of langs) {
    if (languages[lang]) {
      result.push(lang)
    } else {
      const language = Object.keys(languages).find(name => {
        const l = languages[name]
        if (l.alias) {
          return l.alias === lang || Array.isArray(l.alias) && l.alias.includes(lang)
        }
        return false
      })

      if (language) {
        result.push(language)
      } else {
        // The lang is not exist, the will handle in `initLoadLanguage`
        result.push(lang)
      }
    }
  }

  return result
}

function initLoadLanguage(Prism) {
  // 每种语言是一个独立的懒加载块，加载完成的先后不再等于调用的先后；
  // 同一语言只发起一次加载，并发的调用共用同一个 Promise。
  const languageLoads = new Map()
  const importLanguage = lang => {
    if (!languageLoads.has(lang)) {
      delete Prism.languages[lang]
      // 按前缀建的上下文会把同名的 .min.js 也收进来，排除掉以免每种语言打两份。
      languageLoads.set(lang, import(
        /* webpackChunkName: "prism/[request]" */
        /* webpackExclude: /\.min\.js$/ */
        'prismjs/components/prism-' + lang
      ).then(() => {
        loadedLanguages.add(lang)
      }, err => {
        languageLoads.delete(lang)
        throw err
      }))
    }
    return languageLoads.get(lang)
  }

  return async function loadLanguages(langs) {
    // If no argument is passed, load all components
    if (!langs) {
      langs = Object.keys(languages).filter(lang => lang !== 'meta')
    }

    if (langs && !langs.length) {
      return Promise.reject(new Error('The first parameter should be a list of load languages or single language.'))
    }

    if (!Array.isArray(langs)) {
      langs = [langs]
    }

    const promises = []
    // The user might have loaded languages via some other way or used `prism.js` which already includes some
    // We don't need to validate the ids because `getLoader` will ignore invalid ones
    const loaded = [...loadedLanguages, ...Object.keys(Prism.languages)]

    // getLoader 按依赖顺序回调（被依赖的语言在前），语言定义里会 extend 依赖语言，
    // 所以这里逐个串行执行，前一个语言的块执行完才加载下一个。
    let chain = Promise.resolve()
    getLoader(components, langs, loaded).load(lang => {
      const defer = getDefer()
      promises.push(defer.promise)
      chain = chain.then(async () => {
        if (!(lang in components.languages)) {
          defer.resolve({
            lang,
            status: 'noexist'
          })
        } else if (loadedLanguages.has(lang)) {
          defer.resolve({
            lang,
            status: 'cached'
          })
        } else {
          await importLanguage(lang)
          defer.resolve({
            lang,
            status: 'loaded'
          })
        }
      }).catch(err => {
        defer.reject(err)
      })
    })

    return Promise.all(promises)
  }
}

export default initLoadLanguage
