import { C as e, S as t, b as n, n as r, o as i, u as a, w as o, x as s } from "./chunk-KEIR6QF5-lXp_uFT8.mjs";
//#region node_modules/@mermaid-js/parser/dist/chunks/mermaid-parser.core/chunk-5FCAYU7R.mjs
var c, l = (c = class extends r {
	runCustomConverter(e, t, n) {
		switch (e.name.toUpperCase()) {
			case "LINK_LABEL": return t.substring(1).trim();
			default: return;
		}
	}
}, s(c, "WardleyValueConverter"), c), u = { parser: { ValueConverter: /* @__PURE__ */ s(() => new l(), "ValueConverter") } };
function d(r = i) {
	let s = o(e(r), a), c = o(t({ shared: s }), n, u);
	return s.ServiceRegistry.register(c), {
		shared: s,
		Wardley: c
	};
}
s(d, "createWardleyServices");
//#endregion
export { d as n, u as t };
