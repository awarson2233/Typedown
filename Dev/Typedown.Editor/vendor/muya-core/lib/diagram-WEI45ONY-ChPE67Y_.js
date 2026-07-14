const e=require("./chunk-Y2CYZVJY-CnmLqFtv.js"),t=require("./src-KoTOUvXq.js"),n=require("./chunk-WYO6CB5R-DqMhWelG.js"),r=require("./chunk-ICXQ74PX-LEVw6uKk.js"),i=require("./chunk-VAUOI2AC-DI8bspjT.js"),a=require("./chunk-JWPE2WC7-DDuoFcgR.js"),o=require("./mermaid-parser.core-sNVg_8v2.js");var s={showLegend:!0,ticks:5,max:null,min:0,graticule:`circle`},c={axes:[],curves:[],options:s},l=structuredClone(c),u=n.f.radar,d=e.n(()=>r.i({...u,...n.b().radar}),`getConfig`),f=e.n(()=>l.axes,`getAxes`),p=e.n(()=>l.curves,`getCurves`),m=e.n(()=>l.options,`getOptions`),h=e.n(e=>{l.axes=e.map(e=>{var t;return{name:e.name,label:(t=e.label)==null?e.name:t}})},`setAxes`),g=e.n(e=>{l.curves=e.map(e=>{var t;return{name:e.name,label:(t=e.label)==null?e.name:t,entries:_(e.entries)}})},`setCurves`),_=e.n(e=>{if(e[0].axis==null)return e.map(e=>e.value);let t=f();if(t.length===0)throw Error(`Axes must be populated before curves for reference entries`);return t.map(t=>{let n=e.find(e=>{var n;return((n=e.axis)==null?void 0:n.$refText)===t.name});if(n===void 0)throw Error(`Missing entry for axis `+t.label);return n.value})},`computeCurveEntries`),v={getAxes:f,getCurves:p,getOptions:m,setAxes:h,setCurves:g,setOptions:e.n(e=>{var t,n,r,i,a,o,c,u,d,f;let p=e.reduce((e,t)=>(e[t.name]=t,e),{});l.options={showLegend:(t=(n=p.showLegend)==null?void 0:n.value)==null?s.showLegend:t,ticks:(r=(i=p.ticks)==null?void 0:i.value)==null?s.ticks:r,max:(a=(o=p.max)==null?void 0:o.value)==null?s.max:a,min:(c=(u=p.min)==null?void 0:u.value)==null?s.min:c,graticule:(d=(f=p.graticule)==null?void 0:f.value)==null?s.graticule:d}},`setOptions`),getConfig:d,clear:e.n(()=>{n.a(),l=structuredClone(c)},`clear`),setAccTitle:n.U,getAccTitle:n.y,setDiagramTitle:n.K,getDiagramTitle:n.w,getAccDescription:n.v,setAccDescription:n.H},y=e.n(e=>{a.t(e,v);let{axes:t,curves:n,options:r}=e;v.setAxes(t),v.setCurves(n),v.setOptions(r)},`populate`),b={parse:e.n(async e=>{let n=await o.n(`radar`,e);t.n.debug(n),y(n)},`parse`)},x=e.n((e,t,n,r)=>{var a;let o=r.db,s=o.getAxes(),c=o.getCurves(),l=o.getOptions(),u=o.getConfig(),d=o.getDiagramTitle(),f=S(i.t(t),u),p=(a=l.max)==null?Math.max(...c.map(e=>Math.max(...e.entries))):a,m=l.min,h=Math.min(u.width,u.height)/2;C(f,s,h,l.ticks,l.graticule),w(f,s,h,u),T(f,s,c,m,p,l.graticule,u),O(f,c,l.showLegend,u),f.append(`text`).attr(`class`,`radarTitle`).text(d).attr(`x`,0).attr(`y`,-u.height/2-u.marginTop)},`draw`),S=e.n((e,t)=>{var r;let i=t.width+t.marginLeft+t.marginRight,a=t.height+t.marginTop+t.marginBottom,o={x:t.marginLeft+t.width/2,y:t.marginTop+t.height/2};return n.c(e,a,i,(r=t.useMaxWidth)==null||r),e.attr(`viewBox`,`0 0 ${i} ${a}`).attr(`overflow`,`visible`),e.append(`g`).attr(`transform`,`translate(${o.x}, ${o.y})`)},`drawFrame`),C=e.n((e,t,n,r,i)=>{if(i===`circle`)for(let t=0;t<r;t++){let i=n*(t+1)/r;e.append(`circle`).attr(`r`,i).attr(`class`,`radarGraticule`)}else if(i===`polygon`){let i=t.length;for(let a=0;a<r;a++){let o=n*(a+1)/r,s=t.map((e,t)=>{let n=2*t*Math.PI/i-Math.PI/2;return`${o*Math.cos(n)},${o*Math.sin(n)}`}).join(` `);e.append(`polygon`).attr(`points`,s).attr(`class`,`radarGraticule`)}}},`drawGraticule`),w=e.n((e,t,n,r)=>{let i=t.length;for(let a=0;a<i;a++){let o=t[a].label,s=2*a*Math.PI/i-Math.PI/2,c=Math.cos(s),l=Math.sin(s);e.append(`line`).attr(`x1`,0).attr(`y1`,0).attr(`x2`,n*r.axisScaleFactor*c).attr(`y2`,n*r.axisScaleFactor*l).attr(`class`,`radarAxisLine`);let u=c>.01?`start`:c<-.01?`end`:`middle`,d=l>.01?`hanging`:l<-.01?`auto`:`central`;e.append(`text`).text(o).attr(`x`,n*r.axisLabelFactor*c+4*c).attr(`y`,n*r.axisLabelFactor*l+4*l).attr(`text-anchor`,u).attr(`dominant-baseline`,d).attr(`class`,`radarAxisLabel`)}},`drawAxes`);function T(e,t,n,r,i,a,o){let s=t.length,c=Math.min(o.width,o.height)/2;n.forEach((t,n)=>{if(t.entries.length!==s)return;let l=t.entries.map((e,t)=>{let n=2*Math.PI*t/s-Math.PI/2,a=E(e,r,i,c);return{x:a*Math.cos(n),y:a*Math.sin(n)}});a===`circle`?e.append(`path`).attr(`d`,D(l,o.curveTension)).attr(`class`,`radarCurve-${n}`):a===`polygon`&&e.append(`polygon`).attr(`points`,l.map(e=>`${e.x},${e.y}`).join(` `)).attr(`class`,`radarCurve-${n}`)})}e.n(T,`drawCurves`);function E(e,t,n,r){return r*(Math.min(Math.max(e,t),n)-t)/(n-t)}e.n(E,`relativeRadius`);function D(e,t){let n=e.length,r=`M${e[0].x},${e[0].y}`;for(let i=0;i<n;i++){let a=e[(i-1+n)%n],o=e[i],s=e[(i+1)%n],c=e[(i+2)%n],l={x:o.x+(s.x-a.x)*t,y:o.y+(s.y-a.y)*t},u={x:s.x-(c.x-o.x)*t,y:s.y-(c.y-o.y)*t};r+=` C${l.x},${l.y} ${u.x},${u.y} ${s.x},${s.y}`}return`${r} Z`}e.n(D,`closedRoundCurve`);function O(e,t,n,r){if(!n)return;let i=(r.width/2+r.marginRight)*3/4,a=-(r.height/2+r.marginTop)*3/4;t.forEach((t,n)=>{let r=e.append(`g`).attr(`transform`,`translate(${i}, ${a+n*20})`);r.append(`rect`).attr(`width`,12).attr(`height`,12).attr(`class`,`radarLegendBox-${n}`),r.append(`text`).attr(`x`,16).attr(`y`,0).attr(`class`,`radarLegendText`).text(t.label)})}e.n(O,`drawLegend`);var k={draw:x},A=e.n((e,t)=>{let n=``;for(let r=0;r<e.THEME_COLOR_LIMIT;r++){let i=e[`cScale${r}`];n+=`
		.radarCurve-${r} {
			color: ${i};
			fill: ${i};
			fill-opacity: ${t.curveOpacity};
			stroke: ${i};
			stroke-width: ${t.curveStrokeWidth};
		}
		.radarLegendBox-${r} {
			fill: ${i};
			fill-opacity: ${t.curveOpacity};
			stroke: ${i};
		}
		`}return n},`genIndexStyles`),j=e.n(e=>{let t=r.i(n.D(),n.b().themeVariables);return{themeVariables:t,radarOptions:r.i(t.radar,e)}},`buildRadarStyleOptions`),M={parser:b,db:v,renderer:k,styles:e.n(({radar:e}={})=>{let{themeVariables:t,radarOptions:n}=j(e);return`
	.radarTitle {
		font-size: ${t.fontSize};
		color: ${t.titleColor};
		dominant-baseline: hanging;
		text-anchor: middle;
	}
	.radarAxisLine {
		stroke: ${n.axisColor};
		stroke-width: ${n.axisStrokeWidth};
	}
	.radarAxisLabel {
		font-size: ${n.axisLabelFontSize}px;
		color: ${n.axisColor};
	}
	.radarGraticule {
		fill: ${n.graticuleColor};
		fill-opacity: ${n.graticuleOpacity};
		stroke: ${n.graticuleColor};
		stroke-width: ${n.graticuleStrokeWidth};
	}
	.radarLegendText {
		text-anchor: start;
		font-size: ${n.legendFontSize}px;
		dominant-baseline: hanging;
	}
	${A(t,n)}
	`},`styles`)};exports.diagram=M;