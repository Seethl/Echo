const fs = require('fs');
const path = require('path');
const out = __dirname;
let verts=[], faces=[], groups=[];
const mats={coat:[.075,.09,.085],lining:[.19,.105,.09],skin:[.34,.37,.29],bone:[.72,.68,.49],black:[.012,.017,.014],eye:[.85,.22,.035],metal:[.32,.27,.14],shoe:[.045,.04,.032],teeth:[.58,.53,.36]};
function mesh(name,mat,v,f){let base=verts.length;verts.push(...v);groups.push({name,mat,start:faces.length});faces.push(...f.map(a=>a.map(i=>i+base+1)));}
function rings(name,mat,rs,n=7){let v=[],f=[];rs.forEach((r,k)=>{for(let i=0;i<n;i++){let t=2*Math.PI*i/n;v.push([r[0]+Math.cos(t)*r[3],r[1]+Math.sin(t)*r[4],r[2]]);}});for(let k=0;k<rs.length-1;k++)for(let i=0;i<n;i++){let a=k*n+i,b=k*n+(i+1)%n,c=(k+1)*n+(i+1)%n,d=(k+1)*n+i;f.push([a,b,c],[a,c,d]);}f.push([...Array(n).keys()].reverse());f.push([...Array(n).keys()].map(i=>i+(rs.length-1)*n));mesh(name,mat,v,f);}
function limb(name,mat,a,b,r1,r2,n=6){let dz=b.map((x,i)=>x-a[i]),len=Math.hypot(...dz);let w=dz.map(x=>x/len),u=Math.abs(w[2])<.9?[-w[1],w[0],0]:[1,0,-w[0]/w[2]];let ul=Math.hypot(...u);u=u.map(x=>x/ul);let v=[w[1]*u[2]-w[2]*u[1],w[2]*u[0]-w[0]*u[2],w[0]*u[1]-w[1]*u[0]];let vs=[],fs=[];[a,b].forEach((p,k)=>{for(let i=0;i<n;i++){let t=i*2*Math.PI/n;vs.push(p.map((x,j)=>x+(Math.cos(t)*u[j]+Math.sin(t)*v[j])*(k?r2:r1)));}});for(let i=0;i<n;i++)fs.push([i,(i+1)%n,(i+1)%n+n,i+n]);fs.push([...Array(n).keys()].reverse(),[...Array(n).keys()].map(i=>i+n));mesh(name,mat,vs,fs);}
function ell(name,mat,c,s,n=8){let rs=[];for(let j=0;j<5;j++){let t=Math.PI*(.04+.92*j/4);rs.push([c[0],c[1],c[2]+Math.cos(t)*s[2],Math.sin(t)*s[0],Math.sin(t)*s[1]]);}rings(name,mat,rs,n);}
// Front faces -Y. Ground at Z=0; model height about 2.9 metres.
for(let side of [-1,1]){
 let x=side*.19;
 ell('Boot_'+side,'shoe',[x,-.105,.105],[.115,.23,.11]);
 limb('Shin_'+side,'coat',[x,0,.19],[side*.16,.055,.67],.075,.10);
 limb('Thigh_'+side,'coat',[side*.16,.055,.67],[side*.20,.10,1.16],.10,.15);
}
rings('Torn_frock_coat','coat',[[0,.10,.77,.40,.21],[0,.10,1.22,.30,.20],[0,.12,1.66,.255,.185],[0,.17,1.98,.39,.22],[0,.19,2.13,.30,.20]],10);
// Separate pointed skirt tails and contrasting shirt exposed at chest.
for(let s of [-1,1]){
 mesh('Ragged_tail_'+s,'coat',[[s*.03,-.105,1.35],[s*.35,-.08,1.20],[s*.40,-.09,.64],[s*.23,-.13,.78],[s*.13,-.15,.59]],[[0,1,2],[0,2,3],[0,3,4]]);
 mesh('Lapel_'+s,'lining',[[s*.025,-.112,1.51],[s*.235,-.075,2.02],[s*.10,-.13,2.10],[s*.08,-.15,1.81]],[[0,1,3],[1,2,3]]);
 let shoulder=[s*.38,.15,2.01],elbow=[s*.57,-.015,1.52],wrist=[s*.69,-.16,1.06];
 ell('Shoulder_'+s,'coat',shoulder,[.17,.18,.17],6);
 limb('Upper_sleeve_'+s,'coat',shoulder,elbow,.14,.095);
 limb('Forearm_sleeve_'+s,'coat',elbow,[s*.66,-.13,1.17],.095,.075);
 limb('Exposed_wrist_'+s,'skin',[s*.66,-.13,1.20],wrist,.058,.048);
 ell('Palm_'+s,'skin',[s*.71,-.18,.99],[.085,.055,.12],6);
 for(let i=0;i<4;i++){
  let a=[s*(.655+i*.038),-.19,.95],b=[s*(.66+i*.052),-.225,.77-(i===1?.04:0)],c=[s*(.64+i*.054),-.29,.71-(i===1?.04:0)];
  limb('Finger_'+s+'_'+i,'skin',a,b,.019,.014,5);limb('Claw_'+s+'_'+i,'bone',b,c,.015,.002,5);
 }
 limb('Thumb_'+s,'skin',[s*.65,-.18,1.03],[s*.59,-.22,.92],.026,.017,5);
 limb('Thumb_tip_'+s,'bone',[s*.59,-.22,.92],[s*.60,-.28,.86],.017,.002,5);
}
mesh('Shirt','black',[[-.09,-.12,2.04],[.09,-.12,2.04],[0,-.15,1.57]],[[0,1,2]]);
for(let i=0;i<4;i++)ell('Brass_button_'+i,'metal',[.035,-.116,1.40-i*.12],[.017,.012,.017],6);
limb('Crooked_neck','skin',[0,.19,2.02],[.045,-.01,2.34],.12,.09);
ell('Skull','skin',[.04,-.035,2.48],[.175,.155,.265],7);
// Angular mask with actual gaps around the eyes, instead of painted sockets.
mesh('Ivory_mask','bone',[[-.14,-.155,2.67],[.04,-.207,2.76],[.20,-.155,2.65],[-.16,-.197,2.55],[-.03,-.244,2.57],[.07,-.244,2.57],[.19,-.193,2.54],[-.13,-.21,2.46],[-.015,-.25,2.47],[.075,-.25,2.47],[.18,-.20,2.45],[-.07,-.205,2.31],[.12,-.20,2.32],[.026,-.29,2.44]],[[0,1,4],[1,5,4],[1,2,5],[0,4,3],[2,6,5],[3,7,8],[3,8,4],[5,9,10],[5,10,6],[8,13,4],[4,13,5],[5,13,9],[7,11,8],[9,12,10]]);
for(let s of [-1,1]){
 ell('Eye_socket_'+s,'black',[.025+s*.084,-.193,2.515],[.063,.033,.043],6);
 ell('Amber_eye_'+s,'eye',[.025+s*.084,-.226,2.513],[.012,.008,.012],6);
}
ell('Mouth_void','black',[.025,-.17,2.355],[.093,.055,.075],7);
for(let i=0;i<6;i++)limb('Broken_tooth_'+i,'teeth',[-.046+i*.028,-.218,2.40],[-.045+i*.028,-.225,2.36+(i%2)*.016],.010,.003,4);
// A few broken iron pins across the hunched back.
for(let i=0;i<3;i++)limb('Back_pin_'+i,'metal',[-.17+i*.17,.30,2.06],[-.20+i*.19,.37,2.22+(i===1?.06:0)],.025,.006,5);
let obj=['# The Last Steward | metres | front -Y | Z up','mtllib last_steward.mtl'];
verts.forEach(v=>obj.push('v '+v.map(x=>x.toFixed(6)).join(' ')));
let gi=0;faces.forEach((f,i)=>{while(gi<groups.length&&groups[gi].start===i){obj.push('o '+groups[gi].name,'usemtl '+groups[gi].mat);gi++;}obj.push('f '+f.join(' '));});
fs.writeFileSync(path.join(out,'last_steward.obj'),obj.join('\n'));
fs.writeFileSync(path.join(out,'last_steward.mtl'),Object.entries(mats).map(([n,c])=>`newmtl ${n}\nKd ${c.join(' ')}\nKa 0 0 0\nKs 0.04 0.04 0.04\nNs 8\n`).join('\n'));
let tris=faces.reduce((s,f)=>s+f.length-2,0);
fs.writeFileSync(path.join(out,'stats.json'),JSON.stringify({vertices:verts.length,triangles:tris,objects:groups.length,height_metres:2.76},null,2));
console.log(`${verts.length} vertices, ${tris} triangles, ${groups.length} mesh parts`);
