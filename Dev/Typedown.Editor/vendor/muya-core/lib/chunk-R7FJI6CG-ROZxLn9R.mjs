import { C as e, S as t, n, o as r, t as i, u as a, w as o, x as s, y as c } from "./chunk-KEIR6QF5-lXp_uFT8.mjs";
//#region node_modules/@mermaid-js/parser/dist/chunks/mermaid-parser.core/chunk-R7FJI6CG.mjs
var l, u, d, f = (l = class extends i {
	constructor() {
		super(["treemap"]);
	}
}, s(l, "TreemapTokenBuilder"), l), p = /classDef\s+([A-Z_a-z]\w+)(?:\s+([^\n\r;]*))?;?/, m = (u = class extends n {
	runCustomConverter(e, t, n) {
		if (e.name === "NUMBER2") return parseFloat(t.replace(/,/g, ""));
		if (e.name === "SEPARATOR" || e.name === "STRING2") return t.substring(1, t.length - 1);
		if (e.name === "INDENTATION") return t.length;
		if (e.name === "ClassDef") {
			if (typeof t != "string") return t;
			let e = p.exec(t);
			if (e) return {
				$type: "ClassDefStatement",
				className: e[1],
				styleText: e[2] || void 0
			};
		}
	}
}, s(u, "TreemapValueConverter"), u);
function h(e) {
	let t = e.validation.TreemapValidator, n = e.validation.ValidationRegistry;
	if (n) {
		let e = { Treemap: t.checkSingleRoot.bind(t) };
		n.register(e, t);
	}
}
s(h, "registerValidationChecks");
var g = (d = class {
	checkSingleRoot(e, t) {
		let n;
		for (let r of e.TreemapRows) r.item && (n === void 0 && r.indent === void 0 ? n = 0 : (r.indent === void 0 || n !== void 0 && n >= parseInt(r.indent, 10)) && t("error", "Multiple root nodes are not allowed in a treemap.", {
			node: r,
			property: "item"
		}));
	}
}, s(d, "TreemapValidator"), d), _ = {
	parser: {
		TokenBuilder: /* @__PURE__ */ s(() => new f(), "TokenBuilder"),
		ValueConverter: /* @__PURE__ */ s(() => new m(), "ValueConverter")
	},
	validation: { TreemapValidator: /* @__PURE__ */ s(() => new g(), "TreemapValidator") }
};
function v(n = r) {
	let i = o(e(n), a), s = o(t({ shared: i }), c, _);
	return i.ServiceRegistry.register(s), h(s), {
		shared: i,
		Treemap: s
	};
}
s(v, "createTreemapServices");
//#endregion
export { v as n, _ as t };
