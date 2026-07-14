import { C as e, S as t, f as n, n as r, o as i, t as a, u as o, w as s, x as c } from "./chunk-KEIR6QF5-lXp_uFT8.mjs";
//#region node_modules/@mermaid-js/parser/dist/chunks/mermaid-parser.core/chunk-YOTPTUD7.mjs
var l, u, d = (l = class extends a {
	constructor() {
		super(["pie", "showData"]);
	}
}, c(l, "PieTokenBuilder"), l), f = (u = class extends r {
	runCustomConverter(e, t, n) {
		if (e.name === "PIE_SECTION_LABEL") return t.replace(/"/g, "").trim();
	}
}, c(u, "PieValueConverter"), u), p = { parser: {
	TokenBuilder: /* @__PURE__ */ c(() => new d(), "TokenBuilder"),
	ValueConverter: /* @__PURE__ */ c(() => new f(), "ValueConverter")
} };
function m(r = i) {
	let a = s(e(r), o), c = s(t({ shared: a }), n, p);
	return a.ServiceRegistry.register(c), {
		shared: a,
		Pie: c
	};
}
c(m, "createPieServices");
//#endregion
export { m as n, p as t };
