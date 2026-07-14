const e=require("./chunk-Y2CYZVJY-CnmLqFtv.js"),t=require("./src-KoTOUvXq.js"),n=require("./chunk-WYO6CB5R-DqMhWelG.js"),r=require("./chunk-VAUOI2AC-DI8bspjT.js");var i,a,o=``,s=``,c=``,l=[],u=new Map,d=e.n(e=>n.z(e,n.x()),`sanitizeText`),f=e.n(e=>{switch(e.type){case`terminal`:return{...e,value:d(e.value)};case`nonterminal`:return{...e,name:d(e.name)};case`sequence`:return{...e,elements:e.elements.map(f)};case`choice`:return{...e,alternatives:e.alternatives.map(f)};case`optional`:return{...e,element:f(e.element)};case`repetition`:return{...e,element:f(e.element),separator:e.separator?f(e.separator):void 0};case`special`:return{...e,text:d(e.text)}}},`sanitizeAstNode`),p=e.n(()=>{o=``,s=``,c=``,l.length=0,u.clear(),n.a(),t.n.debug(`[Railroad] Database cleared`)},`clear`),m=e.n(e=>{o=d(e),t.n.debug(`[Railroad] Title set:`,e)},`setTitle`),h=e.n(()=>o,`getTitle`),g={clear:p,setTitle:m,getTitle:h,addRule:e.n(e=>{let n={...e,name:d(e.name),definition:f(e.definition),comment:e.comment?d(e.comment):void 0};t.n.debug(`[Railroad] Adding rule:`,n.name),u.has(n.name)&&t.n.warn(`[Railroad] Rule '${n.name}' is already defined. Overwriting.`),l.push(n),u.set(n.name,n)},`addRule`),getRules:e.n(()=>l,`getRules`),getRule:e.n(e=>u.get(e),`getRule`),setAccTitle:e.n(e=>{s=d(e).replace(/^\s+/g,``),t.n.debug(`[Railroad] Accessibility title set:`,e)},`setAccTitle`),getAccTitle:e.n(()=>s,`getAccTitle`),setAccDescription:e.n(e=>{c=d(e).replace(/\n\s+/g,`
`),t.n.debug(`[Railroad] Accessibility description set:`,e)},`setAccDescription`),getAccDescription:e.n(()=>c,`getAccDescription`),setDiagramTitle:m,getDiagramTitle:h},_={compactMode:!1,padding:10,verticalSeparation:8,horizontalSeparation:10,arcRadius:10,fontSize:14,fontFamily:`monospace`,terminalFill:`#FFFFC0`,terminalStroke:`#000000`,terminalTextColor:`#000000`,nonTerminalFill:`#FFFFFF`,nonTerminalStroke:`#000000`,nonTerminalTextColor:`#000000`,lineColor:`#000000`,strokeWidth:2,markerFill:`#000000`,commentFill:`#E8E8E8`,commentStroke:`#888888`,commentTextColor:`#666666`,specialFill:`#F0E0FF`,specialStroke:`#8800CC`,ruleNameColor:`#000066`,showMarkers:!0,markerRadius:5},v=/^#(?:[\da-f]{3,4}|[\da-f]{6}|[\da-f]{8})$|^(?:rgb|rgba|hsl|hsla|hwb|lab|lch|oklab|oklch)\([\d\s%+,./-]+\)$|^[a-z]+$/i,y=/^[\w "',.-]+$/,b=new Set([`compactMode`,`padding`,`verticalSeparation`,`horizontalSeparation`,`arcRadius`,`fontSize`,`fontFamily`,`terminalFill`,`terminalStroke`,`terminalTextColor`,`nonTerminalFill`,`nonTerminalStroke`,`nonTerminalTextColor`,`lineColor`,`strokeWidth`,`markerFill`,`commentFill`,`commentStroke`,`commentTextColor`,`specialFill`,`specialStroke`,`ruleNameColor`,`showMarkers`,`markerRadius`]),x=e.n(e=>e?Object.keys(e).every(e=>e===`railroad`||b.has(e)):!1,`isRailroadStyleOptions`),S=e.n(e=>e?`railroad`in e&&e.railroad?e.railroad:x(e)?e:{}:{},`extractRailroadOverrides`),C=e.n(e=>{if(!e||x(e))return{};let{railroad:t,svgId:n,theme:r,look:i,...a}=e;return a},`extractThemeOverrides`),w=e.n((e,t)=>{if(typeof e!=`string`)return t;let n=e.trim();return v.test(n)?n:t},`sanitizeColorValue`),T=e.n((e,t)=>{if(typeof e!=`string`)return t;let n=e.trim();return y.test(n)?n:t},`sanitizeFontFamilyValue`),E=e.n((e,t)=>{let n=typeof e==`number`?e:typeof e==`string`?Number.parseFloat(e):NaN;return Number.isFinite(n)&&n>=0?n:t},`sanitizeNumberValue`),D=e.n(e=>{let t=typeof e==`number`?e:typeof e==`string`?Number.parseFloat(e):NaN;return Number.isFinite(t)&&t>0?t:void 0},`parseThemeFontSize`),O=e.n(e=>{var t,n,r,i,a,o,s,c,l,u,d,f,p;let m=T(e.fontFamily,_.fontFamily),h=(t=D(e.fontSize))==null?_.fontSize:t;return{..._,fontFamily:m,fontSize:h,terminalFill:w((n=e.secondBkg)==null?e.secondaryColor:n,_.terminalFill),terminalStroke:w((r=e.secondaryBorderColor)==null?e.lineColor:r,_.terminalStroke),terminalTextColor:w((i=e.secondaryTextColor)==null?e.textColor:i,_.terminalTextColor),nonTerminalFill:w((a=e.mainBkg)==null?e.background:a,_.nonTerminalFill),nonTerminalStroke:w((o=e.primaryBorderColor)==null?e.lineColor:o,_.nonTerminalStroke),nonTerminalTextColor:w((s=e.primaryTextColor)==null?e.textColor:s,_.nonTerminalTextColor),lineColor:w(e.lineColor,_.lineColor),markerFill:w(e.lineColor,_.markerFill),commentFill:w((c=e.labelBackground)==null?e.tertiaryColor:c,_.commentFill),commentStroke:w((l=e.tertiaryBorderColor)==null?e.lineColor:l,_.commentStroke),commentTextColor:w((u=e.tertiaryTextColor)==null?e.textColor:u,_.commentTextColor),specialFill:w((d=e.tertiaryColor)==null?e.secondaryColor:d,_.specialFill),specialStroke:w((f=e.tertiaryBorderColor)==null?e.secondaryBorderColor:f,_.specialStroke),ruleNameColor:w((p=e.titleColor)==null?e.textColor:p,_.ruleNameColor)}},`buildThemeDefaults`),k=e.n(e=>{var t,r,i,a;let o=n.b(),s=O({...n.D(),...(t=o.themeVariables)==null?{}:t,...C(e)}),c={...(r=o.railroad)==null?{}:r,...S(e)};return{compactMode:(i=c.compactMode)==null?s.compactMode:i,padding:E(c.padding,s.padding),verticalSeparation:E(c.verticalSeparation,s.verticalSeparation),horizontalSeparation:E(c.horizontalSeparation,s.horizontalSeparation),arcRadius:E(c.arcRadius,s.arcRadius),fontSize:E(c.fontSize,s.fontSize),fontFamily:T(c.fontFamily,s.fontFamily),terminalFill:w(c.terminalFill,s.terminalFill),terminalStroke:w(c.terminalStroke,s.terminalStroke),terminalTextColor:w(c.terminalTextColor,s.terminalTextColor),nonTerminalFill:w(c.nonTerminalFill,s.nonTerminalFill),nonTerminalStroke:w(c.nonTerminalStroke,s.nonTerminalStroke),nonTerminalTextColor:w(c.nonTerminalTextColor,s.nonTerminalTextColor),lineColor:w(c.lineColor,s.lineColor),strokeWidth:E(c.strokeWidth,s.strokeWidth),markerFill:w(c.markerFill,s.markerFill),commentFill:w(c.commentFill,s.commentFill),commentStroke:w(c.commentStroke,s.commentStroke),commentTextColor:w(c.commentTextColor,s.commentTextColor),specialFill:w(c.specialFill,s.specialFill),specialStroke:w(c.specialStroke,s.specialStroke),ruleNameColor:w(c.ruleNameColor,s.ruleNameColor),showMarkers:(a=c.showMarkers)==null?s.showMarkers:a,markerRadius:E(c.markerRadius,s.markerRadius)}},`buildRailroadStyleOptions`),A=e.n(e=>{let{fontFamily:t,fontSize:n,terminalFill:r,terminalStroke:i,terminalTextColor:a,nonTerminalFill:o,nonTerminalStroke:s,nonTerminalTextColor:c,lineColor:l,strokeWidth:u,markerFill:d,commentFill:f,commentStroke:p,commentTextColor:m,specialFill:h,specialStroke:g,ruleNameColor:_}=k(e);return`
  .railroad-diagram {
    font-family: ${t};
    font-size: ${n}px;
  }

  .railroad-terminal rect {
    fill: ${r};
    stroke: ${i};
    stroke-width: ${u}px;
  }

  .railroad-terminal text {
    fill: ${a};
    font-family: ${t};
    font-size: ${n}px;
    text-anchor: middle;
    dominant-baseline: middle;
  }

  .railroad-nonterminal rect {
    fill: ${o};
    stroke: ${s};
    stroke-width: ${u}px;
  }

  .railroad-nonterminal text {
    fill: ${c};
    font-family: ${t};
    font-size: ${n}px;
    text-anchor: middle;
    dominant-baseline: middle;
  }

  .railroad-line {
    stroke: ${l};
    stroke-width: ${u}px;
    fill: none;
  }

  .railroad-start circle,
  .railroad-end circle {
    fill: ${d};
  }

  .railroad-comment ellipse {
    fill: ${f};
    stroke: ${p};
    stroke-width: ${u}px;
  }

  .railroad-comment text {
    fill: ${m};
    font-style: italic;
    font-family: ${t};
    font-size: ${n}px;
    text-anchor: middle;
    dominant-baseline: middle;
  }

  .railroad-special rect {
    fill: ${h};
    stroke: ${g};
    stroke-width: ${u}px;
    stroke-dasharray: 5,3;
  }

  .railroad-special text {
    fill: ${c};
    font-family: ${t};
    font-size: ${n}px;
    text-anchor: middle;
    dominant-baseline: middle;
  }

  .railroad-rule-name {
    font-weight: bold;
    fill: ${_};
    font-family: ${t};
    font-size: ${n}px;
  }

  .railroad-group {
    /* Grouping container, no specific styles */
  }
