import { n as e } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n as t } from "./src-upoOno_g.mjs";
import { x as n } from "./chunk-WYO6CB5R-_U4dPty4.mjs";
import "./chunk-ICXQ74PX-5xZ0PKaW.mjs";
import "./chunk-HOUHSVGY-2PLbOYM-.mjs";
import "./chunk-Q4XR5HBZ-KP7VNg4F.mjs";
import "./chunk-7BUUIJ7U-Cnx5Zlja.mjs";
import { n as r } from "./chunk-OGEWGWER-Cn1Z4hFw.mjs";
import { t as i } from "./graphlib-B6n0CQ68.mjs";
import { t as a } from "./dagre-DNn4Bkfn.mjs";
import { a as o, i as s, n as c, o as l, r as u, t as d } from "./chunk-RYQCIY6F-B5BzgP3I.mjs";
import "./chunk-C7G6YPKG-BGTLz-ew.mjs";
import { a as f, c as p, i as m, l as h, n as g, t as _, u as v } from "./chunk-ZGVPDNZ5-CA-MSk8G.mjs";
import { a as y, i as b, o as x, r as S, t as C } from "./chunk-52WLFC77-CEdJfckq.mjs";
//#region node_modules/mermaid/dist/chunks/mermaid.core/dagre-VKFMJZFB.mjs
var w = /* @__PURE__ */ e((e, t, n) => Math.max(t, Math.min(n, e)), "clamp"), T = /* @__PURE__ */ e((e = "TB") => {
	switch (e) {
		case "BT": return "bottom";
		case "LR": return "right";
		case "RL": return "left";
		default: return "top";
	}
}, "getDefaultSelfLoopSide"), E = /* @__PURE__ */ e((e) => e === "flowchart" || e === "flowchart-v2" || e === "stateDiagram", "shouldMergeSelfLoopSegments"), D = /* @__PURE__ */ e((e, t, n, r, i) => {
	let a = [], o = /* @__PURE__ */ new Set();
	if (n.forEach(({ start: e, end: t }) => {
		e !== r && o.add(e), t !== r && o.add(t);
	}), o.forEach((t) => {
		let n = e.node(t);
		typeof (n == null ? void 0 : n.x) == "number" && typeof (n == null ? void 0 : n.y) == "number" && a.push(n);
	}), a.length === 0 && n.forEach(({ edge: e }) => {
		var t;
		((t = e.points) == null ? [] : t).forEach((e) => {
			typeof (e == null ? void 0 : e.x) == "number" && typeof (e == null ? void 0 : e.y) == "number" && a.push(e);
		});
	}), a.length === 0) return T(i);
	let s = a.reduce((e, t) => ({
		x: e.x + t.x / a.length,
		y: e.y + t.y / a.length
	}), {
		x: 0,
		y: 0
	}), c = s.x - t.x, l = s.y - t.y;
	return Math.abs(c) > Math.abs(l) ? c > 0 ? "right" : "left" : Math.abs(l) > 0 ? l > 0 ? "bottom" : "top" : T(i);
}, "getSelfLoopSide"), O = /* @__PURE__ */ e((e, t = "top", n = 0, r = 0) => {
	let i = e.x, a = e.y - n, o = e.width / 2, s = e.height / 2, c = Math.max(36, Math.min(100, e.width * .8)), l = w(Math.max(r, e.width * .35), 36, c), u = w(Math.min(e.width, e.height) * .45, 24, 48);
	switch (t) {
		case "bottom": {
			let e = a + s;
			return [
				{
					x: i - l / 2,
					y: e
				},
				{
					x: i - l / 2,
					y: e + u
				},
				{
					x: i + l / 2,
					y: e + u
				},
				{
					x: i + l / 2,
					y: e
				}
			];
		}
		case "right": {
			let e = i + o;
			return [
				{
					x: e,
					y: a - l / 2
				},
				{
					x: e + u,
					y: a - l / 2
				},
				{
					x: e + u,
					y: a + l / 2
				},
				{
					x: e,
					y: a + l / 2
				}
			];
		}
		case "left": {
			let e = i - o;
			return [
				{
					x: e,
					y: a - l / 2
				},
				{
					x: e - u,
					y: a - l / 2
				},
				{
					x: e - u,
					y: a + l / 2
				},
				{
					x: e,
					y: a + l / 2
				}
			];
		}
		default: {
			let e = a - s;
			return [
				{
					x: i - l / 2,
					y: e
				},
				{
					x: i - l / 2,
					y: e - u
				},
				{
					x: i + l / 2,
					y: e - u
				},
				{
					x: i + l / 2,
					y: e
				}
			];
		}
	}
}, "getSelfLoopPoints"), k = /* @__PURE__ */ e((e, t, n = "top", r = 0, i = {}) => {
	var a, o;
	let s = e.x, c = e.y - r, l = (a = i.width) == null ? 0 : a, u = (o = i.height) == null ? 0 : o;
	switch (n) {
		case "bottom": return {
			x: s,
			y: Math.max(...t.map((e) => e.y)) + u / 2 + 4
		};
		case "right": return {
			x: Math.max(...t.map((e) => e.x)) + l / 2 + 4,
			y: c
		};
		case "left": return {
			x: Math.min(...t.map((e) => e.x)) - l / 2 - 4,
			y: c
		};
		default: return {
			x: s,
			y: Math.min(...t.map((e) => e.y)) - u / 2 - 4
		};
	}
}, "getSelfLoopLabelPosition"), A = /* @__PURE__ */ e((e, t = 0, { mergeSelfLoops: n = !0 } = {}) => {
	var r;
	let i = /* @__PURE__ */ new Map(), a = [], o = (r = e.graph()) == null ? void 0 : r.rankdir;
	return e.edges().forEach((t) => {
		let r = e.edge(t);
		if (n && r.selfLoop) {
			let e = r.selfLoop.id;
			i.has(e) || i.set(e, []), i.get(e).push({
				edge: r,
				start: t.v,
				end: t.w
			});
		} else a.push({
			edge: r,
			start: t.v,
			end: t.w
		});
	}), i.forEach((n) => {
		var r, i, s, c, l, u, d, f;
		if (n.length !== 3) {
			n.forEach((e) => a.push(e));
			return;
		}
		n.sort((e, t) => e.edge.selfLoop.order - t.edge.selfLoop.order);
		let [p, m, h] = n, g = (r = (i = (s = p.edge.originalEdge) == null ? m.edge.originalEdge : s) == null ? h.edge.originalEdge : i) == null ? m.edge : r, _ = e.node(g.start);
		if (!_) {
			n.forEach((e) => a.push(e));
			return;
		}
		let v = {
			width: m.edge.width,
			height: m.edge.height
		}, y = D(e, _, n, g.start, o), b = O(_, y, t, (c = v.width) == null ? 0 : c), x = k(_, b, y, t, v), S = {
			...m.edge,
			...g,
			id: g.id,
			points: b,
			start: g.start,
			end: g.end,
			x: x.x,
			y: x.y,
			width: v.width,
			height: v.height,
			labelStyle: m.edge.labelStyle,
			fromCluster: (l = (u = p.edge.fromCluster) == null ? m.edge.fromCluster : u) == null ? h.edge.fromCluster : l,
			toCluster: (d = (f = p.edge.toCluster) == null ? m.edge.toCluster : f) == null ? h.edge.toCluster : d
		};
		delete S.selfLoop, delete S.originalEdge, a.push({
			edge: S,
			start: S.start,
			end: S.end
		});
	}), a;
}, "getEdgesToRender"), j = /* @__PURE__ */ e(async (n, i, c, d, g, _) => {
	t.warn("Graph in recursive render:XAX", l(i), g);
	let y = i.graph().rankdir;
	t.trace("Dir in recursive render - dir:", y);
	let C = n.insert("g").attr("class", "root");
	i.nodes() ? t.info("Recursive render XXX", i.nodes()) : t.info("No nodes found for", i), i.edges().length > 0 && t.info("Recursive edges", i.edge(i.edges()[0]));
	let w = C.insert("g").attr("class", "clusters"), T = C.insert("g").attr("class", "edgePaths"), D = C.insert("g").attr("class", "edgeLabels"), O = C.insert("g").attr("class", "nodes"), k = E(c);
	await Promise.all(i.nodes().map(async function(e) {
		let n = i.node(e);
		if (g !== void 0) {
			let n = JSON.parse(JSON.stringify(g.clusterData));
			t.trace("Setting data for parent cluster XXX\n Node.id = ", e, "\n data=", n.height, "\nParent cluster", g.height), i.setNode(g.id, n), i.parent(e) || (t.trace("Setting parent", e, g.id), i.setParent(e, g.id, n));
		}
		if (t.info("(Insert) Node XXX" + e + ": " + JSON.stringify(i.node(e))), n != null && n.clusterNode) {
			t.info("Cluster identified XBX", e, n.width, i.node(e));
			let { ranksep: r, nodesep: a } = i.graph();
			n.graph.setGraph({
				...n.graph.graph(),
				ranksep: r + 25,
				nodesep: a
			});
			let o = await j(O, n.graph, c, d, i.node(e), _), s = o.elem;
			v(n, s), n.diff = o.diff || 0, t.info("New compound node after recursive render XAX", e, "width", n.width, "height", n.height), h(s, n);
		} else i.children(e).length > 0 ? (t.trace("Cluster - the non recursive path XBX", e, n.id, n, n.width, "Graph:", i), t.trace(s(n.id, i)), u.set(n.id, {
			id: s(n.id, i),
			node: n
		})) : (t.trace("Node - the non recursive path XAX", e, O, i.node(e), y), await f(O, i.node(e), {
			config: _,
			dir: y
		}));
	})), await (/* @__PURE__ */ e(async () => {
		let e = i.edges().map(async function(e) {
			let n = i.edge(e.v, e.w, e.name);
			if (t.info("Edge " + e.v + " -> " + e.w + ": " + JSON.stringify(e)), t.info("Edge " + e.v + " -> " + e.w + ": ", e, " ", JSON.stringify(i.edge(e))), t.info("Fix", u, "ids:", e.v, e.w, "Translating: ", u.get(e.v), u.get(e.w)), k && n.selfLoop) {
				if (n.selfLoop.order !== 1) return;
				let e = n.id;
				n.id = n.selfLoop.id, await b(D, n), n.id = e;
				return;
			}
			await b(D, n);
		});
		await Promise.all(e);
	}, "processEdges"))(), t.info("Graph before layout:", JSON.stringify(l(i))), t.info("############################################# XXX"), t.info("###                Layout                 ### XXX"), t.info("############################################# XXX"), a(i), t.info("Graph after layout:", JSON.stringify(l(i)));
	let M = 0, { subGraphTitleTotalMargin: N } = r(_);
	await Promise.all(o(i).map(async function(e) {
		let n = i.node(e);
		if (t.info("Position XBX => " + e + ": (" + n.x, "," + n.y, ") width: ", n.width, " height: ", n.height), n != null && n.clusterNode) n.y += N, t.info("A tainted cluster node XBX1", e, n.id, n.width, n.height, n.x, n.y, i.parent(e)), u.get(n.id).node = n, p(n);
		else if (i.children(e).length > 0) {
			var r;
			t.info("A pure cluster node XBX1", e, n.id, n.x, n.y, n.width, n.height, i.parent(e)), n.height += N, i.node(n.parentId);
			let a = (n == null ? void 0 : n.padding) / 2 || 0, o = (n == null || (r = n.labelBBox) == null ? void 0 : r.height) || 0, s = o - a || 0;
			t.debug("OffsetY", s, "labelHeight", o, "halfPadding", a), await m(w, n), u.get(n.id).node = n;
		} else {
			let e = i.node(n.parentId);
			n.y += N / 2, t.info("A regular node XBX1 - using the padding", n.id, "parent", n.parentId, n.width, n.height, n.x, n.y, "offsetY", n.offsetY, "parent", e, e == null ? void 0 : e.offsetY, n), p(n);
		}
	}));
	let P = N / 2;
	return A(i, P, { mergeSelfLoops: k }).forEach(function({ edge: e, start: n, end: r }) {
		t.info("Edge " + n + " -> " + r + ": " + JSON.stringify(e), e), e.points.forEach((e) => e.y += P);
		let a = i.node(n), o = i.node(r);
		x(e, S(T, e, u, c, a, o, d));
	}), i.nodes().forEach(function(e) {
		let n = i.node(e);
		t.info(e, n.type, n.diff), n.isGroup && (M = n.diff);
	}), t.warn("Returning from recursive render XAX", C, M), {
		elem: C,
		diff: M
	};
}, "recursiveRender"), M = /* @__PURE__ */ e(async (e, r) => {
	var a, o, s, u;
	let f = new i({
		multigraph: !0,
		compound: !0
	}).setGraph({
		rankdir: e.direction,
		nodesep: ((a = e.config) == null ? void 0 : a.nodeSpacing) || ((o = e.config) == null || (o = o.flowchart) == null ? void 0 : o.nodeSpacing) || e.nodeSpacing,
		ranksep: ((s = e.config) == null ? void 0 : s.rankSpacing) || ((u = e.config) == null || (u = u.flowchart) == null ? void 0 : u.rankSpacing) || e.rankSpacing,
		marginx: 8,
		marginy: 8
	}).setDefaultEdgeLabel(function() {
		return {};
	}), p = r.select("g");
	y(p, e.markers, e.type, e.diagramId), g(), C(), _(), c(), e.nodes.forEach((e) => {
		f.setNode(e.id, { ...e }), e.parentId && f.setParent(e.id, e.parentId);
	}), t.debug("Edges:", e.edges), e.edges.forEach((e) => {
		if (e.start === e.end) {
			let t = e.start, n = t + "---" + t + "---1", r = t + "---" + t + "---2", i = f.node(t);
			f.setNode(n, {
				domId: n,
				id: n,
				parentId: i.parentId,
				labelStyle: "",
				label: "",
				padding: 0,
				shape: "labelRect",
				style: "",
				width: 10,
				height: 10
			}), f.setParent(n, i.parentId), f.setNode(r, {
				domId: r,
				id: r,
				parentId: i.parentId,
				labelStyle: "",
				padding: 0,
				shape: "labelRect",
				label: "",
				style: "",
				width: 10,
				height: 10
			}), f.setParent(r, i.parentId);
			let a = structuredClone(e), o = structuredClone(e), s = structuredClone(e), c = structuredClone(e);
			o.originalEdge = a, o.selfLoop = {
				id: a.id,
				order: 0
			}, s.originalEdge = a, s.selfLoop = {
				id: a.id,
				order: 1
			}, c.originalEdge = a, c.selfLoop = {
				id: a.id,
				order: 2
			}, o.label = "", o.arrowTypeEnd = "none", o.endLabelLeft = "", o.endLabelRight = "", o.startLabelLeft = "", o.id = t + "-cyclic-special-1", s.startLabelRight = "", s.startLabelLeft = "", s.endLabelLeft = "", s.endLabelRight = "", s.arrowTypeStart = "none", s.arrowTypeEnd = "none", s.id = t + "-cyclic-special-mid", c.label = "", c.startLabelRight = "", c.startLabelLeft = "", c.arrowTypeStart = "none", i.isGroup && (o.fromCluster = t, c.toCluster = t), c.id = t + "-cyclic-special-2", c.arrowTypeStart = "none", f.setEdge(t, n, o, t + "-cyclic-special-0"), f.setEdge(n, r, s, t + "-cyclic-special-1"), f.setEdge(r, t, c, t + "-cyclic-special-2");
		} else f.setEdge(e.start, e.end, { ...e }, e.id);
	}), t.warn("Graph at first:", JSON.stringify(l(f))), d(f), t.warn("Graph after XAX:", JSON.stringify(l(f)));
	let m = n();
	await j(p, f, e.type, e.diagramId, void 0, m);
}, "render");
//#endregion
export { A as getEdgesToRender, M as render };
