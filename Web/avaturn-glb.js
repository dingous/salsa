const MAX=150*1024*1024;
export function avaturnProject(value){
 const url=new URL(value);if(url.protocol!=='https:'||!url.hostname.endsWith('.avaturn.dev')||url.username||url.password)throw new Error('Use a URL HTTPS do projeto em avaturn.dev.');return url.href;
}
export function avaturnBones(){
 const map={hips:'Hips',spine:'Spine',chest:'Spine1',upperChest:'Spine2',neck:'Neck',head:'Head'};
 for(const side of ['left','right']){const prefix=side[0].toUpperCase()+side.slice(1);
  for(const [vrm,source] of Object.entries({Shoulder:'Shoulder',UpperArm:'Arm',LowerArm:'ForeArm',Hand:'Hand',UpperLeg:'UpLeg',LowerLeg:'Leg',Foot:'Foot',Toes:'ToeBase'}))map[side+vrm]=prefix+source;
  for(const finger of ['Thumb','Index','Middle','Ring','Little'])(finger==='Thumb'?['Metacarpal','Proximal','Distal']:['Proximal','Intermediate','Distal']).forEach((joint,i)=>map[side+finger+joint]=prefix+'Hand'+(finger==='Little'?'Pinky':finger)+(i+1));
 }return map;
}
export function convertAvaturnGlb(input){
 const bytes=input instanceof Uint8Array?input:new Uint8Array(input),view=new DataView(bytes.buffer,bytes.byteOffset,bytes.byteLength);
 if(bytes.length<28||bytes.length>MAX||view.getUint32(0,true)!==0x46546c67||view.getUint32(4,true)!==2||view.getUint32(8,true)!==bytes.length||view.getUint32(16,true)!==0x4e4f534a)throw new Error('GLB inválido ou acima de 150 MB.');
 const length=view.getUint32(12,true);if(length<2||length>bytes.length-20)throw new Error('Cabeçalho GLB inválido.');
 const doc=JSON.parse(new TextDecoder().decode(bytes.subarray(20,20+length)));
 if(doc.extensions?.VRMC_vrm)return {buffer:bytes.slice().buffer,expressions:Object.keys(doc.extensions.VRMC_vrm.expressions?.custom||{}).length};
 for(const section of ['buffers','images'])for(const item of doc[section]||[])if(item.uri&&!item.uri.startsWith('data:'))throw new Error('Exporte um GLB com texturas e buffers incorporados.');
 const nodes=doc.nodes||[],lookup=new Map();nodes.forEach((node,i)=>{if(node.name&&!lookup.has(node.name.split(':').at(-1).toLowerCase()))lookup.set(node.name.split(':').at(-1).toLowerCase(),i);});
 const humanBones={};for(const [bone,name] of Object.entries(avaturnBones())){const node=lookup.get(name.toLowerCase());if(node!==undefined)humanBones[bone]={node};else if(!['upperChest','leftToes','rightToes'].includes(bone))throw new Error('Osso Avaturn ausente: '+name);}
 const custom={};nodes.forEach((node,n)=>{if(node.mesh===undefined)return;for(const [index,name] of (doc.meshes?.[node.mesh]?.extras?.targetNames||[]).entries()){if(!name)continue;const key=name[0].toLowerCase()+name.slice(1);(custom[key]??={isBinary:false,morphTargetBinds:[]}).morphTargetBinds.push({node:n,index,weight:1});}});
 if(!custom.jawOpen&&custom.mouthOpen)custom.jawOpen=custom.mouthOpen;
 const preset={};if(custom.jawOpen)preset.aa=custom.jawOpen;for(const side of ['Left','Right'])if(custom['eyeBlink'+side])preset['blink'+side]=custom['eyeBlink'+side];
 doc.extensions??={};doc.extensions.VRMC_vrm={specVersion:'1.0',meta:{name:'Avaturn · Meu avatar',version:'1.0',authors:['Avaturn / proprietário do avatar'],licenseUrl:'https://vrm.dev/licenses/1.0/',otherLicenseUrl:'https://avaturn.me/terms',avatarPermission:'onlyAuthor',commercialUsage:'personalNonProfit',creditNotation:'required',allowRedistribution:false,modification:'allowModification',copyrightInformation:'Arquivo importado pelo usuário. Direitos e metadados originais preservados; consulte os termos do fornecedor.'},humanoid:{humanBones},expressions:{preset,custom}};
 doc.extensionsUsed=[...new Set([...(doc.extensionsUsed||[]),'VRMC_vrm'])];
 const json=new TextEncoder().encode(JSON.stringify(doc)),padded=(json.length+3)&~3,tail=bytes.subarray(20+length),result=new Uint8Array(20+padded+tail.length),header=new DataView(result.buffer);
 header.setUint32(0,0x46546c67,true);header.setUint32(4,2,true);header.setUint32(8,result.length,true);header.setUint32(12,padded,true);header.setUint32(16,0x4e4f534a,true);result.fill(32,20,20+padded);result.set(json,20);result.set(tail,20+padded);
 return {buffer:result.buffer,expressions:Object.keys(custom).length};
}
export async function readAvaturnExport(data){
 if(!data||!['httpURL','dataURL'].includes(data.urlType)||typeof data.url!=='string')throw new Error('Exportação Avaturn inválida.');
 const url=new URL(data.url);if(data.urlType==='httpURL'&&url.protocol!=='https:')throw new Error('O download do avatar exige HTTPS.');
 if(data.urlType==='dataURL'&&!/^data:(model\/gltf-binary|application\/octet-stream);base64,/i.test(data.url))throw new Error('Formato GLB exportado inválido.');
 if(data.urlType==='dataURL'&&data.url.length>MAX*1.4)throw new Error('O avatar deve ter até 150 MB.');
 const response=await fetch(data.url,{credentials:'omit',signal:AbortSignal.timeout(90000)});if(!response.ok)throw new Error('Não foi possível baixar o avatar.');
 const reader=response.body.getReader(),parts=[];let size=0;
 try{for(;;){const {done,value}=await reader.read();if(done)break;size+=value.length;if(size>MAX)throw new Error('O avatar deve ter até 150 MB.');parts.push(value);}}finally{await reader.cancel();}
 const bytes=new Uint8Array(size);let offset=0;for(const part of parts){bytes.set(part,offset);offset+=part.length;}return convertAvaturnGlb(bytes);
}
