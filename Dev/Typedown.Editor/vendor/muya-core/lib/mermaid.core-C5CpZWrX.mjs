import { t as e } from "./purify.es-T0ACKwv7.mjs";
import { n as t } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n, r, t as i } from "./src-upoOno_g.mjs";
import { C as a, E as o, I as s, L as c, N as l, P as u, Q as d, S as f, T as p, V as m, W as h, X as g, Z as _, _ as v, b as y, c as b, g as x, l as S, m as C, n as w, p as T, q as ee, r as te, t as E, u as D } from "./chunk-WYO6CB5R-_U4dPty4.mjs";
import { S as ne, a as O, f as k, g as A, h as re, i as j, o as M, v as ie, x as ae, y as oe } from "./chunk-ICXQ74PX-5xZ0PKaW.mjs";
import { t as se } from "./chunk-VAUOI2AC-DvRR_ttS.mjs";
import { r as ce } from "./chunk-HOUHSVGY-2PLbOYM-.mjs";
import { r as le } from "./chunk-Q4XR5HBZ-KP7VNg4F.mjs";
import "./chunk-7BUUIJ7U-Cnx5Zlja.mjs";
import "./chunk-OGEWGWER-Cn1Z4hFw.mjs";
import "./chunk-C7G6YPKG-BGTLz-ew.mjs";
import "./chunk-ZGVPDNZ5-CA-MSk8G.mjs";
import "./chunk-52WLFC77-CEdJfckq.mjs";
import { n as ue } from "./chunk-FWX5IMBZ-Dx6oqe1x.mjs";
import { n as de, t as fe } from "./chunk-ZIRB5QZD-BdqCGXAd.mjs";
//#region node_modules/es-toolkit/dist/compat/_internal/isPrototype.mjs
function pe(e) {
	let t = e == null ? void 0 : e.constructor;
	return e === (typeof t == "function" ? t.prototype : Object.prototype);
}
//#endregion
//#region node_modules/es-toolkit/dist/compat/predicate/isEmpty.mjs
function me(e) {
	if (e == null) return !0;
	if (ae(e)) return typeof e.splice != "function" && typeof e != "string" && !ne(e) && !ie(e) && !oe(e) ? !1 : e.length === 0;
	if (typeof e == "object" || typeof e == "function") {
		if (e instanceof Map || e instanceof Set) return e.size === 0;
		let t = Object.keys(e);
		return pe(e) ? t.filter((e) => e !== "constructor").length === 0 : t.length === 0;
	}
	return !0;
}
//#endregion
//#region node_modules/stylis/src/Enum.js
var he = "comm", ge = "rule", _e = "decl", ve = "@import", ye = "@namespace", be = "@keyframes", xe = "@layer", Se = Math.abs, N = String.fromCharCode;
function Ce(e) {
	return e.trim();
}
function we(e, t, n) {
	return e.replace(t, n);
}
function P(e, t) {
	return e.charCodeAt(t) | 0;
}
function F(e, t, n) {
	return e.slice(t, n);
}
function I(e) {
	return e.length;
}
function Te(e) {
	return e.length;
}
function L(e, t) {
	return t.push(e), e;
}
//#endregion
//#region node_modules/stylis/src/Tokenizer.js
var R = 1, z = 1, Ee = 0, B = 0, V = 0, H = "";
function U(e, t, n, r, i, a, o, s) {
	return {
		value: e,
		root: t,
		parent: n,
		type: r,
		props: i,
		children: a,
		line: R,
		column: z,
		length: o,
		return: "",
		siblings: s
	};
}
function De() {
	return V;
}
function Oe() {
	return V = B > 0 ? P(H, --B) : 0, z--, V === 10 && (z = 1, R--), V;
}
function W() {
	return V = B < Ee ? P(H, B++) : 0, z++, V === 10 && (z = 1, R++), V;
}
function G() {
	return P(H, B);
}
function K() {
	return B;
}
function q(e, t) {
	return F(H, e, t);
}
function J(e) {
	switch (e) {
		case 0:
		case 9:
		case 10:
		case 13:
		case 32: return 5;
		case 33:
		case 43:
		case 44:
		case 47:
		case 62:
		case 64:
		case 126:
		case 59:
		case 123:
		case 125: return 4;
		case 58: return 3;
		case 34:
		case 39:
		case 40:
		case 91: return 2;
		case 41:
		case 93: return 1;
	}
	return 0;
}
function ke(e) {
	return R = z = 1, Ee = I(H = e), B = 0, [];
}
function Ae(e) {
	return H = "", e;
}
function je(e) {
	return Ce(q(B - 1, Pe(e === 91 ? e + 2 : e === 40 ? e + 1 : e)));
}
function Me(e) {
	for (; (V = G()) && V < 33;) W();
	return J(e) > 2 || J(V) > 3 ? "" : " ";
}
function Ne(e, t) {
	for (; --t && W() && !(V < 48 || V > 102 || V > 57 && V < 65 || V > 70 && V < 97););
	return q(e, K() + (t < 6 && G() == 32 && W() == 32));
}
function Pe(e) {
	for (; W();) switch (V) {
		case e: return B;
		case 34:
		case 39:
			e !== 34 && e !== 39 && Pe(V);
			break;
		case 40:
			e === 41 && Pe(e);
			break;
		case 92:
			W();
			break;
	}
	return B;
}
function Fe(e, t) {
	for (; W() && e + V !== 57 && !(e + V === 84 && G() === 47););
	return "/*" + q(t, B - 1) + "*" + N(e === 47 ? e : W());
}
function Ie(e) {
	for (; !J(G());) W();
	return q(e, B);
}
//#endregion
//#region node_modules/stylis/src/Parser.js
function Le(e) {
	return Ae(Y("", null, null, null, [""], e = ke(e), 0, [0], e));
}
function Y(e, t, n, r, i, a, o, s, c) {
	for (var l = 0, u = 0, d = o, f = 0, p = 0, m = 0, h = 1, g = 1, _ = 1, v = 0, y = 0, b = "", x = i, S = a, C = r, w = b; g;) switch (m = y, y = W()) {
		case 40:
			m != 108 && P(w, d - 1) == 58 ? (v++, w += "(") : w += je(y);
			break;
		case 41:
			v--, w += ")";
			break;
		case 34:
		case 39:
		case 91:
			w += je(y);
			break;
		case 9:
		case 10:
		case 13:
		case 32:
			if (v > 0) {
				w += N(y);
				break;
			}
			w += Me(m);
			break;
		case 92:
			w += Ne(K() - 1, 7);
			continue;
		case 47:
			switch (G()) {
				case 42:
				case 47:
					L(ze(Fe(W(), K()), t, n, c), c), (J(m || 1) == 5 || J(G() || 1) == 5) && I(w) && F(w, -1, void 0) !== " " && (w += " ");
					break;
				default: w += "/";
			}
			break;
		case 123 * h: s[l++] = I(w) * _;
		case 125 * h:
		case 59:
		case 0:
			if (v > 0 && y) {
				w += N(y);
				break;
			}
			switch (y) {
				case 0:
				case 125: g = 0;
				case 59 + u:
					_ == -1 && (w = we(w, /\f/g, "")), p > 0 && (I(w) - d || h === 0) && L(p > 32 ? Be(w + ";", r, n, d - 1, c) : Be(we(w, " ", "") + ";", r, n, d - 2, c), c);
					break;
				case 59: w += ";";
				default: if (L(C = Re(w, t, n, l, u, i, s, b, x = [], S = [], d, a), a), y === 123) if (u === 0) Y(w, t, C, C, x, a, d, s, S);
				else {
					switch (f) {
						case 99: if (P(w, 3) === 110) break;
						case 108: if (P(w, 2) === 97) break;
						default: u = 0;
						case 100:
						case 109:
						case 115:
					}
					u ? Y(e, C, C, r && L(Re(e, C, C, 0, 0, i, s, b, i, x = [], d, S), S), i, S, d, s, r ? x : S) : Y(w, C, C, C, [""], S, 0, s, S);
				}
			}
			l = u = p = 0, h = _ = 1, b = w = "", d = o;
			break;
		case 58: d = 1 + I(w), p = m;
		default:
			if (h < 1) {
				if (y == 123) --h;
				else if (y == 125 && h++ == 0 && Oe() == 125) continue;
			}
			switch (w += N(y), y * h) {
				case 38:
					_ = u > 0 ? 1 : (w += "\f", -1);
					break;
				case 44:
					if (v > 0) break;
					s[l++] = (I(w) - 1) * _, _ = 1;
					break;
				case 64:
					G() === 45 && (w += je(W())), f = G(), u = d = I(b = w += Ie(K())), y++;
					break;
				case 45: m === 45 && I(w) == 2 && (h = 0);
			}
	}
	return a;
}
function Re(e, t, n, r, i, a, o, s, c, l, u, d) {
	for (var f = i - 1, p = i === 0 ? a : [""], m = Te(p), h = 0, g = 0, _ = 0; h < r; ++h) for (var v = 0, y = F(e, f + 1, f = Se(g = o[h])), b = e; v < m; ++v) (b = Ce(g > 0 ? p[v] + " " + y : we(y, /&\f/g, p[v]))) && (c[_++] = b);
	return U(e, t, n, i === 0 ? ge : s, c, l, u, d);
}
function ze(e, t, n, r) {
	return U(e, t, n, he, N(De()), F(e, 2, -2), 0, r);
}
function Be(e, t, n, r, i) {
	return U(e, t, n, _e, F(e, 0, r), F(e, r + 1, -1), r, i);
}
//#endregion
//#region node_modules/stylis/src/Serializer.js
function Ve(e, t) {
	for (var n = "", r = 0; r < e.length; r++) n += t(e[r], r, e, t) || "";
	return n;
}
function He(e, t, n, r) {
	switch (e.type) {
		case xe: if (e.children.length) break;
		case ve:
		case ye:
		case _e: return e.return = e.return || e.value;
		case he: return "";
		case be: return e.return = e.value + "{" + Ve(e.children, r) + "}";
		case ge: if (!I(e.value = e.props.join(","))) return "";
	}
	return I(n = Ve(e.children, r)) ? e.return = e.value + "{" + n + "}" : "";
}
//#endregion
//#region node_modules/stylis/src/Middleware.js
function Ue(e) {
	var t = Te(e);
	return function(n, r, i, a) {
		for (var o = "", s = 0; s < t; s++) o += e[s](n, r, i, a) || "";
		return o;
	};
}
//#endregion
//#region node_modules/mermaid/dist/mermaid.core.mjs
var We, Ge = "c4", Ke = {
	id: Ge,
	detector: /* @__PURE__ */ t((e) => /^\s*C4Context|C4Container|C4Component|C4Dynamic|C4Deployment/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./c4Diagram-LMCZKHZV-przuIy8T.mjs");
		return {
			id: Ge,
			diagram: e
		};
	}, "loader")
}, qe = "flowchart", Je = {
	id: qe,
	detector: /* @__PURE__ */ t((e, t) => {
		var n, r;
		return (t == null || (n = t.flowchart) == null ? void 0 : n.defaultRenderer) === "dagre-wrapper" || (t == null || (r = t.flowchart) == null ? void 0 : r.defaultRenderer) === "elk" ? !1 : /^\s*graph/.test(e);
	}, "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./flowDiagram-23GEKE2U-ycbk4-o7.mjs");
		return {
			id: qe,
			diagram: e
		};
	}, "loader")
}, Ye = "flowchart-v2", Xe = {
	id: Ye,
	detector: /* @__PURE__ */ t((e, t) => {
		var n, r, i;
		return (t == null || (n = t.flowchart) == null ? void 0 : n.defaultRenderer) === "dagre-d3" ? !1 : ((t == null || (r = t.flowchart) == null ? void 0 : r.defaultRenderer) === "elk" && (t.layout = "elk"), /^\s*graph/.test(e) && (t == null || (i = t.flowchart) == null ? void 0 : i.defaultRenderer) === "dagre-wrapper" ? !0 : /^\s*flowchart/.test(e));
	}, "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./flowDiagram-23GEKE2U-ycbk4-o7.mjs");
		return {
			id: Ye,
			diagram: e
		};
	}, "loader")
}, Ze = "swimlane", Qe = {
	id: Ze,
	detector: /* @__PURE__ */ t((e) => /^\s*swimlane-beta\b/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./swimlanesDiagram-G3AALYLV-C9HaEPpi.mjs");
		return {
			id: Ze,
			diagram: e
		};
	}, "loader")
}, $e = "er", et = {
	id: $e,
	detector: /* @__PURE__ */ t((e) => /^\s*erDiagram/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./erDiagram-Q63AITRT-DWmQLm--.mjs");
		return {
			id: $e,
			diagram: e
		};
	}, "loader")
}, tt = "gitGraph", nt = {
	id: tt,
	detector: /* @__PURE__ */ t((e) => /^\s*gitGraph/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./gitGraphDiagram-IHSO6WYX-BjhDGP_q.mjs");
		return {
			id: tt,
			diagram: e
		};
	}, "loader")
}, rt = "gantt", it = {
	id: rt,
	detector: /* @__PURE__ */ t((e) => /^\s*gantt/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./ganttDiagram-NO4QXBWP-BJgFpnME.mjs");
		return {
			id: rt,
			diagram: e
		};
	}, "loader")
}, at = "info", ot = {
	id: at,
	detector: /* @__PURE__ */ t((e) => /^\s*info/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./infoDiagram-FWYZ7A6U-D6gjc1aW.mjs");
		return {
			id: at,
			diagram: e
		};
	}, "loader")
}, st = "pie", ct = {
	id: st,
	detector: /* @__PURE__ */ t((e) => /^\s*pie/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./pieDiagram-ENE6RG2P-CCiqkjb9.mjs");
		return {
			id: st,
			diagram: e
		};
	}, "loader")
}, lt = "quadrantChart", ut = {
	id: lt,
	detector: /* @__PURE__ */ t((e) => /^\s*quadrantChart/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./quadrantDiagram-ABIIQ3AL-CpcH7oUm.mjs");
		return {
			id: lt,
			diagram: e
		};
	}, "loader")
}, dt = "xychart", ft = {
	id: dt,
	detector: /* @__PURE__ */ t((e) => /^\s*xychart(-beta)?/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./xychartDiagram-FW5EYKEG-BgBFrLNF.mjs");
		return {
			id: dt,
			diagram: e
		};
	}, "loader")
}, pt = "requirement", mt = {
	id: pt,
	detector: /* @__PURE__ */ t((e) => /^\s*requirement(Diagram)?/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./requirementDiagram-TGXJPOKE-Dt2jfiMu.mjs");
		return {
			id: pt,
			diagram: e
		};
	}, "loader")
}, ht = "sequence", gt = {
	id: ht,
	detector: /* @__PURE__ */ t((e) => /^\s*sequenceDiagram/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./sequenceDiagram-DBY2YBRQ-Dm0DFElw.mjs");
		return {
			id: ht,
			diagram: e
		};
	}, "loader")
}, _t = "class", vt = {
	id: _t,
	detector: /* @__PURE__ */ t((e, t) => {
		var n;
		return (t == null || (n = t.class) == null ? void 0 : n.defaultRenderer) !== "dagre-wrapper" && /^\s*classDiagram/.test(e);
	}, "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./classDiagram-OUVF2IWQ-B_yyupW5.mjs");
		return {
			id: _t,
			diagram: e
		};
	}, "loader")
}, yt = "classDiagram", bt = {
	id: yt,
	detector: /* @__PURE__ */ t((e, t) => {
		var n;
		return /^\s*classDiagram/.test(e) && (t == null || (n = t.class) == null ? void 0 : n.defaultRenderer) === "dagre-wrapper" ? !0 : /^\s*classDiagram-v2/.test(e);
	}, "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./classDiagram-v2-EOCWNBFH-BTR8R5t_.mjs");
		return {
			id: yt,
			diagram: e
		};
	}, "loader")
}, xt = "state", St = {
	id: xt,
	detector: /* @__PURE__ */ t((e, t) => {
		var n;
		return (t == null || (n = t.state) == null ? void 0 : n.defaultRenderer) !== "dagre-wrapper" && /^\s*stateDiagram/.test(e);
	}, "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./stateDiagram-2N3HPSRC-84m8_5--.mjs");
		return {
			id: xt,
			diagram: e
		};
	}, "loader")
}, Ct = "stateDiagram", wt = {
	id: Ct,
	detector: /* @__PURE__ */ t((e, t) => {
		var n;
		return !!(/^\s*stateDiagram-v2/.test(e) || /^\s*stateDiagram/.test(e) && (t == null || (n = t.state) == null ? void 0 : n.defaultRenderer) === "dagre-wrapper");
	}, "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./stateDiagram-v2-6OUMAXLB-CtdDYrvA.mjs");
		return {
			id: Ct,
			diagram: e
		};
	}, "loader")
}, Tt = "journey", Et = {
	id: Tt,
	detector: /* @__PURE__ */ t((e) => /^\s*journey/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./journeyDiagram-5HDEW3XC-0t3jr4Ad.mjs");
		return {
			id: Tt,
			diagram: e
		};
	}, "loader")
}, Dt = { draw: /* @__PURE__ */ t((e, t, r) => {
	n.debug("rendering svg for syntax error\n");
	let i = se(t), a = i.append("g");
	i.attr("viewBox", "0 0 2412 512"), b(i, 100, 512, !0), a.append("path").attr("class", "error-icon").attr("d", "m411.313,123.313c6.25-6.25 6.25-16.375 0-22.625s-16.375-6.25-22.625,0l-32,32-9.375,9.375-20.688-20.688c-12.484-12.5-32.766-12.5-45.25,0l-16,16c-1.261,1.261-2.304,2.648-3.31,4.051-21.739-8.561-45.324-13.426-70.065-13.426-105.867,0-192,86.133-192,192s86.133,192 192,192 192-86.133 192-192c0-24.741-4.864-48.327-13.426-70.065 1.402-1.007 2.79-2.049 4.051-3.31l16-16c12.5-12.492 12.5-32.758 0-45.25l-20.688-20.688 9.375-9.375 32.001-31.999zm-219.313,100.687c-52.938,0-96,43.063-96,96 0,8.836-7.164,16-16,16s-16-7.164-16-16c0-70.578 57.422-128 128-128 8.836,0 16,7.164 16,16s-7.164,16-16,16z"), a.append("path").attr("class", "error-icon").attr("d", "m459.02,148.98c-6.25-6.25-16.375-6.25-22.625,0s-6.25,16.375 0,22.625l16,16c3.125,3.125 7.219,4.688 11.313,4.688 4.094,0 8.188-1.563 11.313-4.688 6.25-6.25 6.25-16.375 0-22.625l-16.001-16z"), a.append("path").attr("class", "error-icon").attr("d", "m340.395,75.605c3.125,3.125 7.219,4.688 11.313,4.688 4.094,0 8.188-1.563 11.313-4.688 6.25-6.25 6.25-16.375 0-22.625l-16-16c-6.25-6.25-16.375-6.25-22.625,0s-6.25,16.375 0,22.625l15.999,16z"), a.append("path").attr("class", "error-icon").attr("d", "m400,64c8.844,0 16-7.164 16-16v-32c0-8.836-7.156-16-16-16-8.844,0-16,7.164-16,16v32c0,8.836 7.156,16 16,16z"), a.append("path").attr("class", "error-icon").attr("d", "m496,96.586h-32c-8.844,0-16,7.164-16,16 0,8.836 7.156,16 16,16h32c8.844,0 16-7.164 16-16 0-8.836-7.156-16-16-16z"), a.append("path").attr("class", "error-icon").attr("d", "m436.98,75.605c3.125,3.125 7.219,4.688 11.313,4.688 4.094,0 8.188-1.563 11.313-4.688l32-32c6.25-6.25 6.25-16.375 0-22.625s-16.375-6.25-22.625,0l-32,32c-6.251,6.25-6.251,16.375-0.001,22.625z"), a.append("text").attr("class", "error-text").attr("x", 1440).attr("y", 250).attr("font-size", "150px").style("text-anchor", "middle").text("Syntax error in text"), a.append("text").attr("class", "error-text").attr("x", 1250).attr("y", 400).attr("font-size", "100px").style("text-anchor", "middle").text(`mermaid version ${r}`);
}, "draw") }, Ot = Dt, kt = {
	db: {},
	renderer: Dt,
	parser: { parse: /* @__PURE__ */ t(() => {}, "parse") }
}, At = "flowchart-elk", jt = {
	id: At,
	detector: /* @__PURE__ */ t((e, t = {}) => {
		var n;
		return /^\s*flowchart-elk/.test(e) || /^\s*(flowchart|graph)/.test(e) && (t == null || (n = t.flowchart) == null ? void 0 : n.defaultRenderer) === "elk" ? (t.layout = "elk", !0) : !1;
	}, "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./flowDiagram-23GEKE2U-ycbk4-o7.mjs");
		return {
			id: At,
			diagram: e
		};
	}, "loader")
}, Mt = "timeline", Nt = {
	id: Mt,
	detector: /* @__PURE__ */ t((e) => /^\s*timeline/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./timeline-definition-FHXFAJF6-LnygOXQT.mjs");
		return {
			id: Mt,
			diagram: e
		};
	}, "loader")
}, Pt = "mindmap", Ft = {
	id: Pt,
	detector: /* @__PURE__ */ t((e) => /^\s*mindmap/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./mindmap-definition-LN4V7U3C-BIM3gzol.mjs");
		return {
			id: Pt,
			diagram: e
		};
	}, "loader")
}, It = "kanban", Lt = {
	id: It,
	detector: /* @__PURE__ */ t((e) => /^\s*kanban/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./kanban-definition-HUTT4EX6-Cg7W_O9K.mjs");
		return {
			id: It,
			diagram: e
		};
	}, "loader")
}, Rt = "sankey", zt = {
	id: Rt,
	detector: /* @__PURE__ */ t((e) => /^\s*sankey(-beta)?/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./sankeyDiagram-HTMAVEWB-C0XBEUOb.mjs");
		return {
			id: Rt,
			diagram: e
		};
	}, "loader")
}, Bt = "packet", Vt = {
	id: Bt,
	detector: /* @__PURE__ */ t((e) => /^\s*packet(-beta)?/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./diagram-NH7WQ7WH-Bl265Ncx.mjs");
		return {
			id: Bt,
			diagram: e
		};
	}, "loader")
}, Ht = "radar", Ut = {
	id: Ht,
	detector: /* @__PURE__ */ t((e) => /^\s*radar-beta/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./diagram-WEI45ONY-BSJK_DXa.mjs");
		return {
			id: Ht,
			diagram: e
		};
	}, "loader")
}, Wt = "block", Gt = {
	id: Wt,
	detector: /* @__PURE__ */ t((e) => /^\s*block(-beta)?/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./blockDiagram-677ZJIJ3-C0EwCrDi.mjs");
		return {
			id: Wt,
			diagram: e
		};
	}, "loader")
}, Kt = "treeView", qt = {
	id: Kt,
	detector: /* @__PURE__ */ t((e) => /^\s*treeView-beta/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./diagram-OA4YK3LP-B_cVeorH.mjs");
		return {
			id: Kt,
			diagram: e
		};
	}, "loader")
}, Jt = "architecture", Yt = {
	id: Jt,
	detector: /* @__PURE__ */ t((e) => /^\s*architecture/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./architectureDiagram-ZJ3FMSHR-auULrQXq.mjs");
		return {
			id: Jt,
			diagram: e
		};
	}, "loader")
}, Xt = "eventmodeling", Zt = {
	id: Xt,
	detector: /* @__PURE__ */ t((e) => /^\s*eventmodeling/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./diagram-FQU43EPY-pnbznZkt.mjs");
		return {
			id: Xt,
			diagram: e
		};
	}, "loader")
}, Qt = "ishikawa", $t = {
	id: Qt,
	detector: /* @__PURE__ */ t((e) => /^\s*ishikawa(-beta)?\b/i.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./ishikawaDiagram-FXEZZL3T-f8GPVEe-.mjs");
		return {
			id: Qt,
			diagram: e
		};
	}, "loader")
}, en = "venn", tn = {
	id: en,
	detector: /* @__PURE__ */ t((e) => /^\s*venn-beta/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./vennDiagram-L72KCM5P-BP3_yPIR.mjs");
		return {
			id: en,
			diagram: e
		};
	}, "loader")
}, nn = "treemap", rn = {
	id: nn,
	detector: /* @__PURE__ */ t((e) => /^\s*treemap/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./diagram-G47NLZAW-CRTXjlkx.mjs");
		return {
			id: nn,
			diagram: e
		};
	}, "loader")
}, an = "wardley", on = {
	id: an,
	detector: /* @__PURE__ */ t((e) => /^\s*wardley-beta/i.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./wardleyDiagram-EHGQE667-BDwdI1Rl.mjs");
		return {
			id: an,
			diagram: e
		};
	}, "loader")
}, sn = "cynefin", cn = {
	id: sn,
	detector: /* @__PURE__ */ t((e) => /^\s*cynefin-beta(?:[\s:]|$)/.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./cynefinDiagram-TSTJHNR4-wtoG8izr.mjs");
		return {
			id: sn,
			diagram: e
		};
	}, "loader")
}, ln = "railroad", un = {
	id: ln,
	detector: /* @__PURE__ */ t((e) => /^\s*railroad-beta/i.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./railroadDiagram-RFXS5EU6-C87f4weC.mjs");
		return {
			id: ln,
			diagram: e
		};
	}, "loader")
}, dn = "railroadEbnf", fn = {
	id: dn,
	detector: /* @__PURE__ */ t((e) => /^\s*railroad-ebnf-beta/i.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./ebnfDiagram-CCIWWBDH-CRvST8Hh.mjs");
		return {
			id: dn,
			diagram: e
		};
	}, "loader")
}, pn = "railroadAbnf", mn = {
	id: pn,
	detector: /* @__PURE__ */ t((e) => /^\s*railroad-abnf-beta/i.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./abnfDiagram-VRR7QNED-MCsBspxu.mjs");
		return {
			id: pn,
			diagram: e
		};
	}, "loader")
}, hn = "railroadPeg", gn = {
	id: hn,
	detector: /* @__PURE__ */ t((e) => /^\s*railroad-peg-beta/i.test(e), "detector"),
	loader: /* @__PURE__ */ t(async () => {
		let { diagram: e } = await import("./pegDiagram-2B236MQR-BbcDFfVF.mjs");
		return {
			id: hn,
			diagram: e
		};
	}, "loader")
}, _n = !1, X = /* @__PURE__ */ t(() => {
	_n || (_n = !0, l("error", kt, (e) => e.toLowerCase().trim() === "error"), l("---", {
		db: { clear: /* @__PURE__ */ t(() => {}, "clear") },
		styles: {},
		renderer: { draw: /* @__PURE__ */ t(() => {}, "draw") },
		parser: { parse: /* @__PURE__ */ t(() => {
			throw Error("Diagrams beginning with --- are not valid. If you were trying to use a YAML front-matter, please ensure that you've correctly opened and closed the YAML front-matter with un-indented `---` blocks");
		}, "parse") },
		init: /* @__PURE__ */ t(() => null, "init")
	}, (e) => e.toLowerCase().trimStart().startsWith("---")), u(jt, Ft, Yt), u(Ke, Lt, bt, vt, et, it, ot, ct, mt, gt, Qe, Xe, Je, Nt, nt, wt, St, Et, ut, zt, Vt, ft, Gt, Zt, qt, Ut, $t, rn, un, fn, mn, gn, tn, on, cn));
}, "addDiagrams"), vn = /* @__PURE__ */ t(async () => {
	n.debug("Loading registered diagrams");
	let e = (await Promise.allSettled(Object.entries(C).map(async ([e, { detector: t, loader: r }]) => {
		if (r) try {
			f(e);
		} catch {
			try {
				let { diagram: e, id: n } = await r();
				l(n, e, t);
			} catch (t) {
				throw n.error(`Failed to load external diagram with key ${e}. Removing from detectors.`), delete C[e], t;
			}
		}
	}))).filter((e) => e.status === "rejected");
	if (e.length > 0) {
		n.error(`Failed to load ${e.length} external diagrams`);
		for (let t of e) n.error(t);
		throw Error(`Failed to load ${e.length} external diagrams`);
	}
}, "loadRegisteredDiagrams"), yn = "graphics-document document";
function bn(e, t) {
	e.attr("role", yn), t !== "" && e.attr("aria-roledescription", t);
}
t(bn, "setA11yDiagramInfo");
function xn(e, t, n, r) {
	if (e.insert !== void 0) {
		if (n) {
			let t = `chart-desc-${r}`;
			e.attr("aria-describedby", t), e.insert("desc", ":first-child").attr("id", t).text(n);
		}
		if (t) {
			let n = `chart-title-${r}`;
			e.attr("aria-labelledby", n), e.insert("title", ":first-child").attr("id", n).text(t);
		}
	}
}
t(xn, "addSVGa11yTitleDescription");
var Sn = (We = class e {
	constructor(e, t, n, r, i) {
		this.type = e, this.text = t, this.db = n, this.parser = r, this.renderer = i;
	}
	static async fromText(t, n = {}) {
		var r;
		let i = y(), o = T(t, i);
		t = M(t) + "\n";
		try {
			f(o);
		} catch {
			let e = a(o);
			if (!e) throw new E(`Diagram ${o} not found.`);
			let { id: t, diagram: n } = await e();
			l(t, n);
		}
		let { db: s, parser: c, renderer: u, init: d } = f(o);
		if (c.parser && (c.parser.yy = s), (r = s.clear) == null || r.call(s), d == null || d(i), n.title) {
			var p;
			(p = s.setDiagramTitle) == null || p.call(s, n.title);
		}
		return await c.parse(t), new e(o, t, s, c, u);
	}
	async render(e, t) {
		await this.renderer.draw(this.text, e, t, this);
	}
	getParser() {
		return this.parser;
	}
	getType() {
		return this.type;
	}
}, t(We, "Diagram"), We), Cn = [], wn = /* @__PURE__ */ t(() => {
	Cn.forEach((e) => {
		e();
	}), Cn = [];
}, "attachFunctions"), Tn = /* @__PURE__ */ t((e) => e.replace(/^\s*%%(?!{)[^\n]+\n?/gm, "").trimStart(), "cleanupComments");
function En(e) {
	var t;
	let n = e.match(v);
	if (!n) return {
		text: e,
		metadata: {}
	};
	let r = n[1], i = (t = de(r ? n[2].split("\n").map((e) => e.startsWith(r) ? e.slice(r.length) : e).join("\n") : n[2], { schema: fe })) == null ? {} : t;
	i = typeof i == "object" && !Array.isArray(i) ? i : {};
	let a = {};
	return i.displayMode && (a.displayMode = i.displayMode.toString()), i.title && (a.title = i.title.toString()), i.config && (a.config = i.config), {
		text: e.slice(n[0].length),
		metadata: a
	};
}
t(En, "extractFrontMatter");
var Dn = /* @__PURE__ */ t((e) => e.replace(/\r\n?/g, "\n").replace(/<(\w+)([^>]*)>/g, (e, t, n) => "<" + t + n.replace(/="([^"]*)"/g, "='$1'") + ">"), "cleanupText"), On = /* @__PURE__ */ t((e) => {
	let { text: t, metadata: n } = En(e), { displayMode: r, title: i, config: a = {} } = n;
	return r && (a.gantt || (a.gantt = {}), a.gantt.displayMode = r), {
		title: i,
		config: a,
		text: t
	};
}, "processFrontmatter"), kn = /* @__PURE__ */ t((e) => {
	var t;
	let n = (t = A.detectInit(e)) == null ? {} : t, r = A.detectDirective(e, "wrap");
	return Array.isArray(r) ? n.wrap = r.some(({ type: e }) => e === "wrap") : (r == null ? void 0 : r.type) === "wrap" && (n.wrap = !0), {
		text: re(e),
		directive: n
	};
}, "processDirectives");
function An(e) {
	let t = On(Dn(e)), n = kn(t.text), r = j(t.config, n.directive);
	return e = Tn(n.text), {
		code: e,
		title: t.title,
		config: r
	};
}
t(An, "preprocessDiagram");
function jn(e) {
	let t = new TextEncoder().encode(e), n = Array.from(t, (e) => String.fromCodePoint(e)).join("");
	return btoa(n);
}
t(jn, "toBase64");
var Mn = 5e4, Nn = "graph TB;a[Maximum text size in diagram exceeded];style a fill:#faa", Pn = "sandbox", Fn = "loose", In = "http://www.w3.org/2000/svg", Ln = "http://www.w3.org/1999/xlink", Rn = "http://www.w3.org/1999/xhtml", zn = "100%", Bn = "100%", Vn = "border:0;margin:0;", Hn = "margin:0", Un = "allow-top-navigation-by-user-activation allow-popups", Wn = "The \"iframe\" tag is not supported by your browser.", Gn = ["foreignobject"], Kn = ["dominant-baseline"];
function qn(e) {
	var t;
	let n = An(e);
	return s(), w((t = n.config) == null ? {} : t), n;
}
t(qn, "processAndSetConfigs");
async function Jn(e, t) {
	X();
	try {
		let { code: t, config: n } = qn(e);
		return {
			diagramType: (await or(t)).type,
			config: n
		};
	} catch (e) {
		if (t != null && t.suppressErrors) return !1;
		throw e;
	}
}
t(Jn, "parse");
var Yn = /* @__PURE__ */ t((e, t, n = []) => `.${e} ${t} ${c(`{ ${n.join(" !important; ")} !important; }`)}`, "cssImportantStyles"), Xn = /* @__PURE__ */ t((e, t = /* @__PURE__ */ new Map()) => {
	let n = new CSSStyleSheet();
	if (e.fontFamily !== void 0 && n.insertRule(`:root { --mermaid-font-family: ${e.fontFamily}}`, n.cssRules.length), e.altFontFamily !== void 0 && n.insertRule(`:root { --mermaid-alt-font-family: ${e.altFontFamily}}`, n.cssRules.length), t instanceof Map) {
		let r = p(e) ? ["> *", "span"] : [
			"rect",
			"polygon",
			"ellipse",
			"circle",
			"path"
		];
		t.forEach((e) => {
			me(e.styles) || r.forEach((t) => {
				n.insertRule(Yn(e.id, t, e.styles), n.cssRules.length);
			}), me(e.textStyles) || n.insertRule(Yn(e.id, "tspan", ((e == null ? void 0 : e.textStyles) || []).map((e) => e.replace("color", "fill"))), n.cssRules.length);
		});
	}
	let r = "";
	if (e.themeCSS !== void 0) if (typeof n.replaceSync == "function") {
		let t = new CSSStyleSheet();
		t.replaceSync(e.themeCSS), r = S(t) + "\n";
	} else r += `${e.themeCSS}
`;
	return r + S(n);
}, "createCssStyles"), Zn = /* @__PURE__ */ t((e, r) => Ve(Le(`${e}{${r}}`), Ue([/* @__PURE__ */ t(function(t, r, i, a) {
	if (t.type === "rule" && Array.isArray(t.props)) {
		if (t.parent && t.parent.type === "@keyframes") return;
		t.props = t.props.map((t) => t.startsWith(e) ? t : `${e} ${t}`);
	} else t.type.startsWith("@") && ([
		"@media",
		"@supports",
		"@layer",
		"@scope",
		"@container",
		"@starting-style",
		"@keyframes"
	].includes(t.type) || (n.warn(`Removing unsupported at-rule ${t.type} from CSS`), t.type = he));
}, "addNamespace"), He])), "compileCSS"), Qn = /* @__PURE__ */ t((e, t, n, r) => Zn(r, g(t, Xn(e, n), {
	...e.themeVariables,
	theme: e.theme,
	look: e.look
}, r)), "createUserStyles"), $n = /* @__PURE__ */ t((e = "", t, n) => {
	let r = e;
	return !n && !t && (r = r.replace(/marker-end="url\([\d+./:=?A-Za-z-]*?#/g, "marker-end=\"url(#")), r = O(r), r = r.replace(/<br>/g, "<br/>"), r;
}, "cleanUpSvgCode"), er = /* @__PURE__ */ t((e = "", t) => {
	var n;
	return `<iframe style="width:${zn};height:${!(t == null || (n = t.viewBox) == null || (n = n.baseVal) == null) && n.height ? t.viewBox.baseVal.height + "px" : Bn};${Vn}" src="data:text/html;charset=UTF-8;base64,${jn(`<body style="${Hn}">${e}</body>`)}" sandbox="${Un}">
  ${Wn}
</iframe>`;
}, "putIntoIFrame"), tr = /* @__PURE__ */ t((e, t, n, r, i) => {
	let a = e.append("div");
	a.attr("id", n), r && a.attr("style", r);
	let o = a.append("svg").attr("id", t).attr("width", "100%").attr("xmlns", In);
	return i && o.attr("xmlns:xlink", i), o.append("g"), e;
}, "appendDivSvgG");
function nr(e, t) {
	return e.append("iframe").attr("id", t).attr("style", "width: 100%; height: 100%;").attr("sandbox", "");
}
t(nr, "sandboxedIframe");
var rr = /* @__PURE__ */ t((e, t, n, r) => {
	var i, a, o;
	(i = e.getElementById(t)) == null || i.remove(), (a = e.getElementById(n)) == null || a.remove(), (o = e.getElementById(r)) == null || o.remove();
}, "removeExistingElements"), ir = /* @__PURE__ */ t(async function(r, a, o) {
	var s, c, l, u, d, f, p;
	X();
	let m = qn(a);
	a = m.code;
	let h = y();
	n.debug(h), a.length > ((s = h == null ? void 0 : h.maxTextSize) == null ? Mn : s) && (a = Nn);
	let g = `#${r}`, _ = "i" + r, v = "#" + _, b = "d" + r, S = "#" + b, C = /* @__PURE__ */ t(() => {
		let e = i(T ? v : S).node();
		e && "remove" in e && e.remove();
	}, "removeTempElements"), w = i(document.body), T = h.securityLevel === Pn, ee = h.securityLevel === Fn, te = h.fontFamily;
	o === void 0 ? (rr(document, r, b, _), T ? (w = i(nr(i(document.body), _).nodes()[0].contentDocument.body), w.node().style.margin = "0") : w = i("body"), tr(w, r, b)) : (o && (o.innerHTML = ""), T ? (w = i(nr(i(o), _).nodes()[0].contentDocument.body), w.node().style.margin = "0") : w = i(o), tr(w, r, b, `font-family: ${te}`, Ln));
	let E, D;
	try {
		E = await Sn.fromText(a, { title: m.title });
	} catch (e) {
		if (h.suppressErrorRendering) throw C(), e;
		E = await Sn.fromText("error"), D = e;
	}
	let ne = w.select(S).node(), O = E.type, k = ne.firstChild, A = k.firstChild, re = Qn(h, O, (c = (l = E.renderer).getClasses) == null ? void 0 : c.call(l, a, E), g), j = document.createElement("style");
	j.innerHTML = re, k.insertBefore(j, A);
	try {
		await E.renderer.draw(a, r, "11.16.0", E);
	} catch (e) {
		throw h.suppressErrorRendering ? C() : Ot.draw(a, r, "11.16.0"), e;
	}
	sr(O, w.select(`${S} svg`), (u = (d = E.db).getAccTitle) == null ? void 0 : u.call(d), (f = (p = E.db).getAccDescription) == null ? void 0 : f.call(p)), w.select(`[id="${r}"]`).selectAll("foreignobject > *").attr("xmlns", Rn);
	let M = w.select(S).node().innerHTML;
	if (n.debug("config.arrowMarkerAbsolute", h.arrowMarkerAbsolute), M = $n(M, T, x(h.arrowMarkerAbsolute)), T) {
		let e = w.select(S + " svg").node();
		M = er(M, e);
	} else ee || (M = e.sanitize(M, {
		ADD_TAGS: Gn,
		ADD_ATTR: Kn,
		HTML_INTEGRATION_POINTS: { foreignobject: !0 }
	}));
	if (wn(), D) throw D;
	return C(), {
		diagramType: O,
		svg: M,
		bindFunctions: E.db.bindFunctions
	};
}, "render");
function ar(e = {}) {
	var t;
	let n = te({}, e);
	n != null && n.fontFamily && !((t = n.themeVariables) != null && t.fontFamily) && (n.themeVariables || (n.themeVariables = {}), n.themeVariables.fontFamily = n.fontFamily), m(n), n != null && n.theme && n.theme in _ ? n.themeVariables = _[n.theme].getThemeVariables(n.themeVariables) : n && (n.themeVariables = _.default.getThemeVariables(n.themeVariables)), r((typeof n == "object" ? ee(n) : o()).logLevel), X();
}
t(ar, "initialize");
var or = /* @__PURE__ */ t((e, t = {}) => {
	let { code: n } = An(e);
	return Sn.fromText(n, t);
}, "getDiagramFromText");
function sr(e, t, n, r) {
	bn(t, e), xn(t, n, r, t.attr("id"));
}
t(sr, "addA11yInfo");
var Z = Object.freeze({
	render: ir,
	parse: Jn,
	getDiagramFromText: or,
	initialize: ar,
	getConfig: y,
	setConfig: h,
	getSiteConfig: o,
	updateSiteConfig: d,
	reset: /* @__PURE__ */ t(() => {
		s();
	}, "reset"),
	globalReset: /* @__PURE__ */ t(() => {
		s(D);
	}, "globalReset"),
	defaultConfig: D
});
r(y().logLevel), s(y());
var cr = /* @__PURE__ */ t((e, t, r) => {
	n.warn(e), k(e) ? (r && r(e.str, e.hash), t.push({
		...e,
		message: e.str,
		error: e
	})) : (r && r(e), e instanceof Error && t.push({
		str: e.message,
		message: e.message,
		hash: e.name,
		error: e
	}));
}, "handleError"), lr = /* @__PURE__ */ t(async function(e = { querySelector: ".mermaid" }) {
	try {
		await ur(e);
	} catch (t) {
		if (k(t) && n.error(t.str), $.parseError && $.parseError(t), !e.suppressErrors) throw n.error("Use the suppressErrors option to suppress these errors"), t;
	}
}, "run"), ur = /* @__PURE__ */ t(async function({ postRenderCallback: e, querySelector: t, nodes: r } = { querySelector: ".mermaid" }) {
	let i = Z.getConfig();
	n.debug(`${e ? "" : "No "}Callback function found`);
	let a;
	if (r) a = r;
	else if (t) a = document.querySelectorAll(t);
	else throw Error("Nodes and querySelector are both undefined");
	n.debug(`Found ${a.length} diagrams`), (i == null ? void 0 : i.startOnLoad) !== void 0 && (n.debug("Start On Load: " + (i == null ? void 0 : i.startOnLoad)), Z.updateSiteConfig({ startOnLoad: i == null ? void 0 : i.startOnLoad }));
	let o = new A.InitIDGenerator(i.deterministicIds, i.deterministicIDSeed), s, c = [];
	for (let t of Array.from(a)) {
		if (n.info("Rendering diagram: " + t.id), t.getAttribute("data-processed")) continue;
		t.setAttribute("data-processed", "true");
		let r = `mermaid-${o.next()}`;
		s = t.innerHTML, s = le(A.entityDecode(s)).trim().replace(/<br\s*\/?>/gi, "<br/>");
		let i = A.detectInit(s);
		i && n.debug("Detected early reinit: ", i);
		try {
			let { svg: n, bindFunctions: i } = await yr(r, s, t);
			t.innerHTML = n, e && await e(r), i && i(t);
		} catch (e) {
			cr(e, c, $.parseError);
		}
	}
	if (c.length > 0) throw c[0];
}, "runThrowsErrors"), dr = /* @__PURE__ */ t(function(e) {
	Z.initialize(e);
}, "initialize"), fr = /* @__PURE__ */ t(async function(e, t, r) {
	n.warn("mermaid.init is deprecated. Please use run instead."), e && dr(e);
	let i = {
		postRenderCallback: r,
		querySelector: ".mermaid"
	};
	typeof t == "string" ? i.querySelector = t : t && (t instanceof HTMLElement ? i.nodes = [t] : i.nodes = t), await lr(i);
}, "init"), pr = /* @__PURE__ */ t(async (e, { lazyLoad: t = !0 } = {}) => {
	X(), u(...e), t === !1 && await vn();
}, "registerExternalDiagrams"), mr = /* @__PURE__ */ t(function() {
	if ($.startOnLoad) {
		let { startOnLoad: e } = Z.getConfig();
		e && $.run().catch((e) => n.error("Mermaid failed to initialize", e));
	}
}, "contentLoaded");
typeof document < "u" && window.addEventListener("load", mr, !1);
var hr = /* @__PURE__ */ t(function(e) {
	$.parseError = e;
}, "setParseErrorHandler"), Q = [], gr = !1, _r = /* @__PURE__ */ t(async () => {
	if (!gr) {
		for (gr = !0; Q.length > 0;) {
			let e = Q.shift();
			if (e) try {
				await e();
			} catch (e) {
				n.error("Error executing queue", e);
			}
		}
		gr = !1;
	}
}, "executeQueue"), vr = /* @__PURE__ */ t(async (e, r) => new Promise((i, a) => {
	let o = /* @__PURE__ */ t(() => new Promise((t, o) => {
		Z.parse(e, r).then((e) => {
			t(e), i(e);
		}, (e) => {
			var t;
			n.error("Error parsing", e), (t = $.parseError) == null || t.call($, e), o(e), a(e);
		});
	}), "performCall");
	Q.push(o), _r().catch(a);
}), "parse"), yr = /* @__PURE__ */ t((e, r, i) => new Promise((a, o) => {
	let s = /* @__PURE__ */ t(() => new Promise((t, s) => {
		Z.render(e, r, i).then((e) => {
			t(e), a(e);
		}, (e) => {
			var t;
			n.error("Error parsing", e), (t = $.parseError) == null || t.call($, e), s(e), o(e);
		});
	}), "performCall");
	Q.push(s), _r().catch(o);
}), "render"), $ = {
	startOnLoad: !0,
	mermaidAPI: Z,
	parse: vr,
	render: yr,
	init: fr,
	run: lr,
	registerExternalDiagrams: pr,
	registerLayoutLoaders: ue,
	initialize: dr,
	parseError: void 0,
	contentLoaded: mr,
	setParseErrorHandler: hr,
	detectType: T,
	registerIconPacks: ce,
	getRegisteredDiagramsMetadata: /* @__PURE__ */ t(() => Object.keys(C).map((e) => ({ id: e })), "getRegisteredDiagramsMetadata")
}, br = $;
//#endregion
export { br as default };
