import { n as e } from "./chunk-JG7HCLWE-CR_rylZZ.mjs";
import { n as t } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n } from "./src-upoOno_g.mjs";
import "./chunk-WYO6CB5R-_U4dPty4.mjs";
import "./chunk-VAUOI2AC-DvRR_ttS.mjs";
import { n as r, r as i, t as a } from "./chunk-MOJQB5TN-U2w9jYtv.mjs";
import { t as o } from "./chunk-JWPE2WC7-PoIEzJPT.mjs";
import { t as s } from "./mermaid-parser.core-Dnt89Q92.mjs";
//#region node_modules/mermaid/dist/chunks/mermaid.core/pegDiagram-2B236MQR.mjs
var c = e().RailroadPeg.parser.LangiumParser, l = /* @__PURE__ */ t((e) => {
	let t = e.alternatives.map(u);
	return t.length === 1 ? t[0] : {
		type: "choice",
		alternatives: t
	};
}, "transformOrderedChoice"), u = /* @__PURE__ */ t((e) => {
	let t = e.elements.map(d);
	return t.length === 1 ? t[0] : {
		type: "sequence",
		elements: t
	};
}, "transformSequence"), d = /* @__PURE__ */ t((e) => {
	let t = p(e.suffix);
	return e.operator ? {
		type: "special",
		text: e.operator === "&" ? `&${f(t)}` : `!${f(t)}`
	} : t;
}, "transformPrefix"), f = /* @__PURE__ */ t((e) => {
	switch (e.type) {
		case "terminal": return `"${e.value}"`;
		case "nonterminal": return e.name;
		case "special": return e.text;
		default: return "(...)";
	}
}, "nodeToLabel"), p = /* @__PURE__ */ t((e) => {
	let t = m(e.primary);
	if (!e.operator) return t;
	switch (e.operator) {
		case "?": return {
			type: "optional",
			element: t
		};
		case "*": return {
			type: "repetition",
			element: t,
			min: 0,
			max: Infinity
		};
		case "+": return {
			type: "repetition",
			element: t,
			min: 1,
			max: Infinity
		};
		default: throw Error(`Unsupported PEG suffix operator: ${e.operator}`);
	}
}, "transformSuffix"), m = /* @__PURE__ */ t((e) => {
	switch (e.$type) {
		case "PegLiteral": return {
			type: "terminal",
			value: e.value
		};
		case "PegIdentifier": return {
			type: "nonterminal",
			name: e.name
		};
		case "PegGroup": return l(e.element);
		case "PegAny": return {
			type: "special",
			text: e.dot
		};
		default: throw Error(`Unsupported PEG primary node: ${e.$type}`);
	}
}, "transformPrimary"), h = /* @__PURE__ */ t((e) => ({
	name: e.name,
	definition: l(e.definition)
}), "transformRule"), g = /* @__PURE__ */ t((e) => {
	o(e, a), e.title && a.setTitle(e.title), e.rules.map((e) => a.addRule(h(e)));
}, "populateDb"), _ = {
	parser: {
		parse: /* @__PURE__ */ t((e) => {
			a.clear(), n.debug("[PEG Parser] Starting Langium parse");
			let t = c.parse(e);
			if (t.lexerErrors.length > 0 || t.parserErrors.length > 0) throw new s(t);
			let r = t.value;
			n.debug("[PEG Parser] Parsed rules:", r.rules.length), g(r), n.debug("[PEG Parser] Parse complete");
		}, "parse"),
		parser: { yy: a }
	},
	db: a,
	renderer: i,
	styles: r
};
//#endregion
export { _ as diagram };
