import { C as e, S as t, i as n, l as r, o as i, t as a, u as o, w as s, x as c } from "./chunk-KEIR6QF5-lXp_uFT8.mjs";
//#region node_modules/@mermaid-js/parser/dist/chunks/mermaid-parser.core/chunk-BIQX33UG.mjs
var l, u = (l = class extends a {
	constructor() {
		super(["info", "showInfo"]);
	}
}, c(l, "InfoTokenBuilder"), l), d = { parser: {
	TokenBuilder: /* @__PURE__ */ c(() => new u(), "TokenBuilder"),
	ValueConverter: /* @__PURE__ */ c(() => new n(), "ValueConverter")
} };
function f(n = i) {
	let a = s(e(n), o), c = s(t({ shared: a }), r, d);
	return a.ServiceRegistry.register(c), {
		shared: a,
		Info: c
	};
}
c(f, "createInfoServices");
//#endregion
export { f as n, d as t };
