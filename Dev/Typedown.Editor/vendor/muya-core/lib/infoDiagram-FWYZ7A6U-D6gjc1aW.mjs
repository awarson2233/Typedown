import { n as e } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n as t } from "./src-upoOno_g.mjs";
import { c as n } from "./chunk-WYO6CB5R-_U4dPty4.mjs";
import { t as r } from "./chunk-VAUOI2AC-DvRR_ttS.mjs";
import { n as i } from "./mermaid-parser.core-Dnt89Q92.mjs";
//#region node_modules/mermaid/dist/chunks/mermaid.core/infoDiagram-FWYZ7A6U.mjs
var a = { parse: /* @__PURE__ */ e(async (e) => {
	let n = await i("info", e);
	t.debug(n);
}, "parse") }, o = { version: "11.16.0" }, s = {
	parser: a,
	db: { getVersion: /* @__PURE__ */ e(() => o.version, "getVersion") },
	renderer: { draw: /* @__PURE__ */ e((e, i, a) => {
		t.debug("rendering info diagram\n" + e);
		let o = r(i);
		n(o, 100, 400, !0), o.append("g").append("text").attr("x", 100).attr("y", 40).attr("class", "version").attr("font-size", 32).style("text-anchor", "middle").text(`v${a}`);
	}, "draw") }
};
//#endregion
export { s as diagram };