`},`getStyles`),j=(i=class{constructor(){this.d=``}moveTo(e,t){return this.d+=`M ${e} ${t} `,this}lineTo(e,t){return this.d+=`L ${e} ${t} `,this}horizontalTo(e){return this.d+=`H ${e} `,this}verticalTo(e){return this.d+=`V ${e} `,this}arcTo(e,t,n,r,i,a,o){return this.d+=`A ${e} ${t} ${n} ${+!!r} ${+!!i} ${a} ${o} `,this}build(){return this.d.trim()}},e.n(i,`PathBuilder`),i),M=(a=class{constructor(e,t=k()){this.textCache=new Map,this.svg=e,this.config=t}measureText(e){if(this.textCache.has(e))return this.textCache.get(e);let t=this.svg.append(`text`).attr(`font-family`,this.config.fontFamily).attr(`font-size`,this.config.fontSize).text(e),n=t.node().getBBox(),r={width:n.width,height:n.height};return t.remove(),this.textCache.set(e,r),r}renderTerminal(e,t){let n=this.measureText(t),r=n.width+this.config.padding*2,i=n.height+this.config.padding*2,a=e.append(`g`).attr(`class`,`railroad-terminal`);return a.append(`rect`).attr(`x`,0).attr(`y`,0).attr(`width`,r).attr(`height`,i).attr(`rx`,10).attr(`ry`,10),a.append(`text`).attr(`x`,r/2).attr(`y`,i/2).text(t),{element:a.node(),dimensions:{width:r,height:i,up:i/2,down:i/2}}}renderNonTerminal(e,t){let n=this.measureText(t),r=n.width+this.config.padding*2,i=n.height+this.config.padding*2,a=e.append(`g`).attr(`class`,`railroad-nonterminal`);return a.append(`rect`).attr(`x`,0).attr(`y`,0).attr(`width`,r).attr(`height`,i),a.append(`text`).attr(`x`,r/2).attr(`y`,i/2).text(t),{element:a.node(),dimensions:{width:r,height:i,up:i/2,down:i/2}}}renderSequence(e,t){let n=t.map(t=>this.renderExpression(e,t)),r=0,i=0,a=0;for(let e of n)r+=e.dimensions.width,i=Math.max(i,e.dimensions.up),a=Math.max(a,e.dimensions.down);r+=(n.length-1)*this.config.horizontalSeparation;let o=e.append(`g`).attr(`class`,`railroad-sequence`),s=0;for(let e=0;e<n.length;e++){let t=n[e],r=i-t.dimensions.up;if(o.node().appendChild(t.element).setAttribute(`transform`,`translate(${s}, ${r})`),e<n.length-1){let e=s+t.dimensions.width,n=e+this.config.horizontalSeparation,r=i;o.append(`path`).attr(`class`,`railroad-line`).attr(`d`,new j().moveTo(e,r).lineTo(n,r).build())}s+=t.dimensions.width+this.config.horizontalSeparation}return{element:o.node(),dimensions:{width:r,height:i+a,up:i,down:a}}}renderChoice(e,t){let n=t.map(t=>this.renderExpression(e,t)),r=0,i=0;for(let e of n)r=Math.max(r,e.dimensions.width),i+=e.dimensions.height;i+=(n.length-1)*this.config.verticalSeparation;let a=this.config.arcRadius,o=a*4,s=r+o,c=e.append(`g`).attr(`class`,`railroad-choice`),l=0,u=i/2;for(let e of n){let t=l,n=t+e.dimensions.up,i=a*2+(r-e.dimensions.width)/2;c.node().appendChild(e.element).setAttribute(`transform`,`translate(${i}, ${t})`);let o=new j,d=n>u;n===u?o.moveTo(0,u).lineTo(i,n):o.moveTo(0,u).arcTo(a,a,0,!1,d,a,u+(d?a:-a)).lineTo(a,n-(d?a:-a)).arcTo(a,a,0,!1,!d,a*2,n).lineTo(i,n),c.append(`path`).attr(`class`,`railroad-line`).attr(`d`,o.build());let f=new j,p=i+e.dimensions.width,m=s-a*2;n===u?f.moveTo(p,n).lineTo(s,u):f.moveTo(p,n).lineTo(m,n).arcTo(a,a,0,!1,!d,s-a,n+(d?-a:a)).lineTo(s-a,u+(d?a:-a)).arcTo(a,a,0,!1,d,s,u),c.append(`path`).attr(`class`,`railroad-line`).attr(`d`,f.build()),l+=e.dimensions.height+this.config.verticalSeparation}return{element:c.node(),dimensions:{width:s,height:i,up:u,down:i-u}}}renderOptional(e,t){let n=this.renderExpression(e,t),r=this.config.arcRadius,i=r*2,a=n.dimensions.width+r*4,o=n.dimensions.height+i,s=e.append(`g`).attr(`class`,`railroad-optional`),c=r*2,l=i;s.node().appendChild(n.element).setAttribute(`transform`,`translate(${c}, ${l})`);let u=l+n.dimensions.up,d=new j().moveTo(0,u).lineTo(r*2,u);s.append(`path`).attr(`class`,`railroad-line`).attr(`d`,d.build());let f=new j().moveTo(c+n.dimensions.width,u).lineTo(a,u);s.append(`path`).attr(`class`,`railroad-line`).attr(`d`,f.build());let p=new j().moveTo(0,u).arcTo(r,r,0,!1,!1,r,u-r).lineTo(r,r).arcTo(r,r,0,!1,!0,r*2,0).lineTo(a-r*2,0).arcTo(r,r,0,!1,!0,a-r,r).lineTo(a-r,u-r).arcTo(r,r,0,!1,!1,a,u);return s.append(`path`).attr(`class`,`railroad-line`).attr(`d`,p.build()),{element:s.node(),dimensions:{width:a,height:o,up:u,down:o-u}}}renderRepetition(e,t,n){let r=this.renderExpression(e,t),i=this.config.arcRadius,a=i*2,o=r.dimensions.width+i*4,s=n===0,c=r.dimensions.height+a+(s?a:0),l=e.append(`g`).attr(`class`,`railroad-repetition`),u=i*2,d=s?a:0;l.node().appendChild(r.element).setAttribute(`transform`,`translate(${u}, ${d})`);let f=d+r.dimensions.up;l.append(`path`).attr(`class`,`railroad-line`).attr(`d`,new j().moveTo(0,f).lineTo(i*2,f).build()),l.append(`path`).attr(`class`,`railroad-line`).attr(`d`,new j().moveTo(u+r.dimensions.width,f).lineTo(o,f).build());let p=d+r.dimensions.height+i,m=new j().moveTo(u+r.dimensions.width,f).arcTo(i,i,0,!1,!0,u+r.dimensions.width+i,f+i).lineTo(u+r.dimensions.width+i,p).arcTo(i,i,0,!1,!0,u+r.dimensions.width,p+i).lineTo(i*2,p+i).arcTo(i,i,0,!1,!0,i,p).lineTo(i,f+i).arcTo(i,i,0,!1,!0,i*2,f);if(l.append(`path`).attr(`class`,`railroad-line`).attr(`d`,m.build()),s){let e=new j().moveTo(0,f).arcTo(i,i,0,!1,!1,i,f-i).lineTo(i,i).arcTo(i,i,0,!1,!0,i*2,0).lineTo(o-i*2,0).arcTo(i,i,0,!1,!0,o-i,i).lineTo(o-i,f-i).arcTo(i,i,0,!1,!1,o,f);l.append(`path`).attr(`class`,`railroad-line`).attr(`d`,e.build())}return{element:l.node(),dimensions:{width:o,height:c,up:f,down:c-f}}}renderSpecial(e,t){let n=this.measureText(`? `+t+` ?`),r=n.width+this.config.padding*2,i=n.height+this.config.padding*2,a=e.append(`g`).attr(`class`,`railroad-special`);return a.append(`rect`).attr(`x`,0).attr(`y`,0).attr(`width`,r).attr(`height`,i),a.append(`text`).attr(`x`,r/2).attr(`y`,i/2).text(`? `+t+` ?`),{element:a.node(),dimensions:{width:r,height:i,up:i/2,down:i/2}}}renderExpression(e,t){switch(t.type){case`terminal`:return this.renderTerminal(e,t.value);case`nonterminal`:return this.renderNonTerminal(e,t.name);case`sequence`:return this.renderSequence(e,t.elements);case`choice`:return this.renderChoice(e,t.alternatives);case`optional`:return this.renderOptional(e,t.element);case`repetition`:return this.renderRepetition(e,t.element,t.min);case`special`:return this.renderSpecial(e,t.text);default:throw Error(`Unknown node type: ${t.type}`)}}renderRule(e,t){let n=this.svg.append(`g`).attr(`class`,`railroad-rule`).attr(`transform`,`translate(0, ${t})`),r=e.name+` =`,i=this.measureText(r).width+20,a=i+20,o=n.append(`g`),s=this.renderExpression(o,e.definition),c=Math.max(20,s.dimensions.up),l=c-s.dimensions.up;return o.attr(`transform`,`translate(${a}, ${l})`),n.append(`g`).attr(`class`,`railroad-rule-name-group`).append(`text`).attr(`class`,`railroad-rule-name`).attr(`x`,0).attr(`y`,c).text(r),n.append(`g`).attr(`class`,`railroad-start`).append(`circle`).attr(`cx`,i).attr(`cy`,c).attr(`r`,this.config.markerRadius),n.append(`g`).attr(`class`,`railroad-end`).append(`circle`).attr(`cx`,a+s.dimensions.width+10).attr(`cy`,c).attr(`r`,this.config.markerRadius),n.append(`path`).attr(`class`,`railroad-line`).attr(`d`,new j().moveTo(i+this.config.markerRadius,c).lineTo(a,c).build()),n.append(`path`).attr(`class`,`railroad-line`).attr(`d`,new j().moveTo(a+s.dimensions.width,c).lineTo(a+s.dimensions.width+10-this.config.markerRadius,c).build()),{height:Math.max(40,l+s.dimensions.height+this.config.padding*2),width:a+s.dimensions.width+10+this.config.markerRadius}}renderDiagram(e){let t=this.config.padding,n=0;for(let r of e){let e=this.renderRule(r,t);t+=e.height+this.config.verticalSeparation,n=Math.max(n,e.width)}return{width:n+this.config.padding*2,height:t+this.config.padding}}},e.n(a,`RailroadRenderer`),a),N=e.n((e,t,r)=>{n.c(e,t.height,t.width,r),e.attr(`viewBox`,`0 0 ${t.width} ${t.height}`)},`configureRailroadSvgSize`),P={draw:e.n((e,i,a)=>{t.n.debug(`[Railroad] Rendering diagram
`+e);try{var o;let e=r.t(i);e.attr(`class`,`railroad-diagram`);let a=n.b().railroad,s=(o=a==null?void 0:a.useMaxWidth)==null||o,c=g.getRules();if(t.n.debug(`[Railroad] Rendering ${c.length} rules`),c.length===0){t.n.warn(`[Railroad] No rules to render`),N(e,{height:100,width:200},s);return}N(e,new M(e,k()).renderDiagram(c),s),t.n.debug(`[Railroad] Render complete`)}catch(e){throw t.n.error(`[Railroad] Render error:`,e),e}},`draw`)};Object.defineProperty(exports,"n",{enumerable:!0,get:function(){return A}}),Object.defineProperty(exports,"r",{enumerable:!0,get:function(){return P}}),Object.defineProperty(exports,"t",{enumerable:!0,get:function(){return g}});