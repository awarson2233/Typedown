import { x as e } from "./chunk-KEIR6QF5-lXp_uFT8.mjs";
import "./chunk-MOZMSUNE-NslE-00e.mjs";
import "./chunk-OSBZ3O6U-CJZJD9L9.mjs";
import "./chunk-5JV3BV7I-BFbbZ0Nc.mjs";
import "./chunk-CYSBUYHQ-DqytT7PH.mjs";
import "./chunk-BIQX33UG-BE440lWx.mjs";
import "./chunk-EMLP6XTP-BHIwFJEC.mjs";
import "./chunk-YOTPTUD7-C5Mm7qDx.mjs";
import "./chunk-QBLGF6JB-uOF7wzn4.mjs";
import "./chunk-5TONJI2A-Lc0KTx1u.mjs";
import "./chunk-5HE753X5-CJhmzr4m.mjs";
import "./chunk-U6XO7XAA-BceJbjIt.mjs";
import "./chunk-JG7HCLWE-CR_rylZZ.mjs";
import "./chunk-CQNSW5MT-DlMUAAQR.mjs";
import "./chunk-R7FJI6CG-ROZxLn9R.mjs";
import "./chunk-5FCAYU7R-C4INqxnh.mjs";
//#region node_modules/@mermaid-js/parser/dist/mermaid-parser.core.mjs
var t, n = {}, r = {
	info: /* @__PURE__ */ e(async () => {
		let { createInfoServices: e } = await import("./info-DKCQHKI2-lo3oh-qy.mjs");
		n.info = e().Info.parser.LangiumParser;
	}, "info"),
	packet: /* @__PURE__ */ e(async () => {
		let { createPacketServices: e } = await import("./packet-7NZHBO7P-BX-opvtt.mjs");
		n.packet = e().Packet.parser.LangiumParser;
	}, "packet"),
	pie: /* @__PURE__ */ e(async () => {
		let { createPieServices: e } = await import("./pie-RZYD4A2V-Cr0BkhrT.mjs");
		n.pie = e().Pie.parser.LangiumParser;
	}, "pie"),
	treeView: /* @__PURE__ */ e(async () => {
		let { createTreeViewServices: e } = await import("./treeView-QDETBFTQ-ZmHa_jFz.mjs");
		n.treeView = e().TreeView.parser.LangiumParser;
	}, "treeView"),
	architecture: /* @__PURE__ */ e(async () => {
		let { createArchitectureServices: e } = await import("./architecture-TIHT7OUA-C4erIn6k.mjs");
		n.architecture = e().Architecture.parser.LangiumParser;
	}, "architecture"),
	gitGraph: /* @__PURE__ */ e(async () => {
		let { createGitGraphServices: e } = await import("./gitGraph-TEB2WS4Q-Ds0-MnKA.mjs");
		n.gitGraph = e().GitGraph.parser.LangiumParser;
	}, "gitGraph"),
	eventmodeling: /* @__PURE__ */ e(async () => {
		let { createEventModelingServices: e } = await import("./eventmodeling-45OFAUF4-Dh87CG4t.mjs");
		n.eventmodeling = e().EventModel.parser.LangiumParser;
	}, "eventmodeling"),
	radar: /* @__PURE__ */ e(async () => {
		let { createRadarServices: e } = await import("./radar-I7S5WNFK-DQS3UivK.mjs");
		n.radar = e().Radar.parser.LangiumParser;
	}, "radar"),
	railroad: /* @__PURE__ */ e(async () => {
		let { createRailroadServices: e } = await import("./railroad-3IZDKUUU-CTi-VPqx.mjs");
		n.railroad = e().Railroad.parser.LangiumParser;
	}, "railroad"),
	railroadEbnf: /* @__PURE__ */ e(async () => {
		let { createRailroadEbnfServices: e } = await import("./railroad-ebnf-EBAXGLYW-01Fhc8H_.mjs");
		n.railroadEbnf = e().RailroadEbnf.parser.LangiumParser;
	}, "railroadEbnf"),
	railroadAbnf: /* @__PURE__ */ e(async () => {
		let { createRailroadAbnfServices: e } = await import("./railroad-abnf-AHOZXSZD-DfWaa4_E.mjs");
		n.railroadAbnf = e().RailroadAbnf.parser.LangiumParser;
	}, "railroadAbnf"),
	railroadPeg: /* @__PURE__ */ e(async () => {
		let { createRailroadPegServices: e } = await import("./railroad-peg-LSFZ7HO6-DLgMR4ru.mjs");
		n.railroadPeg = e().RailroadPeg.parser.LangiumParser;
	}, "railroadPeg"),
	treemap: /* @__PURE__ */ e(async () => {
		let { createTreemapServices: e } = await import("./treemap-6X3UGDF4-DhdEc4DS.mjs");
		n.treemap = e().Treemap.parser.LangiumParser;
	}, "treemap"),
	wardley: /* @__PURE__ */ e(async () => {
		let { createWardleyServices: e } = await import("./wardley-OPB4EBWU-DczjKS4b.mjs");
		n.wardley = e().Wardley.parser.LangiumParser;
	}, "wardley"),
	cynefin: /* @__PURE__ */ e(async () => {
		let { createCynefinServices: e } = await import("./cynefin-VYW2F7L2-Br82ujNJ.mjs");
		n.cynefin = e().Cynefin.parser.LangiumParser;
	}, "cynefin")
};
async function i(e, t) {
	let i = r[e];
	if (!i) throw Error(`Unknown diagram type: ${e}`);
	n[e] || await i();
	let o = n[e].parse(t);
	if (o.lexerErrors.length > 0 || o.parserErrors.length > 0) throw new a(o);
	return o.value;
}
e(i, "parse");
var a = (t = class extends Error {
	constructor(e) {
		let t = e.lexerErrors.map((e) => `Lexer error on line ${e.line !== void 0 && !isNaN(e.line) ? e.line : "?"}, column ${e.column !== void 0 && !isNaN(e.column) ? e.column : "?"}: ${e.message}`).join("\n"), n = e.parserErrors.map((e) => `Parse error on line ${e.token.startLine !== void 0 && !isNaN(e.token.startLine) ? e.token.startLine : "?"}, column ${e.token.startColumn !== void 0 && !isNaN(e.token.startColumn) ? e.token.startColumn : "?"}: ${e.message}`).join("\n");
		super(`Parsing failed: ${t} ${n}`), this.result = e;
	}
}, e(t, "MermaidParseError"), t);
//#endregion
export { i as n, a as t };
