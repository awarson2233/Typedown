const e=require("./chunk-Y2CYZVJY-CnmLqFtv.js"),t=require("./src-KoTOUvXq.js"),n=require("./chunk-WYO6CB5R-DqMhWelG.js"),r=require("./chunk-ICXQ74PX-LEVw6uKk.js"),i=require("./chunk-VAUOI2AC-DI8bspjT.js"),a=require("./chunk-JWPE2WC7-DDuoFcgR.js"),o=require("./mermaid-parser.core-sNVg_8v2.js");var s,c=n.f.packet,l=(s=class{constructor(){this.packet=[],this.setAccTitle=n.U,this.getAccTitle=n.y,this.setDiagramTitle=n.K,this.getDiagramTitle=n.w,this.getAccDescription=n.v,this.setAccDescription=n.H}getConfig(){let e=r.i({...c,...n.b().packet});return e.showBits&&(e.paddingY+=10),e}getPacket(){return this.packet}pushWord(e){e.length>0&&this.packet.push(e)}clear(){n.a(),this.packet=[]}},e.n(s,`PacketDB`),s),u=1e4,d=e.n((e,n)=>{a.t(e,n);let r=-1,i=[],o=1,{bitsPerRow:s}=n.getConfig();for(let{start:a,end:d,bits:p,label:m}of e.blocks){var c;if(a!==void 0&&d!==void 0&&d<a)throw Error(`Packet block ${a} - ${d} is invalid. End must be greater than start.`);if(a!=null||(a=r+1),a!==r+1){var l;throw Error(`Packet block ${a} - ${(l=d)==null?a:l} is not contiguous. It should start from ${r+1}.`)}if(p===0)throw Error(`Packet block ${a} is invalid. Cannot have a zero bit field.`);for(d!=null||(d=a+((c=p)==null?1:c)-1),p!=null||(p=d-a+1),r=d,t.n.debug(`Packet block ${a} - ${r} with label ${m}`);i.length<=s+1&&n.getPacket().length<u;){let[e,t]=f({start:a,end:d,bits:p,label:m},o,s);if(i.push(e),e.end+1===o*s&&(n.pushWord(i),i=[],o++),!t)break;({start:a,end:d,bits:p,label:m}=t)}}n.pushWord(i)},`populate`),f=e.n((e,t,n)=>{if(e.start===void 0)throw Error(`start should have been set during first phase`);if(e.end===void 0)throw Error(`end should have been set during first phase`);if(e.start>e.end)throw Error(`Block start ${e.start} is greater than block end ${e.end}.`);if(e.end+1<=t*n)return[e,void 0];let r=t*n-1,i=t*n;return[{start:e.start,end:r,label:e.label,bits:r-e.start},{start:i,end:e.end,label:e.label,bits:e.end-i}]},`getNextFittingBlock`),p={parser:{yy:void 0},parse:e.n(async e=>{var n;let r=await o.n(`packet`,e),i=(n=p.parser)==null?void 0:n.yy;if(!(i instanceof l))throw Error(`parser.parser?.yy was not a PacketDB. This is due to a bug within Mermaid, please report this issue at https://github.com/mermaid-js/mermaid/issues.`);t.n.debug(r),d(r,i)},`parse`)},m=e.n((e,t,r,a)=>{let o=a.db,s=o.getConfig(),{rowHeight:c,paddingY:l,bitWidth:u,bitsPerRow:d}=s,f=o.getPacket(),p=o.getDiagramTitle(),m=c+l,g=m*(f.length+1)-(p?0:c),_=u*d+2,v=i.t(t);v.attr(`viewBox`,`0 0 ${_} ${g}`),n.c(v,g,_,s.useMaxWidth);for(let[e,t]of f.entries())h(v,t,e,s);v.append(`text`).text(p).attr(`x`,_/2).attr(`y`,g-m/2).attr(`dominant-baseline`,`middle`).attr(`text-anchor`,`middle`).attr(`class`,`packetTitle`)},`draw`),h=e.n((e,t,n,{rowHeight:r,paddingX:i,paddingY:a,bitWidth:o,bitsPerRow:s,showBits:c})=>{let l=e.append(`g`),u=n*(r+a)+a;for(let e of t){let t=e.start%s*o+1,n=(e.end-e.start+1)*o-i;if(l.append(`rect`).attr(`x`,t).attr(`y`,u).attr(`width`,n).attr(`height`,r).attr(`class`,`packetBlock`),l.append(`text`).attr(`x`,t+n/2).attr(`y`,u+r/2).attr(`class`,`packetLabel`).attr(`dominant-baseline`,`middle`).attr(`text-anchor`,`middle`).text(e.label),!c)continue;let a=e.end===e.start,d=u-2;l.append(`text`).attr(`x`,t+(a?n/2:0)).attr(`y`,d).attr(`class`,`packetByte start`).attr(`dominant-baseline`,`auto`).attr(`text-anchor`,a?`middle`:`start`).text(e.start),a||l.append(`text`).attr(`x`,t+n).attr(`y`,d).attr(`class`,`packetByte end`).attr(`dominant-baseline`,`auto`).attr(`text-anchor`,`end`).text(e.end)}},`drawWord`),g={draw:m},_={byteFontSize:`10px`,startByteColor:`black`,endByteColor:`black`,labelColor:`black`,labelFontSize:`12px`,titleColor:`black`,titleFontSize:`14px`,blockStrokeColor:`black`,blockStrokeWidth:`1`,blockFillColor:`#efefef`},v={parser:p,get db(){return new l},renderer:g,styles:e.n(({packet:e}={})=>{let t=r.i(_,e);return`
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
	`},`styles`)};exports.diagram=v;