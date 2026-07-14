import { C as e, S as t, m as n, n as r, o as i, t as a, u as o, w as s, x as c } from "./chunk-KEIR6QF5-lXp_uFT8.mjs";
//#region node_modules/@mermaid-js/parser/dist/chunks/mermaid-parser.core/chunk-5HE753X5.mjs
var l, u, d = (l = class extends a {
	constructor() {
		super(["railroad-abnf-beta"]);
	}
}, c(l, "RailroadAbnfTokenBuilder"), l), f = (u = class extends r {
	runConverter(e, t, n) {
		let r = super.runConverter(e, t, n);
		if (e.name === "TITLE" && typeof r == "string") {
			let e = r.trim();
			if (e.startsWith("\"") && e.endsWith("\"") || e.startsWith("'") && e.endsWith("'")) return e.slice(1, -1);
		}
		return r;
	}
	runCustomConverter(e, t, n) {
		if (e.name === "ABNF_STRING") return t.slice(1, -1);
	}
}, c(u, "RailroadAbnfValueConverter"), u), p = { parser: {
	TokenBuilder: /* @__PURE__ */ c(() => new d(), "TokenBuilder"),
	ValueConverter: /* @__PURE__ */ c(() => new f(), "ValueConverter")
} };
function m(r = i) {
	let a = s(e(r), o), c = s(t({ shared: a }), n, p);
	return a.ServiceRegistry.register(c), {
		shared: a,
		RailroadAbnf: c
	};
}
c(m, "createRailroadAbnfServices");
//#endregion
export { m as n, p as t };
