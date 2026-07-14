import { n as e } from "./chunk-5HE753X5-CJhmzr4m.mjs";
import { n as t } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n } from "./src-upoOno_g.mjs";
import "./chunk-WYO6CB5R-_U4dPty4.mjs";
import "./chunk-VAUOI2AC-DvRR_ttS.mjs";
import { n as r, r as i, t as a } from "./chunk-MOJQB5TN-U2w9jYtv.mjs";
import { t as o } from "./chunk-JWPE2WC7-PoIEzJPT.mjs";
import { t as s } from "./mermaid-parser.core-Dnt89Q92.mjs";
//#region node_modules/mermaid/dist/chunks/mermaid.core/abnfDiagram-VRR7QNED.mjs
var c = e().RailroadAbnf.parser.LangiumParser, l = /* @__PURE__ */ t((e) => {
	let t = e.alternatives.map(u);
	return t.length === 1 ? t[0] : {
		type: "choice",
		alternatives: t
	};
}, "transformAlternation"), u = /* @__PURE__ */ t((e) => {
	let t = e.elements.map(f);
	return t.length === 1 ? t[0] : {
		type: "sequence",
		elements: t
	};
}, "transformConcatenation"), d = /* @__PURE__ */ t((e) => {
	if (e.includes("*")) {
		let [t, n] = e.split("*");
		return {
			min: t ? parseInt(t, 10) : 0,
			max: n ? parseInt(n, 10) : Infinity
		};
	}
	let t = parseInt(e, 10);
	return {
		min: t,
		max: t
	};
}, "parseRepeat"), f = /* @__PURE__ */ t((e) => {
	let t = p(e.primary);
	if (!e.repeat) return t;
	let { min: n, max: r } = d(e.repeat);
	return n === 0 && r === 1 ? {
		type: "optional",
		element: t
	} : {
		type: "repetition",
		element: t,
		min: n,
		max: r
	};
}, "transformElement"), p = /* @__PURE__ */ t((e) => {
	switch (e.$type) {
		case "AbnfStringLiteral": return {
			type: "terminal",
			value: e.value
		};
		case "AbnfNumVal": return {
			type: "terminal",
			value: e.value
		};
		case "AbnfRuleName": return {
			type: "nonterminal",
			name: e.name
		};
		case "AbnfGroup": return l(e.element);
		case "AbnfOptionalGroup": return {
			type: "optional",
			element: l(e.element)
		};
		default: throw Error(`Unsupported ABNF primary node: ${e.$type}`);
	}
}, "transformPrimary"), m = /* @__PURE__ */ t((e) => ({
	name: e.name,
	definition: l(e.definition)
}), "transformRule"), h = /* @__PURE__ */ t((e) => {
	o(e, a), e.title && a.setTitle(e.title), e.rules.map((e) => a.addRule(m(e)));
}, "populateDb"), g = {
	parser: {
		parse: /* @__PURE__ */ t((e) => {
			a.clear(), n.debug("[ABNF Parser] Starting Langium parse");
			let t = c.parse(e);
			if (t.lexerErrors.length > 0 || t.parserErrors.length > 0) throw new s(t);
			let r = t.value;
			n.debug("[ABNF Parser] Parsed rules:", r.rules.length), h(r), n.debug("[ABNF Parser] Parse complete");
		}, "parse"),
		parser: { yy: a }
	},
	db: a,
	renderer: i,
	styles: r
};
//#endregion
export { g as diagram };
