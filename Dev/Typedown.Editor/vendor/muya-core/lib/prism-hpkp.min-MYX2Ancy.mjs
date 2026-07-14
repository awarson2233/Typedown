//#region node_modules/prismjs/components/prism-hpkp.min.js
Prism.languages.hpkp = {
	directive: {
		pattern: /\b(?:includeSubDomains|max-age|pin-sha256|preload|report-to|report-uri|strict)(?=[\s;=]|$)/i,
		alias: "property"
	},
	operator: /=/,
	punctuation: /;/
};
//#endregion
