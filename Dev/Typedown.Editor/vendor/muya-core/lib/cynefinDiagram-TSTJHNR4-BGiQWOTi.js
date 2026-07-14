const e=require("./chunk-Y2CYZVJY-CnmLqFtv.js"),t=require("./src-KoTOUvXq.js"),n=require("./chunk-WYO6CB5R-DqMhWelG.js"),r=require("./chunk-ICXQ74PX-LEVw6uKk.js"),i=require("./chunk-VAUOI2AC-DI8bspjT.js"),a=require("./chunk-JWPE2WC7-DDuoFcgR.js"),o=require("./mermaid-parser.core-sNVg_8v2.js");var s=e.n(()=>({domains:new Map,transitions:[]}),`createDefaultData`),c=s(),l={getDomains:e.n(()=>c.domains,`getDomains`),getTransitions:e.n(()=>c.transitions,`getTransitions`),setDomains:e.n(e=>{if(e)for(let n of e){var t;let e=n.domain,r=((t=n.items)==null?[]:t).map(e=>({label:e.label}));c.domains.set(e,{name:e,items:r})}},`setDomains`),setTransitions:e.n(e=>{e&&(c.transitions=e.filter(e=>e.from===e.to?(t.n.warn(`Cynefin: self-loop transition on domain "${e.from}" is not meaningful and will be skipped.`),!1):!0).map(e=>({from:e.from,to:e.to,label:e.label||void 0})))},`setTransitions`),getConfig:e.n(()=>r.i({...n.f.cynefin,...n.b().cynefin}),`getConfig`),clear:e.n(()=>{n.a(),c=s()},`clear`),setAccTitle:n.U,getAccTitle:n.y,setDiagramTitle:n.K,getDiagramTitle:n.w,getAccDescription:n.v,setAccDescription:n.H},u=e.n(e=>{a.t(e,l),l.setDomains(e.domains),l.setTransitions(e.transitions)},`populate`),d={parse:e.n(async e=>{let n=await o.n(`cynefin`,e);t.n.debug(n),u(n)},`parse`)};function f(e){let t=e+1831565813|0;return t=Math.imul(t^t>>>15,t|1),t^=t+Math.imul(t^t>>>7,t|61),((t^t>>>14)>>>0)/4294967296}e.n(f,`seededRandom`);function p(e){let t=0;for(let n=0;n<e.length;n++){let r=e.charCodeAt(n);t=(t<<5)-t+r,t|=0}return t}e.n(p,`hashString`);function m(e,t){return typeof e==`number`&&Number.isFinite(e)&&e!==0?e:p(t)}e.n(m,`resolveSeed`);function h(e,t,n,r){let i=e/2,a=r==null?e*.015:r,o=t/7,s=[];for(let e=0;e<=7;e++){let t=f(n+e*17)*a*2-a;s.push({x:i+t,y:e*o})}let c=`M${s[0].x},${s[0].y}`;for(let e=0;e<s.length-1;e++){let t=s[e],r=s[e+1],i=(t.y+r.y)/2,o=e%2==0?1:-1,l=a*1.5*o*f(n+e*31+7),u=t.x+l,d=i,p=r.x-l;c+=` C${u},${d} ${p},${i} ${r.x},${r.y}`}return c}e.n(h,`generateFoldPath`);function g(e,t,n,r){let i=t/2,a=r==null?t*.015:r,o=e/7,s=[];for(let e=0;e<=7;e++){let t=f(n+e*23)*a*2-a;s.push({x:e*o,y:i+t})}let c=`M${s[0].x},${s[0].y}`;for(let e=0;e<s.length-1;e++){let t=s[e],r=s[e+1],i=(t.x+r.x)/2,o=e%2==0?1:-1,l=a*1.5*o*f(n+e*37+11),u=i,d=t.y+l,p=i,m=r.y-l;c+=` C${u},${d} ${p},${m} ${r.x},${r.y}`}return c}e.n(g,`generateHorizontalBoundary`);function _(e,t){let n=e/2,r=t*.5,i=t,a=e*.03;return[`M${n},${r}`,`C${n+a},${r+(i-r)*.2}`,`${n-a*1.5},${r+(i-r)*.55}`,`${n+a*.5},${r+(i-r)*.75}`,`C${n-a},${r+(i-r)*.85}`,`${n+a*.3},${r+(i-r)*.95}`,`${n},${i}`].join(` `)}e.n(_,`generateCliffPath`);function v(e,t,n,r){return[`M${e-n},${t}`,`A${n},${r} 0 1,1 ${e+n},${t}`,`A${n},${r} 0 1,1 ${e-n},${t}`,`Z`].join(` `)}e.n(v,`generateConfusionPath`);var y={complex:{model:`Probe → Sense → Respond`,practice:`Emergent Practices`},complicated:{model:`Sense → Analyse → Respond`,practice:`Good Practices`},clear:{model:`Sense → Categorise → Respond`,practice:`Best Practices`},chaotic:{model:`Act → Sense → Respond`,practice:`Novel Practices`},confusion:{model:``,practice:`Disorder`}},b=e.n((e,t)=>{let n=e/2,r=t/2;return{complex:{cx:n/2,cy:r/2,x:0,y:0,w:n,h:r},complicated:{cx:n+n/2,cy:r/2,x:n,y:0,w:n,h:r},chaotic:{cx:n/2,cy:r+r/2,x:0,y:r,w:n,h:r},clear:{cx:n+n/2,cy:r+r/2,x:n,y:r,w:n,h:r},confusion:{cx:n,cy:r,x:n*.7,y:r*.7,w:n*.6,h:r*.6}}},`getDomainLayouts`),x=e.n(()=>r.i(n.D(),n.b().themeVariables).cynefin,`getCynefinDomainColors`),S=3,C={draw:e.n((e,r,a,o)=>{var s;let c=o.db,l=c.getDomains(),u=c.getTransitions(),d=c.getDiagramTitle(),f=c.getAccTitle(),p=c.getAccDescription(),C=c.getConfig(),w=x();t.n.debug(`Rendering Cynefin diagram`);let T=C.width,E=C.height,D=C.padding,O=C.showDomainDescriptions,k=C.boundaryAmplitude,A=T+D*2,j=E+D*2,M={complex:w.complexBg,complicated:w.complicatedBg,clear:w.clearBg,chaotic:w.chaoticBg,confusion:w.confusionBg},N=i.t(r);n.c(N,j,A,(s=C.useMaxWidth)==null||s),N.attr(`viewBox`,`0 0 ${A} ${j}`),f&&N.append(`title`).text(f),p&&N.append(`desc`).text(p);let P=N.append(`g`).attr(`transform`,`translate(${D}, ${D})`),F=b(T,E),I=m(C.seed,r),L=P.append(`g`).attr(`class`,`cynefin-backgrounds`),R=[`complex`,`complicated`,`chaotic`,`clear`];for(let e of R){let t=F[e];L.append(`rect`).attr(`class`,`cynefinDomain`).attr(`x`,t.x).attr(`y`,t.y).attr(`width`,t.w).attr(`height`,t.h).attr(`fill`,M[e]).attr(`fill-opacity`,.4).attr(`stroke`,`none`)}let z=P.append(`g`).attr(`class`,`cynefin-boundaries`);z.append(`path`).attr(`class`,`cynefinBoundary`).attr(`d`,h(T,E,I,k)).attr(`fill`,`none`),z.append(`path`).attr(`class`,`cynefinBoundary`).attr(`d`,g(T,E,I+100,k)).attr(`fill`,`none`),z.append(`path`).attr(`class`,`cynefinCliff`).attr(`d`,_(T,E)).attr(`fill`,`none`);let B=T*.15,V=E*.15;P.append(`path`).attr(`class`,`cynefinConfusion`).attr(`d`,v(T/2,E/2,B,V)).attr(`fill`,M.confusion).attr(`fill-opacity`,.5);let H=P.append(`g`).attr(`class`,`cynefin-labels`);for(let e of R){let t=F[e];H.append(`text`).attr(`class`,`cynefinDomainLabel`).attr(`x`,t.cx).attr(`y`,O?t.cy-30:t.cy).attr(`text-anchor`,`middle`).attr(`dominant-baseline`,`middle`).text(e.charAt(0).toUpperCase()+e.slice(1))}if(H.append(`text`).attr(`class`,`cynefinDomainLabel`).attr(`x`,T/2).attr(`y`,O?E/2-10:E/2).attr(`text-anchor`,`middle`).attr(`dominant-baseline`,`middle`).text(`Confusion`),O){let e=P.append(`g`).attr(`class`,`cynefin-subtitles`);for(let t of R){let n=F[t],r=y[t];e.append(`text`).attr(`class`,`cynefinSubtitle`).attr(`x`,n.cx).attr(`y`,n.cy-10).attr(`text-anchor`,`middle`).attr(`dominant-baseline`,`middle`).text(r.model),e.append(`text`).attr(`class`,`cynefinSubtitle`).attr(`x`,n.cx).attr(`y`,n.cy+5).attr(`text-anchor`,`middle`).attr(`dominant-baseline`,`middle`).text(r.practice)}e.append(`text`).attr(`class`,`cynefinSubtitle`).attr(`x`,T/2).attr(`y`,E/2+8).attr(`text-anchor`,`middle`).attr(`dominant-baseline`,`middle`).text(y.confusion.practice)}let U=P.append(`g`).attr(`class`,`cynefin-items`);for(let e of[`complex`,`complicated`,`chaotic`,`clear`,`confusion`]){let t=l.get(e);if(!t||t.items.length===0)continue;let n=F[e],r=e===`confusion`,i=t.items,a=0;r&&t.items.length>S&&(a=t.items.length-S,i=t.items.slice(0,S));let o;if(r){let e=O?22:14;o=n.cy+e}else o=n.cy+(O?25:15);if([...i].forEach((t,r)=>{let i=o+r*30,a=U.append(`g`),s=a.append(`text`).attr(`class`,`cynefinItemText`).attr(`x`,0).attr(`y`,26/2).attr(`text-anchor`,`middle`).attr(`dominant-baseline`,`central`).text(t.label),c=t.label.length*7,l=s.node();if(l&&typeof l.getBBox==`function`){let e=l.getBBox();e.width>0&&(c=e.width)}let u=c+20,d=n.cx-u/2;a.attr(`transform`,`translate(${d}, ${i})`),a.insert(`rect`,`text`).attr(`class`,`cynefinItem`).attr(`x`,0).attr(`y`,0).attr(`width`,u).attr(`height`,26).attr(`rx`,4).attr(`ry`,4).attr(`fill`,M[e]).attr(`fill-opacity`,.95),s.attr(`x`,u/2).attr(`y`,26/2)}),a>0){let t=o+i.length*30,r=`+${a} more`,s=U.append(`g`),c=s.append(`text`).attr(`class`,`cynefinItemText`).attr(`x`,0).attr(`y`,26/2).attr(`text-anchor`,`middle`).attr(`dominant-baseline`,`central`).text(r),l=r.length*7,u=c.node();if(u&&typeof u.getBBox==`function`){let e=u.getBBox();e.width>0&&(l=e.width)}let d=l+20,f=n.cx-d/2;s.attr(`transform`,`translate(${f}, ${t})`),s.insert(`rect`,`text`).attr(`class`,`cynefinItemOverflow`).attr(`x`,0).attr(`y`,0).attr(`width`,d).attr(`height`,26).attr(`rx`,4).attr(`ry`,4).attr(`fill`,M[e]).attr(`fill-opacity`,.6),c.attr(`x`,d/2).attr(`y`,26/2)}}if(u.length>0){let e=N.select(`defs`).empty()?N.append(`defs`):N.select(`defs`),n=`cynefin-arrow-${r}`;e.append(`marker`).attr(`id`,n).attr(`viewBox`,`0 0 10 10`).attr(`refX`,9).attr(`refY`,5).attr(`markerWidth`,6).attr(`markerHeight`,6).attr(`orient`,`auto-start-reverse`).append(`path`).attr(`d`,`M 0 0 L 10 5 L 0 10 z`).attr(`class`,`cynefinArrowHead`);let i=P.append(`g`).attr(`class`,`cynefin-arrows`);u.forEach(e=>{let r=F[e.from],a=F[e.to];if(!r||!a)return;if(e.from===e.to){t.n.warn(`Cynefin renderer: skipping self-loop on domain "${e.from}"`);return}let o=r.cx,s=r.cy,c=a.cx,l=a.cy,u=(o+c)/2,d=(s+l)/2,f=c-o,p=l-s,m=Math.sqrt(f*f+p*p),h=m*.15,g=-p/m,_=f/m,v=u+g*h,y=d+_*h;i.append(`path`).attr(`class`,`cynefinArrowLine`).attr(`d`,`M${o},${s} Q${v},${y} ${c},${l}`).attr(`fill`,`none`).attr(`marker-end`,`url(#${n})`),e.label&&i.append(`text`).attr(`class`,`cynefinArrowLabel`).attr(`x`,v).attr(`y`,y-6).attr(`text-anchor`,`middle`).attr(`dominant-baseline`,`auto`).text(e.label)})}d&&P.append(`text`).attr(`class`,`cynefinTitle`).attr(`x`,T/2).attr(`y`,-D/2).attr(`text-anchor`,`middle`).attr(`dominant-baseline`,`middle`).text(d)},`draw`)},w=e.n(()=>r.i(n.D(),n.b().themeVariables).cynefin,`getCynefinTheme`),T={parser:d,db:l,renderer:C,styles:e.n(()=>{let e=w();return`
	.cynefinDomain {
		stroke: none;
	}
	.cynefinDomainLabel {
		font-size: ${e.domainFontSize}px;
		font-weight: bold;
		fill: ${e.labelColor};
	}
	.cynefinSubtitle {
		font-size: ${e.itemFontSize-1}px;
		fill: ${e.textColor};
		font-style: italic;
	}
	.cynefinItem {
		fill-opacity: 0.95;
		stroke: ${e.boundaryColor};
		stroke-width: 1;
	}
	.cynefinItemText {
		font-size: ${e.itemFontSize}px;
		fill: ${e.textColor};
	}
	.cynefinItemOverflow {
		fill-opacity: 0.6;
		stroke: ${e.boundaryColor};
		stroke-width: 1;
		stroke-dasharray: 3 2;
	}
	.cynefinBoundary {
		stroke: ${e.boundaryColor};
		stroke-width: ${e.boundaryWidth};
		stroke-dasharray: 6 3;
	}
	.cynefinCliff {
		stroke: ${e.cliffColor};
		stroke-width: ${e.cliffWidth};
	}
	.cynefinConfusion {
		stroke: ${e.boundaryColor};
		stroke-width: 1.5;
		stroke-dasharray: 4 2;
	}
	.cynefinArrowLine {
		stroke: ${e.arrowColor};
		stroke-width: ${e.arrowWidth};
		fill: none;
	}
	.cynefinArrowHead {
		fill: ${e.arrowColor};
		stroke: none;
	}
	.cynefinArrowLabel {
		font-size: ${e.itemFontSize-1}px;
		fill: ${e.textColor};
	}
	.cynefinTitle {
		font-size: ${e.domainFontSize+2}px;
		font-weight: bold;
		fill: ${e.labelColor};
	}
	`},`styles`)};exports.diagram=T;