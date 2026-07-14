import { n as e } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n as t } from "./src-upoOno_g.mjs";
import { c as n } from "./chunk-WYO6CB5R-_U4dPty4.mjs";
//#region node_modules/mermaid/dist/chunks/mermaid.core/chunk-VR4S4FIN.mjs
var r = /* @__PURE__ */ e((e, r, o, s) => {
	e.attr("class", o);
	let { width: c, height: l, x: u, y: d } = i(e, r);
	n(e, l, c, s);
	let f = a(u, d, c, l, r);
	e.attr("viewBox", f), t.debug(`viewBox configured: ${f} with padding: ${r}`);
}, "setupViewPortForSVG"), i = /* @__PURE__ */ e((e, t) => {
	var n;
	let r = ((n = e.node()) == null ? void 0 : n.getBBox()) || {
		width: 0,
		height: 0,
		x: 0,
		y: 0
	};
	return {
		width: r.width + t * 2,
		height: r.height + t * 2,
		x: r.x,
		y: r.y
	};
}, "calculateDimensionsWithPadding"), a = /* @__PURE__ */ e((e, t, n, r, i) => `${e - i} ${t - i} ${n} ${r}`, "createViewBox");
//#endregion
export { r as t };
