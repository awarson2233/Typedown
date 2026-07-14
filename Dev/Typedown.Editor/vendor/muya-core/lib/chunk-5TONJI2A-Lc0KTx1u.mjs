import { C as e, S as t, g as n, n as r, o as i, t as a, u as o, w as s, x as c } from "./chunk-KEIR6QF5-lXp_uFT8.mjs";
//#region node_modules/@mermaid-js/parser/dist/chunks/mermaid-parser.core/chunk-5TONJI2A.mjs
var l, u, d = (l = class extends a {
	constructor() {
		super(["railroad-beta"]);
	}
}, c(l, "RailroadTokenBuilder"), l), f = /* @__PURE__ */ c((e) => {
	let t = e.slice(1, -1), n = "";
	for (let e = 0; e < t.length; e++) {
		let r = t[e];
		if (r === "\\" && e + 1 < t.length) {
			e++;
			let r = t[e];
			switch (r) {
				case "n":
					n += "\n";
					break;
				case "r":
					n += "\r";
					break;
				case "t":
					n += "	";
					break;
				default: n += r;
			}
			continue;
		}
		n += r;
	}
	return n;
}, "decodeEscapedString"), p = (u = class extends r {
	runConverter(e, t, n) {
		let r = super.runConverter(e, t, n);
		if (e.name === "TITLE" && typeof r == "string") {
			let e = r.trim();
			if (e.startsWith("\"") && e.endsWith("\"") || e.startsWith("'") && e.endsWith("'")) return f(e);
		}
		return r;
	}
	runCustomConverter(e, t, n) {
		if (e.name === "RR_STRING") return f(t);
	}
}, c(u, "RailroadValueConverter"), u), m = { parser: {
	TokenBuilder: /* @__PURE__ */ c(() => new d(), "TokenBuilder"),
	ValueConverter: /* @__PURE__ */ c(() => new p(), "ValueConverter")
} };
function h(r = i) {
	let a = s(e(r), o), c = s(t({ shared: a }), n, m);
	return a.ServiceRegistry.register(c), {
		shared: a,
		Railroad: c
	};
}
c(h, "createRailroadServices");
//#endregion
export { h as n, m as t };
