import {avaturnBones} from './avaturn-glb.js';

// A prepared VRM is required: raw Unreal DNA/RigLogic is not a glTF face rig.
export function prepareMetaHumanVrm(input){
 const bytes=input instanceof Uint8Array?input:new Uint8Array(input);
 if(bytes.length<28||bytes.length>150*1024*1024)throw new Error('Use um VRM 1.0 preparado de até 150 MB.');
 const view=new DataView(bytes.buffer,bytes.byteOffset,bytes.length);
 if(view.getUint32(0,true)!==0x46546c67||view.getUint32(4,true)!==2||view.getUint32(8,true)!==bytes.length||view.getUint32(16,true)!==0x4e4f534a)throw new Error('Arquivo VRM inválido. DNA, FBX e .uasset precisam de conversão.');
 const length=view.getUint32(12,true);if(length>bytes.length-20)throw new Error('Cabeçalho VRM inválido.');
 const doc=JSON.parse(new TextDecoder().decode(bytes.subarray(20,20+length))),vrm=doc.extensions?.VRMC_vrm;
 if(!vrm||vrm.specVersion!=='1.0')throw new Error('Converta o MetaHuman para VRM 1.0 no Unity/UniVRM antes de importar.');
 for(const section of ['buffers','images'])for(const item of doc[section]||[])if(item.uri&&!item.uri.startsWith('data:'))throw new Error('O VRM deve incluir suas texturas e buffers.');
 const optional=new Set(['upperChest','chest','neck','leftShoulder','rightShoulder','leftToes','rightToes']),used=new Set();
 for(const key of Object.keys(avaturnBones()).filter(k=>!optional.has(k))){
  const index=vrm.humanoid?.humanBones?.[key]?.node;
  if(!Number.isInteger(index)||index<0||index>=(doc.nodes?.length||0)||used.has(index))throw new Error('Rig MetaHuman preparado incompleto ou ambíguo: '+key);
  used.add(index);
 }
 const facialBindings=Object.values({...vrm.expressions?.preset,...vrm.expressions?.custom}).some(e=>e.morphTargetBinds?.length>0);
 doc.asset??={version:'2.0'};doc.asset.extras??={};doc.asset.extras.dingousFramework='metahuman';
 const json=new TextEncoder().encode(JSON.stringify(doc)),padded=(json.length+3)&~3,tail=bytes.subarray(20+length),result=new Uint8Array(20+padded+tail.length),header=new DataView(result.buffer);
 header.setUint32(0,0x46546c67,true);header.setUint32(4,2,true);header.setUint32(8,result.length,true);header.setUint32(12,padded,true);header.setUint32(16,0x4e4f534a,true);result.fill(32,20,20+padded);result.set(json,20);result.set(tail,20+padded);
 return {buffer:result.buffer,facialBindings};
}
