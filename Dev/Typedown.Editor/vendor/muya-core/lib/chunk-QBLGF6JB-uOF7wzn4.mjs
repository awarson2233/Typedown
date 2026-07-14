import { C as e, S as t, i as n, o as r, p as i, t as a, u as o, w as s, x as c } from "./chunk-KEIR6QF5-lXp_uFT8.mjs";
//#region node_modules/@mermaid-js/parser/dist/chunks/mermaid-parser.core/chunk-QBLGF6JB.mjs
var l, u = (l = class extends a {
	constructor() {
		super(["radar-beta"]);
	}
}, c(l, "RadarTokenBuilder"), l), d = { parser: {
	TokenBuilder: /* @__PURE__ */ c(() => new u(), "TokenBuilder"),
	ValueConverter: /* @__PURE__ */ c(() => new n(), "ValueConverter")
} };
function f(n = r) {
	let a = s(e(n), o), c = s(t({ shared: a }), i, d);
	return a.ServiceRegistry.register(c), {
		shared: a,
		Radar: c
	};
}
c(f, "createRadarServices");
//#endregion
export { f as n, d as t };
