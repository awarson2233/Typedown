import { n as e } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n as t, t as n } from "./src-upoOno_g.mjs";
import { D as r, H as i, K as a, U as o, a as s, b as c, c as l, f as u, v as d, w as f, y as p } from "./chunk-WYO6CB5R-_U4dPty4.mjs";
import { n as m } from "./ordinal-CbbzlgrI.mjs";
import { t as h } from "./defaultLocale-DtQF3lqp.mjs";
import { p as g, t as _ } from "./treemap-CVp_NkFo.mjs";
import { i as v } from "./chunk-ICXQ74PX-5xZ0PKaW.mjs";
import { t as y } from "./chunk-VAUOI2AC-DvRR_ttS.mjs";
import { t as b } from "./chunk-JWPE2WC7-PoIEzJPT.mjs";
import { n as x } from "./mermaid-parser.core-Dnt89Q92.mjs";
import { t as S } from "./chunk-VR4S4FIN-HQ9uQ67o.mjs";
import { i as C, n as w } from "./chunk-C7G6YPKG-BGTLz-ew.mjs";
//#region node_modules/mermaid/dist/chunks/mermaid.core/diagram-G47NLZAW.mjs
var T, E = (T = class {
	constructor() {
		this.nodes = [], this.levels = /* @__PURE__ */ new Map(), this.outerNodes = [], this.classes = /* @__PURE__ */ new Map(), this.setAccTitle = o, this.getAccTitle = p, this.setDiagramTitle = a, this.getDiagramTitle = f, this.getAccDescription = d, this.setAccDescription = i;
	}
	getNodes() {
		return this.nodes;
	}
	getConfig() {
		var e;
		let t = u, n = c();
		return v({
			...t.treemap,
			...(e = n.treemap) == null ? {} : e
		});
	}
	addNode(e, t) {
		this.nodes.push(e), this.levels.set(e, t), t === 0 && (this.outerNodes.push(e), this.root != null || (this.root = e));
	}
	getRoot() {
		return {
			name: "",
			children: this.outerNodes
		};
	}
	addClass(e, t) {
		var n;
		let r = (n = this.classes.get(e)) == null ? {
			id: e,
			styles: [],
			textStyles: []
		} : n, i = t.replace(/\\,/g, "§§§").replace(/,/g, ";").replace(/§§§/g, ",").split(";");
		i && i.forEach((e) => {
			w(e) && (r != null && r.textStyles ? r.textStyles.push(e) : r.textStyles = [e]), r != null && r.styles ? r.styles.push(e) : r.styles = [e];
		}), this.classes.set(e, r);
	}
	getClasses() {
		return this.classes;
	}
	getStylesForClass(e) {
		var t, n;
		return (t = (n = this.classes.get(e)) == null ? void 0 : n.styles) == null ? [] : t;
	}
	clear() {
		s(), this.nodes = [], this.levels = /* @__PURE__ */ new Map(), this.outerNodes = [], this.classes = /* @__PURE__ */ new Map(), this.root = void 0;
	}
}, e(T, "TreeMapDB"), T);
function D(e) {
	if (!e.length) return [];
	let t = [], n = [];
	return e.forEach((e) => {
		let r = {
			name: e.name,
			children: e.type === "Leaf" ? void 0 : []
		};
		for (r.classSelector = e == null ? void 0 : e.classSelector, e != null && e.cssCompiledStyles && (r.cssCompiledStyles = e.cssCompiledStyles), e.type === "Leaf" && e.value !== void 0 && (r.value = e.value); n.length > 0 && n[n.length - 1].level >= e.level;) n.pop();
		if (n.length === 0) t.push(r);
		else {
			let e = n[n.length - 1].node;
			e.children ? e.children.push(r) : e.children = [r];
		}
		e.type !== "Leaf" && n.push({
			node: r,
			level: e.level
		});
	}), t;
}
e(D, "buildHierarchy");
var O = /* @__PURE__ */ e((t, n) => {
	var r, i;
	b(t, n);
	let a = [];
	for (let e of (r = t.TreemapRows) == null ? [] : r) if (e.$type === "ClassDefStatement") {
		var o, s;
		n.addClass((o = e.className) == null ? "" : o, (s = e.styleText) == null ? "" : s);
	}
	for (let e of (i = t.TreemapRows) == null ? [] : i) {
		let t = e.item;
		if (!t) continue;
		let r = e.indent ? parseInt(e.indent) : 0, i = k(t), o = t.classSelector ? n.getStylesForClass(t.classSelector) : [], s = o.length > 0 ? o : void 0, c = {
			level: r,
			name: i,
			type: t.$type,
			value: t.value,
			classSelector: t.classSelector,
			cssCompiledStyles: s
		};
		a.push(c);
	}
	let c = D(a), l = /* @__PURE__ */ e((e, t) => {
		for (let r of e) n.addNode(r, t), r.children && r.children.length > 0 && l(r.children, t + 1);
	}, "addNodesRecursively");
	l(c, 0);
}, "populate"), k = /* @__PURE__ */ e((e) => e.name ? String(e.name) : "", "getItemName"), A = {
	parser: { yy: void 0 },
	parse: /* @__PURE__ */ e(async (e) => {
		try {
			var n;
			let r = await x("treemap", e);
			t.debug("Treemap AST:", r);
			let i = (n = A.parser) == null ? void 0 : n.yy;
			if (!(i instanceof E)) throw Error("parser.parser?.yy was not a TreemapDB. This is due to a bug within Mermaid, please report this issue at https://github.com/mermaid-js/mermaid/issues.");
			O(r, i);
		} catch (e) {
			throw t.error("Error parsing treemap:", e), e;
		}
	}, "parse")
}, j = 10, M = 10, N = 25, P = {
	draw: /* @__PURE__ */ e((r, i, a, o) => {
		var s, u;
		let d = o.db, f = d.getConfig(), p = (s = f.padding) == null ? j : s, v = d.getDiagramTitle(), b = d.getRoot(), { themeVariables: x } = c();
		if (!b) return;
		let w = v ? 30 : 0, T = y(i), E = f.nodeWidth ? f.nodeWidth * M : 960, D = f.nodeHeight ? f.nodeHeight * M : 500, O = E, k = D + w;
		T.attr("viewBox", `0 0 ${O} ${k}`), l(T, k, O, f.useMaxWidth);
		let A;
		try {
			let t = f.valueFormat || ",";
			if (t === "$0,0") A = /* @__PURE__ */ e((e) => "$" + h(",")(e), "valueFormat");
			else if (t.startsWith("$") && t.includes(",")) {
				let n = /\.\d+/.exec(t), r = n ? n[0] : "";
				A = /* @__PURE__ */ e((e) => "$" + h("," + r)(e), "valueFormat");
			} else if (t.startsWith("$")) {
				let n = t.substring(1);
				A = /* @__PURE__ */ e((e) => "$" + h(n || "")(e), "valueFormat");
			} else A = h(t);
		} catch (e) {
			t.error("Error creating format function:", e), A = h(",");
		}
		let P = m().range([
			"transparent",
			x.cScale0,
			x.cScale1,
			x.cScale2,
			x.cScale3,
			x.cScale4,
			x.cScale5,
			x.cScale6,
			x.cScale7,
			x.cScale8,
			x.cScale9,
			x.cScale10,
			x.cScale11
		]), F = m().range([
			"transparent",
			x.cScalePeer0,
			x.cScalePeer1,
			x.cScalePeer2,
			x.cScalePeer3,
			x.cScalePeer4,
			x.cScalePeer5,
			x.cScalePeer6,
			x.cScalePeer7,
			x.cScalePeer8,
			x.cScalePeer9,
			x.cScalePeer10,
			x.cScalePeer11
		]), I = m().range([
			x.cScaleLabel0,
			x.cScaleLabel1,
			x.cScaleLabel2,
			x.cScaleLabel3,
			x.cScaleLabel4,
			x.cScaleLabel5,
			x.cScaleLabel6,
			x.cScaleLabel7,
			x.cScaleLabel8,
			x.cScaleLabel9,
			x.cScaleLabel10,
			x.cScaleLabel11
		]);
		v && T.append("text").attr("x", O / 2).attr("y", w / 2).attr("class", "treemapTitle").attr("text-anchor", "middle").attr("dominant-baseline", "middle").text(v);
		let L = T.append("g").attr("transform", `translate(0, ${w})`).attr("class", "treemapContainer"), R = g(b).sum((e) => {
			var t;
			return (t = e.value) == null ? 0 : t;
		}).sort((e, t) => {
			var n, r;
			return ((n = t.value) == null ? 0 : n) - ((r = e.value) == null ? 0 : r);
		}), z = _().size([E, D]).paddingTop((e) => e.children && e.children.length > 0 ? N + M : 0).paddingInner(p).paddingLeft((e) => e.children && e.children.length > 0 ? M : 0).paddingRight((e) => e.children && e.children.length > 0 ? M : 0).paddingBottom((e) => e.children && e.children.length > 0 ? M : 0).round(!0)(R), B = z.descendants().filter((e) => e.children && e.children.length > 0), V = L.selectAll(".treemapSection").data(B).enter().append("g").attr("class", "treemapSection").attr("transform", (e) => `translate(${e.x0},${e.y0})`);
		V.append("rect").attr("width", (e) => e.x1 - e.x0).attr("height", N).attr("class", "treemapSectionHeader").attr("fill", "none").attr("fill-opacity", .6).attr("stroke-width", .6).attr("style", (e) => e.depth === 0 ? "display: none;" : ""), V.append("clipPath").attr("id", (e, t) => `clip-section-${i}-${t}`).append("rect").attr("width", (e) => Math.max(0, e.x1 - e.x0 - 12)).attr("height", N), V.append("rect").attr("width", (e) => e.x1 - e.x0).attr("height", (e) => e.y1 - e.y0).attr("class", (e, t) => `treemapSection section${t}`).attr("fill", (e) => P(e.data.name)).attr("fill-opacity", .6).attr("stroke", (e) => F(e.data.name)).attr("stroke-width", 2).attr("stroke-opacity", .4).attr("style", (e) => {
			if (e.depth === 0) return "display: none;";
			let t = C({ cssCompiledStyles: e.data.cssCompiledStyles });
			return t.nodeStyles + ";" + t.borderStyles.join(";");
		}), V.append("text").attr("class", "treemapSectionLabel").attr("x", 6).attr("y", N / 2).attr("dominant-baseline", "middle").text((e) => e.depth === 0 ? "" : e.data.name).attr("font-weight", "bold").attr("clip-path", (e, t) => `url(#clip-section-${i}-${t})`).attr("style", (e) => e.depth === 0 ? "display: none;" : "dominant-baseline: middle; font-size: 12px; fill:" + I(e.data.name) + "; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" + C({ cssCompiledStyles: e.data.cssCompiledStyles }).labelStyles.replace("color:", "fill:")).each(function(e) {
			if (e.depth === 0) return;
			let t = n(this), r = e.data.name;
			t.text(r);
			let i = e.x1 - e.x0, a;
			a = f.showValues !== !1 && e.value ? i - 10 - 30 - 10 - 6 : i - 6 - 6;
			let o = Math.max(15, a), s = t.node();
			if (s.getComputedTextLength() > o) {
				let e = r;
				for (; e.length > 0;) {
					if (e = r.substring(0, e.length - 1), e.length === 0) {
						t.text("..."), s.getComputedTextLength() > o && t.text("");
						break;
					}
					if (t.text(e + "..."), s.getComputedTextLength() <= o) break;
				}
			}
		}), f.showValues !== !1 && V.append("text").attr("class", "treemapSectionValue").attr("x", (e) => e.x1 - e.x0 - 10).attr("y", N / 2).attr("text-anchor", "end").attr("dominant-baseline", "middle").text((e) => e.value ? A(e.value) : "").attr("font-style", "italic").attr("style", (e) => e.depth === 0 ? "display: none;" : "text-anchor: end; dominant-baseline: middle; font-size: 10px; fill:" + I(e.data.name) + "; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" + C({ cssCompiledStyles: e.data.cssCompiledStyles }).labelStyles.replace("color:", "fill:"));
		let H = z.leaves(), U = H.length > 20, W = U ? 16 : 38, G = U ? 14 : 28, K = U ? 4 : 8, q = U ? 4 : 6, J = U ? 2 : 4, Y = U ? 8 : 10, X = U ? 1 : 2, Z = L.selectAll(".treemapLeafGroup").data(H).enter().append("g").attr("class", (e, t) => `treemapNode treemapLeafGroup leaf${t}${e.data.classSelector ? ` ${e.data.classSelector}` : ""}x`).attr("transform", (e) => `translate(${e.x0},${e.y0})`);
		Z.append("rect").attr("width", (e) => e.x1 - e.x0).attr("height", (e) => e.y1 - e.y0).attr("class", "treemapLeaf").attr("fill", (e) => e.parent ? P(e.parent.data.name) : P(e.data.name)).attr("style", (e) => C({ cssCompiledStyles: e.data.cssCompiledStyles }).nodeStyles).attr("fill-opacity", .3).attr("stroke", (e) => e.parent ? P(e.parent.data.name) : P(e.data.name)).attr("stroke-width", 3), Z.append("clipPath").attr("id", (e, t) => `clip-${i}-${t}`).append("rect").attr("width", (e) => Math.max(0, e.x1 - e.x0 - 4)).attr("height", (e) => Math.max(0, e.y1 - e.y0 - 4)), Z.append("text").attr("class", "treemapLabel").attr("x", (e) => (e.x1 - e.x0) / 2).attr("y", (e) => (e.y1 - e.y0) / 2).attr("style", (e) => `text-anchor: middle; dominant-baseline: middle; font-size: ${W}px;fill:` + I(e.data.name) + ";" + C({ cssCompiledStyles: e.data.cssCompiledStyles }).labelStyles.replace("color:", "fill:")).attr("clip-path", (e, t) => `url(#clip-${i}-${t})`).text((e) => e.data.name).each(function(e) {
			let t = n(this), r = e.x1 - e.x0, i = e.y1 - e.y0, a = t.node(), o = r - 2 * J, s = i - 2 * J;
			if (o < Y || s < Y) {
				t.style("display", "none");
				return;
			}
			let c = parseInt(t.style("font-size"), 10), l = .6;
			for (; a.getComputedTextLength() > o && c > K;) c--, t.style("font-size", `${c}px`);
			let u = Math.max(q, Math.min(G, Math.round(c * l))), d = c + X + u;
			for (; d > s && c > K && (c--, u = Math.max(q, Math.min(G, Math.round(c * l))), !(u < q && c === K));) t.style("font-size", `${c}px`), d = c + X + u;
			t.style("font-size", `${c}px`), U ? (c < K || s < K) && t.style("display", "none") : (a.getComputedTextLength() > o || c < K || s < c) && t.style("display", "none");
		}), f.showValues !== !1 && Z.append("text").attr("class", "treemapValue").attr("x", (e) => (e.x1 - e.x0) / 2).attr("y", function(e) {
			return (e.y1 - e.y0) / 2;
		}).attr("style", (e) => `text-anchor: middle; dominant-baseline: hanging; font-size: ${G}px;fill:` + I(e.data.name) + ";" + C({ cssCompiledStyles: e.data.cssCompiledStyles }).labelStyles.replace("color:", "fill:")).attr("clip-path", (e, t) => `url(#clip-${i}-${t})`).text((e) => e.value ? A(e.value) : "").each(function(e) {
			let t = n(this), r = this.parentNode;
			if (!r) {
				t.style("display", "none");
				return;
			}
			let i = n(r).select(".treemapLabel");
			if (i.empty() || i.style("display") === "none") {
				t.style("display", "none");
				return;
			}
			let a = parseFloat(i.style("font-size")), o = Math.max(q, Math.min(G, Math.round(a * .6)));
			t.style("font-size", `${o}px`);
			let s = (e.y1 - e.y0) / 2 + a / 2 + X;
			t.attr("y", s);
			let c = e.x1 - e.x0, l = e.y1 - e.y0 - 4, u = c - 2 * J;
			t.node().getComputedTextLength() > u || s + o > l || o < q ? t.style("display", "none") : t.style("display", null);
		}), S(T, (u = f.diagramPadding) == null ? 8 : u, "flowchart", (f == null ? void 0 : f.useMaxWidth) || !1);
	}, "draw"),
	getClasses: /* @__PURE__ */ e(function(e, t) {
		return t.db.getClasses();
	}, "getClasses")
}, F = {
	sectionStrokeColor: "black",
	sectionStrokeWidth: "1",
	sectionFillColor: "#efefef",
	leafStrokeColor: "black",
	leafStrokeWidth: "1",
	leafFillColor: "#efefef",
	labelFontSize: "12px",
	valueFontSize: "10px",
	titleFontSize: "14px"
}, I = {
	parser: A,
	get db() {
		return new E();
	},
	renderer: P,
	styles: /* @__PURE__ */ e(({ treemap: e } = {}) => {
		var t, n, i;
		let a = v(r(), c().themeVariables), o = v(F, e), s = (t = o.titleColor) == null ? a.titleColor : t, l = (n = o.labelColor) == null ? a.textColor : n, u = (i = o.valueColor) == null ? a.textColor : i;
		return `
  .treemapNode.section {
    stroke: ${o.sectionStrokeColor};
    stroke-width: ${o.sectionStrokeWidth};
    fill: ${o.sectionFillColor};
  }
  .treemapNode.leaf {
    stroke: ${o.leafStrokeColor};
    stroke-width: ${o.leafStrokeWidth};
    fill: ${o.leafFillColor};
  }
  .treemapLabel {
    fill: ${l};
    font-size: ${o.labelFontSize};
  }
  .treemapValue {
    fill: ${u};
    font-size: ${o.valueFontSize};
  }
  .treemapTitle {
    fill: ${s};
    font-size: ${o.titleFontSize};
  }
  `;
	}, "getStyles")
};
//#endregion
export { I as diagram };
