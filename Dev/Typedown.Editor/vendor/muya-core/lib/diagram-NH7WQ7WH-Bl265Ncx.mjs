import { n as e } from "./chunk-Y2CYZVJY-i11wjrBe.mjs";
import { n as t } from "./src-upoOno_g.mjs";
import { H as n, K as r, U as i, a, b as o, c as s, f as c, v as l, w as u, y as d } from "./chunk-WYO6CB5R-_U4dPty4.mjs";
import { i as f } from "./chunk-ICXQ74PX-5xZ0PKaW.mjs";
import { t as p } from "./chunk-VAUOI2AC-DvRR_ttS.mjs";
import { t as m } from "./chunk-JWPE2WC7-PoIEzJPT.mjs";
import { n as h } from "./mermaid-parser.core-Dnt89Q92.mjs";
//#region node_modules/mermaid/dist/chunks/mermaid.core/diagram-NH7WQ7WH.mjs
var g, _ = c.packet, v = (g = class {
	constructor() {
		this.packet = [], this.setAccTitle = i, this.getAccTitle = d, this.setDiagramTitle = r, this.getDiagramTitle = u, this.getAccDescription = l, this.setAccDescription = n;
	}
	getConfig() {
		let e = f({
			..._,
			...o().packet
		});
		return e.showBits && (e.paddingY += 10), e;
	}
	getPacket() {
		return this.packet;
	}
	pushWord(e) {
		e.length > 0 && this.packet.push(e);
	}
	clear() {
		a(), this.packet = [];
	}
}, e(g, "PacketDB"), g), y = 1e4, b = /* @__PURE__ */ e((e, n) => {
	m(e, n);
	let r = -1, i = [], a = 1, { bitsPerRow: o } = n.getConfig();
	for (let { start: l, end: u, bits: d, label: f } of e.blocks) {
		var s;
		if (l !== void 0 && u !== void 0 && u < l) throw Error(`Packet block ${l} - ${u} is invalid. End must be greater than start.`);
		if (l != null || (l = r + 1), l !== r + 1) {
			var c;
			throw Error(`Packet block ${l} - ${(c = u) == null ? l : c} is not contiguous. It should start from ${r + 1}.`);
		}
		if (d === 0) throw Error(`Packet block ${l} is invalid. Cannot have a zero bit field.`);
		for (u != null || (u = l + ((s = d) == null ? 1 : s) - 1), d != null || (d = u - l + 1), r = u, t.debug(`Packet block ${l} - ${r} with label ${f}`); i.length <= o + 1 && n.getPacket().length < y;) {
			let [e, t] = x({
				start: l,
				end: u,
				bits: d,
				label: f
			}, a, o);
			if (i.push(e), e.end + 1 === a * o && (n.pushWord(i), i = [], a++), !t) break;
			({start: l, end: u, bits: d, label: f} = t);
		}
	}
	n.pushWord(i);
}, "populate"), x = /* @__PURE__ */ e((e, t, n) => {
	if (e.start === void 0) throw Error("start should have been set during first phase");
	if (e.end === void 0) throw Error("end should have been set during first phase");
	if (e.start > e.end) throw Error(`Block start ${e.start} is greater than block end ${e.end}.`);
	if (e.end + 1 <= t * n) return [e, void 0];
	let r = t * n - 1, i = t * n;
	return [{
		start: e.start,
		end: r,
		label: e.label,
		bits: r - e.start
	}, {
		start: i,
		end: e.end,
		label: e.label,
		bits: e.end - i
	}];
}, "getNextFittingBlock"), S = {
	parser: { yy: void 0 },
	parse: /* @__PURE__ */ e(async (e) => {
		var n;
		let r = await h("packet", e), i = (n = S.parser) == null ? void 0 : n.yy;
		if (!(i instanceof v)) throw Error("parser.parser?.yy was not a PacketDB. This is due to a bug within Mermaid, please report this issue at https://github.com/mermaid-js/mermaid/issues.");
		t.debug(r), b(r, i);
	}, "parse")
}, C = /* @__PURE__ */ e((e, t, n, r) => {
	let i = r.db, a = i.getConfig(), { rowHeight: o, paddingY: c, bitWidth: l, bitsPerRow: u } = a, d = i.getPacket(), f = i.getDiagramTitle(), m = o + c, h = m * (d.length + 1) - (f ? 0 : o), g = l * u + 2, _ = p(t);
	_.attr("viewBox", `0 0 ${g} ${h}`), s(_, h, g, a.useMaxWidth);
	for (let [e, t] of d.entries()) w(_, t, e, a);
	_.append("text").text(f).attr("x", g / 2).attr("y", h - m / 2).attr("dominant-baseline", "middle").attr("text-anchor", "middle").attr("class", "packetTitle");
}, "draw"), w = /* @__PURE__ */ e((e, t, n, { rowHeight: r, paddingX: i, paddingY: a, bitWidth: o, bitsPerRow: s, showBits: c }) => {
	let l = e.append("g"), u = n * (r + a) + a;
	for (let e of t) {
		let t = e.start % s * o + 1, n = (e.end - e.start + 1) * o - i;
		if (l.append("rect").attr("x", t).attr("y", u).attr("width", n).attr("height", r).attr("class", "packetBlock"), l.append("text").attr("x", t + n / 2).attr("y", u + r / 2).attr("class", "packetLabel").attr("dominant-baseline", "middle").attr("text-anchor", "middle").text(e.label), !c) continue;
		let a = e.end === e.start, d = u - 2;
		l.append("text").attr("x", t + (a ? n / 2 : 0)).attr("y", d).attr("class", "packetByte start").attr("dominant-baseline", "auto").attr("text-anchor", a ? "middle" : "start").text(e.start), a || l.append("text").attr("x", t + n).attr("y", d).attr("class", "packetByte end").attr("dominant-baseline", "auto").attr("text-anchor", "end").text(e.end);
	}
}, "drawWord"), T = { draw: C }, E = {
	byteFontSize: "10px",
	startByteColor: "black",
	endByteColor: "black",
	labelColor: "black",
	labelFontSize: "12px",
	titleColor: "black",
	titleFontSize: "14px",
	blockStrokeColor: "black",
	blockStrokeWidth: "1",
	blockFillColor: "#efefef"
}, D = {
	parser: S,
	get db() {
		return new v();
	},
	renderer: T,
	styles: /* @__PURE__ */ e(({ packet: e } = {}) => {
		let t = f(E, e);
		return `
	.packetByte {
		font-size: ${t.byteFontSize};
	}
	.packetByte.start {
		fill: ${t.startByteColor};
	}
	.packetByte.end {
		fill: ${t.endByteColor};
	}
	.packetLabel {
		fill: ${t.labelColor};
		font-size: ${t.labelFontSize};
	}
	.packetTitle {
		fill: ${t.titleColor};
		font-size: ${t.titleFontSize};
	}
	.packetBlock {
		stroke: ${t.blockStrokeColor};
		stroke-width: ${t.blockStrokeWidth};
		fill: ${t.blockFillColor};
	}
	`;
	}, "styles")
};
//#endregion
export { D as diagram };
