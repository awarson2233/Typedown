import { C as e, S as t, c as n, i as r, o as i, t as a, u as o, w as s, x as c } from "./chunk-KEIR6QF5-lXp_uFT8.mjs";
//#region node_modules/@mermaid-js/parser/dist/chunks/mermaid-parser.core/chunk-CYSBUYHQ.mjs
var l, u = (l = class extends a {
	constructor() {
		super(["gitGraph"]);
	}
}, c(l, "GitGraphTokenBuilder"), l), d = { parser: {
	TokenBuilder: /* @__PURE__ */ c(() => new u(), "TokenBuilder"),
	ValueConverter: /* @__PURE__ */ c(() => new r(), "ValueConverter")
} };
function f(r = i) {
	let a = s(e(r), o), c = s(t({ shared: a }), n, d);
	return a.ServiceRegistry.register(c), {
		shared: a,
		GitGraph: c
	};
}
c(f, "createGitGraphServices");
//#endregion
export { f as n, d as t };
