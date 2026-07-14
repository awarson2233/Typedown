const e=require("./chunk-Y2CYZVJY-CnmLqFtv.js"),t=require("./src-KoTOUvXq.js"),n=require("./chunk-WYO6CB5R-DqMhWelG.js"),r=require("./ordinal-CNSNh8ZZ.js"),i=require("./path-BnbsE6Av.js"),a=require("./math-G8rF7UWn.js"),o=require("./arc-ydUFWMf2.js"),s=require("./array-DZSRzHA_.js"),c=require("./chunk-ICXQ74PX-LEVw6uKk.js"),l=require("./chunk-VAUOI2AC-DI8bspjT.js"),u=require("./chunk-JWPE2WC7-DDuoFcgR.js"),d=require("./mermaid-parser.core-sNVg_8v2.js");function f(e,t){return t<e?-1:t>e?1:t>=e?0:NaN}function p(e){return e}function m(){var e=p,t=f,n=null,r=i.n(0),o=i.n(a.p),c=i.n(0);function l(i){var l,u=(i=s.t(i)).length,d,f,p=0,m=Array(u),h=Array(u),g=+r.apply(this,arguments),_=Math.min(a.p,Math.max(-a.p,o.apply(this,arguments)-g)),v,y=Math.min(Math.abs(_)/u,c.apply(this,arguments)),b=y*(_<0?-1:1),x;for(l=0;l<u;++l)(x=h[m[l]=l]=+e(i[l],l,i))>0&&(p+=x);for(t==null?n!=null&&m.sort(function(e,t){return n(i[e],i[t])}):m.sort(function(e,n){return t(h[e],h[n])}),l=0,f=p?(_-u*b)/p:0;l<u;++l,g=v)d=m[l],x=h[d],v=g+(x>0?x*f:0)+b,h[d]={data:i[d],index:l,value:x,startAngle:g,endAngle:v,padAngle:y};return h}return l.value=function(t){return arguments.length?(e=typeof t==`function`?t:i.n(+t),l):e},l.sortValues=function(e){return arguments.length?(t=e,n=null,l):t},l.sort=function(e){return arguments.length?(n=e,t=null,l):n},l.startAngle=function(e){return arguments.length?(r=typeof e==`function`?e:i.n(+e),l):r},l.endAngle=function(e){return arguments.length?(o=typeof e==`function`?e:i.n(+e),l):o},l.padAngle=function(e){return arguments.length?(c=typeof e==`function`?e:i.n(+e),l):c},l}var h=n.f.pie,g={sections:new Map,showData:!1,config:h},_=g.sections,v=g.showData,y=structuredClone(h),b={getConfig:e.n(()=>structuredClone(y),`getConfig`),clear:e.n(()=>{_=new Map,v=g.showData,n.a()},`clear`),setDiagramTitle:n.K,getDiagramTitle:n.w,setAccTitle:n.U,getAccTitle:n.y,setAccDescription:n.H,getAccDescription:n.v,addSection:e.n(({label:e,value:n})=>{if(n<0)throw Error(`"${e}" has invalid value: ${n}. Negative values are not allowed in pie charts. All slice values must be >= 0.`);_.has(e)||(_.set(e,n),t.n.debug(`added new section: ${e}, with value: ${n}`))},`addSection`),getSections:e.n(()=>_,`getSections`),setShowData:e.n(e=>{v=e},`setShowData`),getShowData:e.n(()=>v,`getShowData`)},x=e.n((e,t)=>{u.t(e,t),t.setShowData(e.showData),e.sections.map(t.addSection)},`populateDb`),S={parse:e.n(async e=>{let n=await d.n(`pie`,e);t.n.debug(n),x(n,b)},`parse`)},C=e.n(e=>`
  .pieCircle{
    stroke: ${e.pieStrokeColor};
    stroke-width : ${e.pieStrokeWidth};
    opacity : ${e.pieOpacity};
  }
  .pieCircle.highlighted{
    scale: 1.05;
    opacity: 1;
  }
  .pieCircle.highlightedOnHover:hover{
    transition-duration: 250ms;
    scale: 1.05;
    opacity: 1;
  }
  .pieOuterCircle{
    stroke: ${e.pieOuterStrokeColor};
    stroke-width: ${e.pieOuterStrokeWidth};
    fill: none;
  }
  .pieTitleText {
    text-anchor: middle;
    font-size: ${e.pieTitleTextSize};
    fill: ${e.pieTitleTextColor};
    font-family: ${e.fontFamily};
  }
  .slice {
    font-family: ${e.fontFamily};
    fill: ${e.pieSectionTextColor};
    font-size:${e.pieSectionTextSize};
    // fill: white;
  }
  .legend text {
    fill: ${e.pieLegendTextColor};
    font-family: ${e.fontFamily};
    font-size: ${e.pieLegendTextSize};
  }
`,`getStyles`),w=e.n(e=>{let t=[...e.values()].reduce((e,t)=>e+t,0),n=[...e.entries()].map(([e,t])=>({label:e,value:t})).filter(e=>e.value/t*100>=1);return m().value(e=>e.value).sort(null)(n)},`createPieArcs`),T={parser:S,db:b,renderer:{draw:e.n((e,i,a,s)=>{var u,d;t.n.debug(`rendering pie chart
`+e);let f=s.db,p=n.x(),m=c.i(f.getConfig(),p.pie),h=l.t(i),g=h.append(`g`);g.attr(`transform`,`translate(225,225)`);let{themeVariables:_}=p,[v]=c.p(_.pieOuterStrokeWidth);v!=null||(v=2);let y=m.legendPosition,b=m.textPosition,x=m.donutHole>0&&m.donutHole<=.9?m.donutHole:0,S=o.t().innerRadius(x*185).outerRadius(185),C=o.t().innerRadius(185*b).outerRadius(185*b),T=g.append(`g`);T.append(`circle`).attr(`cx`,0).attr(`cy`,0).attr(`r`,185+v/2).attr(`class`,`pieOuterCircle`);let E=f.getSections(),D=w(E),O=[_.pie1,_.pie2,_.pie3,_.pie4,_.pie5,_.pie6,_.pie7,_.pie8,_.pie9,_.pie10,_.pie11,_.pie12],k=0;E.forEach(e=>{k+=e});let A=D.filter(e=>(e.data.value/k*100).toFixed(0)!==`0`),j=r.n(O).domain([...E.keys()]);T.selectAll(`mySlices`).data(A).enter().append(`path`).attr(`d`,S).attr(`fill`,e=>j(e.data.label)).attr(`class`,e=>{let t=`pieCircle`;return m.highlightSlice===`hover`?t+=` highlightedOnHover`:m.highlightSlice===e.data.label&&(t+=` highlighted`),t}),T.selectAll(`mySlices`).data(A).enter().append(`text`).text(e=>(e.data.value/k*100).toFixed(0)+`%`).attr(`transform`,e=>`translate(`+C.centroid(e)+`)`).style(`text-anchor`,`middle`).attr(`class`,`slice`);let M=g.append(`text`).text(f.getDiagramTitle()).attr(`x`,0).attr(`y`,-400/2).attr(`class`,`pieTitleText`),N=[...E.entries()].map(([e,t])=>({label:e,value:t})),P=g.selectAll(`.legend`).data(N).enter().append(`g`).attr(`class`,`legend`);P.append(`rect`).attr(`width`,18).attr(`height`,18).style(`fill`,e=>j(e.label)).style(`stroke`,e=>j(e.label)),P.append(`text`).attr(`x`,22).attr(`y`,14).text(e=>f.getShowData()?`${e.label} [${e.value}]`:e.label);let F=Math.max(...P.selectAll(`text`).nodes().map(e=>{var t;return(t=e==null?void 0:e.getBoundingClientRect().width)==null?0:t})),I=450,L=490,R=N.length*22;switch(y){case`center`:P.attr(`transform`,(e,t)=>{let n=22*N.length/2,r=-F/2-22,i=t*22-n;return`translate(`+r+`,`+i+`)`});break;case`top`:I+=R,P.attr(`transform`,(e,t)=>`translate(${-F/2-22}, ${t*22-185})`),T.attr(`transform`,()=>`translate(0, ${R+22})`);break;case`bottom`:I+=R,P.attr(`transform`,(e,t)=>{let n=-F/2-22,r=t*22- -207;return`translate(`+n+`,`+r+`)`});break;case`left`:L+=22+F,P.attr(`transform`,(e,t)=>{let n=22*N.length/2;return`translate(-207,`+(t*22-n)+`)`}),T.attr(`transform`,()=>`translate(${F+18+4}, 0)`);break;default:L+=22+F,P.attr(`transform`,(e,t)=>{let n=22*N.length/2;return`translate(216,`+(t*22-n)+`)`});break}let z=(u=(d=M.node())==null?void 0:d.getBoundingClientRect().width)==null?0:u,B=450/2-z/2,V=450/2+z/2,H=Math.min(0,B),U=Math.max(L,V)-H;h.attr(`viewBox`,`${H} 0 ${U} ${I}`),n.c(h,I,U,m.useMaxWidth)},`draw`)},styles:C};exports.diagram=T;