import { n as e } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n as t } from "./src-upoOno_g.mjs";
import { b as n, x as r } from "./chunk-WYO6CB5R-_U4dPty4.mjs";
import { g as i } from "./chunk-ICXQ74PX-5xZ0PKaW.mjs";
import "./chunk-HOUHSVGY-2PLbOYM-.mjs";
import "./chunk-Q4XR5HBZ-KP7VNg4F.mjs";
import { r as a } from "./chunk-7BUUIJ7U-Cnx5Zlja.mjs";
import { n as o } from "./chunk-OGEWGWER-Cn1Z4hFw.mjs";
import { t as s } from "./graphlib-B6n0CQ68.mjs";
import { n as c } from "./chunk-RYQCIY6F-B5BzgP3I.mjs";
import "./chunk-C7G6YPKG-BGTLz-ew.mjs";
import { a as l, c as u, i as d, n as f, t as p } from "./chunk-ZGVPDNZ5-CA-MSk8G.mjs";
import { a as m, i as h, n as g, r as _, s as v, t as y } from "./chunk-52WLFC77-CEdJfckq.mjs";
//#region node_modules/mermaid/dist/chunks/mermaid.core/swimlanes-5IMT3BWC.mjs
async function b(e, t) {
	let n = new s({
		multigraph: !0,
		compound: !0
	}), i = [...t.edges], a = r(), o = e.insert("g").attr("class", "root"), c = o.insert("g").attr("class", "clusters"), u = o.insert("g").attr("class", "edges edgePath"), d = o.insert("g").attr("class", "edgeLabels"), f = o.insert("g").attr("class", "nodes"), p = /* @__PURE__ */ new Map(), m = e.node() != null;
	await Promise.all(t.nodes.map(async (e) => {
		if (e.isGroup) n.setNode(e.id, { ...e });
		else {
			if (m) {
				var t, r;
				let n = await l(f, e, {
					config: a,
					dir: e.dir
				}), i = (t = (r = n.node()) == null ? void 0 : r.getBBox()) == null ? {
					width: 0,
					height: 0
				} : t;
				p.set(e.id, n), e.width = i.width, e.height = i.height;
			}
			n.setNode(e.id, { ...e });
		}
	}));
	for (let e of i) n.setEdge(e.start, e.end, { ...e }, e.id), t.edges.some((t) => t.id === e.id) || t.edges.push(e);
	if (globalThis.mermaidCaptureSizes) {
		let { captureNodeSizes: n } = await import("./sizeCapture-X5ZJPWSS-CTxzJ546.mjs");
		n(e, t);
	}
	return {
		graph: n,
		groups: {
			clusters: c,
			edgePaths: u,
			edgeLabels: d,
			nodes: f,
			rootGroups: o
		},
		nodeElements: p
	};
}
e(b, "createGraphWithElements");
var x = 5, S = 1e-5, C = 1e-6;
function w(e) {
	let t = [];
	for (let n = 0; n < e.length - 1; n++) t.push({
		a: e[n],
		b: e[n + 1]
	});
	return t;
}
e(w, "buildSegmentList");
function T(e, t, n, r) {
	let i = t.x - e.x, a = t.y - e.y, o = r.x - n.x, s = r.y - n.y, c = i * s - a * o;
	if (c === 0) return null;
	let l = n.x - e.x, u = n.y - e.y, d = (l * s - u * o) / c, f = (l * a - u * i) / c;
	return d <= C || d >= 1 - C || f <= C || f >= 1 - C ? null : {
		point: {
			x: e.x + d * i,
			y: e.y + d * a
		},
		tA: d,
		tB: f
	};
}
e(T, "segmentIntersection");
function E(e) {
	return Math.abs(e.b.x - e.a.x) >= Math.abs(e.b.y - e.a.y);
}
e(E, "isHorizontalSeg");
function D(e) {
	let t = [];
	for (let n = 0; n < e.length; n++) {
		let r = e[n], i = w(r.points);
		for (let a = n + 1; a < e.length; a++) {
			let n = e[a], o = w(n.points);
			for (let [e, a] of i.entries()) for (let [i, s] of o.entries()) {
				let o = T(a.a, a.b, s.a, s.b);
				if (!o) continue;
				let c = E(a);
				c !== E(s) && c ? t.push({
					jumpEdgeId: r.id,
					otherEdgeId: n.id,
					segIndex: e,
					t: o.tA,
					point: o.point
				}) : t.push({
					jumpEdgeId: n.id,
					otherEdgeId: r.id,
					segIndex: i,
					t: o.tB,
					point: o.point
				});
			}
		}
	}
	return t;
}
e(D, "findEdgeIntersections");
function O(e) {
	return `${Math.round(e * 1e3) / 1e3}`;
}
e(O, "fmt");
function k(e) {
	return `${O(e.x)},${O(e.y)}`;
}
e(k, "pointToString");
function A(e) {
	let t = e.b.x - e.a.x, n = e.b.y - e.a.y;
	return Math.abs(t) >= Math.abs(n) ? +(t >= 0) : +(n >= 0);
}
e(A, "getArcSweepFlag");
var j = .001;
function M(e, t) {
	if (e.length < 2) return e.map((e) => ({ ...e }));
	let n = e.map((e) => ({ ...e })), r = t.arrowTypeStart && a[t.arrowTypeStart];
	if (r) {
		let t = e[0], i = e[1], a = Math.atan2(i.y - t.y, i.x - t.x);
		n[0].x = t.x + r * Math.cos(a), n[0].y = t.y + r * Math.sin(a);
	}
	let i = t.arrowTypeEnd && a[t.arrowTypeEnd];
	if (i) {
		let t = e.length, r = e[t - 2], a = e[t - 1], o = Math.atan2(a.y - r.y, a.x - r.x);
		n[t - 1].x = a.x - i * Math.cos(o), n[t - 1].y = a.y - i * Math.sin(o);
	}
	return n;
}
e(M, "applyMarkerOffsets");
function N(e, t, n, r, i) {
	let a = e.point.x, o = e.point.y, s = {
		x: a - t * e.r,
		y: o - n * e.r
	}, c = {
		x: a + t * e.r,
		y: o + n * e.r
	}, l = [`L${k(s)}`];
	return i === "arc" ? l.push(`A${O(e.r)},${O(e.r)} 0 0 ${r} ${k(c)}`) : l.push(`M${k(c)}`), l;
}
e(N, "emitJump");
function P(e, t, n, r) {
	let i = t.x - e.x, a = t.y - e.y, o = n.x - t.x, s = n.y - t.y, c = Math.hypot(i, a), l = Math.hypot(o, s);
	if (c < S || l < S) return null;
	let u = i / c, d = a / c, f = o / l, p = s / l, m = u * f + d * p, h = Math.acos(Math.max(-1, Math.min(1, m)));
	if (h < S || Math.abs(Math.PI - h) < S) return null;
	let g = Math.min(r / Math.sin(h / 2), c / 2, l / 2);
	return {
		startX: t.x - u * g,
		startY: t.y - d * g,
		endX: t.x + f * g,
		endY: t.y + p * g,
		ctrlX: t.x,
		ctrlY: t.y,
		cutLen: g
	};
}
e(P, "computeRoundedCorner");
function F(e, t, n) {
	let r = e.points;
	if (r.length < 2) return "";
	let i = M(r, e), a = e.curve === "rounded", o = w(i), s = /* @__PURE__ */ new Map();
	for (let e of t) {
		var c;
		let t = o[e.segIndex];
		if (!t) continue;
		let r = Math.hypot(t.b.x - t.a.x, t.b.y - t.a.y), i = (c = s.get(e.segIndex)) == null ? [] : c;
		i.push({
			t: e.t,
			point: e.point,
			d: e.t * r,
			r: n.jumpRadius
		}), s.set(e.segIndex, i);
	}
	let l = [`M${k(i[0])}`];
	for (let e = 0; e < o.length; e++) {
		var u;
		let t = o[e], r = Math.hypot(t.b.x - t.a.x, t.b.y - t.a.y), c = r === 0 ? 0 : (t.b.x - t.a.x) / r, p = r === 0 ? 0 : (t.b.y - t.a.y) / r, m = A(t), h = 0;
		if (a && e > 0) {
			var d;
			let t = P(i[e - 1], i[e], (d = i[e + 1]) == null ? i[e] : d, x);
			t && (h = t.cutLen);
		}
		let g = r, _ = null;
		if (a && e < o.length - 1) {
			var f;
			_ = P(i[e], i[e + 1], (f = i[e + 2]) == null ? i[e + 1] : f, x), _ && (g = r - _.cutLen);
		}
		let v = [...(u = s.get(e)) == null ? [] : u].sort((e, t) => e.t - t.t);
		for (let e of v) e.r = Math.min(e.r, e.d - h, g - e.d);
		for (let e = 0; e < v.length - 1; e++) {
			let t = v[e + 1].d - v[e].d;
			if (v[e].r + v[e + 1].r > t) {
				let n = t / 2;
				v[e].r = Math.min(v[e].r, n), v[e + 1].r = Math.min(v[e + 1].r, n);
			}
		}
		for (let e of v) e.r < j || l.push(...N(e, c, p, m, n.jumpStyle));
		a && _ ? (l.push(`L${O(_.startX)},${O(_.startY)}`), l.push(`Q${O(_.ctrlX)},${O(_.ctrlY)} ${O(_.endX)},${O(_.endY)}`)) : l.push(`L${k(t.b)}`);
	}
	return l.join(" ");
}
e(F, "rewriteEdgePath");
function I(e) {
	return /^[\d\s+,.LMelm-]*$/.test(e);
}
e(I, "isStraightPath");
function L(e) {
	return !e || e === "linear" || e === "rounded" || e === "step" || e === "stepBefore" || e === "stepAfter";
}
e(L, "curveSupportsLineHops");
function ee(e) {
	if (!e) return null;
	try {
		let t = typeof atob == "function" ? atob(e) : Buffer.from(e, "base64").toString(), n = JSON.parse(t);
		if (!Array.isArray(n)) return null;
		let r = [];
		for (let e of n) e && typeof e.x == "number" && typeof e.y == "number" && r.push({
			x: e.x,
			y: e.y
		});
		return r.length >= 2 ? r : null;
	} catch {
		return null;
	}
}
e(ee, "decodeDataPoints");
function te(e, t, n) {
	if (!n.enabled) return;
	let r = e.node();
	if (!r) return;
	let i = /* @__PURE__ */ new Map();
	for (let e of t) i.set(e.id, e);
	let a = [], o = /* @__PURE__ */ new Map();
	for (let e of t) {
		let t = typeof CSS < "u" && CSS.escape ? CSS.escape(e.id) : e.id, n = r.querySelector(`path[data-id="${t}"]`);
		if (!n) continue;
		o.set(e.id, n);
		let i = ee(n.getAttribute("data-points")), s = i == null ? e.points : i;
		a.push({
			...e,
			points: s
		});
	}
	let s = D(a);
	if (s.length === 0) return;
	let c = /* @__PURE__ */ new Map();
	for (let e of s) {
		var l;
		let t = (l = c.get(e.jumpEdgeId)) == null ? [] : l;
		t.push(e), c.set(e.jumpEdgeId, t);
	}
	for (let e of a) {
		var u;
		let t = c.get(e.id);
		if (!t || t.length === 0) continue;
		let r = i.get(e.id), a = r == null ? void 0 : r.curve;
		if (a !== void 0 && !L(a)) continue;
		let s = o.get(e.id);
		if (!s) continue;
		if (a === void 0) {
			var d;
			if (!I((d = s.getAttribute("d")) == null ? "" : d)) continue;
		}
		let l = (u = s.getAttribute("style")) == null ? "" : u, f = /stroke-dasharray\s*:\s*0\s+([\d.]+)\s+[\d.]+\s+([\d.]+)/.exec(l), p = f ? Number.parseFloat(f[1]) : null, m = f ? Number.parseFloat(f[2]) : null, h = F(e, t, n);
		if (s.setAttribute("d", h), p !== null && m !== null && typeof s.getTotalLength == "function") {
			let e = s.getTotalLength(), t = `0 ${p} ${Math.max(0, e - p - m)} ${m}`, n = l.replace(/stroke-dasharray\s*:[^;]*;?/g, `stroke-dasharray: ${t};`).replace(/;\s*;+/g, ";");
			s.setAttribute("style", n);
		}
	}
}
e(te, "applyLineJumpsToSvg");
async function ne(e, t) {
	var n;
	for (let n of e.nodes) n.isGroup ? await d(t.clusters, n) : u(n);
	let r = /* @__PURE__ */ new Map();
	for (let t of e.nodes) t != null && t.id && r.set(t.id, t);
	for (let n of e.edges) {
		var i, a;
		let o = n.start ? (i = r.get(n.start)) == null ? {} : i : {}, s = n.end ? (a = r.get(n.end)) == null ? {} : a : {}, c = _(t.edgePaths, { ...n }, {}, e.type, o, s, e.diagramId);
		n.label && await h(t.rootGroups, n), n.label && re(n, c);
	}
	let o = (n = e.config) == null || (n = n.swimlane) == null ? void 0 : n.lineHops;
	if (o !== !1) {
		let n = o === "gap" ? "gap" : "arc", r = e.edges.filter((e) => Array.isArray(e.points) && e.points.length >= 2).map((e) => ({
			id: e.id,
			points: e.points,
			curve: e.curve,
			arrowTypeStart: e.arrowTypeStart,
			arrowTypeEnd: e.arrowTypeEnd
		}));
		te(t.edgePaths, r, {
			enabled: !0,
			jumpRadius: 6,
			jumpStyle: n
		});
	}
}
e(ne, "adjustLayout");
function re(e, r) {
	var a, s;
	let c = (a = r == null ? void 0 : r.updatedPath) == null ? r == null ? void 0 : r.originalPath : a, { subGraphTitleTotalMargin: l } = o({ flowchart: (s = n().flowchart) == null ? {} : s });
	if (e.label) {
		let n = g.get(e.id), a = e.x, o = e.y;
		if (c) {
			let n = i.calcLabelPosition(c);
			t.debug("Moving label " + e.label + " from (", a, ",", o, ") to (", n.x, ",", n.y, ") abc88"), r && (a = n.x, o = n.y);
		}
		n.attr("transform", `translate(${a}, ${o + l / 2})`);
	}
	if (e != null && e.startLabelLeft) {
		let t = v.get(e.id).startLeft, n = e == null ? void 0 : e.x, r = e == null ? void 0 : e.y;
		if (c) {
			let t = i.calcTerminalLabelPosition(e.arrowTypeStart ? 10 : 0, "start_left", c);
			n = t.x, r = t.y;
		}
		t.attr("transform", `translate(${n}, ${r})`);
	}
	if (e.startLabelRight) {
		let t = v.get(e.id).startRight, n = e.x, r = e.y;
		if (c) {
			let t = i.calcTerminalLabelPosition(e.arrowTypeStart ? 10 : 0, "start_right", c);
			n = t.x, r = t.y;
		}
		t.attr("transform", `translate(${n}, ${r})`);
	}
	if (e.endLabelLeft) {
		let t = v.get(e.id).endLeft, n = e.x, r = e.y;
		if (c) {
			let t = i.calcTerminalLabelPosition(e.arrowTypeEnd ? 10 : 0, "end_left", c);
			n = t.x, r = t.y;
		}
		t.attr("transform", `translate(${n}, ${r})`);
	}
	if (e.endLabelRight) {
		let t = v.get(e.id).endRight, n = e.x, r = e.y;
		if (c) {
			let t = i.calcTerminalLabelPosition(e.arrowTypeEnd ? 10 : 0, "end_right", c);
			n = t.x, r = t.y;
		}
		t.attr("transform", `translate(${n}, ${r})`);
	}
}
e(re, "positionEdgeLabel");
var R = "__swimlane_default__", z = 21, ie = 20;
function ae(e) {
	var t;
	return Math.max((t = e.padding) == null ? ie : t, ie);
}
e(ae, "topLaneHorizontalPadding");
function oe(e) {
	let { x: t, y: n, width: r, height: i } = e, a = e.swimlaneContentTop;
	if (typeof t != "number" || typeof n != "number" || typeof r != "number" || typeof i != "number" || typeof a != "number" || !Number.isFinite(t) || !Number.isFinite(n) || !Number.isFinite(r) || !Number.isFinite(i) || !Number.isFinite(a) || r <= 0 || i <= 0) {
		delete e.groupTitleRect;
		return;
	}
	let o = n - i / 2, s = Math.min(a, n + i / 2), c = o + Math.min(z, Math.max(0, s - o));
	if (c <= o) {
		delete e.groupTitleRect;
		return;
	}
	e.groupTitleRect = {
		left: t - r / 2,
		right: t + r / 2,
		top: o,
		bottom: c
	};
}
e(oe, "assignTopLaneTitleRect");
function B(e) {
	var t, n;
	let r = e.direction, i = (t = e.nodes) == null ? e.nodes = [] : t;
	for (let t of (n = e.nodes) == null ? [] : n) t.isGroup && !t.parentId && (t.shape = "swimlane", r && (t.direction = r));
	let a = i.filter((e) => !e.isGroup && !e.parentId);
	if (a.length === 0) return;
	let o = i.find((e) => e.id === R);
	o ? o.isGroup && (o.shape = "swimlane", r && (o.direction = r)) : (o = {
		id: R,
		label: "",
		isGroup: !0,
		shape: "swimlane",
		padding: 20,
		...r ? { direction: r } : {}
	}, i.push(o));
	for (let e of a) e.parentId = R;
}
e(B, "prepareLayoutForSwimlanes");
function se(e) {
	var t, n, r;
	let i = /* @__PURE__ */ new Map();
	for (let n of (t = e.nodes) == null ? [] : t) i.set(n.id, n);
	let a = [];
	for (let t of (n = e.edges) == null ? [] : n) {
		let e = typeof t.start == "string" ? t.start : void 0, n = typeof t.end == "string" ? t.end : void 0;
		!e || !n || t.labelNodeId || a.push({
			id: t.id,
			src: e,
			dst: n,
			ref: t
		});
	}
	let o = (r = e.nodes) == null ? [] : r, s = o.filter((e) => e.isGroup), c = o.filter((e) => !e.isGroup);
	return {
		nodes: [...[...s].reverse(), ...c].map((e) => e.id),
		edges: a,
		layout: e,
		nodeById: i
	};
}
e(se, "toGraphView");
function ce(e, t, n, r) {
	var i, a, o;
	let { layout: s } = e, c = e.nodeById, l = (i = r == null ? void 0 : r.layerGap) == null ? 100 : i, u = (a = r == null ? void 0 : r.nodeGap) == null ? 40 : a, d = 0;
	for (let e of t.layers) {
		let t = 0;
		for (let r of e) {
			var f, p;
			let e = c.get(r);
			if (!e) {
				t++;
				continue;
			}
			e.layer = d, e.order = t;
			let i = (f = n.x[r]) == null ? t * u : f, a = (p = n.y[r]) == null ? d * l : p;
			e.x = i, e.y = a, t++;
		}
		d++;
	}
	let m = (o = s.nodes) == null ? [] : o, h = /* @__PURE__ */ new Map(), g = [];
	for (let e of m) {
		if (!(e != null && e.isGroup)) continue;
		e.parentId || g.push(e);
		let t = m.filter((t) => t.parentId === e.id), r = Infinity, i = -Infinity, a = Infinity, o = -Infinity;
		for (let e of t) {
			var _, v, y, b;
			let t = (_ = e.x) == null ? n.x[e.id] : _, s = (v = e.y) == null ? n.y[e.id] : v, c = (y = e.width) == null ? 0 : y, l = (b = e.height) == null ? 0 : b;
			t != null && s != null && (r = Math.min(r, t - c / 2), i = Math.max(i, t + c / 2), a = Math.min(a, s - l / 2), o = Math.max(o, s + l / 2));
		}
		if (r === Infinity || a === Infinity) {
			var x, S, C, w;
			e.x = (x = e.x) == null ? 0 : x, e.y = (S = e.y) == null ? 0 : S, e.width = (C = e.width) == null ? 0 : C, e.height = (w = e.height) == null ? 0 : w;
		} else {
			var T;
			let t = (T = e.padding) == null ? 20 : T, n = e.parentId ? t : 2 * ae(e), s = t, c = Math.max(0, i - r) + n, l = Math.max(0, o - a) + s, u = (r + i) / 2, d = (a + o) / 2;
			e.x = u, e.y = d, e.width = c, e.height = l, h.set(e.id, {
				minX: r,
				maxX: i,
				minY: a,
				maxY: o
			});
		}
	}
	if (g.length > 0 && h.size > 0) {
		let e = Infinity, t = -Infinity, n = 0;
		for (let r of g) {
			var E;
			let i = (E = r.padding) == null ? 20 : E;
			i > n && (n = i);
			let a = h.get(r.id);
			a && (e = Math.min(e, a.minY), t = Math.max(t, a.maxY));
		}
		if (e !== Infinity && t !== -Infinity) {
			let r = Math.max(0, t - e) + 2 * Math.max(n, 36), i = (e + t) / 2;
			for (let t of g) t.y = i, t.height = r, t.swimlaneContentTop = e;
			let a = [...g].sort((e, t) => {
				var n, r;
				return ((n = e.x) == null ? 0 : n) - ((r = t.x) == null ? 0 : r);
			}), o = [], s = [], c = [];
			for (let e of a) {
				let t = h.get(e.id);
				if (!t) continue;
				let n = Math.max(0, t.maxX - t.minX) + 2 * ae(e), r = (t.minX + t.maxX) / 2;
				o.push(e.id), s.push(r), c.push(n);
			}
			let l = o.length;
			if (l > 0) {
				let e = /* @__PURE__ */ new Map();
				if (l === 1) e.set(o[0], c[0]);
				else {
					let t = [];
					for (let e = 0; e < l - 1; e++) t.push(s[e + 1] - s[e]);
					let n = Array(l);
					n[0] = 0;
					for (let e = 0; e < l - 1; e++) n[e + 1] = 2 * t[e] - n[e];
					let r = 0, i = Infinity;
					for (let e = 0; e < l; e++) {
						let t = c[e];
						e % 2 == 0 ? r = Math.max(r, t - n[e]) : i = Math.min(i, n[e] - t);
					}
					let a = r;
					a = r <= i ? (r + i) / 2 : r;
					for (let t = 0; t < l; t++) {
						let r = n[t] + (t % 2 == 0 ? a : -a), i = Math.max(c[t], r);
						e.set(o[t], i);
					}
				}
				for (let t of g) {
					let n = e.get(t.id);
					n != null && (t.width = n), oe(t);
				}
			}
		}
	}
}
e(ce, "writeBackToLayoutData");
var le = "[EdgeLabelNodes]";
function ue(e) {
	let n = [], r = [], i = /* @__PURE__ */ new Map();
	for (let t of e.nodes) i.set(t.id, t);
	for (let c of e.edges) {
		var a, o, s;
		if (!c.label || c.label.length === 0 || c.isLayoutOnly || c.labelNodeId) continue;
		let e = c.start ? i.get(c.start) : void 0, l = c.end ? i.get(c.end) : void 0;
		if (!e || !l) {
			t.warn(le, `Edge ${c.id} has missing source or target node`);
			continue;
		}
		let u = `edge-label-${c.start}-${c.end}-${c.id}`, d = e.parentId === l.parentId ? e.parentId : l.parentId, f = {
			id: u,
			label: c.label,
			edgeStart: (a = c.start) == null ? "" : a,
			edgeEnd: (o = c.end) == null ? "" : o,
			shape: "labelRect",
			width: 0,
			height: 0,
			isEdgeLabel: !0,
			isDummy: !0,
			parentId: d,
			isGroup: !1,
			labelStyle: Array.isArray(c.labelStyle) ? c.labelStyle[0] : (s = c.labelStyle) == null ? "" : s,
			...e.dir ? { dir: e.dir } : {}
		};
		n.push(f), c.labelNodeId = u, c.label = void 0, c.text = void 0;
		let p = {
			id: `${c.id}-to-label`,
			start: c.start,
			end: u,
			type: "normal",
			isLayoutOnly: !0
		}, m = {
			id: `${c.id}-from-label`,
			start: u,
			end: c.end,
			type: "normal",
			isLayoutOnly: !0
		};
		r.push(p, m);
	}
	let c = [...e.nodes, ...n], l = [...e.edges, ...r];
	return {
		...e,
		nodes: c,
		edges: l
	};
}
e(ue, "createEdgeLabelNodes");
var V = .001;
function de(e) {
	var t, n, r, i;
	let a = (t = e.x) == null ? 0 : t, o = (n = e.y) == null ? 0 : n, s = (r = e.width) == null ? 0 : r, c = (i = e.height) == null ? 0 : i;
	return s > 0 && c > 0 ? {
		cx: a,
		cy: o,
		rect: Ce(a, o, s, c)
	} : void 0;
}
e(de, "measuredNodeRect");
function fe(e) {
	var t;
	if (e.isGroup) return;
	let n = de(e);
	if (n) return {
		id: String((t = e.id) == null ? "" : t),
		cx: n.cx,
		cy: n.cy,
		rect: n.rect
	};
}
e(fe, "nodeBoundsInfoFor");
function pe(e, t, n = V) {
	return Math.abs(e.x - t.x) < n && Math.abs(e.y - t.y) < n;
}
e(pe, "samePoint");
function H(e, t, n = V) {
	return Math.abs(e.x - t.x) < n;
}
e(H, "sameX");
function U(e, t, n = V) {
	return Math.abs(e.y - t.y) < n;
}
e(U, "sameY");
function W(e, t, n = V) {
	return U(e, t, n) && Math.abs(e.x - t.x) > n;
}
e(W, "isHorizontalSegment");
function G(e, t, n = V) {
	return H(e, t, n) && Math.abs(e.y - t.y) > n;
}
e(G, "isVerticalSegment");
function K(e, t, n, r) {
	return Math.max(0, Math.min(Math.max(e, t), Math.max(n, r)) - Math.max(Math.min(e, t), Math.min(n, r)));
}
e(K, "overlapLength");
function me(e, t, n = V) {
	return e.horizontal && t.horizontal && U(e.a, t.a, n) ? K(e.a.x, e.b.x, t.a.x, t.b.x) : e.vertical && t.vertical && H(e.a, t.a, n) ? K(e.a.y, e.b.y, t.a.y, t.b.y) : 0;
}
e(me, "sameAxisSegmentOverlapLength");
function he(e, t = V) {
	let n = [];
	for (let r = 0; r < e.length - 1; r++) {
		let i = e[r], a = e[r + 1], o = W(i, a, t), s = G(i, a, t);
		(o || s) && n.push({
			index: r,
			a: i,
			b: a,
			horizontal: o,
			vertical: s
		});
	}
	return n;
}
e(he, "orthogonalSegmentsForPoints");
function ge(e, t = V) {
	let n = he(e, t), r = 0;
	for (let e = 1; e < n.length; e++) n[e - 1].horizontal !== n[e].horizontal && r++;
	return r;
}
e(ge, "countOrthogonalBends");
function q(e, t = V) {
	let n = [];
	for (let r of e) {
		let e = n.length > 0 ? n[n.length - 1] : void 0;
		(!e || !pe(e, r, t)) && n.push({
			x: r.x,
			y: r.y
		});
	}
	return n;
}
e(q, "dedupeConsecutivePoints");
function _e(e, t = V) {
	if (!e || e.length !== 4) return;
	let [n, r, i, a] = e;
	return W(n, r, t) && G(r, i, t) && W(i, a, t) ? {
		kind: "HVH",
		p0: n,
		p1: r,
		p2: i,
		p3: a
	} : G(n, r, t) && W(r, i, t) && G(i, a, t) ? {
		kind: "VHV",
		p0: n,
		p1: r,
		p2: i,
		p3: a
	} : void 0;
}
e(_e, "classifyThreeSegmentRoute");
function ve(e, t, n, r = 0) {
	let i = Math.min(e.x, t.x), a = Math.max(e.x, t.x), o = Math.min(e.y, t.y), s = Math.max(e.y, t.y);
	return a > n.left - r && i < n.right + r && s > n.top - r && o < n.bottom + r;
}
e(ve, "segmentBoundsOverlapRect");
function ye(e, t, n = 0) {
	return e.x > t.left + n && e.x < t.right - n && e.y > t.top + n && e.y < t.bottom - n;
}
e(ye, "pointInsideRect");
function be(e, t) {
	return e.left <= t.left && e.right >= t.right && e.top <= t.top && e.bottom >= t.bottom;
}
e(be, "rectContainsRect");
function xe(e, t) {
	return e.left < t.right && e.right > t.left && e.top < t.bottom && e.bottom > t.top;
}
e(xe, "rectsOverlap");
function Se(e, t) {
	return {
		left: e.left - t,
		right: e.right + t,
		top: e.top - t,
		bottom: e.bottom + t
	};
}
e(Se, "inflateRect");
function Ce(e, t, n, r) {
	return {
		left: e - n / 2,
		right: e + n / 2,
		top: t - r / 2,
		bottom: t + r / 2
	};
}
e(Ce, "rectFromCenterSize");
function we(e) {
	var t;
	return (t = de(e)) == null ? void 0 : t.rect;
}
e(we, "rectOfNodeBounds");
function Te(e, t) {
	switch (t) {
		case "top": return {
			x: e.cx,
			y: e.rect.top
		};
		case "bottom": return {
			x: e.cx,
			y: e.rect.bottom
		};
		case "left": return {
			x: e.rect.left,
			y: e.cy
		};
		case "right": return {
			x: e.rect.right,
			y: e.cy
		};
	}
}
e(Te, "portForRectSide");
function Ee(e, t, n, r, i, a = V) {
	let o = t === "left" || t === "right", s = r === "left" || r === "right";
	if (o && s) {
		if (t === "right" && r === "left" && e.x < n.x || t === "left" && r === "right" && e.x > n.x) {
			if (U(e, n, a)) return [e, n];
			let t = (e.x + n.x) / 2;
			return [
				e,
				{
					x: t,
					y: e.y
				},
				{
					x: t,
					y: n.y
				},
				n
			];
		}
		if (t === r) {
			if (U(e, n, a)) return;
			let r = t === "left" ? Math.min(e.x, n.x) - i : Math.max(e.x, n.x) + i;
			return [
				e,
				{
					x: r,
					y: e.y
				},
				{
					x: r,
					y: n.y
				},
				n
			];
		}
		return;
	}
	if (!o && !s) {
		if (t === r) {
			if (H(e, n, a)) return;
			let r = t === "top" ? Math.min(e.y, n.y) - i : Math.max(e.y, n.y) + i;
			return [
				e,
				{
					x: e.x,
					y: r
				},
				{
					x: n.x,
					y: r
				},
				n
			];
		}
		if (!(t === "bottom" && r === "top" && e.y < n.y || t === "top" && r === "bottom" && e.y > n.y)) return;
		if (H(e, n, a)) return [e, n];
		let o = (e.y + n.y) / 2;
		return [
			e,
			{
				x: e.x,
				y: o
			},
			{
				x: n.x,
				y: o
			},
			n
		];
	}
	if (o && !s) {
		let i = t === "right" && n.x > e.x || t === "left" && n.x < e.x, a = r === "top" && e.y < n.y || r === "bottom" && e.y > n.y;
		return i && a ? [
			e,
			{
				x: n.x,
				y: e.y
			},
			n
		] : void 0;
	}
	let c = t === "bottom" && n.y > e.y || t === "top" && n.y < e.y, l = r === "left" && e.x < n.x || r === "right" && e.x > n.x;
	return c && l ? [
		e,
		{
			x: e.x,
			y: n.y
		},
		n
	] : void 0;
}
e(Ee, "buildOrthogonalPortPath");
function De(e, t, n, r) {
	return t === "left" || t === "right" ? [
		e,
		{
			x: r,
			y: e.y
		},
		{
			x: r,
			y: n.y
		},
		n
	] : [
		e,
		{
			x: e.x,
			y: r
		},
		{
			x: n.x,
			y: r
		},
		n
	];
}
e(De, "buildSameSideTrackPath");
function Oe(e) {
	let t = /* @__PURE__ */ new Map(), n = [];
	for (let r of e) {
		if (r.isEdgeLabel) continue;
		let e = fe(r);
		e && (t.set(e.id, e), n.push({
			id: e.id,
			rect: e.rect
		}));
	}
	return {
		nodeInfoById: t,
		realNodeRects: n
	};
}
e(Oe, "collectRealNodeBounds");
function ke(e) {
	let t = [], n = [];
	for (let r of e) {
		let e = fe(r);
		if (!e) continue;
		let i = {
			id: e.id,
			rect: e.rect
		};
		r.isEdgeLabel ? n.push(i) : t.push(i);
	}
	return {
		realNodeRects: t,
		labelNodeRects: n
	};
}
e(ke, "collectNodeRectEntries");
function Ae(e, { includeEdgeLabels: t = !0 } = {}) {
	let n = [];
	for (let s of e) {
		var r, i, a, o;
		if (s.isGroup || !t && s.isEdgeLabel) continue;
		let e = (r = s.x) == null ? 0 : r, c = (i = s.y) == null ? 0 : i, l = (a = s.width) == null ? 0 : a, u = (o = s.height) == null ? 0 : o;
		n.push({
			nodeId: s.id,
			...Ce(e, c, l, u)
		});
	}
	return n;
}
e(Ae, "collectLayoutNodeRects");
function je(e, t, n = V) {
	let r = e.start, i = e.end;
	if (!r || !i) return;
	let a = t.get(r), o = t.get(i);
	if (!(!a || !o)) return {
		srcId: r,
		dstId: i,
		srcInfo: a,
		dstInfo: o,
		collinearX: Math.abs(a.cx - o.cx) < n,
		collinearY: Math.abs(a.cy - o.cy) < n
	};
}
e(je, "getNodePairGeometry");
function J(e, t, n, r = [], i = 0) {
	for (let a of n) if (!r.includes(a.id) && ve(e, t, a.rect, -i)) return !0;
	return !1;
}
e(J, "segmentHitsAnyRect");
function Me(e, t, n, r, i = V, a = 1e-6) {
	let o = U(e, t, i), s = H(e, t, i), c = U(n, r, i), l = H(n, r, i);
	if (o && c || s && l || !(o || s) || !(c || l)) return !1;
	let u = o ? {
		a: e,
		b: t
	} : {
		a: n,
		b: r
	}, d = s ? {
		a: e,
		b: t
	} : {
		a: n,
		b: r
	}, f = u.a.y, p = Math.min(u.a.x, u.b.x), m = Math.max(u.a.x, u.b.x), h = d.a.x, g = Math.min(d.a.y, d.b.y), _ = Math.max(d.a.y, d.b.y);
	if (h < p || h > m || f < g || f > _) return !1;
	let v = Math.abs(h - u.a.x) < a && Math.abs(f - u.a.y) < a || Math.abs(h - u.b.x) < a && Math.abs(f - u.b.y) < a, y = Math.abs(h - d.a.x) < a && Math.abs(f - d.a.y) < a || Math.abs(h - d.b.x) < a && Math.abs(f - d.b.y) < a;
	return !(v && y);
}
e(Me, "orthogonalSegmentsCross");
function Ne(e, t, n, r, i = V) {
	let a = U(e, t, i), o = H(e, t, i), s = U(n, r, i), c = H(n, r, i);
	return o && c && H(e, n, i) ? K(e.y, t.y, n.y, r.y) > i : a && s && U(e, n, i) ? K(e.x, t.x, n.x, r.x) > i : !1;
}
e(Ne, "sameAxisSegmentsOverlap");
function Pe(e, t, n, r, { epsilon: i = V, skipDegenerateOther: a = !1 } = {}) {
	for (let o of n) {
		if (o === r || o.isLayoutOnly) continue;
		let n = o.points;
		if (!(!n || n.length < 2)) for (let r = 0; r < n.length - 1; r++) {
			let o = n[r], s = n[r + 1];
			if (!(a && pe(o, s, i)) && (Me(e, t, o, s, i) || Ne(e, t, o, s, i))) return !0;
		}
	}
	return !1;
}
e(Pe, "segmentConflictsWithAnyEdge");
function Fe(e, t, n, r, i = V) {
	let a = U(e, t, i), o = H(e, t, i), s = U(n, r, i), c = H(n, r, i);
	if (!(a && c || o && s)) return !1;
	let l = a ? {
		a: e,
		b: t
	} : {
		a: n,
		b: r
	}, u = a ? {
		a: n,
		b: r
	} : {
		a: e,
		b: t
	}, d = l.a.y, f = Math.min(l.a.x, l.b.x), p = Math.max(l.a.x, l.b.x), m = u.a.x, h = Math.min(u.a.y, u.b.y), g = Math.max(u.a.y, u.b.y);
	return m > f + i && m < p - i && d > h + i && d < g - i;
}
e(Fe, "orthogonalSegmentsStrictlyCross");
function Ie(e, t, n) {
	let r = Math.min(t, n), i = Math.max(t, n);
	return e > r + V && e < i - V;
}
e(Ie, "strictlyBetween");
function Le(e, t, n) {
	return H(e, t) && H(t, n) ? Ie(t.y, e.y, n.y) : U(e, t) && U(t, n) ? Ie(t.x, e.x, n.x) : !1;
}
e(Le, "isCollinearIntermediate");
function Re(e) {
	let t = !1, n = [];
	for (let r = 0; r < e.length; r++) {
		let i = n[n.length - 1], a = e[r], o = r + 1 < e.length ? e[r + 1] : void 0;
		if (i && o) {
			if (pe(i, o)) {
				r++, t = !0;
				continue;
			}
			if (Le(i, a, o)) {
				t = !0;
				continue;
			}
		}
		n.push(a);
	}
	return {
		points: n,
		changed: t
	};
}
e(Re, "simplifyPolylineOnce");
function ze(e) {
	let t = [e[0]];
	for (let n = 1; n < e.length; n++) {
		let r = t[t.length - 1], i = e[n];
		if (!H(r, i) && !U(r, i)) {
			let e = t.length >= 2 ? t[t.length - 2] : void 0, n = e && H(e, r) ? {
				x: r.x,
				y: i.y
			} : {
				x: i.x,
				y: r.y
			};
			t.push(n);
		}
		t.push(i);
	}
	let n = [];
	for (let e of t) {
		let t = n[n.length - 1];
		(!t || !pe(t, e)) && n.push(e);
	}
	return n;
}
e(ze, "orthogonalizePolyline");
function Be(e) {
	if (e.length < 3) return e;
	let t = [...e];
	for (let e = 0; e < 32; e++) {
		let e = Re(t);
		if (t = e.points, !e.changed) break;
	}
	return t;
}
e(Be, "simplifyPolyline");
var Y = .001, Ve = .5, He = 4;
function Ue(e, t, n) {
	let r = e;
	if (r.isLayoutOnly || !r.points || r.points.length < n) return;
	let i = r.start ? t.get(r.start) : void 0, a = r.end ? t.get(r.end) : void 0;
	return {
		edge: r,
		points: r.points,
		srcRect: i ? we(i) : void 0,
		dstRect: a ? we(a) : void 0
	};
}
e(Ue, "endpointContextFor");
function We(e, t, n) {
	if (U(e, t, Y)) return {
		x: e.x < n.left ? n.left : n.right,
		y: e.y
	};
	if (H(e, t, Y)) {
		let t = e.y < n.top ? n.top : n.bottom;
		return {
			x: e.x,
			y: t
		};
	}
	return {
		x: Math.min(n.right, Math.max(n.left, e.x)),
		y: Math.min(n.bottom, Math.max(n.top, e.y))
	};
}
e(We, "segmentEnterPoint");
function Ge(e, t, n) {
	let r = n ? 1 : -1, i = n ? 0 : e.length - 1;
	for (; i >= 0 && i < e.length && ye(e[i], t, Ve);) i += r;
	if (i < 0 || i >= e.length) return e;
	let a = i - r;
	if (a < 0 || a >= e.length) return e;
	let o = We(e[i], e[a], t);
	return n ? [o, ...e.slice(i)] : [...e.slice(0, i + 1), o];
}
e(Ge, "clipEndpoint");
function Ke(e, t) {
	for (let n of e) {
		let e = Ue(n, t, 2);
		if (!e) continue;
		let r = [...e.points];
		e.srcRect && (r = Ge(r, e.srcRect, !0)), e.dstRect && (r = Ge(r, e.dstRect, !1)), r = Be(ze(r)), r = rt(r, e.srcRect, e.dstRect), e.edge.points = Be(ze(r));
	}
}
e(Ke, "clipEdgeEndpointsToNodeBoundaries");
function qe(e, t, n, r = !1) {
	if (U(e, t, Y)) {
		if (t.y < n.top - Y || t.y > n.bottom + Y) return t;
		if (r) {
			if (e.x < n.left - Y) return {
				x: n.left,
				y: e.y
			};
			if (e.x > n.right + Y) return {
				x: n.right,
				y: e.y
			};
		}
		return {
			x: Math.abs(t.x - n.left) <= Math.abs(t.x - n.right) ? n.left : n.right,
			y: e.y
		};
	}
	if (H(e, t, Y)) {
		if (t.x < n.left - Y || t.x > n.right + Y) return t;
		if (r) {
			if (e.y < n.top - Y) return {
				x: e.x,
				y: n.top
			};
			if (e.y > n.bottom + Y) return {
				x: e.x,
				y: n.bottom
			};
		}
		let i = Math.abs(t.y - n.top) <= Math.abs(t.y - n.bottom);
		return {
			x: e.x,
			y: i ? n.top : n.bottom
		};
	}
	return t;
}
e(qe, "snapEndpointToBoundary");
function Je(e, t, n) {
	let r = e[t];
	for (let i = t + n; i >= 0 && i < e.length; i += n) {
		let t = e[i];
		if (!pe(t, r, Y)) return t;
	}
	return e[t + n];
}
e(Je, "firstDistinctAdjacent");
function Ye(e, t) {
	let n = e + He, r = t - He;
	return n <= r ? {
		lo: n,
		hi: r
	} : {
		lo: (e + t) / 2,
		hi: (e + t) / 2
	};
}
e(Ye, "cornerClearanceRange");
function Xe(e, t, n) {
	let { lo: r, hi: i } = Ye(t, n);
	return Math.min(i, Math.max(r, e));
}
e(Xe, "clampToCornerClearance");
function Ze(e) {
	let t = Math.max(...e.map((e) => e.lo)), n = Math.min(...e.map((e) => e.hi));
	if (!(t > n)) return {
		lo: t,
		hi: n
	};
}
e(Ze, "intersectRanges");
function Qe(e, t) {
	return t === "left" || t === "right" ? Ye(e.top, e.bottom) : Ye(e.left, e.right);
}
e(Qe, "clearanceRangeForSide");
function $e(e, t, n) {
	let r = e.y >= n.top - Y && e.y <= n.bottom + Y, i = e.x >= n.left - Y && e.x <= n.right + Y;
	if (U(e, t, Y) && r) {
		if (Math.abs(e.x - n.left) < Y) return "left";
		if (Math.abs(e.x - n.right) < Y) return "right";
	}
	if (H(e, t, Y) && i) {
		if (Math.abs(e.y - n.top) < Y) return "top";
		if (Math.abs(e.y - n.bottom) < Y) return "bottom";
	}
}
e($e, "terminalSideForSegment");
function et(e) {
	return e === "left" || e === "right";
}
e(et, "isHorizontalSide");
function tt(e, t, n, r, i) {
	let a = [], o = n ? $e(e, t, n) : void 0, s = r ? $e(t, e, r) : void 0;
	return n && o && et(o) === i && a.push(Qe(n, o)), r && s && et(s) === i && a.push(Qe(r, s)), a.length > 0 ? Ze(a) : void 0;
}
e(tt, "straightClearanceRange");
function nt(e, t, n, r, i) {
	let a = tt(e, t, n, r, i);
	if (!a) return;
	let o = i ? e.y : e.x, s = Math.min(a.hi, Math.max(a.lo, o));
	if (!(Math.abs(s - o) < Y)) return i ? [{
		x: e.x,
		y: s
	}, {
		x: t.x,
		y: s
	}] : [{
		x: s,
		y: e.y
	}, {
		x: s,
		y: t.y
	}];
}
e(nt, "clearStraightEndpointCornerAxis");
function rt(e, t, n) {
	if (e.length !== 2) return e;
	let [r, i] = e;
	if (U(r, i, Y)) {
		var a;
		return (a = nt(r, i, t, n, !0)) == null ? e : a;
	}
	if (H(r, i, Y)) {
		var o;
		return (o = nt(r, i, t, n, !1)) == null ? e : o;
	}
	return e;
}
e(rt, "clearStraightEndpointCornerConnections");
function it(e, t, n) {
	return et(n) ? {
		x: e.x,
		y: Xe(e.y, t.top, t.bottom)
	} : {
		x: Xe(e.x, t.left, t.right),
		y: e.y
	};
}
e(it, "cornerClearedEndpoint");
function at(e, t, n, r, i, a) {
	let o = e.map((e) => ({ ...e }));
	for (let s = t; s >= 0 && s < e.length; s += n) {
		let t = e[s];
		if (a && !U(t, r, Y) || !a && !H(t, r, Y)) break;
		a ? o[s].y = i.y : o[s].x = i.x;
	}
	return o;
}
e(at, "moveCollinearEndpointRun");
function ot(e, t, n) {
	if (e.length < 2) return e;
	let r = n ? 0 : e.length - 1, i = n ? 1 : -1, a = e[r], o = Je(e, r, i);
	if (!o) return e;
	let s = $e(a, o, t);
	if (!s) return e;
	let c = et(s), l = it(a, t, s);
	return pe(a, l, Y) ? e : at(e, r, i, a, l, c);
}
e(ot, "clearEndpointCornerConnection");
function st(e, t, n) {
	let r = Math.min(e.x, t.x) >= n.left - Y && Math.max(e.x, t.x) <= n.right + Y, i = Math.min(e.y, t.y) >= n.top - Y && Math.max(e.y, t.y) <= n.bottom + Y;
	if (Math.abs(e.y - n.top) < Y && Math.abs(t.y - n.top) < Y && r) return "top";
	if (Math.abs(e.y - n.bottom) < Y && Math.abs(t.y - n.bottom) < Y && r) return "bottom";
	if (Math.abs(e.x - n.left) < Y && Math.abs(t.x - n.left) < Y && i) return "left";
	if (Math.abs(e.x - n.right) < Y && Math.abs(t.x - n.right) < Y && i) return "right";
}
e(st, "borderSideForSegment");
function ct(e, t, n, r) {
	switch (e) {
		case "top": return H(t, n, Y) && n.y < r.top - Y;
		case "bottom": return H(t, n, Y) && n.y > r.bottom + Y;
		case "left": return U(t, n, Y) && n.x < r.left - Y;
		case "right": return U(t, n, Y) && n.x > r.right + Y;
	}
}
e(ct, "leavesOutward");
function lt(e, t, n) {
	if (e.length < 3) return e;
	if (n) {
		let n = st(e[0], e[1], t);
		return n && ct(n, e[1], e[2], t) ? e.slice(1) : e;
	}
	let r = e.length - 1, i = st(e[r - 1], e[r], t);
	return i && ct(i, e[r - 1], e[r - 2], t) ? e.slice(0, r) : e;
}
e(lt, "collapseOwnBorderStub");
function ut(e, t, n) {
	let r = e;
	if (t) {
		let e = Je(r, 0, 1);
		if (e) {
			let n = qe(e, r[0], t);
			n !== r[0] && (r = [n, ...r.slice(1)]);
		}
		r = lt(r, t, !0);
	}
	if (n) {
		let e = r.length - 1, t = Je(r, e, -1);
		if (t) {
			let i = qe(t, r[e], n, !0);
			i !== r[e] && (r = [...r.slice(0, e), i]);
		}
		r = lt(r, n, !1);
	}
	let i = rt(r, t, n);
	return i !== r || r.length === 2 ? i : (t && (r = ot(r, t, !0)), n && (r = ot(r, n, !1)), r);
}
e(ut, "snapAndCollapseEndpoints");
function dt(e, t) {
	for (let n of e) {
		let e = Ue(n, t, 2);
		if (!e) continue;
		let r = ut(q(e.points, Y), e.srcRect, e.dstRect);
		if (r.length < 3) {
			e.edge.points = r;
			continue;
		}
		let i = [
			r[0],
			{ ...r[0] },
			...r.slice(1, -1),
			r[r.length - 1],
			{ ...r[r.length - 1] }
		];
		e.edge.points = i;
	}
}
e(dt, "prepareEdgeEndpointsForRenderer");
function ft(e) {
	return new Map(e.map((e) => [e.id, e]));
}
e(ft, "buildNodeMap");
function pt(e, t) {
	let n = e.parentId, r = null;
	for (; n;) {
		let e = t.get(n);
		if (!(e != null && e.isGroup)) break;
		r = e.id, n = e.parentId;
	}
	return r;
}
e(pt, "resolveTopLevelGroupId");
function mt(e, t) {
	let n = 0, r = e.parentId;
	for (; r;) {
		let e = t.get(r);
		if (!(e != null && e.isGroup)) break;
		n++, r = e.parentId;
	}
	return n;
}
e(mt, "groupDepth");
function ht(e) {
	let t = Infinity, n = -Infinity, r = Infinity, i = -Infinity;
	for (let s of e) {
		var a, o;
		let e = s.x, c = s.y;
		if (typeof e != "number" || typeof c != "number") continue;
		let l = (a = s.width) == null ? 0 : a, u = (o = s.height) == null ? 0 : o;
		t = Math.min(t, e - l / 2), n = Math.max(n, e + l / 2), r = Math.min(r, c - u / 2), i = Math.max(i, c + u / 2);
	}
	return t === Infinity || r === Infinity ? null : {
		minX: t,
		maxX: n,
		minY: r,
		maxY: i
	};
}
e(ht, "boundsForChildren");
function gt(e, t) {
	var n;
	let r = (n = e.padding) == null ? 20 : n;
	e.x = (t.minX + t.maxX) / 2, e.y = (t.minY + t.maxY) / 2, e.width = Math.max(0, t.maxX - t.minX) + r, e.height = Math.max(0, t.maxY - t.minY) + r;
}
e(gt, "applyGroupBounds");
function _t(e) {
	let t = ft(e), n = e.filter((e) => e.isGroup && e.parentId).sort((e, n) => mt(n, t) - mt(e, t));
	for (let t of n) {
		let n = ht(e.filter((e) => e.parentId === t.id));
		n && gt(t, n);
	}
}
e(_t, "recomputeNestedGroupBounds");
function vt(t, n) {
	var r, i;
	let a = (r = t.nodes) == null ? [] : r, o = (i = t.edges) == null ? [] : i, s = a.filter((e) => !e.isGroup), c = Infinity, l = -Infinity;
	for (let e of s) {
		let t = e[n];
		typeof t == "number" && (c = Math.min(c, t), l = Math.max(l, t));
	}
	if (!Number.isFinite(c) || !Number.isFinite(l)) return !1;
	let u = /* @__PURE__ */ e((e) => c + l - e, "mirror");
	for (let e of a) {
		let t = e[n];
		typeof t == "number" && (e[n] = u(t));
		let r = e.groupTitleRect;
		r && (e.groupTitleRect = n === "x" ? {
			...r,
			left: u(r.right),
			right: u(r.left)
		} : {
			...r,
			top: u(r.bottom),
			bottom: u(r.top)
		});
	}
	for (let e of o) {
		var d;
		for (let t of (d = e.points) == null ? [] : d) t[n] = u(t[n]);
	}
	return !0;
}
e(vt, "mirrorAxis");
function yt(e) {
	var t;
	return !((t = e.nodes) == null ? [] : t).some((e) => !e.isGroup) || vt(e, "y");
}
e(yt, "applyBtDirectionTransform");
function bt(e, t = "LR") {
	var n, r;
	let i = (n = e.nodes) == null ? [] : n, a = (r = e.edges) == null ? [] : r, o = i.filter((e) => !e.isGroup), s = Infinity, c = Infinity;
	for (let e of o) {
		var l, u;
		let t = (l = e.x) == null ? 0 : l, n = (u = e.y) == null ? 0 : u;
		t < s && (s = t), n < c && (c = n);
	}
	if (!Number.isFinite(s) || !Number.isFinite(c)) return !1;
	let d = 0, f = 0;
	for (let e of o) {
		var p, m;
		d += (p = e.width) == null ? 0 : p, f += (m = e.height) == null ? 0 : m;
	}
	let h = d / o.length, g = f / o.length, _ = g > 0 ? Math.max(1, h / g) : 1;
	for (let e of o) {
		var v, y;
		let t = (v = e.x) == null ? 0 : v, n = (((y = e.y) == null ? 0 : y) - c) * _ + 36, r = t - s;
		e.x = n, e.y = r;
	}
	for (let e of a) if (e.points) for (let t of e.points) {
		let e = t.x, n = (t.y - c) * _ + 36, r = e - s;
		t.x = n, t.y = r;
	}
	_t(i);
	let b = i.filter((e) => e.isGroup && !e.parentId);
	if (b.length === 0) return t === "RL" && vt(e, "x"), !0;
	let x = ft(i), S = /* @__PURE__ */ new Map();
	for (let e of i) {
		var C;
		if (e.isGroup) continue;
		let t = pt(e, x);
		if (!t) continue;
		let n = (C = S.get(t)) == null ? [] : C;
		n.push(e), S.set(t, n);
	}
	let w = 0;
	for (let e of b) {
		var T;
		let t = (T = e.padding) == null ? 0 : T;
		t > w && (w = t);
	}
	let E = [], D = Infinity, O = -Infinity;
	for (let e of b) {
		var k;
		let t = ht((k = S.get(e.id)) == null ? [] : k);
		t && (D = Math.min(D, t.minX), O = Math.max(O, t.maxX), E.push({
			lane: e,
			contentTop: t.minY,
			contentBottom: t.maxY,
			centerY: (t.minY + t.maxY) / 2
		}));
	}
	if (D === Infinity || O === -Infinity) return !0;
	let A = Math.max(0, O - D) + 2 * Math.max(w, 10), j = 36 + A, M = (D + O) / 2 - A / 2 - 36, N = M + j / 2, P = Math.max(w, 36);
	E.sort((e, t) => e.centerY - t.centerY);
	for (let e = 0; e < E.length; e++) {
		let t = E[e], n, r;
		if (n = e === 0 ? t.contentTop - P : (E[e - 1].contentBottom + t.contentTop) / 2, e === E.length - 1) r = t.contentBottom + P;
		else {
			let n = E[e + 1];
			r = (t.contentBottom + n.contentTop) / 2;
		}
		let i = Math.max(0, r - n), a = (n + r) / 2;
		t.lane.x = N, t.lane.y = a, t.lane.width = j, t.lane.height = i, t.lane.swimlaneContentTop = t.contentTop, t.lane.groupTitleRect = {
			left: M,
			right: M + 36,
			top: n,
			bottom: r
		};
	}
	return t === "RL" && vt(e, "x"), !0;
}
e(bt, "applyLrDirectionTransform");
var xt = 1e-6, St = 8, Ct = [
	0,
	St,
	-St,
	2 * St,
	-2 * St
];
function wt(e, t) {
	let { nodeInfoById: n, realNodeRects: r } = Oe(t);
	for (let t of e) {
		if (t.isLayoutOnly) continue;
		let i = t.points;
		if (!i || i.length < 4) continue;
		let a = _e(q(i, xt), xt);
		if (!a) continue;
		let { p3: o } = a, s = a.kind === "HVH", c = je(t, n, xt);
		if (!c) continue;
		let { srcId: l, dstId: u, srcInfo: d, dstInfo: f, collinearX: p, collinearY: m } = c;
		if (p || m) continue;
		let h, g = d.rect;
		for (let n of Ct) {
			let i, a, c;
			if (s) {
				let e = f.cy > d.cy ? g.bottom : g.top, t = d.cx + n;
				if (t <= g.left + xt || t >= g.right - xt) continue;
				i = {
					x: t,
					y: e
				}, a = {
					x: t,
					y: o.y
				}, c = {
					x: o.x,
					y: o.y
				};
			} else {
				let e = f.cx > d.cx ? g.right : g.left, t = d.cy + n;
				if (t <= g.top + xt || t >= g.bottom - xt) continue;
				i = {
					x: e,
					y: t
				}, a = {
					x: o.x,
					y: t
				}, c = {
					x: o.x,
					y: o.y
				};
			}
			let p = pe(i, a, xt), m = pe(a, c, xt);
			if (p && m || !p && J(i, a, r, [l], 1) || !m && J(a, c, r, [u], 1)) continue;
			let _ = !p && Pe(i, a, e, t, {
				epsilon: xt,
				skipDegenerateOther: !0
			}), v = !m && Pe(a, c, e, t, {
				epsilon: xt,
				skipDegenerateOther: !0
			});
			if (!(_ || v)) {
				h = p ? [a, c] : m ? [i, a] : [
					i,
					a,
					c
				];
				break;
			}
		}
		h && (t.points = h);
	}
}
e(wt, "portSwapToLShape");
function Tt(t, n) {
	let r = .001, { realNodeRects: i, labelNodeRects: a } = ke(n.values());
	for (let u of t) {
		var o, s;
		if (u.isLayoutOnly) continue;
		let d = u.points;
		if (!d || d.length < 4) continue;
		let f = q(d, r);
		if (f.length < 4) continue;
		let p = f.length - 1, m = f[p], h = f[p - 1], g = f[p - 2], _ = m.x - h.x, v = m.y - h.y, y = Math.hypot(_, v);
		if (y >= 10 || y < r) continue;
		let b = h.x - g.x, x = h.y - g.y;
		if (Math.hypot(b, x) < r) continue;
		let S = W(h, m, r), C = G(h, m, r), w = W(g, h, r), T = G(g, h, r);
		if (!(S && T || C && w)) continue;
		let E = u.end, D = u.start, O = E ? n.get(E) : void 0;
		if (!O) continue;
		let k = (o = O.x) == null ? 0 : o, A = (s = O.y) == null ? 0 : s, j = we(O);
		if (!j) continue;
		let M, N;
		if (T) {
			let e = x < 0;
			M = {
				x: k,
				y: g.y
			}, N = {
				x: k,
				y: e ? j.bottom : j.top
			};
		} else {
			let e = b > 0;
			M = {
				x: g.x,
				y: A
			}, N = {
				x: e ? j.right : j.left,
				y: A
			};
		}
		if (J(M, N, i, E ? [E] : [], -2) || J(M, N, a, [], -2)) continue;
		if (D) {
			let e = n.get(D), t = e ? we(e) : void 0;
			if (t && ye(M, t, 2)) continue;
		}
		let P = /* @__PURE__ */ e((e, t) => `${e.x.toFixed(3)},${e.y.toFixed(3)}|${t.x.toFixed(3)},${t.y.toFixed(3)}`, "ownSegmentKey"), F = /* @__PURE__ */ new Set();
		for (let e = 0; e < f.length - 1; e++) F.add(P(f[e], f[e + 1]));
		let I = /* @__PURE__ */ e((e, n) => {
			for (let i of t) {
				if (i === u || i.isLayoutOnly) continue;
				let t = i.points;
				if (!(!t || t.length < 2)) for (let i = 0; i < t.length - 1; i++) {
					let a = t[i], o = t[i + 1];
					if (!F.has(P(a, o)) && Fe(e, n, a, o, r)) return !0;
				}
			}
			return !1;
		}, "segmentCrossesOtherEdge");
		if (I(M, N)) continue;
		if (p - 3 >= 0) {
			let e = f[p - 3], t = [D, E].filter((e) => !!e);
			if (J(e, M, i, t, -2) || I(e, M)) continue;
		}
		let L = [
			...f.slice(0, p - 2),
			M,
			N
		];
		u.points = L;
		let ee = u.labelNodeId;
		if (ee) {
			let e = n.get(ee);
			if (e) {
				var c, l;
				let t = (c = e.width) == null ? 0 : c, n = (l = e.height) == null ? 0 : l;
				if (t > 0 && n > 0) {
					let i, a, o = -1;
					for (let e = 0; e < L.length - 1; e++) {
						let s = L[e], c = L[e + 1], l = Math.hypot(c.x - s.x, c.y - s.y), u = U(s, c, r), d = H(s, c, r);
						(u && l >= t + 2 || d && l >= n + 2) && l > o && (o = l, i = (s.x + c.x) / 2, a = (s.y + c.y) / 2);
					}
					i !== void 0 && a !== void 0 && (e.x = i, e.y = a);
				}
			}
		}
	}
}
e(Tt, "collapseShortTerminalStub");
var X = .001, Z = 8, Q = he, Et = /* @__PURE__ */ e((e, t) => H(e, t, X) || U(e, t, X), "orthogonallyAligned");
function Dt(t, n) {
	let r = /* @__PURE__ */ e((e, t) => {
		var n, r, i, a;
		let o = (n = e.x) == null ? 0 : n, s = (r = e.y) == null ? 0 : r, c = t.x - o, l = t.y - s, u = ((i = e.width) == null ? 0 : i) / 2, d = ((a = e.height) == null ? 0 : a) / 2;
		return Math.abs(l) * u > Math.abs(c) * d ? (l < 0 && (d = -d), {
			x: o + (l === 0 ? 0 : d * c / l),
			y: s + d
		}) : (c < 0 && (u = -u), {
			x: o + u,
			y: s + (c === 0 ? 0 : u * l / c)
		});
	}, "rectIntersect"), i = /* @__PURE__ */ e((e, t) => {
		var i;
		let a = q((i = e.points) == null ? [] : i);
		if (a.length < 2) return;
		let o = t ? e.start : e.end, s = o ? n.get(o) : void 0, c = s ? we(s) : void 0;
		if (!s || !o || !c) return;
		let l = t ? a[0] : a[a.length - 1], u = t ? a[1] : a[a.length - 2], d = r(s, l), f = l;
		if (Et(u, d) && (f = u), H(d, f, X)) {
			var p;
			return {
				edge: e,
				edgeId: String((p = e.id) == null ? "" : p),
				nodeId: o,
				atStart: t,
				orientation: "V",
				coord: d.x,
				min: Math.min(d.y, f.y),
				max: Math.max(d.y, f.y),
				boundary: d,
				railEnd: f,
				rect: c
			};
		}
		if (U(d, f, X)) {
			var m;
			return {
				edge: e,
				edgeId: String((m = e.id) == null ? "" : m),
				nodeId: o,
				atStart: t,
				orientation: "H",
				coord: d.y,
				min: Math.min(d.x, f.x),
				max: Math.max(d.x, f.x),
				boundary: d,
				railEnd: f,
				rect: c
			};
		}
	}, "terminalLaneFor"), a = /* @__PURE__ */ e((e, t) => Math.max(0, Math.min(e.max, t.max) - Math.max(e.min, t.min)), "projectedOverlapLength"), o = /* @__PURE__ */ e((e, t) => e.nodeId !== t.nodeId || e.orientation !== t.orientation ? !1 : e.orientation === "H" ? (Math.abs(e.boundary.x - e.rect.left) < 1 || Math.abs(e.boundary.x - e.rect.right) < 1) && H(e.boundary, t.boundary, 1) : (Math.abs(e.boundary.y - e.rect.top) < 1 || Math.abs(e.boundary.y - e.rect.bottom) < 1) && U(e.boundary, t.boundary, 1), "sameTerminalFace"), s = /* @__PURE__ */ e((e, t) => e.nodeId !== t.nodeId || e.orientation !== t.orientation ? !1 : a(e, t) >= Z && Math.abs(e.coord - t.coord) < .5, "exactTerminalLaneConflict"), c = /* @__PURE__ */ e((e, t) => {
		if (e.nodeId !== t.nodeId || e.orientation !== t.orientation || e.orientation !== "H" || e.atStart === t.atStart) return !1;
		let n = a(e, t);
		if (n < Z) return !1;
		let r = e.rect.bottom - e.rect.top;
		return n < r || n > 2 * r ? !1 : o(e, t) && Math.abs(e.coord - t.coord) < 16;
	}, "nearTerminalLaneConflict"), l = /* @__PURE__ */ e((t, n) => {
		var r;
		let i = q((r = t.edge.points) == null ? [] : r);
		if (i.length < 2) return;
		let a = t.orientation === "V" ? {
			x: t.boundary.x + n,
			y: t.boundary.y
		} : {
			x: t.boundary.x,
			y: t.boundary.y + n
		}, o = t.orientation === "V" ? {
			x: t.railEnd.x + n,
			y: t.railEnd.y
		} : {
			x: t.railEnd.x,
			y: t.railEnd.y + n
		};
		if (!(/* @__PURE__ */ e(() => Math.abs(t.boundary.y - t.rect.top) < 1 || Math.abs(t.boundary.y - t.rect.bottom) < 1 ? U(a, t.boundary, X) && a.x >= t.rect.left + 1 && a.x <= t.rect.right - 1 : Math.abs(t.boundary.x - t.rect.left) < 1 || Math.abs(t.boundary.x - t.rect.right) < 1 ? H(a, t.boundary, X) && a.y >= t.rect.top + 1 && a.y <= t.rect.bottom - 1 : !1, "boundaryStaysOnSameFace"))()) return;
		if (t.atStart) {
			let e = i.length > 1 && pe(i[1], t.railEnd, X), n = i.slice(e ? 2 : 1), r = n[0];
			return r && !Et(r, o) ? void 0 : [
				a,
				o,
				...n
			];
		}
		let s = i.length > 1 && pe(i[i.length - 2], t.railEnd, X), c = i.slice(0, s ? -2 : -1), l = c[c.length - 1];
		if (!(l && !Et(l, o))) return [
			...c,
			o,
			a
		];
	}, "shiftedCandidate"), u = /* @__PURE__ */ e((e) => {
		var t, r, i, a, o;
		let s = e.edge, c = q((t = s.points) == null ? [] : t);
		if (c.length !== 2) return !1;
		let l = s.start, u = s.end, d = l ? n.get(l) : void 0, f = u ? n.get(u) : void 0;
		if (!d || !f) return !1;
		let p = (r = d.x) == null ? 0 : r, m = (i = d.y) == null ? 0 : i, h = (a = f.x) == null ? 0 : a, g = (o = f.y) == null ? 0 : o, [_, v] = c;
		return U(_, v, X) && Math.abs(m - g) < 1 && Math.abs(p - h) > 1 || H(_, v, X) && Math.abs(p - h) < 1 && Math.abs(m - g) > 1;
	}, "laneIsStraightCollinearConnector"), d = [
		-7,
		7,
		-14,
		14,
		-21,
		21
	];
	for (let e = 0; e < 8; e++) {
		let e = t.filter((e) => !e.isLayoutOnly).flatMap((e) => [i(e, !0), i(e, !1)]).filter((e) => !!e), n = !1;
		for (let t = 0; t < e.length && !n; t++) for (let r = t + 1; r < e.length && !n; r++) {
			let a = e[t], o = e[r];
			if (a.edge === o.edge || !(s(a, o) || c(a, o))) continue;
			let f = !s(a, o), p = [a, o].sort((e, t) => {
				let n = u(e), r = u(t);
				return n === r ? Number(!t.atStart) - Number(!e.atStart) : Number(n) - Number(r);
			});
			for (let t of p) {
				for (let r of d) {
					let a = l(t, r);
					if (!a) continue;
					let o = i({
						...t.edge,
						points: a
					}, t.atStart);
					if (!(!o || e.some((e) => e.edge !== t.edge && (s(o, e) || f && c(o, e))))) {
						t.edge.points = a, n = !0;
						break;
					}
				}
				if (n) break;
			}
		}
		if (!n) return;
	}
}
e(Dt, "separateSharedRenderedTerminalLanes");
function Ot(t, n) {
	let { realNodeRects: r, labelNodeRects: i } = ke(n.values()), a = /* @__PURE__ */ e((e, n) => {
		let a = e.start, o = e.end, s = Q(n);
		if (s.length !== n.length - 1) return !1;
		let c = [a, o].filter((e) => !!e);
		for (let e of s) if (J(e.a, e.b, r, c, -2) || J(e.a, e.b, i, [], -2)) return !1;
		for (let n of t) {
			if (n === e || n.isLayoutOnly) continue;
			let t = n.points;
			if (!(!t || t.length < 2)) {
				for (let e of s) for (let n of Q(q(t))) if (me(e, n, .5) >= Z || Fe(e.a, e.b, n.a, n.b, X)) return !1;
			}
		}
		return !0;
	}, "candidateIsSafe"), o = /* @__PURE__ */ e((e, t) => {
		if (t + 4 >= e.length) return;
		let n = e[t], r = e[t + 1], i = e[t + 2], a = e[t + 3], o = e[t + 4], s = W(n, r) && G(r, i) && W(i, a) && G(a, o) && H(n, a, X) && H(n, o, X) && H(r, i, X) && (r.x - n.x) * (a.x - i.x) < 0, c = G(n, r) && W(r, i) && G(i, a) && W(a, o) && U(n, a, X) && U(n, o, X) && U(r, i, X) && (r.y - n.y) * (a.y - i.y) < 0;
		if (s || c) return q([
			...e.slice(0, t + 1),
			o,
			...e.slice(t + 5)
		]);
		if (t + 5 >= e.length) return;
		let l = e[t + 5], u = G(n, r) && W(r, i) && G(i, a) && W(a, o) && G(o, l) && H(n, o, X) && H(n, l, X) && H(i, a, X) && (i.x - r.x) * (o.x - a.x) < 0, d = W(n, r) && G(r, i) && W(i, a) && G(a, o) && W(o, l) && U(n, o, X) && U(n, l, X) && U(i, a, X) && (i.y - r.y) * (o.y - a.y) < 0;
		if (!(!u && !d)) return q([
			...e.slice(0, t + 1),
			l,
			...e.slice(t + 6)
		]);
	}, "withoutDogleg");
	for (let e = 0; e < 8; e++) {
		let e = !1;
		for (let n of t) {
			var s;
			if (n.isLayoutOnly) continue;
			let t = q((s = n.points) == null ? [] : s);
			for (let r = 0; r <= t.length - 5; r++) {
				let i = o(t, r);
				if (!(!i || !a(n, i))) {
					n.points = i, e = !0;
					break;
				}
			}
			if (e) break;
		}
		if (!e) return;
	}
}
e(Ot, "collapseRedundantRectangularDoglegs");
function kt(t, n) {
	let { realNodeRects: r, labelNodeRects: i } = ke(n.values()), a = t.filter((e) => !e.isLayoutOnly), o = /* @__PURE__ */ e((e, t, n) => {
		var r;
		return q(e === t ? n == null ? [] : n : (r = e.points) == null ? [] : r);
	}, "pointsFor"), s = /* @__PURE__ */ e((e, t) => {
		let n = 0;
		for (let r = 0; r < a.length; r++) {
			let i = Q(o(a[r], e, t));
			for (let s = r + 1; s < a.length; s++) {
				let r = Q(o(a[s], e, t));
				for (let e of i) for (let t of r) Fe(e.a, e.b, t.a, t.b, X) && n++;
			}
		}
		return n;
	}, "strictCrossingCount"), c = /* @__PURE__ */ e((e) => {
		let t = Q(e);
		if (t.length !== 3) return;
		let n = t[1];
		if (!(t[0].horizontal === n.horizontal || t[2].horizontal === n.horizontal)) return {
			index: n.index,
			horizontal: n.horizontal,
			vertical: n.vertical,
			segment: n
		};
	}, "middleRail"), l = /* @__PURE__ */ e((e, t) => {
		let n = [e.start, e.end].filter((e) => !!e);
		return r.filter((e) => {
			if (n.includes(e.id)) return !1;
			let r = e.rect;
			return t.horizontal ? K(t.a.x, t.b.x, r.left, r.right) >= Z && t.a.y >= r.top - 2 && t.a.y <= r.bottom + 2 : K(t.a.y, t.b.y, r.top, r.bottom) >= Z && t.a.x >= r.left - 2 && t.a.x <= r.right + 2;
		});
	}, "blockingRectsFor"), u = /* @__PURE__ */ e((e, t, n) => {
		let r = e.map((e) => ({ ...e }));
		if (t.horizontal) r[t.index].y = n, r[t.index + 1].y = n;
		else if (t.vertical) r[t.index].x = n, r[t.index + 1].x = n;
		else return;
		let i = Be(q(r));
		return Q(i).length === i.length - 1 ? i : void 0;
	}, "candidateByMovingRail"), d = /* @__PURE__ */ e((e, t, n) => {
		let c = [e.start, e.end].filter((e) => !!e), l = Q(t);
		if (l.length !== t.length - 1) return !1;
		for (let e of l) if (J(e.a, e.b, r, c, -2) || J(e.a, e.b, i, [], -2)) return !1;
		for (let t of a) if (t !== e) {
			for (let e of l) for (let n of Q(o(t))) if (me(e, n, .5) >= Z) return !1;
		}
		return s(e, t) <= n;
	}, "candidateIsSafe");
	for (let e = 0; e < 8; e++) {
		let e = s(), t = !1;
		for (let n of a) {
			let r = o(n), i = c(r);
			if (!i) continue;
			let a = l(n, i.segment);
			if (a.length === 0) continue;
			let s = i.horizontal ? [Math.min(...a.map((e) => e.rect.top)) - 20, Math.max(...a.map((e) => e.rect.bottom)) + 20] : [Math.min(...a.map((e) => e.rect.left)) - 20, Math.max(...a.map((e) => e.rect.right)) + 20];
			for (let a of s) {
				let o = u(r, i.segment, a);
				if (!(!o || !d(n, o, e))) {
					n.points = o, t = !0;
					break;
				}
			}
			if (t) break;
		}
		if (!t) return;
	}
}
e(kt, "liftObstacleHuggingSameSideRails");
function At(t, n) {
	let r = /* @__PURE__ */ e((e) => {
		let t = e.groupTitleRect;
		if (!(!t || typeof t.left != "number" || typeof t.right != "number" || typeof t.top != "number" || typeof t.bottom != "number" || !Number.isFinite(t.left) || !Number.isFinite(t.right) || !Number.isFinite(t.top) || !Number.isFinite(t.bottom) || t.right <= t.left || t.bottom <= t.top)) return {
			left: t.left,
			right: t.right,
			top: t.top,
			bottom: t.bottom
		};
	}, "validTitleRect"), i = /* @__PURE__ */ e((e) => {
		if (!e.isGroup || e.parentId) return;
		let t = e.direction, n = typeof t == "string" ? t.toUpperCase() : "";
		if (n === "LR" || n === "RL" || n === "BT") return;
		let i = r(e), a = e.y, o = e.height;
		if (!i || typeof a != "number" || typeof o != "number" || !Number.isFinite(a) || !Number.isFinite(o) || o <= 0) return;
		let s = i.right - i.left, c = i.bottom - i.top;
		if (!(c <= 0 || s < c)) return {
			node: e,
			rect: i
		};
	}, "topLaneTitleFor"), a = /* @__PURE__ */ e((e, t) => {
		if (!e.horizontal) return !1;
		let n = e.a.y;
		return n <= t.top + X || n >= t.bottom - X ? !1 : K(e.a.x, e.b.x, t.left, t.right) >= Z;
	}, "horizontalSegmentIntersectsTitle"), o = [...n.values()].map(i).filter((e) => !!e);
	if (o.length === 0) return;
	let s = 0;
	for (let e of t) {
		var c;
		if (e.isLayoutOnly) continue;
		let t = q((c = e.points) == null ? [] : c);
		for (let e of Q(t)) for (let t of o) a(e, t.rect) && (s = Math.max(s, t.rect.bottom - e.a.y + 4));
	}
	if (!(s <= X)) for (let e of o) {
		let t = e.node.y, n = e.node.height;
		typeof t != "number" || typeof n != "number" || !Number.isFinite(t) || !Number.isFinite(n) || n <= 0 || (e.node.y = t - s / 2, e.node.height = n + s, e.node.groupTitleRect = {
			...e.rect,
			top: e.rect.top - s,
			bottom: e.rect.bottom - s
		});
	}
}
e(At, "liftTopLaneTitleBandsAboveRails");
function jt(t, n) {
	let r = /* @__PURE__ */ e((e) => {
		let t = e.groupTitleRect;
		if (!(!t || typeof t.left != "number" || typeof t.right != "number" || typeof t.top != "number" || typeof t.bottom != "number" || !Number.isFinite(t.left) || !Number.isFinite(t.right) || !Number.isFinite(t.top) || !Number.isFinite(t.bottom) || t.right <= t.left || t.bottom <= t.top)) return {
			left: t.left,
			right: t.right,
			top: t.top,
			bottom: t.bottom
		};
	}, "validTitleRect"), i = /* @__PURE__ */ e((e) => {
		if (!e.isGroup || e.parentId || e.direction !== "LR") return;
		let t = r(e), n = e.x, i = e.width;
		if (!t || typeof n != "number" || typeof i != "number" || !Number.isFinite(n) || !Number.isFinite(i) || i <= 0) return;
		let a = t.right - t.left, o = t.bottom - t.top;
		if (!(a <= 0 || o < a)) return {
			node: e,
			rect: t
		};
	}, "leftLaneTitleFor"), a = /* @__PURE__ */ e((e, t) => {
		if (!e.vertical) return !1;
		let n = e.a.x;
		return n <= t.left + X || n >= t.right - X ? !1 : K(e.a.y, e.b.y, t.top, t.bottom) >= Z;
	}, "verticalSegmentIntersectsTitle"), o = /* @__PURE__ */ e((e, t) => {
		if (!e.horizontal) return !1;
		let n = e.a.y;
		return n <= t.top + X || n >= t.bottom - X ? !1 : K(e.a.x, e.b.x, t.left, t.right) >= Z;
	}, "horizontalSegmentIntersectsTitle"), s = [...n.values()].map(i).filter((e) => !!e);
	if (s.length === 0) return;
	let c = 0;
	for (let e of t) {
		var l;
		if (e.isLayoutOnly) continue;
		let t = q((l = e.points) == null ? [] : l);
		for (let e of Q(t)) for (let t of s) if (a(e, t.rect)) c = Math.max(c, t.rect.right - e.a.x + 4);
		else if (o(e, t.rect)) {
			let n = Math.min(e.a.x, e.b.x);
			c = Math.max(c, t.rect.right - n + 4);
		}
	}
	if (!(c <= X)) for (let e of s) {
		let t = e.node.x, n = e.node.width;
		typeof t != "number" || typeof n != "number" || !Number.isFinite(t) || !Number.isFinite(n) || n <= 0 || (e.node.x = t - c / 2, e.node.width = n + c, e.node.groupTitleRect = {
			...e.rect,
			left: e.rect.left - c,
			right: e.rect.right - c
		});
	}
}
e(jt, "shiftLeftLaneTitleBandsLeftOfRails");
function Mt(t, n) {
	let { realNodeRects: r } = ke(n.values()), i = t.filter((e) => !e.isLayoutOnly), a = /* @__PURE__ */ e((e, t = /* @__PURE__ */ new Map()) => {
		var n, r;
		return q((n = (r = t.get(e)) == null ? e.points : r) == null ? [] : n);
	}, "replacementPointsFor"), o = /* @__PURE__ */ e((e = /* @__PURE__ */ new Map()) => {
		let t = 0;
		for (let n = 0; n < i.length; n++) {
			let r = Q(a(i[n], e));
			for (let o = n + 1; o < i.length; o++) {
				let n = Q(a(i[o], e));
				for (let e of r) for (let r of n) Fe(e.a, e.b, r.a, r.b, X) && t++;
			}
		}
		return t;
	}, "crossingCount"), s = /* @__PURE__ */ e((e = /* @__PURE__ */ new Map()) => i.reduce((t, n) => t + ge(a(n, e)), 0), "totalBends"), c = /* @__PURE__ */ e((e) => {
		let t = a(e);
		if (t.length < 4) return;
		let n = t[t.length - 2], r = t[t.length - 1];
		if (!(!W(n, r, X) && !G(n, r, X))) return {
			tailStart: n,
			terminal: r
		};
	}, "terminalTailFor"), l = /* @__PURE__ */ e((e, t) => {
		let n = a(e);
		if (n.length < 3) return;
		let r = n[0], i = n[1], o;
		if (W(r, i, X)) o = {
			x: i.x,
			y: t.tailStart.y
		};
		else if (G(r, i, X)) o = {
			x: t.tailStart.x,
			y: i.y
		};
		else return;
		let s = Be(q([
			r,
			i,
			o,
			t.tailStart,
			t.terminal
		]));
		return Q(s).length === s.length - 1 ? s : void 0;
	}, "candidateWithDestinationTail"), u = /* @__PURE__ */ e((e, t) => {
		let n = [e.start, e.end].filter((e) => !!e);
		for (let e of Q(t)) if (J(e.a, e.b, r, n, -2)) return !0;
		return !1;
	}, "pathHasNodeHit"), d = /* @__PURE__ */ e((e, t, n) => {
		for (let r of i) if (r !== e) {
			for (let e of Q(t)) for (let t of Q(a(r, n))) if (me(e, t, .5) >= Z) return !0;
		}
		return !1;
	}, "pathHasSharedTrack"), f = /* @__PURE__ */ e((e, t, n) => !u(e, t) && !d(e, t, n), "candidateIsSafe"), p = /* @__PURE__ */ e(() => {
		let e = /* @__PURE__ */ new Map();
		for (let r of i) {
			var t;
			let i = r.end;
			if (!i || !n.has(i) || a(r).length < 4) continue;
			let o = (t = e.get(i)) == null ? [] : t;
			o.push(r), e.set(i, o);
		}
		return e;
	}, "edgesByDestination");
	for (let e = 0; e < 4; e++) {
		let e = o();
		if (e === 0) return;
		let t = s(), n, r = e, i = t;
		for (let t of p().values()) for (let a = 0; a < t.length; a++) for (let u = a + 1; u < t.length; u++) {
			let d = t[a], p = t[u], m = c(d), h = c(p);
			if (!m || !h) continue;
			let g = l(d, h), _ = l(p, m);
			if (!g || !_) continue;
			let v = /* @__PURE__ */ new Map([[d, g], [p, _]]);
			if (!f(d, g, v) || !f(p, _, v)) continue;
			let y = o(v), b = s(v);
			y >= e || y > r || y === r && b >= i || (n = v, r = y, i = b);
		}
		if (!n) return;
		for (let [e, t] of n) e.points = t;
	}
}
e(Mt, "swapDestinationTerminalTailsToReduceCrossings");
function Nt(t, n) {
	let { realNodeRects: r, labelNodeRects: i } = ke(n.values()), a = t.filter((e) => !e.isLayoutOnly), o = /* @__PURE__ */ e((e, t = /* @__PURE__ */ new Map()) => {
		var n, r;
		return q((n = (r = t.get(e)) == null ? e.points : r) == null ? [] : n);
	}, "replacementPointsFor"), s = /* @__PURE__ */ e((e = /* @__PURE__ */ new Map()) => {
		let t = 0;
		for (let n = 0; n < a.length; n++) {
			let r = Q(o(a[n], e));
			for (let i = n + 1; i < a.length; i++) {
				let n = Q(o(a[i], e));
				for (let e of r) for (let r of n) Fe(e.a, e.b, r.a, r.b, X) && t++;
			}
		}
		return t;
	}, "strictCrossingCount"), c = /* @__PURE__ */ e((e = /* @__PURE__ */ new Map()) => a.reduce((t, n) => t + ge(o(n, e)), 0), "totalBends"), l = /* @__PURE__ */ e((e) => {
		let t = e.start, r = e.end, i = t ? n.get(t) : void 0, a = r ? n.get(r) : void 0, o = i ? we(i) : void 0, s = a ? we(a) : void 0;
		return o && s ? {
			src: o,
			dst: s
		} : void 0;
	}, "endpointRectsFor"), u = /* @__PURE__ */ e((e, t, n) => {
		if (n.index <= 0 || n.index + 1 >= t.length - 1) return;
		let r = l(e);
		if (r) {
			if (n.vertical) {
				let i = n.a.x, a = Math.min(r.src.left, r.dst.left), o = Math.max(r.src.right, r.dst.right), s = i < a - X ? "left" : i > o + X ? "right" : void 0;
				return s ? {
					edge: e,
					points: t,
					segmentIndex: n.index,
					axis: "vertical",
					side: s,
					coord: i,
					min: Math.min(n.a.y, n.b.y),
					max: Math.max(n.a.y, n.b.y)
				} : void 0;
			}
			if (n.horizontal) {
				let i = n.a.y, a = Math.min(r.src.top, r.dst.top), o = Math.max(r.src.bottom, r.dst.bottom), s = i < a - X ? "top" : i > o + X ? "bottom" : void 0;
				return s ? {
					edge: e,
					points: t,
					segmentIndex: n.index,
					axis: "horizontal",
					side: s,
					coord: i,
					min: Math.min(n.a.x, n.b.x),
					max: Math.max(n.a.x, n.b.x)
				} : void 0;
			}
		}
	}, "externalRailForSegment"), d = /* @__PURE__ */ e(() => {
		let e = [];
		for (let t of a) {
			let n = o(t);
			for (let r of Q(n)) {
				let i = u(t, n, r);
				i && e.push(i);
			}
		}
		return e;
	}, "collectExternalRails"), f = /* @__PURE__ */ e((e, t) => e.edge !== t.edge && e.axis === t.axis && e.side === t.side && K(e.min, e.max, t.min, t.max) >= Z, "railsInteract"), p = /* @__PURE__ */ e((e) => {
		let t = [], n = /* @__PURE__ */ new Set();
		for (let r of e) {
			if (n.has(r)) continue;
			let i = [r], a = [];
			for (n.add(r); i.length > 0;) {
				let t = i.pop();
				a.push(t);
				for (let r of e) !n.has(r) && f(t, r) && (n.add(r), i.push(r));
			}
			a.length > 1 && t.push(a);
		}
		return t;
	}, "connectedComponents"), m = /* @__PURE__ */ e((e) => {
		let t = [];
		for (let n of e) t.some((e) => Math.abs(e - n.coord) < X) || t.push(n.coord);
		for (; t.length < e.length;) {
			let n = Math.min(...t), r = Math.max(...t), i = e[0].side;
			t.push(i === "left" || i === "top" ? n - 12 * (e.length - t.length) : r + 12 * (e.length - t.length));
		}
		return t;
	}, "uniqueCoordsFor"), h = /* @__PURE__ */ e((t) => {
		let n = t.map((e) => e.coord), r = m(t), i = [];
		if (t.length <= 6) {
			let a = Array(r.length).fill(!1), o = [], s = /* @__PURE__ */ e(() => {
				if (o.length === t.length) {
					o.some((e, t) => Math.abs(e - n[t]) >= X) && i.push([...o]);
					return;
				}
				for (let [e, t] of r.entries()) a[e] || (a[e] = !0, o.push(t), s(), o.pop(), a[e] = !1);
			}, "visit");
			return s(), i;
		}
		for (let e = 0; e < n.length; e++) for (let t = e + 1; t < n.length; t++) {
			let r = [...n];
			[r[e], r[t]] = [r[t], r[e]], i.push(r);
		}
		return i;
	}, "coordinateAssignmentsFor"), g = /* @__PURE__ */ e((e, t) => {
		let n = /* @__PURE__ */ new Map();
		for (let [i, a] of e.entries()) {
			var r;
			let e = t[i], o = (r = n.get(a.edge)) == null ? a.points.map((e) => ({
				x: e.x,
				y: e.y
			})) : r;
			a.axis === "vertical" ? (o[a.segmentIndex].x = e, o[a.segmentIndex + 1].x = e) : (o[a.segmentIndex].y = e, o[a.segmentIndex + 1].y = e), n.set(a.edge, o);
		}
		let i = /* @__PURE__ */ new Map();
		for (let [e, t] of n) {
			let n = Be(q(t));
			if (Q(n).length !== n.length - 1) return;
			i.set(e, n);
		}
		return i;
	}, "replacementsForAssignment"), _ = /* @__PURE__ */ e((e) => {
		for (let [t, n] of e) {
			let e = [t.start, t.end].filter((e) => !!e);
			for (let t of Q(n)) if (J(t.a, t.b, r, e, -2) || J(t.a, t.b, i, [], -2)) return !1;
		}
		for (let t = 0; t < a.length; t++) {
			let n = a[t], r = e.has(n), i = Q(o(n, e));
			for (let n = t + 1; n < a.length; n++) {
				let t = a[n];
				if (!r && !e.has(t)) continue;
				let s = Q(o(t, e));
				for (let e of i) for (let t of s) if (me(e, t, .5) >= Z) return !1;
			}
		}
		return !0;
	}, "candidateIsSafe");
	for (let e = 0; e < 4; e++) {
		let e = s();
		if (e === 0) return;
		let t, n = e, r = c(), i = Infinity;
		for (let a of p(d())) for (let o of h(a)) {
			let l = g(a, o);
			if (!l || !_(l)) continue;
			let u = s(l);
			if (u >= e) continue;
			let d = c(l), f = a.reduce((e, t, n) => e + Math.abs(o[n] - t.coord), 0);
			u > n || u === n && (d > r || d === r && f >= i) || (t = l, n = u, r = d, i = f);
		}
		if (!t) return;
		for (let [e, n] of t) e.points = n;
	}
}
e(Nt, "reassignCrossingExternalRailChannels");
function Pt(t, n) {
	let { realNodeRects: r, labelNodeRects: i } = ke(n.values()), a = t.filter((e) => !e.isLayoutOnly), o = /* @__PURE__ */ e((e, t, n) => {
		var r;
		return q(e === t ? n == null ? [] : n : (r = e.points) == null ? [] : r);
	}, "pointsFor"), s = /* @__PURE__ */ e((e) => Q(e).reduce((e, t) => {
		let n = t.a.x - t.b.x, r = t.a.y - t.b.y;
		return e + Math.hypot(n, r);
	}, 0), "pathLength"), c = /* @__PURE__ */ e((e, t) => {
		let n = 0;
		for (let r = 0; r < a.length; r++) {
			let i = Q(o(a[r], e, t));
			for (let s = r + 1; s < a.length; s++) {
				let r = Q(o(a[s], e, t));
				for (let e of i) for (let t of r) Fe(e.a, e.b, t.a, t.b, X) && n++;
			}
		}
		return n;
	}, "strictCrossingCount"), l = /* @__PURE__ */ e((e, t) => {
		if (e.horizontal) {
			let n = e.a.y;
			return (Math.abs(n - t.top) < 1 || Math.abs(n - t.bottom) < 1) && K(e.a.x, e.b.x, t.left, t.right) >= Z;
		}
		if (e.vertical) {
			let n = e.a.x;
			return (Math.abs(n - t.left) < 1 || Math.abs(n - t.right) < 1) && K(e.a.y, e.b.y, t.top, t.bottom) >= Z;
		}
		return !1;
	}, "segmentRunsAlongRectBorder"), u = /* @__PURE__ */ e((e) => {
		let t = [e.start, e.end].filter((e) => !!e), r = [];
		for (let e of t) {
			let t = n.get(e), i = t ? we(t) : void 0;
			i && r.push(i);
		}
		return r;
	}, "endpointRectsFor"), d = /* @__PURE__ */ e((e, t) => {
		if (t + 3 >= e.length) return [];
		let n = e[t], r = e[t + 1], i = e[t + 2], a = e[t + 3], o = W(n, r, X) && G(r, i, X) && W(i, a, X), s = G(n, r, X) && W(r, i, X) && G(i, a, X);
		if (!o && !s || !(o ? Math.sign(r.x - n.x) !== Math.sign(a.x - i.x) : Math.sign(r.y - n.y) !== Math.sign(a.y - i.y))) return [];
		let c = H(n, a, X) || U(n, a, X) ? [] : [{
			x: n.x,
			y: a.y
		}, {
			x: a.x,
			y: n.y
		}], l = c.length === 0 ? [[...e.slice(0, t + 1), ...e.slice(t + 3)]] : c.map((n) => [
			...e.slice(0, t + 1),
			n,
			...e.slice(t + 3)
		]), u = /* @__PURE__ */ new Set();
		return l.map((e) => Be(q(e))).filter((e) => {
			if (Q(e).length !== e.length - 1 || !e.some((e) => pe(e, a, X))) return !1;
			let t = e.map((e) => `${e.x.toFixed(3)},${e.y.toFixed(3)}`).join("|");
			return u.has(t) ? !1 : (u.add(t), !0);
		});
	}, "shortcutCandidatesAt"), f = /* @__PURE__ */ e((e, t, n) => {
		let s = [e.start, e.end].filter((e) => !!e), d = u(e);
		for (let e of Q(t)) if (J(e.a, e.b, r, s, -2) || J(e.a, e.b, i, [], -2) || d.some((t) => l(e, t))) return !1;
		for (let n of a) if (n !== e) {
			for (let e of Q(t)) for (let t of Q(o(n))) if (me(e, t, .5) >= Z) return !1;
		}
		return c(e, t) <= n;
	}, "candidateIsSafe");
	for (let e = 0; e < 8; e++) {
		let e = c(), t, n, r = e, i = Infinity, l = Infinity;
		for (let u of a) {
			let a = o(u), p = ge(a, X), m = s(a);
			for (let o = 0; o <= a.length - 4; o++) for (let h of d(a, o)) {
				let a = ge(h, X), o = s(h);
				if (!(a < p || a === p && o < m - X) || !f(u, h, e)) continue;
				let d = c(u, h);
				d > r || d === r && (a > i || a === i && o >= l) || (t = u, n = h, r = d, i = a, l = o);
			}
		}
		if (!t || !n) return;
		t.points = n;
	}
}
e(Pt, "shortcutRedundantOrthogonalJogs");
function Ft(t, n) {
	let r = [];
	for (let e of n.values()) {
		var i, a, o;
		if (e.isGroup || e.isEdgeLabel) continue;
		let t = (i = e.x) == null ? 0 : i, n = (a = e.y) == null ? 0 : a, s = we(e);
		s && r.push({
			id: String((o = e.id) == null ? "" : o),
			cx: t,
			cy: n,
			rect: s
		});
	}
	if (r.length === 0) return;
	let s = new Map(r.map((e) => [e.id, e])), c = r.map((e) => ({
		id: e.id,
		rect: e.rect
	})), l = [
		"top",
		"bottom",
		"left",
		"right"
	], u = {
		top: Math.min(...r.map((e) => e.rect.top)) - 20,
		bottom: Math.max(...r.map((e) => e.rect.bottom)) + 20,
		left: Math.min(...r.map((e) => e.rect.left)) - 20,
		right: Math.max(...r.map((e) => e.rect.right)) + 20
	}, d = t.filter((e) => !e.isLayoutOnly), f = new Map(d.map((e, t) => [e, t])), p = /* @__PURE__ */ e((e) => {
		let t = e === "left" || e === "top" ? -1 : 1, n = [];
		for (let r = 0; r <= 2; r++) n.push(u[e] + t * 20 * r);
		return n;
	}, "outwardTracksForSide"), m = /* @__PURE__ */ e((e, t = /* @__PURE__ */ new Map()) => {
		var n, r;
		return q((n = (r = t.get(e)) == null ? e.points : r) == null ? [] : n);
	}, "replacementPointsFor"), h = /* @__PURE__ */ e((e, t) => {
		let n = 0;
		for (let r of e) for (let e of t) Fe(r.a, r.b, e.a, e.b, X) && n++;
		return n;
	}, "crossingCountBetweenSegments"), g = /* @__PURE__ */ e((e, t) => h(Q(e), Q(t)), "crossingCountBetweenPaths"), _ = /* @__PURE__ */ e((t = /* @__PURE__ */ new Map()) => {
		let n = 0, r = [], i = /* @__PURE__ */ new Set(), a = [], o = /* @__PURE__ */ e((e) => {
			i.has(e) || (i.add(e), a.push(e));
		}, "addEdge");
		for (let e = 0; e < d.length; e++) {
			let i = d[e], a = m(i, t);
			for (let s = e + 1; s < d.length; s++) {
				let e = d[s], c = g(a, m(e, t));
				c > 0 && (n += c, r.push({
					first: i,
					second: e,
					count: c
				}), o(i), o(e));
			}
		}
		return a.sort((e, t) => {
			var n, r;
			return ((n = f.get(e)) == null ? 0 : n) - ((r = f.get(t)) == null ? 0 : r);
		}), {
			count: n,
			pairs: r,
			edgeSet: i,
			edges: a
		};
	}, "crossingSnapshot"), v = /* @__PURE__ */ e((e, t) => {
		let n = new Set(t.keys());
		if (n.size === 0) return e.count;
		let r = 0;
		for (let t of e.pairs) (n.has(t.first) || n.has(t.second)) && (r += t.count);
		let i = 0;
		for (let e = 0; e < d.length; e++) {
			let r = d[e], a = n.has(r), o = m(r, t);
			for (let r = e + 1; r < d.length; r++) {
				let e = d[r];
				!a && !n.has(e) || (i += g(o, m(e, t)));
			}
		}
		return e.count - r + i;
	}, "crossingCountWithReplacements"), y = /* @__PURE__ */ e((e) => {
		let t = /* @__PURE__ */ new Map();
		for (let i of e.pairs) {
			var n, r;
			let e = (n = t.get(i.first)) == null ? /* @__PURE__ */ new Set() : n;
			e.add(i.second), t.set(i.first, e);
			let a = (r = t.get(i.second)) == null ? /* @__PURE__ */ new Set() : r;
			a.add(i.first), t.set(i.second, a);
		}
		let i = [], a = /* @__PURE__ */ new Set();
		for (let n of e.edges) {
			if (a.has(n)) continue;
			let e = [n], r = [];
			for (a.add(n); e.length > 0;) {
				var o;
				let n = e.pop();
				r.push(n);
				for (let r of (o = t.get(n)) == null ? [] : o) a.has(r) || (a.add(r), e.push(r));
			}
			r.sort((e, t) => {
				var n, r;
				return ((n = f.get(e)) == null ? 0 : n) - ((r = f.get(t)) == null ? 0 : r);
			}), r.length > 1 && i.push(r);
		}
		return i;
	}, "crossingComponents"), b = /* @__PURE__ */ e((e) => [e.start, e.end].filter((e) => !!e), "endpointIdsFor"), x = /* @__PURE__ */ e((e) => {
		let t = [];
		for (let n of y(e)) {
			let e = new Set(n), r = new Set(n.flatMap((e) => b(e))), i = [...n];
			for (let t of d) e.has(t) || b(t).some((e) => r.has(e)) && i.push(t);
			i.sort((e, t) => {
				var n, r;
				return ((n = f.get(e)) == null ? 0 : n) - ((r = f.get(t)) == null ? 0 : r);
			}), t.push(i);
		}
		return t;
	}, "pairSearchGroups"), S = /* @__PURE__ */ e((e, t, n) => v(e, /* @__PURE__ */ new Map([[t, n]])), "crossingCountWithSingleReplacement"), C = /* @__PURE__ */ e((e) => {
		let t = /* @__PURE__ */ new Map();
		for (let i of e.pairs) {
			var n, r;
			t.set(i.first, ((n = t.get(i.first)) == null ? 0 : n) + i.count), t.set(i.second, ((r = t.get(i.second)) == null ? 0 : r) + i.count);
		}
		return t;
	}, "currentCrossingsByEdge"), w = /* @__PURE__ */ e((e) => e.slice(1).reduce((t, n, r) => {
		let i = e[r];
		return t + Math.abs(n.x - i.x) + Math.abs(n.y - i.y);
	}, 0), "pathLength"), T = /* @__PURE__ */ e((e = /* @__PURE__ */ new Map()) => d.reduce((t, n) => t + ge(m(n, e)), 0), "totalBends"), E = /* @__PURE__ */ e((e = /* @__PURE__ */ new Map()) => d.reduce((t, n) => t + w(m(n, e)), 0), "totalLength"), D = /* @__PURE__ */ e((e, t, n = /* @__PURE__ */ new Map()) => {
		let r = Q(t);
		for (let t of d) if (t !== e) {
			for (let e of r) for (let r of Q(m(t, n))) if (me(e, r, .5) >= Z) return !0;
		}
		return !1;
	}, "pathHasSegmentConflict"), O = /* @__PURE__ */ e((e, t) => {
		let n = [e.start, e.end].filter((e) => !!e);
		for (let e of Q(t)) if (J(e.a, e.b, c, n, -2)) return !0;
		return !1;
	}, "pathHitsNode"), k = /* @__PURE__ */ e((e, t) => {
		let n = Be(q(t));
		Q(n).length === n.length - 1 && e.push(n);
	}, "pushOrthogonalCandidate"), A = /* @__PURE__ */ e((e) => e === "left" || e === "right", "sideIsHorizontal"), j = /* @__PURE__ */ e((e, t, n) => {
		switch (t) {
			case "left": return Math.min(e.x, n.x) - 20;
			case "right": return Math.max(e.x, n.x) + 20;
			case "top": return Math.min(e.y, n.y) - 20;
			case "bottom": return Math.max(e.y, n.y) + 20;
		}
	}, "localTrackForSameSide"), M = /* @__PURE__ */ e((e, t, n, r) => {
		let i = n === "left" || n === "top" ? -1 : 1, a = [j(t, n, r), u[n]];
		for (let o of a) for (let a = 0; a <= 2; a++) k(e, De(t, n, r, o + i * 20 * a));
	}, "addSameSideCandidates"), N = /* @__PURE__ */ e((e, t, n, r, i) => {
		for (let a of p(n)) for (let n of p(i)) k(e, [
			t,
			{
				x: a,
				y: t.y
			},
			{
				x: a,
				y: n
			},
			{
				x: r.x,
				y: n
			},
			r
		]);
	}, "addHorizontalToVerticalCandidates"), P = /* @__PURE__ */ e((e, t, n, r, i) => {
		for (let a of p(n)) for (let n of p(i)) k(e, [
			t,
			{
				x: t.x,
				y: a
			},
			{
				x: n,
				y: a
			},
			{
				x: n,
				y: r.y
			},
			r
		]);
	}, "addVerticalToHorizontalCandidates"), F = /* @__PURE__ */ e((e, t, n, r, i) => {
		let a = [...p("top"), ...p("bottom")];
		for (let o of p(n)) for (let n of p(i)) for (let i of a) k(e, [
			t,
			{
				x: o,
				y: t.y
			},
			{
				x: o,
				y: i
			},
			{
				x: n,
				y: i
			},
			{
				x: n,
				y: r.y
			},
			r
		]);
	}, "addHorizontalPairCandidates"), I = /* @__PURE__ */ e((e, t, n, r, i) => {
		let a = [...p("left"), ...p("right")];
		for (let o of p(n)) for (let n of p(i)) for (let i of a) k(e, [
			t,
			{
				x: t.x,
				y: o
			},
			{
				x: i,
				y: o
			},
			{
				x: i,
				y: n
			},
			{
				x: r.x,
				y: n
			},
			r
		]);
	}, "addVerticalPairCandidates"), L = /* @__PURE__ */ e((e) => {
		let t = /* @__PURE__ */ new Set();
		return e.map((e) => q(e)).filter((e) => {
			let n = e.map((e) => `${e.x.toFixed(3)},${e.y.toFixed(3)}`).join("|");
			return t.has(n) || e.length < 2 ? !1 : (t.add(n), !0);
		});
	}, "dedupeCandidatePaths"), ee = /* @__PURE__ */ e((e, t, n, r) => {
		let i = [], a = Ee(e, t, n, r, 20, X);
		a && k(i, a), t === r && M(i, e, t, n);
		let o = A(t), s = A(r);
		return o && !s ? N(i, e, t, n, r) : !o && s ? P(i, e, t, n, r) : o ? F(i, e, t, n, r) : I(i, e, t, n, r), L(i);
	}, "buildCandidatesForSides"), te = /* @__PURE__ */ e((e, t, n, r) => {
		let i = [...p("left"), ...p("right")], a = [...p("top"), ...p("bottom")];
		for (let o of l) {
			let s = Te(r, o), c = o === "top" || o === "bottom" ? p(o) : a;
			for (let r of i) {
				k(e, [
					t,
					n,
					{
						x: r,
						y: n.y
					},
					{
						x: r,
						y: s.y
					},
					s
				]);
				for (let i of c) k(e, [
					t,
					n,
					{
						x: r,
						y: n.y
					},
					{
						x: r,
						y: i
					},
					{
						x: s.x,
						y: i
					},
					s
				]);
			}
		}
	}, "addVerticalDepartureOuterTrackCandidates"), ne = /* @__PURE__ */ e((e, t, n, r) => {
		let i = [...p("left"), ...p("right")], a = [...p("top"), ...p("bottom")];
		for (let o of l) {
			let s = Te(r, o), c = o === "left" || o === "right" ? p(o) : i;
			for (let r of a) {
				k(e, [
					t,
					n,
					{
						x: n.x,
						y: r
					},
					{
						x: s.x,
						y: r
					},
					s
				]);
				for (let i of c) k(e, [
					t,
					n,
					{
						x: n.x,
						y: r
					},
					{
						x: i,
						y: r
					},
					{
						x: i,
						y: s.y
					},
					s
				]);
			}
		}
	}, "addHorizontalDepartureOuterTrackCandidates"), re = /* @__PURE__ */ e((e) => {
		var t;
		let n = e.start, r = e.end, i = r ? s.get(r) : void 0;
		if (!n || !i) return [];
		let a = q((t = e.points) == null ? [] : t);
		if (a.length < 4) return [];
		let o = a[0], c = a[1], l = [];
		return G(o, c, X) ? te(l, o, c, i) : W(o, c, X) && ne(l, o, c, i), l;
	}, "terminalPreservingOuterTrackCandidates"), R = /* @__PURE__ */ e((e) => {
		let t = e.start, n = e.end, r = t ? s.get(t) : void 0, i = n ? s.get(n) : void 0;
		if (!r || !i) return [];
		let a = [];
		for (let e of l) {
			let t = Te(r, e);
			for (let n of l) a.push(...ee(t, e, Te(i, n), n));
		}
		return a.push(...re(e)), a;
	}, "candidatePathsFor"), z = /* @__PURE__ */ e(() => new Map(d.map((e) => [e, Q(m(e))])), "currentSegmentsByEdge"), ie = /* @__PURE__ */ e((e, t, n) => {
		let r = /* @__PURE__ */ new Set();
		for (let a of d) {
			var i;
			if (a === e) continue;
			let o = (i = n.get(a)) == null ? Q(m(a)) : i;
			t.some((e) => o.some((t) => me(e, t, .5) >= Z)) && r.add(a);
		}
		return r;
	}, "sharedTrackConflictsFor"), ae = /* @__PURE__ */ e((e, t, n, r) => {
		let i = /* @__PURE__ */ new Set();
		return R(e).map((e) => Be(q(e))).filter((t) => {
			if (O(e, t)) return !1;
			let n = t.map((e) => `${e.x.toFixed(3)},${e.y.toFixed(3)}`).join("|");
			return i.has(n) || t.length < 2 ? !1 : (i.add(n), !0);
		}).map((i) => {
			var a;
			let o = Q(i), s = 0;
			for (let t of d) {
				var c;
				t !== e && (s += h(o, (c = n.get(t)) == null ? Q(m(t)) : c));
			}
			return {
				candidate: i,
				candidateSegments: o,
				crossings: t.count - ((a = r.get(e)) == null ? 0 : a) + s,
				bends: ge(i, X),
				totalBends: ge(i),
				length: w(i)
			};
		}).filter(({ crossings: e }) => e <= t.count).sort((e, t) => e.crossings - t.crossings || e.bends - t.bends || e.length - t.length).slice(0, 48).map((t) => ({
			path: t.candidate,
			segments: t.candidateSegments,
			sharedTrackConflicts: ie(e, t.candidateSegments, n),
			totalBends: t.totalBends,
			length: t.length
		}));
	}, "pairCandidatesFor"), oe = /* @__PURE__ */ e((e, t, n, r, i, a) => {
		let o = 0;
		for (let n of e.pairs) (n.first === t || n.second === t || n.first === r || n.second === r) && (o += n.count);
		let s = h(n.segments, i.segments);
		for (let e of d) {
			var c;
			if (e === t || e === r) continue;
			let o = (c = a.get(e)) == null ? Q(m(e)) : c;
			s += h(n.segments, o) + h(i.segments, o);
		}
		return e.count - o + s;
	}, "pairCrossingCount"), B = /* @__PURE__ */ e((e, t) => {
		for (let n of e.sharedTrackConflicts) if (n !== t) return !1;
		return !0;
	}, "conflictsOnlyWith"), se = /* @__PURE__ */ e((e, t) => e.segments.some((e) => t.segments.some((t) => me(e, t, .5) >= Z)), "candidatesShareTrack"), ce = /* @__PURE__ */ e((e, t, n, r) => B(t, n.edge) && B(r, e.edge) && !se(t, r), "pairCandidatesAreCompatible"), le = /* @__PURE__ */ e((e, t, n, r, i) => {
		var a, o, s, c;
		let l = oe(e.current, t.edge, n, r.edge, i, e.baseSegments);
		if (!(l >= e.current.count)) return {
			replacements: /* @__PURE__ */ new Map([[t.edge, n.path], [r.edge, i.path]]),
			crossings: l,
			bends: e.currentBends - ((a = e.baseBendsByEdge.get(t.edge)) == null ? 0 : a) - ((o = e.baseBendsByEdge.get(r.edge)) == null ? 0 : o) + n.totalBends + i.totalBends,
			length: e.currentLength - ((s = e.baseLengthByEdge.get(t.edge)) == null ? 0 : s) - ((c = e.baseLengthByEdge.get(r.edge)) == null ? 0 : c) + n.length + i.length
		};
	}, "scorePairReplacement"), ue = /* @__PURE__ */ e((e, t) => e.crossings < t.crossings || e.crossings === t.crossings && (e.bends < t.bends || e.bends === t.bends && e.length < t.length), "pairScoreIsBetter"), V = /* @__PURE__ */ e((e, t, n, r) => {
		let i = r;
		for (let r of t.candidates) for (let a of n.candidates) {
			if (!ce(t, r, n, a)) continue;
			let o = le(e, t, r, n, a);
			o && ue(o, i) && (i = o);
		}
		return i;
	}, "bestScoreForOptionPair"), de = /* @__PURE__ */ e((e) => {
		let t = T(), n = E(), r = z(), i = C(e), a = new Map(d.map((e) => [e, ge(m(e))])), o = new Map(d.map((e) => [e, w(m(e))])), s = /* @__PURE__ */ new Map(), c = x(e);
		for (let t of c) for (let n of t) {
			if (s.has(n)) continue;
			let t = ae(n, e, r, i);
			t.length > 0 && s.set(n, {
				edge: n,
				candidates: t
			});
		}
		let l = {
			replacements: /* @__PURE__ */ new Map(),
			crossings: e.count,
			bends: t,
			length: n
		}, u = {
			current: e,
			currentBends: t,
			currentLength: n,
			baseBendsByEdge: a,
			baseLengthByEdge: o,
			baseSegments: r
		};
		for (let t of c) {
			let n = new Set(t.filter((t) => e.edgeSet.has(t))), r = t.map((e) => s.get(e)).filter((e) => !!e);
			for (let e = 0; e < r.length; e++) {
				let t = r[e];
				for (let i = e + 1; i < r.length; i++) {
					let e = r[i];
					!n.has(t.edge) && !n.has(e.edge) || (l = V(u, t, e, l));
				}
			}
		}
		return l.replacements.size > 0 ? l.replacements : void 0;
	}, "bestPairedReplacement");
	for (let e = 0; e < 4; e++) {
		let e = _(), t = e.count;
		if (t === 0) return;
		let n, r, i = t, a = Infinity;
		for (let o of e.edges) {
			let s = ge(m(o), X);
			for (let c of R(o)) {
				let l = O(o, c), u = !l && D(o, c), d = S(e, o, c), f = ge(c, X);
				l || u || (d < t || d === t && f < s) && (d > i || d === i && f >= a || (n = o, r = c, i = d, a = f));
			}
		}
		if (n && r) {
			n.points = r;
			continue;
		}
		let o = de(e);
		if (!o) return;
		for (let [e, t] of o) e.points = t;
	}
}
e(Ft, "resolveRenderedOrthogonalCrossings");
var It = .001, Lt = 8;
function Rt(t, n) {
	let { nodeInfoById: r, realNodeRects: i } = Oe(n), a = [
		"top",
		"bottom",
		"left",
		"right"
	], o = {
		top: Math.min(...i.map((e) => e.rect.top)) - 20,
		bottom: Math.max(...i.map((e) => e.rect.bottom)) + 20,
		left: Math.min(...i.map((e) => e.rect.left)) - 20,
		right: Math.max(...i.map((e) => e.rect.right)) + 20
	}, s = /* @__PURE__ */ e((e, t, n, r) => {
		let i = [], a = Ee(e, t, n, r, 20, It);
		return a && i.push(a), t === r && i.push(De(e, t, n, o[t])), i;
	}, "buildOrthogonalPathCandidates"), c = /* @__PURE__ */ e((e, t) => {
		for (let n = 0; n < e.length - 1; n++) {
			let r = e[n], a = e[n + 1];
			if (J(r, a, i, t, 1)) return !0;
		}
		return !1;
	}, "pathHitsNode"), l = /* @__PURE__ */ e((e, n, r = !1) => {
		let i = 0, a = he(e, It), o = n.start, s = n.end;
		for (let e of t) {
			if (e === n || e.isLayoutOnly) continue;
			let t = e.start, c = e.end;
			if (!r && o && s && (t === o || t === s || c === o || c === s)) continue;
			let l = e.points;
			if (!(!l || l.length < 2)) for (let e of a) for (let t of he(l, It)) {
				if (Me(e.a, e.b, t.a, t.b, It, It)) {
					i++;
					continue;
				}
				me(e, t, It) >= Lt && i++;
			}
		}
		return i;
	}, "pathConflictCount"), u = /* @__PURE__ */ e((e, t) => {
		let n = Math.abs(e.y - t.rect.top), r = Math.abs(e.y - t.rect.bottom), i = Math.abs(e.x - t.rect.left), a = Math.abs(e.x - t.rect.right), o = "top", s = n;
		return r < s && (o = "bottom", s = r), i < s && (o = "left", s = i), a < s && (o = "right", s = a), o;
	}, "nearestSideOfRect"), d = /* @__PURE__ */ new Map(), f = /* @__PURE__ */ e((e, t, n) => {
		var r;
		let i = (r = d.get(e)) == null ? [] : r;
		i.push({
			side: t,
			edgeId: n
		}), d.set(e, i);
	}, "addFaceClaim");
	for (let e of t) {
		var p, m;
		if (e.isLayoutOnly) continue;
		let t = (p = e.points) == null ? [] : p;
		if (t.length < 1) continue;
		let n = (m = e.id) == null ? "" : m, i = e.start, a = e.end;
		if (i) {
			let e = r.get(i);
			e && f(i, u(t[0], e), n);
		}
		if (a) {
			let e = r.get(a);
			e && f(a, u(t[t.length - 1], e), n);
		}
	}
	let h = /* @__PURE__ */ e((e, t, n) => {
		var r, i;
		return (r = (i = d.get(e)) == null ? void 0 : i.some((e) => e.edgeId !== n && e.side === t)) != null && r;
	}, "faceIsClaimed");
	for (let e of t) {
		var g;
		if (e.isLayoutOnly) continue;
		let t = e.points;
		if (!t || t.length < 2) continue;
		let n = ge(t, It);
		if (n < 4) continue;
		let i = e.start, o = e.end;
		if (!i || !o) continue;
		let p = r.get(i), m = r.get(o);
		if (!p || !m) continue;
		let _ = (g = e.id) == null ? "" : g, v = l(t, e, !0), y = l(t, e), b, x = v, S = n;
		for (let t of a) {
			if (h(i, t, _)) continue;
			let n = Te(p, t);
			for (let r of a) {
				if (h(o, r, _)) continue;
				let a = Te(m, r);
				for (let u of s(n, t, a, r)) {
					if (c(u, [i, o])) continue;
					let t = ge(u, It);
					if (v > 0) {
						let n = l(u, e, !0);
						if (n > x || n === x && t >= S) continue;
						x = n, S = t, b = u;
						continue;
					}
					l(u, e) > y || t < S && (S = t, b = u);
				}
			}
		}
		if (b) {
			e.points = b;
			let t = d.get(i);
			t && d.set(i, t.filter((e) => e.edgeId !== _));
			let n = d.get(o);
			n && d.set(o, n.filter((e) => e.edgeId !== _)), f(i, u(b[0], p), _), f(o, u(b[b.length - 1], m), _);
		}
	}
}
e(Rt, "simplifyDetouredEdges");
var zt = .001, Bt = 10, Vt = 7;
function Ht(e, t) {
	let n = t ? 0 : e.length - 1, r = t ? 1 : -1, i = e[n], a = e[n + r];
	if (!i || !a) return;
	let o = a.x - i.x, s = a.y - i.y;
	if (!(Math.abs(o) + Math.abs(s) < zt)) {
		if (Math.abs(s) <= zt) {
			let e = i.x + Math.sign(o) * Bt;
			return {
				left: Math.min(i.x, e),
				right: Math.max(i.x, e),
				top: i.y - Vt,
				bottom: i.y + Vt
			};
		}
		if (Math.abs(o) <= zt) {
			let e = i.y + Math.sign(s) * Bt;
			return {
				left: i.x - Vt,
				right: i.x + Vt,
				top: Math.min(i.y, e),
				bottom: Math.max(i.y, e)
			};
		}
		return {
			left: Math.min(i.x, a.x),
			right: Math.max(i.x, a.x),
			top: Math.min(i.y, a.y),
			bottom: Math.max(i.y, a.y)
		};
	}
}
e(Ht, "markerClearanceRectFor");
function Ut(e) {
	return {
		left: Math.min(e.left, e.right),
		right: Math.max(e.left, e.right),
		top: Math.min(e.top, e.bottom),
		bottom: Math.max(e.top, e.bottom)
	};
}
e(Ut, "normalizeRect");
function Wt(e, t) {
	let n = q(t);
	return [Ht(n, !0), Ht(n, !1)].some((t) => t && xe(e, Ut(t)));
}
e(Wt, "labelOverlapsOwnMarker");
function Gt(t, n) {
	let r = [];
	for (let e of t) {
		if (e.isLayoutOnly) continue;
		let t = e.points;
		if (!(!t || t.length < 2)) for (let n = 0; n < t.length - 1; n++) r.push({
			edgeId: e.id,
			p1: t[n],
			p2: t[n + 1]
		});
	}
	let i = [], a = [];
	for (let e of n.values()) {
		let t = e.isGroup, n = e.parentId;
		if (t && !n) {
			let t = we(e);
			t && a.push({
				id: e.id,
				rect: t
			});
			continue;
		}
		if (t || e.isEdgeLabel) continue;
		let r = we(e);
		r && i.push({
			nodeId: e.id,
			rect: r
		});
	}
	let o = /* @__PURE__ */ e((e, t) => {
		let n = Se(t, 3);
		for (let { nodeId: t, rect: r } of i) if (t !== e && xe(n, r)) return !0;
		return !1;
	}, "labelOverlapsForeignNode"), s = /* @__PURE__ */ e((e, t) => {
		let n = Se(t, 3);
		for (let t of r) if (t.edgeId !== e && ve(t.p1, t.p2, n)) return !0;
		return !1;
	}, "labelOverlapsForeignEdge"), c = /* @__PURE__ */ e((e, t, n) => o(e, n) || s(t, n), "labelOverlapsAnything"), l = [], u = /* @__PURE__ */ e((e) => {
		for (let { id: t, rect: n } of a) if (be(n, e)) return t;
	}, "findContainingLane"), d = /* @__PURE__ */ e((e, t) => l.some((n) => n.labelId !== e && xe(t, n.rect)), "overlapsPlacedLabel");
	for (let r of t) {
		var f, p, m, h, g, _;
		if (r.isLayoutOnly) continue;
		let t = r.labelNodeId;
		if (!t) continue;
		let i = n.get(t);
		if (!i) continue;
		let v = r.points;
		if (!v || v.length < 2) continue;
		let y = (f = i.width) == null ? 0 : f, b = (p = i.height) == null ? 0 : p;
		if (y <= 0 || b <= 0) continue;
		let x = [];
		for (let e = 0; e < v.length - 1; e++) {
			let t = v[e], n = v[e + 1], r = Math.abs(t.x - n.x), i = Math.abs(t.y - n.y);
			r < zt && i < zt || r >= zt && i >= zt || x.push({
				idx: e,
				length: r + i,
				orientation: r >= zt ? "horizontal" : "vertical",
				midX: (t.x + n.x) / 2,
				midY: (t.y + n.y) / 2
			});
		}
		if (x.length === 0) continue;
		let S = x.length >= 3 ? x.filter((e) => e.idx > 0 && e.idx < x.length - 1) : x, C = S.length > 0 ? S : x, w = y >= b ? "horizontal" : "vertical", T = /* @__PURE__ */ e((e) => [...e].sort((e, t) => {
			let n = e.orientation === w;
			if (n !== (t.orientation === w)) return n ? -1 : 1;
			let r = e.length >= (e.orientation === "horizontal" ? y : b) + 2;
			return r === t.length >= (t.orientation === "horizontal" ? y : b) + 2 ? t.length - e.length : r ? -1 : 1;
		}), "rankSegments"), E = x[0], D = x[x.length - 1], O = [
			.5,
			.25,
			.75,
			.05,
			.95,
			.15,
			.85,
			.1,
			.9
		], k = /* @__PURE__ */ e((e, t) => {
			let n = v[e.idx], r = v[e.idx + 1];
			return {
				midX: n.x + (r.x - n.x) * t,
				midY: n.y + (r.y - n.y) * t
			};
		}, "anchorAtT"), A = /* @__PURE__ */ e((e, t, n) => Math.min(n, Math.max(t, e)), "clamp"), j = /* @__PURE__ */ e((e, t) => e.midX >= t.left - zt && e.midX <= t.right + zt && e.midY >= t.top - zt && e.midY <= t.bottom + zt, "pointInsideRectInclusive"), M = /* @__PURE__ */ e((e) => {
			let t = Ce(e.midX, e.midY, y, b), n = u(t);
			if (n) return {
				laneId: n,
				anchor: e,
				rect: t
			};
			let r = a.find(({ rect: t }) => j(e, t));
			if (!r) return;
			let i = r.rect.left + y / 2 + 1, o = r.rect.right - y / 2 - 1, s = r.rect.top + b / 2 + 1, c = r.rect.bottom - b / 2 - 1;
			if (i > o || s > c) return;
			let l = {
				midX: A(e.midX, i, o),
				midY: A(e.midY, s, c)
			}, d = Ce(l.midX, l.midY, y, b);
			return j(e, d) ? {
				laneId: r.id,
				anchor: l,
				rect: d
			} : void 0;
		}, "placementForAnchor"), N = /* @__PURE__ */ e((e, t, n) => e.orientation === "horizontal" ? Math.abs(t.midX - n.x) : Math.abs(t.midY - n.y), "distanceAlongSegment"), P = /* @__PURE__ */ e((e, t) => {
			let n = (e.orientation === "horizontal" ? y / 2 : b / 2) + 12;
			if (e === E) {
				let r = v[e.idx];
				if (N(e, t, r) + zt < n) return !1;
			}
			if (e === D) {
				let r = v[e.idx + 1];
				if (N(e, t, r) + zt < n) return !1;
			}
			return !0;
		}, "labelClearsTerminalEndpoints"), F = /* @__PURE__ */ e((e) => {
			let n = T(e);
			for (let e of n) for (let n of O) {
				let i = k(e, n);
				if (!P(e, i)) continue;
				let a = M(i);
				if (a && !Wt(a.rect, v) && !d(t, a.rect) && !c(t, r.id, a.rect)) return {
					laneId: a.laneId,
					anchor: a.anchor
				};
			}
		}, "tryPool"), I = /* @__PURE__ */ e((e, n, i = !1) => {
			let a = T(e);
			for (let e of a) {
				let a = {
					midX: e.midX,
					midY: e.midY
				};
				if (n && !P(e, a)) continue;
				let c = M(a);
				if (c && !Wt(c.rect, v) && !d(t, c.rect) && !o(t, c.rect) && (i || !s(r.id, c.rect))) return {
					laneId: c.laneId,
					anchor: c.anchor
				};
			}
		}, "findLaneContainingFallback"), L = (m = (h = (g = (_ = F(C)) == null ? C.length < x.length ? F(x) : void 0 : _) == null ? I(x, !0) : g) == null ? I(x, !1) : h) == null ? I(x, !1, !0) : m;
		if (L) {
			i.x = L.anchor.midX, i.y = L.anchor.midY, i.parentId = L.laneId;
			let e = Ce(L.anchor.midX, L.anchor.midY, y, b), n = l.findIndex((e) => e.labelId === t);
			n >= 0 ? l[n] = {
				labelId: t,
				rect: e
			} : l.push({
				labelId: t,
				rect: e
			});
		}
	}
}
e(Gt, "anchorLabelsToPolyline");
var Kt = 1e-6, qt = 8 / 2, Jt = 3;
function Yt(e, t) {
	return e < t ? `${e}::${t}` : `${t}::${e}`;
}
e(Yt, "pairKey");
function Xt(t, n) {
	let { nodeInfoById: r, realNodeRects: i } = Oe(n), a = /* @__PURE__ */ new Map();
	for (let e of n) {
		let t = e.id;
		if (!e.isGroup && e.isEdgeLabel) {
			var o, s;
			a.set(t, {
				w: (o = e.width) == null ? 0 : o,
				h: (s = e.height) == null ? 0 : s
			});
			continue;
		}
	}
	let c = /* @__PURE__ */ e((n, r, i, o) => {
		let s = Yt(r, i), c = 0, l = /* @__PURE__ */ e((e) => {
			if (!e) return;
			let t = a.get(e);
			if (!t) return;
			let n = o === "x" ? t.w / 2 : t.h / 2;
			n > c && (c = n);
		}, "consider");
		l(n.labelNodeId);
		for (let e of t) {
			if (e === n || e.isLayoutOnly) continue;
			let t = e.start, r = e.end;
			!t || !r || Yt(t, r) === s && l(e.labelNodeId);
		}
		return c > 0 ? c + Jt : 0;
	}, "labelClearanceFor");
	for (let e of t) {
		if (e.isLayoutOnly) continue;
		let n = e.points;
		if (!_e(n, Kt)) continue;
		let a = je(e, r, Kt);
		if (!a) continue;
		let { srcId: o, dstId: s, srcInfo: l, dstInfo: u, collinearX: d, collinearY: f } = a;
		if (d === f) continue;
		let p, m;
		if (d) {
			let e = u.cy > l.cy;
			p = {
				x: l.cx,
				y: e ? l.rect.bottom : l.rect.top
			}, m = {
				x: u.cx,
				y: e ? u.rect.top : u.rect.bottom
			};
		} else {
			let e = u.cx > l.cx;
			p = {
				x: e ? l.rect.right : l.rect.left,
				y: l.cy
			}, m = {
				x: e ? u.rect.left : u.rect.right,
				y: u.cy
			};
		}
		if (J(p, m, i, [o, s], 1)) continue;
		let h = c(e, o, s, d ? "x" : "y"), g = h > qt ? h : qt, _ = [
			0,
			g,
			-g
		];
		for (let n of _) {
			let r = { ...p }, a = { ...m };
			if (d) {
				if (r.x += n, a.x += n, r.x <= l.rect.left || r.x >= l.rect.right || a.x <= u.rect.left || a.x >= u.rect.right) continue;
			} else if (r.y += n, a.y += n, r.y <= l.rect.top || r.y >= l.rect.bottom || a.y <= u.rect.top || a.y >= u.rect.bottom) continue;
			if (!J(r, a, i, [o, s], 1) && !Pe(r, a, t, e, { epsilon: Kt })) {
				e.points = [r, a];
				break;
			}
		}
	}
}
e(Xt, "straightenCollinearSiblingDetours");
function Zt(t, n) {
	let r = .001, { realNodeRects: i, labelNodeRects: a } = ke(n.values()), o = /* @__PURE__ */ e((e, t) => he(t, r).map((n) => ({
		...n,
		edge: e,
		interior: n.index >= 1 && n.index <= t.length - 3
	})), "segmentsFor"), s = /* @__PURE__ */ e(() => {
		let e = [];
		for (let n of t) {
			if (n.isLayoutOnly) continue;
			let t = n.points;
			!t || t.length < 2 || e.push(...o(n, q(t)));
		}
		return e;
	}, "allSegments"), c = /* @__PURE__ */ e((e, t) => e.horizontal && t.horizontal ? K(e.a.x, e.b.x, t.a.x, t.b.x) >= 8 && Math.abs(e.a.y - t.a.y) < 7 : e.vertical && t.vertical ? K(e.a.y, e.b.y, t.a.y, t.b.y) >= 8 && Math.abs(e.a.x - t.a.x) < 7 : !1, "hasCrowdedParallelTrack"), l = /* @__PURE__ */ e((e, n) => {
		let s = e.start, l = e.end, u = o(e, n);
		if (u.length !== n.length - 1) return !1;
		let d = [s, l].filter((e) => !!e), f = e.labelNodeId ? [e.labelNodeId] : [];
		for (let e of u) if (J(e.a, e.b, i, d, -2) || J(e.a, e.b, a, f, -2)) return !1;
		for (let n of t) {
			if (n === e || n.isLayoutOnly) continue;
			let t = n.points;
			if (!(!t || t.length < 2)) {
				for (let e of u) for (let i of o(n, q(t))) if (c(e, i) || Fe(e.a, e.b, i.a, i.b, r)) return !1;
			}
		}
		return !0;
	}, "candidateIsSafe"), u = /* @__PURE__ */ e((e, t) => {
		var n;
		let r = q((n = e.edge.points) == null ? [] : n);
		if (r.length < 4 || e.index >= r.length - 1) return;
		let i = r.map((e) => ({ ...e }));
		if (e.horizontal) i[e.index].y += t, i[e.index + 1].y += t;
		else if (e.vertical) i[e.index].x += t, i[e.index + 1].x += t;
		else return;
		return o(e.edge, i).length === i.length - 1 ? i : void 0;
	}, "shiftedCandidate"), d = /* @__PURE__ */ e((e, t) => {
		var n, r;
		return {
			x: (n = e.x) == null ? (t.left + t.right) / 2 : n,
			y: (r = e.y) == null ? (t.top + t.bottom) / 2 : r
		};
	}, "nodeCenter"), f = /* @__PURE__ */ e((e) => {
		var t;
		let r = e.edge, i = q((t = r.points) == null ? [] : t);
		if (i.length !== 4 || e.index !== 1) return;
		let a = r.start ? n.get(r.start) : void 0, o = r.end ? n.get(r.end) : void 0, s = a ? we(a) : void 0, c = o ? we(o) : void 0, l = i.slice(e.index + 2);
		if (!(!a || !o || !s || !c || l.length === 0)) return {
			sourceCenter: d(a, s),
			targetCenter: d(o, c),
			sourceRect: s,
			tail: l
		};
	}, "sourceDetourContextFor"), p = /* @__PURE__ */ e((e, t, n, i, a, o) => {
		let s = i.y >= n.y, c = s ? a.bottom : a.top, l = c + (s ? 20 : -20);
		if (s && e.b.y <= l + r || !s && e.b.y >= l - r) return;
		let u = e.a.x + t;
		return q([
			{
				x: n.x,
				y: c
			},
			{
				x: n.x,
				y: l
			},
			{
				x: u,
				y: l
			},
			{
				x: u,
				y: e.b.y
			},
			...o
		], r);
	}, "verticalSourceDetour"), m = /* @__PURE__ */ e((e, t, n, i, a, o) => {
		let s = i.x >= n.x, c = s ? a.right : a.left, l = c + (s ? 20 : -20);
		if (s && e.b.x <= l + r || !s && e.b.x >= l - r) return;
		let u = e.a.y + t;
		return q([
			{
				x: c,
				y: n.y
			},
			{
				x: l,
				y: n.y
			},
			{
				x: l,
				y: u
			},
			{
				x: e.b.x,
				y: u
			},
			...o
		], r);
	}, "horizontalSourceDetour"), h = /* @__PURE__ */ e((e, t) => {
		let n = f(e);
		if (n) {
			if (e.vertical) return p(e, t, n.sourceCenter, n.targetCenter, n.sourceRect, n.tail);
			if (e.horizontal) return m(e, t, n.sourceCenter, n.targetCenter, n.sourceRect, n.tail);
		}
	}, "sourceDetourCandidate"), g = [
		-7,
		7,
		-14,
		14,
		-21,
		21
	];
	for (let e = 0; e < 12; e++) {
		let e = s(), t = !1;
		for (let n = 0; n < e.length && !t; n++) for (let r = n + 1; r < e.length && !t; r++) {
			let i = e[n], a = e[r];
			if (i.edge === a.edge || !c(i, a)) continue;
			let o = [i, a].filter((e) => e.interior);
			for (let e of o) {
				for (let n of g) {
					let r = u(e, n);
					if (r && l(e.edge, r)) {
						e.edge.points = r, t = !0;
						break;
					}
					let i = h(e, n);
					if (i && l(e.edge, i)) {
						e.edge.points = i, t = !0;
						break;
					}
				}
				if (t) break;
			}
		}
		if (!t) return;
	}
}
e(Zt, "nudgeSharedInteriorSubpaths");
function Qt(e, t, n, r) {
	let i = t.x - e.x, a = t.y - e.y, o = r.x - n.x, s = r.y - n.y, c = i * s - a * o;
	if (Math.abs(c) < 1e-10) return !1;
	let l = n.x - e.x, u = n.y - e.y, d = (l * s - u * o) / c, f = (l * a - u * i) / c, p = .01;
	return d > p && d < 1 - p && f > p && f < 1 - p;
}
e(Qt, "segmentsIntersect");
function $t(e) {
	var n, r;
	let i = (n = e.nodes) == null ? [] : n, a = (r = e.edges) == null ? [] : r, o = [];
	if (!a.length || !i.length) return o;
	let s = Ae(i), c = [];
	for (let e of a) {
		var l;
		if (e.isLayoutOnly) continue;
		let t = e.points;
		if (!t || t.length < 2) continue;
		let n = e.start, r = e.end, i = e.labelNodeId, a = (l = e.id) == null ? `${n}->${r}` : l;
		for (let e of s) if (!(e.nodeId === n || e.nodeId === r) && !(i && e.nodeId === i)) {
			for (let n = 0; n < t.length - 1; n++) if (ve(t[n], t[n + 1], e, -1)) {
				o.push({
					type: "edge-node-overlap",
					edgeId: a,
					targetId: e.nodeId,
					detail: `segment ${n} passes through node "${e.nodeId}"`
				});
				break;
			}
		}
		for (let e = 0; e < t.length - 1; e++) c.push({
			edgeId: a,
			start: n,
			end: r,
			p1: t[e],
			p2: t[e + 1]
		});
	}
	let u = /* @__PURE__ */ new Set();
	for (let e = 0; e < c.length; e++) for (let t = e + 1; t < c.length; t++) {
		let n = c[e], r = c[t];
		if (n.edgeId !== r.edgeId && !(n.start === r.start || n.start === r.end || n.end === r.start || n.end === r.end) && Qt(n.p1, n.p2, r.p1, r.p2)) {
			let e = n.edgeId < r.edgeId ? `${n.edgeId}|${r.edgeId}` : `${r.edgeId}|${n.edgeId}`;
			u.has(e) || (u.add(e), o.push({
				type: "edge-edge-crossing",
				edgeId: n.edgeId,
				targetId: r.edgeId,
				detail: `edges "${n.edgeId}" and "${r.edgeId}" cross`
			}));
		}
	}
	if (o.length > 0) {
		let e = o.filter((e) => e.type === "edge-node-overlap").length, n = o.filter((e) => e.type === "edge-edge-crossing").length;
		t.warn(`[SWIMLANE_VALIDATE] ${o.length} issue(s) detected: ${e} edge-node overlap(s), ${n} edge crossing(s)`);
		for (let e of o) t.warn(`[SWIMLANE_VALIDATE]   ${e.type}: ${e.detail}`);
	}
	return o;
}
e($t, "validateSwimlanesLayout");
function en(t, n) {
	var r, i;
	let a = (r = t.nodes) == null ? [] : r, o = (i = t.edges) == null ? [] : i, s = a.filter((e) => !e.isGroup);
	if ((n === "LR" || n === "RL") && s.length > 0 && !bt(t, n) || n === "BT" && s.length > 0 && !yt(t)) return;
	for (let e of o) {
		if (e.isLayoutOnly) continue;
		let t = e.points;
		!t || t.length < 2 || (e.points = Be(ze(t)));
	}
	Rt(o, a), Xt(o, a), wt(o, a);
	let c = /* @__PURE__ */ new Map();
	for (let e of a) c.set(String(e.id), e);
	Gt(o, c), Ke(o, c), Tt(o, c), Zt(o, c), Dt(o, c), Ot(o, c), kt(o, c), Mt(o, c);
	let l = /* @__PURE__ */ e(() => {
		Ft(o, c), Nt(o, c), Pt(o, c), Gt(o, c), dt(o, c), kt(o, c), Gt(o, c), dt(o, c);
	}, "finalizeRenderedEdges");
	l(), Zt(o, c), l(), At(o, c), jt(o, c), At(o, c), jt(o, c);
}
e(en, "postProcessSwimlaneLayout");
function tn(e) {
	let t = new Map(e.nodeById), n = /* @__PURE__ */ new Set(), r = [];
	for (let i of e.edges) {
		if (!t.has(i.src) || !t.has(i.dst)) continue;
		let e = `${i.id}:${i.src}->${i.dst}`;
		n.has(e) || (n.add(e), r.push(i));
	}
	return {
		nodes: [...t.keys()],
		edges: r,
		layout: e.layout,
		nodeById: t
	};
}
e(tn, "normalizeGraph");
function nn(e, t) {
	return e.edges.filter((e) => e.dst === t);
}
e(nn, "incoming");
function rn(e) {
	let t = /* @__PURE__ */ new Map();
	for (let n of e.nodes) t.set(n, []);
	for (let n of e.edges) t.get(n.src).push(n.dst);
	return t;
}
e(rn, "buildSuccessorMap");
function an(e) {
	let t = rn(e);
	for (let e of t.values()) e.sort((e, t) => e.localeCompare(t));
	return t;
}
e(an, "buildSortedSuccessorMap");
function on(e) {
	let t = /* @__PURE__ */ new Map();
	for (let n of e.nodes) t.set(n, 0);
	for (let r of e.edges) {
		var n;
		t.set(r.dst, ((n = t.get(r.dst)) == null ? 0 : n) + 1);
	}
	return t;
}
e(on, "buildInDegreeMap");
function sn(e) {
	return [...e.entries()].filter(([, e]) => e === 0).map(([e]) => e).sort((e, t) => e.localeCompare(t));
}
e(sn, "sortedZeroInDegreeNodes");
function cn(e, t = () => !0) {
	let n = /* @__PURE__ */ new Map(), r = /* @__PURE__ */ new Map();
	for (let t of e.nodes) n.set(t, []), r.set(t, []);
	for (let i of e.edges) t(i) && (r.get(i.src).push(i.dst), n.get(i.dst).push(i.src));
	return {
		preds: n,
		succs: r
	};
}
e(cn, "buildPredecessorSuccessorMaps");
function ln(e, t, n, r) {
	let i = 0;
	for (let t of e.nodes) {
		var a, o;
		r != null && r.skipGroups && (a = e.nodeById.get(t)) != null && a.isGroup || (i = Math.max(i, (o = n[t]) == null ? 0 : o));
	}
	let s = Array.from({ length: i + 1 }, () => []);
	for (let i of t) {
		var c, l;
		r != null && r.skipGroups && (c = e.nodeById.get(i)) != null && c.isGroup || s[Math.max(0, (l = n[i]) == null ? 0 : l)].push(i);
	}
	return s;
}
e(ln, "buildLayersFromRanks");
function un(e) {
	let t = on(e), n = sn(t), r = [], i = an(e);
	for (; n.length;) {
		var a;
		let e = n.shift();
		r.push(e);
		for (let r of (a = i.get(e)) == null ? [] : a) {
			var o, s;
			if (t.set(r, ((o = t.get(r)) == null ? 0 : o) - 1), ((s = t.get(r)) == null ? 0 : s) === 0) {
				let e = 0;
				for (; e < n.length && n[e] < r;) e++;
				n.splice(e, 0, r);
			}
		}
	}
	return r.length === e.nodes.length ? r : null;
}
e(un, "topoSortIfAcyclic");
function dn(e) {
	let t = /* @__PURE__ */ new Map(), n = 0;
	for (let r of e) t.set(r, n), n++;
	return t;
}
e(dn, "buildLayerIndex");
function fn(t) {
	let n = Array(t.length), r = /* @__PURE__ */ e((e, i) => {
		if (i - e <= 1) return 0;
		let a = e + i >> 1, o = r(e, a) + r(a, i), s = e, c = a, l = e;
		for (; s < a || c < i;) c >= i || s < a && t[s] <= t[c] ? n[l++] = t[s++] : (n[l++] = t[c++], o += a - s);
		for (let r = e; r < i; r++) t[r] = n[r];
		return o;
	}, "count");
	return r(0, t.length);
}
e(fn, "countInversions");
function pn(t) {
	let n = tn(t), r = /* @__PURE__ */ new Map();
	for (let e of n.nodes) r.set(e, []);
	for (let e of n.edges) r.get(e.src).push(e);
	for (let e of r.values()) e.sort((e, t) => e.dst === t.dst ? e.id.localeCompare(t.id) : e.dst.localeCompare(t.dst));
	let i = /* @__PURE__ */ Object.create(null);
	for (let e of n.nodes) i[e] = 0;
	let a = [], o = /* @__PURE__ */ e((e) => {
		var t;
		i[e] = 1;
		for (let n of (t = r.get(e)) == null ? [] : t) {
			let e = n.dst;
			i[e] === 0 ? o(e) : i[e] === 1 && a.push(n);
		}
		i[e] = 2;
	}, "dfs"), s = [...n.nodes].sort((e, t) => e.localeCompare(t));
	for (let e of s) i[e] === 0 && o(e);
	let c = new Set(a.map((e) => `${e.id}:${e.src}->${e.dst}`)), l = n.edges.map((e) => c.has(`${e.id}:${e.src}->${e.dst}`) ? {
		id: e.id,
		src: e.dst,
		dst: e.src,
		weight: e.weight,
		ref: e.ref
	} : e);
	return {
		acyclic: {
			nodes: [...n.nodes],
			edges: l,
			layout: n.layout,
			nodeById: new Map(n.nodeById)
		},
		reversed: a
	};
}
e(pn, "removeCycles_DFS");
function mn(t) {
	let n = /* @__PURE__ */ new Map(), r = /* @__PURE__ */ e((e) => {
		if (n.has(e)) return n.get(e);
		let i = t.nodeById.get(e);
		if (!i) return n.set(e, null), null;
		let a = i.parentId;
		if (!a) return n.set(e, null), null;
		let o = r(a), s = o == null ? a : o;
		return n.set(e, s), s;
	}, "resolve");
	for (let e of t.nodes) r(e);
	return n;
}
e(mn, "buildTopLaneMap");
function hn(e) {
	let t = mn(e);
	return (e) => {
		var n;
		return (n = t.get(e)) == null ? null : n;
	};
}
e(hn, "createTopLaneResolver");
function gn(e) {
	var t;
	let n = [];
	for (let r of (t = e.layout.nodes) == null ? [] : t) r.isGroup && !r.parentId && n.push(r.id);
	return [...new Set(n)].reverse();
}
e(gn, "buildTopLaneOrder");
function _n(e, t) {
	let n = gn(e);
	if (!t || t.length === 0) return n;
	let r = new Set(n), i = /* @__PURE__ */ new Set(), a = [];
	for (let e of t) !r.has(e) || i.has(e) || (i.add(e), a.push(e));
	for (let e of n) i.has(e) || a.push(e);
	return a;
}
e(_n, "resolveTopLaneOrder");
var vn = { EPSILON: 1e-6 }, yn = {
	GRAVITY_ITERATIONS: 8,
	MAX_CROSSING_OPTIMIZATION_PASSES: 4,
	DEFAULT_COMPACT_SINGLE_INPUT: !0
}, bn = {
	DEFAULT_LAYER_GAP: 100,
	DEFAULT_NODE_GAP: 40
};
function xn(t, n) {
	var r, i;
	let a = tn(t), o = (r = n == null ? void 0 : n.laneOf) == null ? (() => null) : r, s = n == null ? void 0 : n.rankHint, { preds: c } = cn(a);
	for (let e of c.values()) e.sort((e, t) => e.localeCompare(t));
	let l = (i = un(a)) == null ? [...a.nodes].sort((e, t) => e.localeCompare(t)) : i, u = /* @__PURE__ */ new Map();
	for (let [e, t] of l.entries()) u.set(t, e);
	let d = /* @__PURE__ */ new Map(), f = /* @__PURE__ */ new Map();
	for (let e of a.nodes) f.set(e, []);
	for (let e of l) {
		var p;
		let t = ((p = c.get(e)) == null ? [] : p).filter((e) => d.has(e));
		if (t.length > 0) {
			let n = Sn(e, t, {
				laneOf: o,
				rankHint: s,
				topoIndex: u
			});
			d.set(e, n), f.get(n).push(e);
		} else d.has(e) || d.set(e, null);
	}
	for (let e of a.nodes) d.has(e) || d.set(e, null);
	let m = /* @__PURE__ */ new Set();
	for (let e of a.nodes) {
		var h;
		((h = d.get(e)) == null ? null : h) === null && m.add(e);
	}
	let g = [...m].sort((e, t) => {
		var n, r;
		let i = (n = u.get(e)) == null ? 0 : n, a = (r = u.get(t)) == null ? 0 : r;
		return i === a ? e.localeCompare(t) : i - a;
	}), _ = Cn(a), v = /* @__PURE__ */ new Map();
	for (let [e, t] of _.entries()) v.set(e, [...t].sort((e, t) => e.localeCompare(t)));
	let y = wn(v), b = Tn(v), x = /* @__PURE__ */ new Map();
	for (let e of a.nodes) x.set(e, []);
	for (let e of b) for (let t of e.nodes) {
		let n = x.get(t);
		n ? n.push(e.id) : x.set(t, [e.id]);
	}
	let S = [], C = [], w = /* @__PURE__ */ new Set(), T = /* @__PURE__ */ e((e) => {
		var t;
		if (!w.has(e)) {
			w.add(e), S.push(e);
			for (let n of (t = f.get(e)) == null ? [] : t) T(n);
			C.push(e);
		}
	}, "walk");
	for (let e of g) T(e);
	for (let e of l) T(e);
	return {
		parent: d,
		children: f,
		roots: g,
		componentOf: y,
		blocks: b,
		nodeBlocks: x,
		adjacency: v,
		preorder: S,
		postorder: C,
		topologicalOrder: l
	};
}
e(xn, "buildDrivingTree");
function Sn(e, t, n) {
	let r = n.laneOf(e);
	return [...t].sort((e, t) => {
		var i, a, o, s;
		let c = n.laneOf(e), l = n.laneOf(t), u = c != null && c === r;
		if (u !== (l != null && l === r)) return u ? -1 : 1;
		let d = (i = n.rankHint) == null ? void 0 : i[e], f = (a = n.rankHint) == null ? void 0 : a[t];
		if (d != null && f != null && d !== f) return f - d;
		let p = (o = n.topoIndex.get(e)) == null ? 0 : o, m = (s = n.topoIndex.get(t)) == null ? 0 : s;
		return p === m ? e.localeCompare(t) : p - m;
	})[0];
}
e(Sn, "chooseParent");
function Cn(e) {
	let t = /* @__PURE__ */ new Map();
	for (let n of e.nodes) t.set(n, /* @__PURE__ */ new Set());
	for (let n of e.edges) t.get(n.src).add(n.dst), t.get(n.dst).add(n.src);
	return t;
}
e(Cn, "buildAdjacency");
function wn(e) {
	let t = /* @__PURE__ */ new Map(), n = 0;
	for (let i of e.keys()) {
		if (t.has(i)) continue;
		let a = [i];
		for (; a.length > 0;) {
			var r;
			let i = a.pop();
			if (!t.has(i)) {
				t.set(i, n);
				for (let n of (r = e.get(i)) == null ? [] : r) t.has(n) || a.push(n);
			}
		}
		n++;
	}
	return t;
}
e(wn, "assignComponents");
function Tn(t) {
	let n = /* @__PURE__ */ new Map(), r = /* @__PURE__ */ new Map(), i = [], a = [], o = 0, s = /* @__PURE__ */ e((e, c) => {
		var l;
		n.set(e, ++o), r.set(e, o);
		for (let v of (l = t.get(e)) == null ? [] : l) {
			var u, d;
			if (v !== c) {
				if (!n.has(v)) {
					var f, p, m, h;
					i.push([e, v]), s(v, e), r.set(e, Math.min((f = r.get(e)) == null ? o : f, (p = r.get(v)) == null ? o : p)), ((m = r.get(v)) == null ? 0 : m) >= ((h = n.get(e)) == null ? 0 : h) && a.push(En(e, v, i, a.length));
				} else if (((u = n.get(v)) == null ? 0 : u) < ((d = n.get(e)) == null ? 0 : d)) {
					var g, _;
					i.push([e, v]), r.set(e, Math.min((g = r.get(e)) == null ? o : g, (_ = n.get(v)) == null ? o : _));
				}
			}
		}
	}, "visit");
	for (let e of t.keys()) n.has(e) || s(e, null);
	return a;
}
e(Tn, "computeBlocks");
function En(e, t, n, r) {
	let i = [], a = /* @__PURE__ */ new Set();
	for (; n.length > 0;) {
		let r = n.pop();
		if (i.push(r), a.add(r[0]), a.add(r[1]), r[0] === e && r[1] === t || r[0] === t && r[1] === e) break;
	}
	return {
		id: r,
		edges: i,
		nodes: [...a]
	};
}
e(En, "popBlock");
function Dn(t, n, r) {
	let i = [...t.nodes], a = /* @__PURE__ */ new Map();
	for (let [e, t] of i.entries()) a.set(t, e);
	let o = i.length, s = Array(o).fill(-1), c = Array(o).fill(0), l = [], u = /* @__PURE__ */ new Set();
	for (let e of i) {
		var d;
		let t = (d = r.parent.get(e)) == null ? null : d, n = a.get(e);
		n != null && t == null && (s[n] = -1, c[n] = 0, u.has(e) || (u.add(e), l.push(e)));
	}
	for (; l.length > 0;) {
		var f;
		let e = l.shift(), t = a.get(e);
		if (t == null) continue;
		let n = (f = r.children.get(e)) == null ? [] : f;
		for (let e of n) {
			if (u.has(e)) continue;
			let n = a.get(e);
			n != null && (s[n] = t, c[n] = c[t] + 1, u.add(e), l.push(e));
		}
	}
	for (let e of i) {
		if (u.has(e)) continue;
		let t = a.get(e);
		t != null && (s[t] = -1, c[t] = 0, u.add(e));
	}
	let p = Math.max(1, Math.ceil(Math.log2(Math.max(1, o))) + 1), m = Array.from({ length: p }, () => Array(o).fill(-1));
	for (let e = 0; e < o; e++) m[0][e] = s[e];
	for (let e = 1; e < p; e++) for (let t = 0; t < o; t++) {
		let n = m[e - 1][t];
		m[e][t] = n === -1 ? -1 : m[e - 1][n];
	}
	let h = /* @__PURE__ */ e((e, t) => {
		if (e === -1 || t === -1) return -1;
		c[e] < c[t] && ([e, t] = [t, e]);
		let n = c[e] - c[t];
		for (let t = 0; t < p; t++) if (n >> t & 1 && (e = m[t][e], e === -1)) return -1;
		if (e === t) return e;
		for (let n = p - 1; n >= 0; n--) {
			let r = m[n][e], i = m[n][t];
			r === -1 || i === -1 || r !== i && (e = r, t = i);
		}
		return m[0][e];
	}, "lcaIndex"), g = Array.from({ length: o }, () => /* @__PURE__ */ new Map());
	for (let e of t.edges) {
		let t = e.src, r = e.dst, i = n[t], o = n[r];
		if (i == null || o == null || (i > o && ([t, r] = [r, t], [i, o] = [o, i]), i == null || o == null || i === o)) continue;
		let s = a.get(t), c = a.get(r);
		if (s == null || c == null) continue;
		let l = h(s, c);
		if (l === -1) continue;
		let u = g[l];
		for (let e = i; e < o; e++) {
			var _;
			u.set(e, ((_ = u.get(e)) == null ? 0 : _) + 1);
		}
	}
	let v = /* @__PURE__ */ new Map(), y = /* @__PURE__ */ e((e, t) => {
		if (t.size !== 0) for (let [r, i] of t) {
			var n;
			e.set(r, ((n = e.get(r)) == null ? 0 : n) + i);
		}
	}, "mergeInto"), b = /* @__PURE__ */ new Set(), x = /* @__PURE__ */ e((e) => {
		var t;
		let i = a.get(e);
		b.add(e);
		let o = i == null ? void 0 : g[i], s = o ? new Map(o) : /* @__PURE__ */ new Map(), c = (t = r.children.get(e)) == null ? [] : t;
		for (let t of c) {
			let r = x(t), i = n[e];
			if (i != null) {
				var l;
				let a = v.get(e);
				a || (a = /* @__PURE__ */ new Map(), v.set(e, a));
				let o = (l = r.get(i)) == null ? 0 : l, s = n[t];
				s != null && s > i && (o += 1), a.set(t, o);
			}
			y(s, r);
		}
		return s;
	}, "dfs");
	for (let e of r.roots) b.has(e) || x(e);
	for (let e of i) b.has(e) || x(e);
	return v;
}
e(Dn, "computeSubtreeCrossCounts");
function On(t, n, r) {
	let i = /* @__PURE__ */ new Map(), a = /* @__PURE__ */ e((e) => {
		var t, o;
		let s = (t = r[e]) == null ? 0 : t, c = [...(o = n.get(e)) == null ? [] : o];
		c.sort(kn(r));
		for (let e of c) {
			a(e);
			let t = i.get(e);
			t != null && (s = Math.min(s, t));
		}
		i.set(e, s);
	}, "annotate");
	for (let e of t) a(e);
	return i;
}
e(On, "annotateMinimumLayers");
function kn(e) {
	return (t, n) => {
		var r, i;
		let a = (r = e[t]) == null ? 0 : r, o = (i = e[n]) == null ? 0 : i;
		return a === o ? t.localeCompare(n) : a - o;
	};
}
e(kn, "compareByRankThenId");
function An(t, n, r, i) {
	let a = 0;
	for (let e of n) {
		var o;
		let t = (o = r[e]) == null ? 0 : o;
		t > a && (a = t);
	}
	let s = Array.from({ length: a + 1 }, () => []), c = /* @__PURE__ */ new Set(), l = /* @__PURE__ */ e((e) => {
		var t;
		if (c.has(e)) return;
		c.add(e);
		let n = (t = r[e]) == null ? 0 : t;
		s[n] || (s[n] = []), s[n].push(e);
		for (let t of i(e)) l(t);
	}, "emit");
	for (let e of t) l(e);
	for (let e of n) if (!c.has(e)) {
		var u;
		let t = (u = r[e]) == null ? 0 : u;
		s[t] || (s[t] = []), s[t].push(e), c.add(e);
	}
	return s;
}
e(An, "emitNodesInTreeOrder");
function jn(e) {
	let t = [];
	for (let n of e) {
		let e = /* @__PURE__ */ new Set(), r = [];
		for (let t of n) e.has(t) || (e.add(t), r.push(t));
		t.push(r);
	}
	return t;
}
e(jn, "deduplicateLayers");
function Mn(e, t, n, r) {
	return (i) => {
		var a, o;
		let s = (a = e.get(i)) == null ? [] : a;
		if (s.length === 0) return [];
		let c = (o = t[i]) == null ? 0 : o, l = [], u = [], d = n.get(i);
		for (let e of s) {
			var f;
			let t = (f = r.get(e)) == null ? c : f;
			t > c ? l.push({
				child: e,
				min: t
			}) : u.push(e);
		}
		return l.sort((e, t) => e.min === t.min ? e.child.localeCompare(t.child) : e.min - t.min), u.sort((e, t) => {
			var n, i, a, o;
			let s = (n = d == null ? void 0 : d.get(e)) == null ? 0 : n, l = (i = d == null ? void 0 : d.get(t)) == null ? 0 : i;
			if (s !== l) return s - l;
			let u = (a = r.get(e)) == null ? c : a, f = (o = r.get(t)) == null ? c : o;
			return u === f ? e.localeCompare(t) : u - f;
		}), [...l.map((e) => e.child), ...u];
	};
}
e(Mn, "createChildOrderer");
function Nn(e, t, n) {
	let r = xn(e, {
		rankHint: t,
		laneOf: n
	}), { children: i, roots: a } = r;
	for (let t of e.nodes) i.has(t) || i.set(t, []);
	let o = Dn(e, t, r), s = [...a].sort(kn(t)), c = Mn(i, t, o, On(s, i, t)), l = An(s, e.nodes, t, c);
	return l = jn(l), l;
}
e(Nn, "buildMultitreeLayerOrder");
function Pn(e, t, n) {
	let r = new Set(e), i = new Set(t), a = dn(t), o = [];
	for (let e of n) r.has(e.src) && i.has(e.dst) && o.push(a.get(e.dst));
	return fn(o);
}
e(Pn, "countCrossingsBetweenAdjacent");
function Fn(e, t, n) {
	let r = [];
	for (let e of t) {
		let t = n[e.src], i = n[e.dst];
		if (t == null || i == null || t === i) continue;
		let a = e.src, o = e.dst, s = t, c = i;
		t > i && (a = e.dst, o = e.src, s = i, c = t);
		for (let t = s; t < c; t++) r.push({
			id: `${e.id}@${t}`,
			src: a,
			dst: o,
			ref: e.ref
		});
	}
	let i = 0;
	for (let t = 0; t + 1 < e.length; t++) i += Pn(e[t], e[t + 1], r);
	return i;
}
e(Fn, "totalCrossings");
function In(e, t) {
	let n = { ...t }, { preds: r } = cn(e), i = hn(e), a = Fn(Nn(e, n, i), e.edges, n), o = yn.MAX_CROSSING_OPTIMIZATION_PASSES;
	for (let t = 0; t < o; t++) {
		let t = !1, o = [...e.nodes].sort((e, t) => {
			var r, i;
			return ((r = n[t]) == null ? 0 : r) - ((i = n[e]) == null ? 0 : i);
		});
		for (let u of o) {
			var s, c;
			let o = (s = n[u]) == null ? 0 : s;
			if (o === 0) continue;
			let d = 0;
			for (let e of (c = r.get(u)) == null ? [] : c) {
				var l;
				d = Math.max(d, ((l = n[e]) == null ? 0 : l) + 1);
			}
			if (d >= o) continue;
			let f = o;
			n[u] = d;
			let p = Fn(Nn(e, n, i), e.edges, n);
			p < a ? (a = p, t = !0) : n[u] = f;
		}
		if (!t) break;
	}
	return n;
}
e(In, "optimizeRanksByCrossings");
function Ln(e, t) {
	let n = hn(e), r = [...e.nodes].sort((e, n) => {
		var r, i;
		return ((r = t[e]) == null ? 0 : r) - ((i = t[n]) == null ? 0 : i) || e.localeCompare(n);
	});
	for (let o of r) {
		var i;
		let r = n(o);
		if (!r) continue;
		let s = e.edges.filter((e) => e.src === o);
		if (s.length === 0) continue;
		let c = !1, l = 0;
		for (let e of s) {
			let t = n(e.dst);
			t == null || t === r ? c = !0 : l++;
		}
		if (l === 0 || c) continue;
		let u = 0, d = !1;
		for (let t of e.edges) {
			if (t.dst !== o) continue;
			let e = n(t.src);
			e && (e === r ? d = !0 : u++);
		}
		if (u > 0 || !d) continue;
		let f = (i = t[o]) == null ? 0 : i, p = f + l, m = 0;
		for (let n of e.edges) if (n.dst === o) {
			var a;
			m = Math.max(m, ((a = t[n.src]) == null ? 0 : a) + 1);
		}
		let h = Math.max(f, m, p);
		h !== f && (t[o] = h);
	}
}
e(Ln, "adjustCrossLaneSources");
function Rn(e, t) {
	var n, r, i;
	let a = tn(e), o = (n = un(a)) == null ? [...a.nodes].sort() : n, s = (r = t == null ? void 0 : t.compactSingleInput) != null && r, c = hn(a), l = /* @__PURE__ */ Object.create(null);
	for (let e of o) {
		let n = nn(a, e), r = t != null && t.ignoreCrossLaneEdges ? n.filter((t) => {
			let n = c(t.src), r = c(e);
			return !n || !r || n === r;
		}) : n;
		if (r.length === 0) l[e] = 0;
		else if (s && r.length === 1) {
			let t = r[0].src;
			if (c(t) !== c(e)) {
				var u;
				l[e] = (u = l[t]) == null ? 0 : u;
			} else {
				var d;
				l[e] = ((d = l[t]) == null ? 0 : d) + 1;
			}
		} else {
			let t = -Infinity;
			for (let e of r) {
				var f;
				t = Math.max(t, ((f = l[e.src]) == null ? 0 : f) + 1);
			}
			l[e] = t === -Infinity ? 0 : t;
		}
	}
	return (i = t == null ? void 0 : t.optimizeRanksByCrossings) != null && i && (l = In(a, l)), t != null && t.ignoreCrossLaneEdges && Ln(a, l), {
		layers: Nn(a, l, c),
		rankOf: l,
		dummy: /* @__PURE__ */ new Set()
	};
}
e(Rn, "assignLayers_LongestPath");
function zn(t, n) {
	var r;
	let i = tn(t), a = { ...Rn(i, {
		compactSingleInput: n == null ? void 0 : n.compactSingleInput,
		ignoreCrossLaneEdges: n == null ? void 0 : n.ignoreCrossLaneEdges,
		optimizeRanksByCrossings: n == null ? void 0 : n.optimizeRanksByCrossings
	}).rankOf }, o = hn(i), { preds: s, succs: c } = cn(i, (e) => {
		if (n != null && n.ignoreCrossLaneEdges) {
			let t = o(e.src), n = o(e.dst);
			if (t && n && t !== n) return !1;
		}
		return !0;
	}), l = (r = un(i)) == null ? [...i.nodes] : r, u = [...l].reverse(), d = /* @__PURE__ */ e((e, t) => {
		var n, r;
		let i = 0;
		for (let t of (n = s.get(e)) == null ? [] : n) {
			var o;
			i = Math.max(i, ((o = a[t]) == null ? 0 : o) + 1);
		}
		let l = Infinity, u = (r = c.get(e)) == null ? [] : r;
		return u.length > 0 && (l = Math.min(...u.map((e) => {
			var t;
			return ((t = a[e]) == null ? 0 : t) - 1;
		}))), Number.isFinite(l) || (l = Math.max(i, t)), Math.min(Math.max(t, i), l);
	}, "clampFeasible"), f = yn.GRAVITY_ITERATIONS, p = /* @__PURE__ */ e((e) => {
		let t = !1;
		for (let l of e) {
			var n, r, i, o;
			let e = (n = s.get(l)) == null ? [] : n, u = (r = c.get(l)) == null ? [] : r;
			if (e.length === 0 && u.length === 0) continue;
			let f = e.length > 0 ? e.reduce((e, t) => {
				var n;
				return e + ((n = a[t]) == null ? 0 : n) + 1;
			}, 0) / e.length : (i = a[l]) == null ? 0 : i, p = u.length > 0 ? u.reduce((e, t) => {
				var n;
				return e + ((n = a[t]) == null ? 0 : n) - 1;
			}, 0) / u.length : (o = a[l]) == null ? 0 : o, m = Math.round((f + p) / 2), h = d(l, m);
			h !== a[l] && (a[l] = h, t = !0);
		}
		return t;
	}, "relaxOrder");
	for (let e = 0; e < f; e++) {
		let e = p(l), t = p(u);
		if (!e && !t) break;
	}
	for (let e of l) {
		var m, h;
		let t = 0;
		for (let n of (m = s.get(e)) == null ? [] : m) {
			var g;
			t = Math.max(t, ((g = a[n]) == null ? 0 : g) + 1);
		}
		((h = a[e]) == null ? 0 : h) < t && (a[e] = t);
	}
	for (let e of u) {
		var _;
		let t = (_ = c.get(e)) == null ? [] : _;
		if (t.length > 0) {
			var v;
			let n = Math.min(...t.map((e) => {
				var t;
				return ((t = a[e]) == null ? 0 : t) - 1;
			}));
			((v = a[e]) == null ? 0 : v) > n && (a[e] = n);
		}
	}
	return {
		layers: ln(i, l, a),
		rankOf: a,
		dummy: /* @__PURE__ */ new Set()
	};
}
e(zn, "assignLayers_Gravity");
function Bn(e) {
	let t = on(e), n = an(e), r = sn(t), i = [];
	for (; r.length > 0;) {
		let e = [];
		for (let c of r) {
			var a;
			i.push(c);
			for (let r of (a = n.get(c)) == null ? [] : a) {
				var o, s;
				t.set(r, ((o = t.get(r)) == null ? 0 : o) - 1), ((s = t.get(r)) == null ? 0 : s) === 0 && e.push(r);
			}
		}
		r = e.sort((e, t) => e.localeCompare(t));
	}
	return i.length === e.nodes.length ? i : null;
}
e(Bn, "topoSortByGenerationIfAcyclic");
function Vn(t, n) {
	var r, i;
	let a = tn(t), o = (n == null ? void 0 : n.direction) === "LR" ? (r = Bn(a)) == null ? [...a.nodes].sort() : r : (i = un(a)) == null ? [...a.nodes].sort() : i, s = hn(a), c = /* @__PURE__ */ e((e) => {
		var t;
		return (t = s(e)) == null ? e : t;
	}, "laneOf"), l = /* @__PURE__ */ Object.create(null), u = /* @__PURE__ */ new Map(), d = /* @__PURE__ */ e((e, t) => {
		var r;
		return (r = n == null ? void 0 : n.ignoreCrossLaneEdges) == null || r ? +(c(e) === c(t)) : 1;
	}, "edgeWeight");
	for (let e of o) {
		var f;
		let t = a.nodeById.get(e);
		if (t != null && t.isGroup) continue;
		let n = nn(a, e), r = 0;
		if (n.length > 0) for (let t of n) {
			var p;
			let n = t.src, i = (p = l[n]) == null ? 0 : p;
			r = Math.max(r, i + d(n, e));
		}
		let i = c(e), o = (f = u.get(i)) == null ? 0 : f, s = Math.max(r, o);
		l[e] = s, u.set(i, s + 1);
	}
	return {
		layers: ln(a, o, l, { skipGroups: !0 }),
		rankOf: l,
		dummy: /* @__PURE__ */ new Set()
	};
}
e(Vn, "assignLayers_LaneAwareCompact");
function Hn(t, n) {
	let r = tn(n), { rankOf: i } = t, a = t.layers.map((e) => [...e]), o = new Set(t.dummy ? [...t.dummy] : []), s = 0, c = new Map(r.nodeById), l = /* @__PURE__ */ e((e) => {
		let t = `placeholder-${s++}`, n = {
			id: t,
			isGroup: !1,
			isDummy: !0,
			width: 0,
			height: 0
		};
		for (c.set(t, n), o.add(t); a.length <= e;) a.push([]);
		return a[e].push(t), i[t] = e, t;
	}, "addDummyAt"), u = [...r.edges].sort((e, t) => e.id === t.id ? e.src === t.src ? e.dst.localeCompare(t.dst) : e.src.localeCompare(t.src) : e.id.localeCompare(t.id)), d = [];
	for (let e of u) {
		var f, p;
		let t = (f = i[e.src]) == null ? 0 : f, n = (p = i[e.dst]) == null ? 0 : p;
		if (n - t <= 1) {
			d.push(e);
			continue;
		}
		let r = e.src;
		for (let i = t + 1, a = 0; i < n; i++, a++) {
			let t = l(i);
			d.push({
				id: `${e.id}#${a}`,
				src: r,
				dst: t,
				weight: e.weight,
				ref: e.ref
			}), r = t;
		}
		let a = n - t - 2;
		d.push({
			id: `${e.id}#${Math.max(a + 1, 0)}`,
			src: r,
			dst: e.dst,
			weight: e.weight,
			ref: e.ref
		});
	}
	let m = {
		nodes: [...r.nodes, ...[...o].filter((e) => !r.nodes.includes(e))],
		edges: d,
		layout: r.layout,
		nodeById: c
	};
	return {
		layering: {
			layers: a,
			rankOf: i,
			dummy: o
		},
		graphWithDummies: m
	};
}
e(Hn, "makeProperLayering");
function Un(e) {
	let t = e.length;
	if (t === 0) return Infinity;
	let n = [...e].sort((e, t) => e - t);
	return t % 2 == 1 ? n[(t - 1) / 2] : .5 * (n[t / 2 - 1] + n[t / 2]);
}
e(Un, "median");
function Wn(e) {
	return e.length === 0 ? Infinity : e.reduce((e, t) => e + t, 0) / e.length;
}
e(Wn, "barycenter");
function Gn(e, t, n, r) {
	let i = /* @__PURE__ */ new Map();
	for (let t of e) i.set(t, []);
	for (let e of n) r === "down" ? t.has(e.src) && i.has(e.dst) && i.get(e.dst).push(t.get(e.src)) : t.has(e.dst) && i.has(e.src) && i.get(e.src).push(t.get(e.dst));
	return i;
}
e(Gn, "neighborPositionsFor");
function Kn(e, t, n) {
	var r, i;
	let a = (r = n.get(e)) == null ? 0 : r, o = (i = n.get(t)) == null ? 0 : i;
	return a === o ? e.localeCompare(t) : a - o;
}
e(Kn, "currentOrderTieBreak");
function qn(e, t, n) {
	let r = new Set(e), i = new Set(t), a = dn(e), o = dn(t), s = [];
	for (let e of n) r.has(e.src) && i.has(e.dst) && s.push({
		u: a.get(e.src),
		v: o.get(e.dst)
	});
	return s.sort((e, t) => e.u === t.u ? e.v - t.v : e.u - t.u), fn(s.map((e) => e.v));
}
e(qn, "countCrossingsBetweenAdjacent");
function Jn(e, t, n) {
	return [...e].sort((e, r) => {
		var i, a;
		let o = Un((i = t.get(e)) == null ? [] : i), s = Un((a = t.get(r)) == null ? [] : a);
		return o === s ? Kn(e, r, n) : isFinite(o) ? isFinite(s) ? o - s : -1 : 1;
	});
}
e(Jn, "sortByHeuristic");
function Yn(e, t, n, r, i, a) {
	let o = dn(e), s = dn(t), c = Gn(t, o, n, r);
	if (!i || !a || a.length === 0) return Jn(t, c, s);
	let l = /* @__PURE__ */ new Map();
	for (let e of t) {
		var u;
		let t = i(e), n = (u = l.get(t)) == null ? [] : u;
		n.push(e), l.set(t, n);
	}
	let d = [];
	for (let e of a) {
		let t = l.get(e);
		if (!t || t.length === 0) continue;
		let n = Jn(t, c, s);
		d.push(...n);
	}
	let f = l.get(null);
	if (f && f.length > 0) {
		let e = Jn(f, c, s);
		for (let t of e) {
			var p;
			let e = Wn((p = c.get(t)) == null ? [] : p), n = d.length;
			if (isFinite(e)) for (let [t, r] of d.entries()) {
				var m;
				if (e < Wn((m = c.get(r)) == null ? [] : m)) {
					n = t;
					break;
				}
			}
			d.splice(n, 0, t);
		}
	}
	return d;
}
e(Yn, "reorderLayer");
function Xn(t, n, r, i, a) {
	let o = [...n], s = new Set(t), c = new Set(n), l = i ? new Set(i) : null, u = r.filter((e) => s.has(e.src) && c.has(e.dst)), d = l ? r.filter((e) => c.has(e.src) && l.has(e.dst)) : void 0, f = /* @__PURE__ */ e((e) => {
		let n = qn(t, e, u);
		return d && i && (n += qn(e, i, d)), n;
	}, "crossingScore"), p = a ? /* @__PURE__ */ new Map() : null;
	if (a && p) for (let e of n) p.set(e, a(e));
	let m = !0, h = f(o);
	for (; m;) {
		m = !1;
		for (let e = 0; e + 1 < o.length; e++) {
			if (p && p.get(o[e]) !== p.get(o[e + 1])) continue;
			let t = h;
			[o[e], o[e + 1]] = [o[e + 1], o[e]];
			let n = f(o);
			n < t ? (h = n, m = !0) : [o[e], o[e + 1]] = [o[e + 1], o[e]];
		}
	}
	return o;
}
e(Xn, "transposeImprove");
function Zn(e, t, n) {
	let r = e.layers.map((e) => [...e]), i = t.edges, a = hn(t), o = _n(t, n == null ? void 0 : n.laneOrder);
	for (let e = 0; e < 3; e++) {
		for (let e = 1; e < r.length; e++) r[e] = Yn(r[e - 1], r[e], i, "down", a, o), r[e] = Xn(r[e - 1], r[e], i, r[e + 1], a);
		for (let e = r.length - 2; e >= 0; e--) r[e] = Yn(r[e + 1], r[e], i, "up", a, o), r[e] = Xn(r[e + 1], r[e], i, r[e - 1], a);
	}
	return { layers: r };
}
e(Zn, "orderLayers");
function Qn(t, n, r) {
	var i, a, o, s;
	let c = (i = r == null ? void 0 : r.layerGap) == null ? bn.DEFAULT_LAYER_GAP : i, l = (a = r == null ? void 0 : r.nodeGap) == null ? bn.DEFAULT_NODE_GAP : a, u = (o = r == null ? void 0 : r.laneGap) == null ? l * 2 : o, d = (s = r == null ? void 0 : r.direction) == null ? "TB" : s, f = d === "LR" || d === "RL", p = t.layers, m = /* @__PURE__ */ Object.create(null), h = /* @__PURE__ */ Object.create(null), g = /* @__PURE__ */ e((e) => n.nodeById.get(e), "getNode"), _ = /* @__PURE__ */ e((e) => {
		var t, n;
		return (t = (n = g(e)) == null ? void 0 : n.width) == null ? 0 : t;
	}, "getWidth"), v = /* @__PURE__ */ e((e) => {
		var t, n;
		return (t = (n = g(e)) == null ? void 0 : n.height) == null ? 0 : t;
	}, "getHeight"), y = hn(n), b = _n(n, r == null ? void 0 : r.laneOrder), x = p.map((e) => e.reduce((e, t) => Math.max(e, v(t)), 0)), S = [];
	if (f) for (let e = 0; e + 1 < p.length; e++) {
		let t = p[e].reduce((e, t) => Math.max(e, _(t)), 0), n = p[e + 1].reduce((e, t) => Math.max(e, _(t)), 0), r = x[e], i = x[e + 1], a = r / 2 + i / 2, o = (t + n) / 2, s = Math.max(0, o - a - c);
		S.push(s);
	}
	let C = /* @__PURE__ */ new Set();
	for (let e of p) for (let t of e) C.add(y(t));
	let w = C.has(null), T = b.filter((e) => C.has(e)), E = [...w ? [null] : [], ...T], D = /* @__PURE__ */ Object.create(null);
	for (let e of T) D[e] = 0;
	w && (D.null = 0);
	for (let e of p) {
		let t = /* @__PURE__ */ Object.create(null), n = [];
		for (let r of e) {
			let e = y(r);
			e === null ? n.push(r) : (t[e] || (t[e] = [])).push(r);
		}
		for (let [e, n] of Object.entries(t)) {
			var O;
			let t = n.reduce((e, t) => e + _(t), 0) + l * Math.max(0, n.length - 1);
			D[e] = Math.max((O = D[e]) == null ? 0 : O, t);
		}
		if (w && n.length) {
			var k;
			let e = n.reduce((e, t) => e + _(t), 0) + l * Math.max(0, n.length - 1);
			D.null = Math.max((k = D.null) == null ? 0 : k, e);
		}
	}
	let A = /* @__PURE__ */ new Map();
	{
		let e = E.map((e) => {
			var t;
			return (t = e === null ? D.null : D[e]) == null ? 0 : t;
		}), t = -(e.reduce((e, t) => e + t, 0) + u * Math.max(0, E.length - 1)) / 2;
		for (let n = 0; n < E.length; n++) {
			var j;
			let r = E[n], i = (j = e[n]) == null ? 0 : j, a = t + i / 2;
			A.set(r, a), t += i, n < E.length - 1 && (t += u);
		}
	}
	let M = 0;
	for (let [e, t] of p.entries()) {
		var N, P;
		let n = (N = x[e]) == null ? 0 : N, r = /* @__PURE__ */ new Map();
		for (let e of t) {
			var F;
			let t = y(e), n = (F = r.get(t)) == null ? [] : F;
			n.push(e), r.set(t, n);
		}
		for (let e of E) {
			var I;
			let t = (I = r.get(e)) == null ? [] : I;
			if (t.length === 0) continue;
			let i = A.get(e);
			if (t.length === 1) {
				let e = t[0];
				m[e] = i, h[e] = M + n / 2;
			} else {
				let e = t.map((e) => _(e)), r = i - (e.reduce((e, t) => e + t, 0) + l * (t.length - 1)) / 2;
				for (let [i, a] of t.entries()) {
					let t = e[i];
					m[a] = r + t / 2, h[a] = M + n / 2, r += t + l;
				}
			}
		}
		let i = (P = S[e]) == null ? 0 : P;
		M += n + c + i;
	}
	let L = /* @__PURE__ */ new Map();
	for (let e of n.edges) {
		let t = e.ref.id;
		L.has(t) || L.set(t, []), L.get(t).push(e);
	}
	for (let [, e] of L) {
		var ee, te;
		if (e.length === 0) continue;
		let t = e[0].ref, r = t.start, i = t.end;
		if (r == null || i == null) continue;
		let a = Math.round((((ee = m[r]) == null ? 0 : ee) + ((te = m[i]) == null ? 0 : te)) / 2), o = /* @__PURE__ */ new Set();
		for (let t of e) o.add(t.src), o.add(t.dst);
		for (let e of o) {
			if (e === r || e === i) continue;
			let t = n.nodeById.get(e);
			t != null && t.isDummy && (m[e] = a);
		}
	}
	return {
		x: m,
		y: h
	};
}
e(Qn, "assignCoordinates");
var $n = 8;
function er(e) {
	let t = 2166136261;
	for (let n = 0; n < e.length; n++) t ^= e.charCodeAt(n), t = Math.imul(t, 16777619);
	return t >>> 0;
}
e(er, "hashString");
function tr(e) {
	let t = e >>> 0;
	return () => {
		t += 1831565813;
		let e = t;
		return e = Math.imul(e ^ e >>> 15, e | 1), e ^= e + Math.imul(e ^ e >>> 7, e | 61), ((e ^ e >>> 14) >>> 0) / 4294967296;
	};
}
e(tr, "mulberry32");
function nr(e, t) {
	let n = [...e], r = tr(t);
	for (let e = n.length - 1; e > 0; e--) {
		let t = Math.floor(r() * (e + 1));
		[n[e], n[t]] = [n[t], n[e]];
	}
	return n;
}
e(nr, "deterministicShuffle");
function rr(e, t) {
	let n = 0;
	for (let [i, a] of e.entries()) {
		var r;
		n += Math.abs(i - ((r = t.get(a)) == null ? i : r));
	}
	return n;
}
e(rr, "sourceDistance");
function ir(e, t) {
	let n = /* @__PURE__ */ new Map();
	for (let [t, r] of e.entries()) n.set(r, t);
	let r = 0;
	for (let { a: e, b: i, weight: a } of t) {
		let t = n.get(e), o = n.get(i);
		t == null || o == null || (r += a * Math.abs(t - o));
	}
	return r;
}
e(ir, "laneArrangementCost");
function ar(e) {
	var t;
	let n = gn(e);
	if (n.length < 2) return [];
	let r = new Map(n.map((e, t) => [e, t])), i = hn(e), a = /* @__PURE__ */ new Map();
	for (let n of (t = e.layout.edges) == null ? [] : t) {
		if (n.isLayoutOnly) continue;
		let t = typeof n.start == "string" ? n.start : void 0, o = typeof n.end == "string" ? n.end : void 0;
		if (!t || !o || !e.nodeById.has(t) || !e.nodeById.has(o)) continue;
		let s = i(t), c = i(o);
		if (!s || !c || s === c) continue;
		let l = r.get(s), u = r.get(c);
		if (l == null || u == null) continue;
		let [d, f] = l <= u ? [s, c] : [c, s], p = `${d}\0${f}`, m = a.get(p);
		m ? m.weight++ : a.set(p, {
			a: d,
			b: f,
			weight: 1
		});
	}
	return [...a.values()];
}
e(ar, "buildWeightedLaneEdges");
function or(e, t, n) {
	let r = [...e], i = ir(r, t), a = !0, o = 0, s = Math.max(1, r.length);
	for (; a && o < s;) {
		a = !1, o++;
		for (let e = 0; e + 1 < r.length; e++) {
			[r[e], r[e + 1]] = [r[e + 1], r[e]];
			let n = ir(r, t);
			n < i ? (i = n, a = !0) : [r[e], r[e + 1]] = [r[e + 1], r[e]];
		}
	}
	return {
		order: r,
		cost: i,
		sourceDistance: rr(r, n)
	};
}
e(or, "greedySwitch");
function sr(e, t) {
	return e.cost === t.cost ? e.sourceDistance < t.sourceDistance : e.cost < t.cost;
}
e(sr, "isBetterCandidate");
function cr(e, t, n) {
	let r = [...t].sort((e, t) => e.a === t.a ? e.b.localeCompare(t.b) : e.a.localeCompare(t.a)).map(({ a: e, b: t, weight: n }) => `${e}:${t}:${n}`).join("|");
	return er(`${e.join("|")}#${r}#${n}`);
}
e(cr, "seedForRestart");
function lr(e, t = {}) {
	var n;
	let r = gn(e);
	if (r.length < 2) return r;
	let i = ar(e);
	if (i.length === 0) return r;
	let a = new Map(r.map((e, t) => [e, t])), o = or(r, i, a), s = Math.max(0, (n = t.restarts) == null ? $n : n);
	for (let e = 0; e < s; e++) {
		let t = or(nr(r, cr(r, i, e)), i, a);
		sr(t, o) && (o = t);
	}
	return o.order;
}
e(lr, "optimizeTopLaneOrder");
function ur(e, t) {
	var n, r, i, a;
	let o = (n = t == null ? void 0 : t.ignoreCrossLaneEdges) == null || n, s = (r = t == null ? void 0 : t.optimizeRanksByCrossings) == null || r, c = tn(e), l = t != null && t.automaticLaneOrdering ? lr(c, { restarts: $n }) : void 0, u = pn(c), d = u.acyclic, { layering: f, graphWithDummies: p } = Hn(o ? Vn(d, {
		compactSingleInput: (i = t == null ? void 0 : t.compactSingleInput) == null ? yn.DEFAULT_COMPACT_SINGLE_INPUT : i,
		ignoreCrossLaneEdges: !0,
		direction: t == null ? void 0 : t.direction
	}) : zn(d, {
		compactSingleInput: (a = t == null ? void 0 : t.compactSingleInput) == null ? yn.DEFAULT_COMPACT_SINGLE_INPUT : a,
		ignoreCrossLaneEdges: !1,
		optimizeRanksByCrossings: s
	}), d), m = Zn(f, p, { laneOrder: l }), h = Qn(m, p, {
		layerGap: t == null ? void 0 : t.layerGap,
		nodeGap: t == null ? void 0 : t.nodeGap,
		direction: t == null ? void 0 : t.direction,
		laneOrder: l
	});
	return {
		acyclic: d,
		reversed: u.reversed,
		layering: f,
		ordered: m,
		coordinates: h
	};
}
e(ur, "sugiyamaLayout");
var $ = vn.EPSILON, dr = 8, fr = 15, pr = 15, mr = 25, hr = 20, gr = 10;
function _r(e, t, n) {
	var r, i;
	let a = (r = e.x) == null ? 0 : r, o = (i = e.y) == null ? 0 : i, s = t.x - a, c = t.y - o, l = Math.abs(s), u = Math.abs(c);
	return l < $ && u < $ ? n : u > $ && u * 3 >= l ? c > 0 ? "bottom" : "top" : l > $ ? s > 0 ? "right" : "left" : n;
}
e(_r, "chooseOrthogonalSide");
function vr(e, t) {
	return Math.abs(e.to - t.from) < $ || Math.abs(e.to - t.to) < $ ? e.to : e.from;
}
e(vr, "sharedLineEndpointCoord");
function yr(e, t) {
	return e.orient === "vertical" ? {
		x: e.coord,
		y: t
	} : {
		x: t,
		y: e.coord
	};
}
e(yr, "pointOnLine");
function br(t, n) {
	var r, i, a;
	let o = (r = t.nodes) == null ? [] : r, s = (i = t.edges) == null ? [] : i, c = [];
	for (let e of s) e.isLayoutOnly || c.push({
		...e,
		__originalEdge: e
	});
	let l = /* @__PURE__ */ new Map(), u = /* @__PURE__ */ new Map(), d = [], f = n === "LR";
	for (let e of o) l.set(e.id, e);
	let p = o.filter((e) => e.isGroup && !e.parentId);
	for (let t of p) {
		let n = { id: t.id }, r = /* @__PURE__ */ e((e) => {
			u.set(e.id, n), o.filter((t) => t.parentId === e.id).forEach(r);
		}, "assignLane");
		r(t);
	}
	let m = o.filter((e) => !e.isGroup && !e.isEdgeLabel).map((e) => {
		var t, n, r, i;
		let a = (t = e.width) == null ? 10 : t, o = (n = e.height) == null ? 10 : n, s = (r = e.x) == null ? 0 : r, c = (i = e.y) == null ? 0 : i, l = dr;
		return {
			nodeId: e.id,
			minX: s - a / 2 - l,
			maxX: s + a / 2 + l,
			minY: c - o / 2 - l,
			maxY: c + o / 2 + l,
			visualXHalfExtent: f ? o / 2 + l : a / 2 + l
		};
	}), h = /* @__PURE__ */ e((e, t, n, r) => {
		let i = d.find((n) => n.orientation === e && Math.abs(n.coord - t) < 1);
		return i || (i = {
			id: `pipe-${e}-${t.toFixed(0)}`,
			orientation: e,
			coord: t,
			spanMin: n,
			spanMax: r,
			tracks: []
		}, d.push(i)), i.spanMin = Math.min(i.spanMin, n), i.spanMax = Math.max(i.spanMax, r), i;
	}, "getOrAddPipe"), g = /* @__PURE__ */ e((e, t) => {
		var n, r, i, a;
		let o = (n = e.width) == null ? 10 : n, s = (r = e.height) == null ? 10 : r, c = (i = e.x) == null ? 0 : i, l = (a = e.y) == null ? 0 : a;
		switch (t) {
			case "top": return {
				x: c,
				y: l - s / 2
			};
			case "bottom": return {
				x: c,
				y: l + s / 2
			};
			case "left": return {
				x: c - o / 2,
				y: l
			};
			case "right": return {
				x: c + o / 2,
				y: l
			};
		}
	}, "portForSide"), _ = /* @__PURE__ */ e((e, t, n) => g(e, _r(e, t, n ? "bottom" : "top")), "getOrthogonalPort"), v = [], y = [], b = /* @__PURE__ */ new Set(), x = 1e3, S = /* @__PURE__ */ e((e, t, n) => {
		if (v.length === 0) return 0;
		let r = Math.abs(t.y - n.y) < $, i = Math.abs(t.x - n.x) < $;
		if (!r && !i) return 0;
		let a = 0;
		if (r) {
			let r = t.y, i = Math.min(t.x, n.x) - $, o = Math.max(t.x, n.x) + $;
			if (o <= i) return 0;
			for (let t of v) t.edgeIndex === e || t.orientation !== "vertical" || t.pipe.coord < i || t.pipe.coord > o || t.from - $ <= r && t.to + $ >= r && (a += x);
		} else if (i) {
			let r = t.x, i = Math.min(t.y, n.y) - $, o = Math.max(t.y, n.y) + $;
			if (o <= i) return 0;
			for (let t of v) t.edgeIndex === e || t.orientation !== "horizontal" || t.pipe.coord < i || t.pipe.coord > o || t.from - $ <= r && t.to + $ >= r && (a += x);
		}
		return a;
	}, "crossingPenalty"), C = c.map((e, t) => {
		var n, r, i, a;
		if (!e.start || !e.end) return {
			idx: t,
			crossLane: 0,
			dx: 0,
			dy: 0
		};
		let o = l.get(e.start), s = l.get(e.end), c = u.get(e.start), d = u.get(e.end);
		return {
			idx: t,
			crossLane: c && d && c.id !== d.id ? 1 : 0,
			dx: o && s ? Math.abs(((n = s.x) == null ? 0 : n) - ((r = o.x) == null ? 0 : r)) : 0,
			dy: o && s ? Math.abs(((i = s.y) == null ? 0 : i) - ((a = o.y) == null ? 0 : a)) : 0
		};
	}).sort((e, t) => {
		if (e.crossLane !== t.crossLane) return t.crossLane - e.crossLane;
		let n = e.dx + e.dy, r = t.dx + t.dy;
		return Math.abs(n - r) > 1 ? n - r : e.idx - t.idx;
	}).map((e) => e.idx), w = /* @__PURE__ */ e((e, t, n, r) => {
		let i = Math.min(e.x, t.x), a = Math.max(e.x, t.x), o = Math.min(e.y, t.y), s = Math.max(e.y, t.y);
		return !!m.find((c) => n && c.nodeId === n || r && c.nodeId === r ? !1 : Math.abs(e.x - t.x) > $ ? c.minY < e.y && c.maxY > e.y && c.maxX > i && c.minX < a : c.minX < e.x && c.maxX > e.x && c.maxY > o && c.minY < s);
	}, "isSegmentBlocked"), T = /* @__PURE__ */ new Map(), E = /* @__PURE__ */ new Map();
	for (let e of c) {
		var D, O;
		!e.start || !e.end || e.start === e.end || (E.set(e.start, ((D = E.get(e.start)) == null ? 0 : D) + 1), E.set(e.end, ((O = E.get(e.end)) == null ? 0 : O) + 1));
	}
	let k = /* @__PURE__ */ e((e, t) => _r(e, t, "bottom"), "determineSide"), A = /* @__PURE__ */ new Map();
	for (let [e, t] of c.entries()) {
		var j, M, N, P, F, I, L, ee;
		if (!t.start || !t.end || t.start === t.end || t.points && t.points.length > 0) continue;
		let n = l.get(t.start), r = l.get(t.end);
		if (!n || !r) continue;
		let i = ((j = r.x) == null ? 0 : j) - ((M = n.x) == null ? 0 : M), a = ((N = r.y) == null ? 0 : N) - ((P = n.y) == null ? 0 : P);
		A.set(e, {
			edgeIdx: e,
			srcId: t.start,
			dstId: t.end,
			srcSide: k(n, {
				x: (F = r.x) == null ? 0 : F,
				y: (I = r.y) == null ? 0 : I
			}),
			dstSide: k(r, {
				x: (L = n.x) == null ? 0 : L,
				y: (ee = n.y) == null ? 0 : ee
			}),
			absDx: Math.abs(i),
			absDy: Math.abs(a),
			dxSign: Math.sign(i),
			dySign: Math.sign(a)
		});
	}
	let te = /* @__PURE__ */ e((e) => e.srcSide === "top" || e.srcSide === "bottom" ? e.absDx === 0 ? Infinity : e.absDy / e.absDx : e.absDy === 0 ? Infinity : e.absDx / e.absDy, "preferenceStrength"), ne = /* @__PURE__ */ e((e) => e.srcSide === "top" || e.srcSide === "bottom" ? e.dxSign >= 0 ? "right" : "left" : e.dySign >= 0 ? "bottom" : "top", "secondarySide"), re = /* @__PURE__ */ new Map();
	for (let e of A.values()) {
		let t = `${e.srcId}:${e.srcSide}`;
		re.has(t) || re.set(t, []), re.get(t).push(e);
	}
	let R = /* @__PURE__ */ new Map(), z = /* @__PURE__ */ e((e, t) => `${e}:${t}`, "loadKey");
	for (let e of A.values()) {
		var ie, ae;
		R.set(z(e.srcId, e.srcSide), ((ie = R.get(z(e.srcId, e.srcSide))) == null ? 0 : ie) + 1), R.set(z(e.dstId, e.dstSide), ((ae = R.get(z(e.dstId, e.dstSide))) == null ? 0 : ae) + 1);
	}
	for (let e of re.values()) if (!(e.length < 2)) {
		e.sort((e, t) => {
			let n = te(e), r = te(t);
			return Math.abs(n - r) > 1e-9 ? r - n : e.edgeIdx - t.edgeIdx;
		});
		for (let t = 1; t < e.length; t++) {
			var oe, B;
			let n = e[t], r = ne(n), i = (oe = R.get(z(n.srcId, n.srcSide))) == null ? 0 : oe, a = (B = R.get(z(n.srcId, r))) == null ? 0 : B;
			a >= i || (R.set(z(n.srcId, n.srcSide), i - 1), R.set(z(n.srcId, r), a + 1), n.srcSide = r);
		}
	}
	let se = /* @__PURE__ */ e((e) => {
		let t = e == null ? void 0 : e.shape;
		return t === "question" || t === "diamond";
	}, "isDiamondNode"), ce = /* @__PURE__ */ new Map();
	for (let e of A.values()) ce.has(e.dstId) || ce.set(e.dstId, /* @__PURE__ */ new Set()), ce.get(e.dstId).add(e.dstSide);
	for (let e of A.values()) {
		var le, ue;
		if (!se(l.get(e.srcId))) continue;
		let t = ce.get(e.srcId);
		if (!(t != null && t.has(e.srcSide))) continue;
		let n = ne(e);
		if (t.has(n) || ((le = R.get(z(e.srcId, n))) == null ? 0 : le) > 0) continue;
		let r = (ue = R.get(z(e.srcId, e.srcSide))) == null ? 0 : ue;
		R.set(z(e.srcId, e.srcSide), Math.max(0, r - 1)), R.set(z(e.srcId, n), 1), e.srcSide = n;
	}
	for (let e of A.values()) {
		var V, de, fe, pe;
		let { edgeIdx: t, srcId: n, dstId: r, srcSide: i, dstSide: a } = e, o = l.get(n), s = l.get(r), c = `${n}:${i}:src`, u = i === "top" || i === "bottom" ? (V = s.x) == null ? 0 : V : (de = s.y) == null ? 0 : de;
		T.has(c) || T.set(c, []), T.get(c).push({
			edgeIdx: t,
			oppositeCoord: u
		});
		let d = `${r}:${a}:dst`, f = a === "top" || a === "bottom" ? (fe = o.x) == null ? 0 : fe : (pe = o.y) == null ? 0 : pe;
		T.has(d) || T.set(d, []), T.get(d).push({
			edgeIdx: t,
			oppositeCoord: f
		});
	}
	let H = /* @__PURE__ */ new Map();
	for (let [e, t] of T) {
		var U, W;
		if (t.length < 2) continue;
		t.sort((e, t) => e.oppositeCoord - t.oppositeCoord);
		let n = e.split(":"), r = n.slice(0, -2).join(":"), i = n[n.length - 2], a = n[n.length - 1], o = l.get(r);
		if (!o) continue;
		let s = i === "left" || i === "right" ? (U = o.height) == null ? 10 : U : (W = o.width) == null ? 10 : W, c = o.shape, u = c === "question" || c === "diamond" ? s * .3 : s, d = Math.min(20, Math.max(8, u / (t.length + 1))), f = -(d * (t.length - 1)) / 2;
		for (let [e, n] of t.entries()) {
			let t = f + e * d, r = `${n.edgeIdx}:${a}`;
			H.set(r, t);
		}
	}
	let G = /* @__PURE__ */ e((e) => {
		var t;
		return !!((t = c[e]) != null && t.labelNodeId);
	}, "edgeHasLabelNode"), K = /* @__PURE__ */ e((e, t) => {
		var n, r;
		return e ? ((n = T.get(`${e}:${t}:src`)) == null ? [] : n).some(({ edgeIdx: e }) => G(e)) || ((r = T.get(`${e}:${t}:dst`)) == null ? [] : r).some(({ edgeIdx: e }) => G(e)) : !1;
	}, "faceHasLabelNode"), me = /* @__PURE__ */ e((e, t, n) => t === "top" || t === "bottom" ? {
		x: e.x + n,
		y: e.y
	} : {
		x: e.x,
		y: e.y + n
	}, "applyPortOffset"), he = /* @__PURE__ */ e((e, t, n) => {
		var r, i, a, o, s, c;
		let l = A.get(e), u = {
			x: (r = n.x) == null ? 0 : r,
			y: (i = n.y) == null ? 0 : i
		}, d = {
			x: (a = t.x) == null ? 0 : a,
			y: (o = t.y) == null ? 0 : o
		}, f = (s = l == null ? void 0 : l.srcSide) == null ? k(t, u) : s, p = (c = l == null ? void 0 : l.dstSide) == null ? k(n, d) : c, m = l ? g(t, l.srcSide) : _(t, u, !0), h = l ? g(n, l.dstSide) : _(n, d, !1), v = H.get(`${e}:src`), y = H.get(`${e}:dst`);
		return v !== void 0 && (m = me(m, f, v)), y !== void 0 && (h = me(h, p, y)), {
			pSrcPort: m,
			pDstPort: h,
			srcSide: f,
			dstSide: p
		};
	}, "portsForEdge");
	for (let t of C) {
		let n = c[t];
		if (y[t] = [], !n.start || !n.end || n.points && n.points.length > 0 || n.start === n.end) continue;
		let r = l.get(n.start), i = l.get(n.end);
		if (!r || !i) continue;
		let { pSrcPort: a, pDstPort: o, srcSide: s, dstSide: u } = he(t, r, i), p = { ...a }, g = { ...o }, _ = s === "top" || s === "bottom", x = u === "top" || u === "bottom";
		if (_) {
			var ge;
			p.y = a.y > ((ge = r.y) == null ? 0 : ge) ? a.y + hr : a.y - hr;
		} else {
			var q;
			p.x = a.x > ((q = r.x) == null ? 0 : q) ? a.x + hr : a.x - hr;
		}
		if (x) {
			var _e;
			g.y = o.y > ((_e = i.y) == null ? 0 : _e) ? o.y + hr : o.y - hr;
		} else {
			var ve;
			g.x = o.x > ((ve = i.x) == null ? 0 : ve) ? o.x + hr : o.x - hr;
		}
		let C = /* @__PURE__ */ e((e, t) => {
			for (let n of m) if (!t.includes(n.nodeId) && e.x > n.minX && e.x < n.maxX && e.y > n.minY && e.y < n.maxY) return {
				inside: !0,
				obstacle: n
			};
			return { inside: !1 };
		}, "isPointInObstacle"), D = /* @__PURE__ */ e((e, t, n, r, i) => {
			var a, o;
			if (i) {
				var s, c;
				let i = e.y > ((s = t.y) == null ? 0 : s);
				return {
					x: ((c = n.x) == null ? 0 : c) >= e.x ? r.maxX + fr : r.minX - fr,
					y: i ? r.maxY + pr : r.minY - pr,
					leavesPositiveSide: i
				};
			}
			let l = e.x > ((a = t.x) == null ? 0 : a), u = ((o = n.y) == null ? 0 : o) >= e.y;
			return {
				x: l ? r.maxX + fr : r.minX - fr,
				y: u ? r.maxY + pr : r.minY - pr,
				leavesPositiveSide: l
			};
		}, "obstacleDetour"), O = [], k = [n.start, n.end], A = C(p, k);
		if (A.inside && A.obstacle) {
			let e = A.obstacle;
			if (_) {
				let t = D(a, r, i, e, !0);
				p.x = t.x, p.y = t.y;
				let n = t.leavesPositiveSide ? Math.min(e.minY - 2, a.y + hr) : Math.max(e.maxY + 2, a.y - hr);
				O = [
					{
						x: a.x,
						y: n
					},
					{
						x: t.x,
						y: n
					},
					{
						x: t.x,
						y: t.y
					}
				];
			} else {
				let t = D(a, r, i, e, !1), n = t.leavesPositiveSide ? Math.min(e.minX - 2, a.x + hr) : Math.max(e.maxX + 2, a.x - hr);
				p.x = t.x, p.y = t.y, O = [
					{
						x: n,
						y: a.y
					},
					{
						x: n,
						y: t.y
					},
					{
						x: t.x,
						y: t.y
					}
				];
			}
		}
		let j = [], M = C(g, k);
		if (M.inside && M.obstacle) {
			let e = M.obstacle;
			if (x) {
				let t = D(o, i, r, e, !0);
				g.x = t.x, g.y = t.y, j = [{
					x: t.x,
					y: t.y
				}, {
					x: o.x,
					y: t.y
				}];
			} else {
				let t = D(o, i, r, e, !1);
				g.x = t.x, g.y = t.y, j = [{
					x: t.x,
					y: t.y
				}, {
					x: t.x,
					y: o.y
				}];
			}
		}
		if (O.length === 0 && j.length === 0) {
			var ye, be, xe, Se, Ce, we, Te, Ee, De, Oe, ke, Ae, je, J, Me, Ne;
			let e = fr, r = Math.abs(p.x - g.x) < e, i = Math.abs(p.y - g.y) < e, c = H.get(`${t}:src`) !== void 0 || H.get(`${t}:dst`) !== void 0, l = ((ye = (be = T.get(`${(xe = n.start) == null ? "" : xe}:${s}:src`)) == null ? void 0 : be.length) == null ? 0 : ye) + ((Se = (Ce = T.get(`${(we = n.start) == null ? "" : we}:${s}:dst`)) == null ? void 0 : Ce.length) == null ? 0 : Se), d = ((Te = (Ee = T.get(`${(De = n.end) == null ? "" : De}:${u}:src`)) == null ? void 0 : Ee.length) == null ? 0 : Te) + ((Oe = (ke = T.get(`${(Ae = n.end) == null ? "" : Ae}:${u}:dst`)) == null ? void 0 : ke.length) == null ? 0 : Oe), f = l > 1 || d > 1, m = (je = E.get((J = n.start) == null ? "" : J)) == null ? 0 : je, h = (Me = E.get((Ne = n.end) == null ? "" : Ne)) == null ? 0 : Me, _ = l > 1 && K(n.start, s) || d > 1 && K(n.end, u);
			if ((r || i) && !c && (!f || f && !_ && (l <= 1 || m <= 2) && (d <= 1 || h <= 2)) && !w(a, o, n.start, n.end)) {
				n.points = [
					{ ...a },
					{ ...p },
					{ ...g },
					{ ...o }
				], b.add(t);
				let e = i ? "horizontal" : "vertical", r = i ? a.y : a.x, s = i ? Math.min(a.x, o.x) : Math.min(a.y, o.y), c = i ? Math.max(a.x, o.x) : Math.max(a.y, o.y), l = {
					id: `fast-path-${e}-${r.toFixed(0)}-${t}`,
					orientation: e,
					coord: r,
					spanMin: s,
					spanMax: c,
					tracks: []
				};
				v.push({
					edgeIndex: t,
					segmentIndex: 0,
					orientation: e,
					pipe: l,
					trackIndex: 0,
					from: s,
					to: c
				});
				continue;
			}
		}
		p.x = h("vertical", p.x, p.y, p.y).coord, g.x = h("vertical", g.x, g.y, g.y).coord;
		let N = Math.min(p.x, g.x) - 50, P = Math.max(p.x, g.x) + 50, F = Math.min(p.y, g.y) - 50, I = Math.max(p.y, g.y) + 50;
		for (let e of m) {
			let t = Math.min(p.x, g.x), n = Math.max(p.x, g.x), r = Math.min(p.y, g.y), i = Math.max(p.y, g.y);
			e.minX < n && e.maxX > t && e.minY < i && e.maxY > r && (N = Math.min(N, e.minX - mr), P = Math.max(P, e.maxX + mr), F = Math.min(F, e.minY - mr), I = Math.max(I, e.maxY + mr));
		}
		for (let e of m) {
			if (e.maxX < N || e.minX > P || e.maxY < F || e.minY > I) continue;
			let t = fr;
			h("horizontal", e.minY - t, N, P), h("horizontal", e.maxY + t, N, P);
			let n = pr;
			h("vertical", e.minX - n, F, I), h("vertical", e.maxX + n, F, I);
		}
		h("horizontal", p.y, N, P), h("horizontal", g.y, N, P);
		let L = d.filter((e) => e.orientation === "horizontal" && e.coord >= F && e.coord <= I), ee = d.filter((e) => e.orientation === "vertical" && e.coord >= N && e.coord <= P), te = /* @__PURE__ */ e((e, t) => `${e.toFixed(1)},${t.toFixed(1)}`, "getKey"), ne = te(p.x, p.y), re = te(g.x, g.y), R = /* @__PURE__ */ new Map(), z = /* @__PURE__ */ new Map(), ie = /* @__PURE__ */ new Map(), ae = /* @__PURE__ */ new Set(), oe = [];
		R.set(ne, 0), ie.set(ne, "n"), oe.push({
			key: ne,
			f: Math.hypot(g.x - p.x, g.y - p.y),
			pt: p
		}), ae.add(ne);
		let B = [], se = /* @__PURE__ */ e((e, t) => w(e, t, n.start, n.end), "checkSegmentBlocked"), ce = {
			x: g.x,
			y: p.y
		}, le = se(p, ce), ue = se(ce, g), V = le || ue, de = {
			x: p.x,
			y: g.y
		}, fe = se(p, de), pe = se(de, g);
		if (V ? fe || pe || (B = Math.abs(p.x - g.x) < $ ? [p, g] : [
			p,
			de,
			g
		]) : B = Math.abs(p.y - g.y) < $ || Math.abs(p.x - g.x) < $ ? [p, g] : [
			p,
			ce,
			g
		], B.length === 0) for (; oe.length > 0;) {
			oe.sort((e, t) => e.f - t.f);
			let e = oe.shift();
			if (ae.delete(e.key), e.key === re) {
				let e = re, t = g;
				for (B = [t]; z.has(e);) {
					let n = z.get(e);
					B.unshift(n), t = n, e = te(n.x, n.y);
				}
				break;
			}
			let r = e.pt.x, i = e.pt.y, a = ee.sort((e, t) => e.coord - t.coord), o = a.findIndex((e) => Math.abs(e.coord - r) < 1), s = L.sort((e, t) => e.coord - t.coord), c = s.findIndex((e) => Math.abs(e.coord - i) < 1), l = [];
			o > 0 && l.push({
				x: a[o - 1].coord,
				y: i
			}), o >= 0 && o < a.length - 1 && l.push({
				x: a[o + 1].coord,
				y: i
			}), c > 0 && l.push({
				x: r,
				y: s[c - 1].coord
			}), c >= 0 && c < s.length - 1 && l.push({
				x: r,
				y: s[c + 1].coord
			});
			for (let a of l) {
				var Pe, Fe, Ie;
				let o = Math.min(r, a.x), s = Math.max(r, a.x), c = Math.min(i, a.y), l = Math.max(i, a.y);
				if (m.some((e) => e.nodeId === n.start || e.nodeId === n.end ? !1 : o === s ? e.minX < r && e.maxX > r && e.maxY > c && e.minY < l : e.minY < i && e.maxY > i && e.maxX > o && e.minX < s)) continue;
				let u = te(a.x, a.y), d = Math.abs(a.x - r) + Math.abs(a.y - i), f = S(t, e.pt, a), h = 0, _ = g.x - p.x, v = g.y - p.y, y = a.x - r, b = a.y - i;
				(v > 10 && b < -5 || v < -10 && b > 5) && (h = Math.abs(b) * 100), (_ > 10 && y < -5 || _ < -10 && y > 5) && (h += Math.abs(y) * 50);
				let x = 0, C = (Pe = ie.get(e.key)) == null ? "n" : Pe, w = Math.abs(y) > $ ? "h" : "v";
				C !== "n" && C !== w && (x = 50);
				let T = d + f + h + x, E = ((Fe = R.get(e.key)) == null ? Infinity : Fe) + T, D = Math.abs(g.x - a.x) + Math.abs(g.y - a.y);
				if (E < ((Ie = R.get(u)) == null ? Infinity : Ie)) if (z.set(u, e.pt), R.set(u, E), ie.set(u, w), !ae.has(u)) oe.push({
					key: u,
					f: E + D,
					pt: a
				}), ae.add(u);
				else {
					let e = oe.findIndex((e) => e.key === u);
					e !== -1 && (oe[e].f = E + D);
				}
			}
		}
		if (B.length === 0 && (B = [
			p,
			{
				x: p.x,
				y: g.y
			},
			g
		]), B.length > 4) {
			let t = B[0], n = B[B.length - 1], r = Math.min(t.x, n.x), i = Math.max(t.x, n.x), a = Math.min(t.y, n.y), o = Math.max(t.y, n.y);
			for (let e of B) r = Math.min(r, e.x), i = Math.max(i, e.x), a = Math.min(a, e.y), o = Math.max(o, e.y);
			let s = i > Math.max(t.x, n.x), c = r < Math.min(t.x, n.x);
			if (f) {
				let e = pr;
				if (s) {
					let r = Math.max(t.x, n.x), a = Math.min(t.y, n.y), o = Math.max(t.y, n.y), s = m.filter((e) => e.minX < r && e.maxX > r && e.minY < o && e.maxY > a);
					if (s.length > 0) {
						let r = Math.max(t.x, n.x);
						for (let t of s) {
							let n = (t.minX + t.maxX) / 2;
							if (t.visualXHalfExtent === void 0 || isNaN(t.visualXHalfExtent)) continue;
							let i = n + t.visualXHalfExtent + e;
							r = Math.max(r, i);
						}
						isNaN(r) || (i = r);
					}
				}
				if (c) {
					let i = m.filter((r) => r.minX < Math.min(t.x, n.x) + e && r.minY < Math.max(t.y, n.y) && r.maxY > Math.min(t.y, n.y));
					if (i.length > 0) {
						let a = Math.min(t.x, n.x);
						for (let t of i) {
							let n = (t.minX + t.maxX) / 2 - t.visualXHalfExtent - e;
							a = Math.min(a, n);
						}
						r = a;
					}
				}
			}
			let l = /* @__PURE__ */ e((e) => {
				let r = n.y > t.y, i = m.filter((e) => {
					let r = Math.min(t.x, n.x) < e.maxX && Math.max(t.x, n.x) > e.minX, i = Math.min(t.y, n.y) < e.maxY && Math.max(t.y, n.y) > e.minY;
					return r && i;
				}), a = i;
				if (f && i.length > 0) {
					let t = i.filter((t) => t.minX < e && t.maxX > e);
					t.length > 0 && (a = t);
				}
				if (a.length === 0) return n.y;
				let o = fr;
				if (r) {
					let e = Math.max(...a.map((e) => e.maxY)) + o;
					if (e < n.y - $) return e;
				} else {
					let e = Math.min(...a.map((e) => e.minY)) - o;
					if (e > n.y + $) return e;
				}
				return n.y;
			}, "findBestReturnY"), u = /* @__PURE__ */ e((e) => {
				let r = l(e), i = {
					x: e,
					y: t.y
				}, a = {
					x: e,
					y: r
				}, o = {
					x: n.x,
					y: r
				}, s = se(t, i), c = se(i, a), u = se(a, o), d = r !== n.y && se(o, n);
				return !s && !c && !u && !d ? Math.abs(r - n.y) < $ ? [
					t,
					i,
					a,
					n
				] : [
					t,
					i,
					a,
					o,
					n
				] : null;
			}, "trySimplifyWithDetourX"), d = s && !c ? u(i) : c && !s ? u(r) : null;
			d && (B = d);
		}
		let U = [
			a,
			...O,
			...B,
			...j.reverse(),
			o
		];
		if (U.length >= 3) {
			let e = U[U.length - 1], t = U[U.length - 2], n = U[U.length - 3], r = Math.abs(n.y - t.y) < $ && Math.abs(t.y - e.y) < $, i = Math.abs(n.x - t.x) < $ && Math.abs(t.x - e.x) < $;
			if (r) {
				let r = Math.sign(t.x - n.x), i = Math.sign(e.x - n.x);
				r !== 0 && r === i && Math.abs(t.x - n.x) > Math.abs(e.x - n.x) && U.splice(-2, 1);
			} else if (i) {
				let r = Math.sign(t.y - n.y), i = Math.sign(e.y - n.y);
				r !== 0 && r === i && Math.abs(t.y - n.y) > Math.abs(e.y - n.y) && U.splice(-2, 1);
			}
		}
		let W = [U[0]];
		for (let e = 1; e < U.length - 1; e++) {
			if (e === 1) {
				W.push(U[e]);
				continue;
			}
			let t = W[W.length - 1], n = U[e], r = U[e + 1];
			if (Math.abs(t.y - n.y) < $ && Math.abs(n.y - r.y) < $) {
				if (n.x > t.x != r.x > n.x) {
					W.push(n);
					continue;
				}
				continue;
			}
			if (Math.abs(t.x - n.x) < $ && Math.abs(n.x - r.x) < $) {
				if (n.y > t.y != r.y > n.y) {
					W.push(n);
					continue;
				}
				continue;
			}
			W.push(n);
		}
		W.push(U[U.length - 1]);
		for (let e = 0; e < W.length - 1; e++) {
			let n = W[e], r = W[e + 1], i = Math.abs(n.x - r.x) < $ ? "vertical" : "horizontal", a = i === "vertical" ? n.x : n.y, o = i === "vertical" ? Math.min(n.y, r.y) : Math.min(n.x, r.x), s = i === "vertical" ? Math.max(n.y, r.y) : Math.max(n.x, r.x), c = h(i, a, o, s), l = {
				edgeIndex: t,
				segmentIndex: e,
				orientation: i,
				pipe: c,
				trackIndex: 0,
				from: o,
				to: s
			};
			v.push(l), y[t].push(v.length - 1), c.tracks[0] || (c.tracks[0] = {
				index: 0,
				coord: c.coord,
				segments: []
			}), c.tracks[0].segments.push({
				edgeIndex: t,
				segmentIndex: e,
				from: o,
				to: s
			});
		}
	}
	let Le = /* @__PURE__ */ e((e, t) => e.from < t.to && t.from < e.to, "segmentsOverlap"), Re = /* @__PURE__ */ e((e, t, n, r) => {
		let i = !r.segments.some((n) => (n.edgeIndex !== t.edgeIndex || n.segmentIndex !== t.segmentIndex) && Le(n, e)), a = !n.segments.some((n) => (n.edgeIndex !== e.edgeIndex || n.segmentIndex !== e.segmentIndex) && Le(n, t));
		return i && a ? (e.trackIndex = r.index, t.trackIndex = n.index, n.segments = [...n.segments.filter((t) => t.edgeIndex !== e.edgeIndex || t.segmentIndex !== e.segmentIndex), {
			edgeIndex: t.edgeIndex,
			segmentIndex: t.segmentIndex,
			from: t.from,
			to: t.to
		}], r.segments = [...r.segments.filter((e) => e.edgeIndex !== t.edgeIndex || e.segmentIndex !== t.segmentIndex), {
			edgeIndex: e.edgeIndex,
			segmentIndex: e.segmentIndex,
			from: e.from,
			to: e.to
		}], !0) : !1;
	}, "trySwapSegmentsAcrossTracks"), ze = /* @__PURE__ */ e((e) => {
		let t = e.tracks.length;
		return e.tracks[t] = {
			index: t,
			coord: e.coord,
			segments: []
		}, t;
	}, "createNewTrack"), Be = /* @__PURE__ */ e((e, t) => {
		let n = e.pipe.tracks[e.trackIndex];
		n.segments = n.segments.filter((t) => t.edgeIndex !== e.edgeIndex || t.segmentIndex !== e.segmentIndex), e.trackIndex = t, e.pipe.tracks[t].segments.push({
			edgeIndex: e.edgeIndex,
			segmentIndex: e.segmentIndex,
			from: e.from,
			to: e.to
		});
	}, "moveSegmentToTrack"), Y = /* @__PURE__ */ e((e, t) => {
		let n = y[e.edgeIndex];
		for (let r of n) {
			let n = v[r];
			n.pipe === e.pipe && Be(n, t);
		}
	}, "moveSegmentChainToTrack"), Ve = /* @__PURE__ */ e((e) => {
		let t = y[e.edgeIndex], n = t.indexOf(v.indexOf(e)), r = [];
		return n > 0 && r.push(v[t[n - 1]]), n < t.length - 1 && r.push(v[t[n + 1]]), r;
	}, "getAdjacentSegmentsAlongEdge"), He = /* @__PURE__ */ e((e, t) => {
		if (e.orientation === t.orientation) return !1;
		let n = e.orientation === "horizontal" ? e : t, r = e.orientation === "horizontal" ? t : e;
		return r.pipe.coord > n.from && r.pipe.coord < n.to && n.pipe.coord > r.from && n.pipe.coord < r.to;
	}, "haveAnyCrossing"), Ue = /* @__PURE__ */ e((e, t) => {
		for (let n of e.tracks) if (!n.segments.some((e) => (e.edgeIndex !== t.edgeIndex || e.segmentIndex !== t.segmentIndex) && Le(e, t))) return n.index;
		return -1;
	}, "findAvailableTrack"), We = /* @__PURE__ */ e((e, t) => {
		if (e.trackIndex === t.trackIndex) return Le(e, t);
		let n = Ve(e), r = Ve(t);
		return n.some((e) => r.some((t) => He(e, t)));
	}, "segmentsConflict"), Ge = /* @__PURE__ */ e((e, t, n) => {
		if (Re(e, t, e.pipe.tracks[e.trackIndex], t.pipe.tracks[t.trackIndex])) return;
		let r = Ue(e.pipe, t);
		n(t, r === -1 ? ze(e.pipe) : r);
	}, "resolveTrackConflict"), Ke = /* @__PURE__ */ e((e) => {
		let t = 0;
		for (let n = 0; n < e.length; n++) for (let r = n + 1; r < e.length; r++) {
			let i = e[n], a = e[r];
			i.pipe === a.pipe && We(i, a) && (t++, Ge(i, a, Y));
		}
		return t;
	}, "resolveHandleConflicts"), qe = /* @__PURE__ */ new Map(), Je = /* @__PURE__ */ e((e) => {
		if (qe.has(e)) return qe.get(e);
		let t = y[e];
		if (t.length === 0) {
			let t = {
				dest: 0,
				deviation: 0,
				base: 0,
				delta: 0
			};
			return qe.set(e, t), t;
		}
		let n = v[t[0]].pipe.coord, r = n;
		for (let e = 1; e < t.length; e++) {
			let i = v[t[e]];
			if (i.orientation === "horizontal") {
				let e = i.from, t = i.to;
				r = Math.abs(e - n) > Math.abs(t - n) ? e : t;
				break;
			}
		}
		let i = Math.abs(r - n), a = {
			dest: r,
			deviation: i,
			base: n,
			delta: r - n
		};
		return qe.set(e, a), a;
	}, "getDestInfo"), Ye = /* @__PURE__ */ e(() => {
		let t = 0, n = /* @__PURE__ */ new Map();
		for (let [e, t] of c.entries()) y[e].length !== 0 && t.start && (n.has(t.start) || n.set(t.start, []), n.get(t.start).push(e));
		let r = /* @__PURE__ */ e((e) => {
			var t, n, r, i;
			let a = c[e];
			if (!a.start || !a.end) return 0;
			let o = l.get(a.start), s = l.get(a.end);
			if (!o || !s) return 0;
			let u = ((t = s.x) == null ? 0 : t) - ((n = o.x) == null ? 0 : n), d = ((r = s.y) == null ? 0 : r) - ((i = o.y) == null ? 0 : i);
			return Math.abs(u) + Math.abs(d);
		}, "getEdgeDistance");
		for (let e of n.values()) {
			e.sort((e, t) => {
				let n = Je(e), i = Je(t);
				if (Math.abs(n.deviation - i.deviation) > 1) return n.deviation - i.deviation;
				if (Math.abs(n.dest - i.dest) > 1) return n.dest - i.dest;
				let a = r(e), o = r(t);
				if (Math.abs(a - o) > 1) return o - a;
				let s = y[e].length, c = y[t].length;
				if (s !== c) return s - c;
				if (s === 1) {
					let n = y[e][0], r = y[t][0];
					if (v[n] && v[r]) {
						let e = v[n], t = v[r], i = Math.abs(e.to - e.from), a = Math.abs(t.to - t.from);
						if (Math.abs(i - a) > 1) return i - a;
					}
				}
				return 0;
			});
			let n = e.map((e) => v[y[e][0]]);
			t += Ke(n);
		}
		return t;
	}, "fixSourceHandleCrossings"), Xe = /* @__PURE__ */ e(() => {
		let t = 0, n = /* @__PURE__ */ new Map();
		for (let [e, t] of c.entries()) y[e].length !== 0 && t.end && (n.has(t.end) || n.set(t.end, []), n.get(t.end).push(e));
		for (let r of n.values()) {
			r.sort((t, n) => {
				let r = /* @__PURE__ */ e((e) => {
					let t = y[e];
					if (t.length < 2) return 0;
					let n = v[t[t.length - 2]];
					return Math.abs(n.to - n.from);
				}, "getDist"), i = r(t), a = r(n);
				return Math.abs(i - a) > .1 ? i - a : t - n;
			});
			let n = r.map((e) => v[y[e][y[e].length - 1]]);
			t += Ke(n);
		}
		return t;
	}, "fixTargetHandleCrossings"), Ze = /* @__PURE__ */ e(() => {
		let e = 0;
		for (let t of d) {
			let n = [];
			for (let e of t.tracks) for (let t of e.segments) {
				let e = y[t.edgeIndex].find((e) => v[e].segmentIndex === t.segmentIndex);
				e !== void 0 && n.push(v[e]);
			}
			n.sort((e, t) => e.edgeIndex - t.edgeIndex || e.segmentIndex - t.segmentIndex);
			for (let t = 0; t < n.length; t++) for (let r = t + 1; r < n.length; r++) {
				let i = n[t], a = n[r];
				We(i, a) && (e++, Ge(i, a, Be));
			}
		}
		return e;
	}, "fixPipeCrossings"), Qe = 0;
	for (; Qe < 10;) {
		let e = 0;
		if (e += Ye(), e += Xe(), e += Ze(), e === 0) break;
		Qe++;
	}
	let $e = /* @__PURE__ */ new Map();
	for (let t of d) {
		let n = [];
		t.tracks.forEach((e) => {
			e.segments.forEach((t) => {
				n.push({
					edgeIndex: t.edgeIndex,
					segmentIndex: t.segmentIndex,
					trackIndex: e.index,
					from: t.from,
					to: t.to
				});
			});
		}), n.sort((e, t) => e.from - t.from);
		let r = [];
		if (n.length > 0) {
			let e = [n[0]], t = n[0].to;
			for (let i = 1; i < n.length; i++) {
				let a = n[i];
				a.from < t ? (e.push(a), t = Math.max(t, a.to)) : (r.push(e), e = [a], t = a.to);
			}
			r.push(e);
		}
		for (let n of r) {
			let r = /* @__PURE__ */ new Set();
			n.forEach((e) => r.add(e.trackIndex));
			let i = /* @__PURE__ */ new Map();
			n.forEach((e) => {
				var t;
				let n = Je(e.edgeIndex);
				i.set(e.trackIndex, ((t = i.get(e.trackIndex)) == null ? 0 : t) + n.delta);
			});
			let a = [...r].filter((e) => {
				var t;
				return ((t = i.get(e)) == null ? 0 : t) < -1;
			}), o = [...r].filter((e) => {
				var t;
				return ((t = i.get(e)) == null ? 0 : t) > 1;
			}), s = [...r].filter((e) => {
				var t;
				return Math.abs((t = i.get(e)) == null ? 0 : t) <= 1;
			});
			a.sort((e, t) => {
				var n, r;
				return ((n = i.get(t)) == null ? 0 : n) - ((r = i.get(e)) == null ? 0 : r);
			}), o.sort((e, t) => {
				var n, r;
				return ((n = i.get(e)) == null ? 0 : n) - ((r = i.get(t)) == null ? 0 : r);
			});
			let c = /* @__PURE__ */ e((e, r) => {
				n.filter((t) => t.trackIndex === e).forEach((e) => {
					let n = b.has(e.edgeIndex) ? t.coord : r;
					$e.set(`${e.edgeIndex}-${e.segmentIndex}`, n);
				});
			}, "assignCoord"), l = 0;
			for (let e of a) l++, c(e, t.coord - l * gr);
			if (s.length === 0 && r.size > 0) {
				let e = [...r].sort((e, t) => {
					var n, r;
					return Math.abs((n = i.get(e)) == null ? 0 : n) - Math.abs((r = i.get(t)) == null ? 0 : r);
				})[0], t = a.indexOf(e);
				t !== -1 && a.splice(t, 1);
				let n = o.indexOf(e);
				n !== -1 && o.splice(n, 1), s.push(e);
			}
			let u = 0;
			for (let e of s) {
				if (u === 0) c(e, t.coord);
				else {
					let n = u % 2 == 1 ? 1 : -1, r = Math.ceil(u / 2);
					c(e, t.coord + n * r * gr * .5);
				}
				u++;
			}
			let d = 0;
			for (let e of o) d++, c(e, t.coord + d * gr);
		}
	}
	for (let [e, t] of c.entries()) {
		var et;
		let n = (et = y[e]) == null ? [] : et;
		if (n.length === 0) continue;
		let r = [], { pSrcPort: i, pDstPort: a } = he(e, l.get(t.start), l.get(t.end)), o = n.map((e) => {
			var t;
			let n = v[e], r = (t = $e.get(`${n.edgeIndex}-${n.segmentIndex}`)) == null ? n.pipe.coord : t;
			return {
				orient: n.orientation,
				coord: r,
				from: n.from,
				to: n.to
			};
		});
		r.push(i);
		for (let e = 0; e < o.length; e++) {
			let t = o[e], n = r[r.length - 1], i = t.orient === "vertical" ? n.y : n.x, a = t.orient === "vertical" ? n.x : n.y, s = o[e + 1], c = e < o.length - 1;
			if (Math.abs(a - t.coord) > $ && r.push(yr(t, i)), c && s.orient === t.orient) if (Math.abs(t.coord - s.coord) > $) {
				let e = t.orient === "vertical" ? (i + s.from) / 2 : vr(t, s);
				r.push(yr(t, e), yr(s, e));
			} else (e === 0 || e === o.length - 2) && r.push(yr(t, vr(t, s)));
			else if (c) r.push(yr(t, s.coord));
			else {
				let e = Math.abs(t.from - i) < Math.abs(t.to - i) ? t.to : t.from;
				r.push(yr(t, e));
			}
		}
		let s = r[r.length - 1];
		(Math.abs(s.x - a.x) > $ || Math.abs(s.y - a.y) > $) && r.push(a);
		let c = [];
		r.length > 0 && c.push(r[0]);
		for (let e = 1; e < r.length; e++) {
			let t = r[e], n = c[c.length - 1];
			(Math.abs(t.x - n.x) > $ || Math.abs(t.y - n.y) > $) && c.push(t);
		}
		t.points = c;
	}
	for (let e of c) {
		let t = e.__originalEdge;
		t && e.points && (t.points = e.points);
	}
	t.edges = ((a = t.edges) == null ? [] : a).filter((e) => !e.isLayoutOnly);
	let tt = /* @__PURE__ */ e((e, t) => {
		var n, r, i, a;
		let o = (n = t.x) == null ? 0 : n, s = (r = t.y) == null ? 0 : r, c = (i = t.width) == null ? 0 : i, l = (a = t.height) == null ? 0 : a;
		if (c <= 0 || l <= 0) return e;
		let u = o - c / 2, d = o + c / 2, f = s - l / 2, p = s + l / 2;
		if (e.x < u || e.x > d || e.y < f || e.y > p) return e;
		let m = e.x - u, h = d - e.x, g = e.y - f, _ = p - e.y, v = Math.min(m, h, g, _);
		return v === m ? {
			x: u,
			y: e.y
		} : v === h ? {
			x: d,
			y: e.y
		} : v === g ? {
			x: e.x,
			y: f
		} : {
			x: e.x,
			y: p
		};
	}, "nodeBoundaryClamp");
	for (let e of t.edges) {
		let t = e.points;
		if (!t || t.length < 2) continue;
		let n = e.start, r = e.end, i = n ? l.get(n) : void 0, a = r ? l.get(r) : void 0;
		i && (t[0] = tt(t[0], i)), a && (t[t.length - 1] = tt(t[t.length - 1], a));
	}
	return t;
}
e(br, "routeEdgesOrthogonal");
function xr(e) {
	var t;
	return (t = e.direction) == null ? "TB" : t;
}
e(xr, "getSwimlaneDirection");
function Sr(e) {
	var t, n, r, i, a, o, s, c, l, u, d, f;
	let p = se(e), m = (t = (n = e.config.flowchart) == null ? void 0 : n.nodeSpacing) == null ? 40 : t, h = (r = (i = e.config.flowchart) == null ? void 0 : i.rankSpacing) == null ? 100 : r, g = (a = (o = e.config.swimlane) == null ? void 0 : o.ignoreCrossLaneEdges) == null || a, _ = (s = (c = e.config.swimlane) == null ? void 0 : c.optimizeRanksByCrossings) == null || s, v = (l = (u = e.config.swimlane) == null ? void 0 : u.automaticLaneOrdering) != null && l, y = xr(e), { ordered: b, coordinates: x } = ur(p, {
		nodeGap: m,
		layerGap: h,
		ignoreCrossLaneEdges: g,
		optimizeRanksByCrossings: _,
		automaticLaneOrdering: v,
		direction: y
	});
	ce(p, b, x, {
		nodeGap: m,
		layerGap: h
	});
	for (let t of (d = e.edges) == null ? [] : d) delete t.points;
	br(e, y);
	for (let t of (f = e.edges) == null ? [] : f) (!t.curve || t.curve === "basis") && (t.curve = "rounded");
	return en(e, y), $t(e), y;
}
e(Sr, "runSwimlaneLayoutCore");
async function Cr(e, t) {
	let n = t.select("g");
	m(n, e.markers, e.type, e.diagramId), f(), y(), p(), c(), B(e);
	let r = ue(e);
	e.nodes = r.nodes, e.edges = r.edges;
	let { groups: i } = await b(n, e);
	Sr(e), await ne(e, i);
}
e(Cr, "render");
//#endregion
export { Cr as render };
