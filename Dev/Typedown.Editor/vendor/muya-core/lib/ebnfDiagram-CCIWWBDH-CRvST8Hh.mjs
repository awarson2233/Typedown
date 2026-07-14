import { n as e } from "./chunk-U6XO7XAA-BceJbjIt.mjs";
import { n as t } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n } from "./src-upoOno_g.mjs";
import "./chunk-WYO6CB5R-_U4dPty4.mjs";
import "./chunk-VAUOI2AC-DvRR_ttS.mjs";
import { n as r, r as i, t as a } from "./chunk-MOJQB5TN-U2w9jYtv.mjs";
import { t as o } from "./chunk-JWPE2WC7-PoIEzJPT.mjs";
import { t as s } from "./mermaid-parser.core-Dnt89Q92.mjs";
//#region node_modules/mermaid/dist/chunks/mermaid.core/ebnfDiagram-CCIWWBDH.mjs
var c = e().RailroadEbnf.parser.LangiumParser, l = /* @__PURE__ */ t((e) => {
	let t = e.alternatives.map(u);
	return t.length === 1 ? t[0] : {
		type: "choice",
		alternatives: t
	};
}, "transformChoice"), u = /* @__PURE__ */ t((e) => {
	let t = e.elements.map(p);
	return t.length === 1 ? t[0] : {
		type: "sequence",
		elements: t
	};
}, "transformSequence"), d = /* @__PURE__ */ t((e) => {
	switch (e.$type) {
		case "EbnfTerminal": return {
			type: "terminal",
			value: e.value
		};
		case "EbnfNonTerminal": return {
			type: "nonterminal",
			name: e.name
		};
		case "EbnfSpecial": return {
			type: "special",
			text: e.text
		};
		case "EbnfGroup": return l(e.element);
		case "EbnfOptional": return {
			type: "optional",
			element: l(e.element)
		};
		case "EbnfRepetition": return {
			type: "repetition",
			element: l(e.element),
			min: 0,
			max: Infinity
		};
		default: throw Error(`Unsupported EBNF primary node: ${e.$type}`);
	}
}, "transformPrimary"), f = /* @__PURE__ */ t((e, t) => {
	switch (t.$type) {
		case "EbnfOptionalPostfix": return {
			type: "optional",
			element: e
		};
		case "EbnfZeroOrMorePostfix": return {
			type: "repetition",
			element: e,
			min: 0,
			max: Infinity
		};
		case "EbnfOneOrMorePostfix": return {
			type: "repetition",
			element: e,
			min: 1,
			max: Infinity
		};
		case "EbnfExceptionPostfix": return {
			type: "sequence",
			elements: [
				e,
				{
					type: "terminal",
					value: "-"
				},
				d(t.except)
			]
		};
		default: throw Error(`Unsupported EBNF postfix node: ${t.$type}`);
	}
}, "transformPostfix"), p = /* @__PURE__ */ t((e) => e.postfixes.reduce((e, t) => f(e, t), d(e.base)), "transformTerm"), m = /* @__PURE__ */ t((e) => ({
	name: e.name,
	definition: l(e.definition)
}), "transformRule"), h = /* @__PURE__ */ t((e) => {
	o(e, a), e.title && a.setTitle(e.title), e.rules.map((e) => a.addRule(m(e)));
}, "populateDb"), g = {
	parser: {
		parse: /* @__PURE__ */ t((e) => {
			a.clear(), n.debug("[EBNF Parser] Starting Langium parse");
			let t = c.parse(e);
			if (t.lexerErrors.length > 0 || t.parserErrors.length > 0) throw new s(t);
			let r = t.value;
			n.debug("[EBNF Parser] Parsed rules:", r.rules.length), h(r), n.debug("[EBNF Parser] Parse complete");
		}, "parse"),
		parser: { yy: a }
	},
	db: a,
	renderer: i,
	styles: r
};
//#endregion
export { g as diagram };
