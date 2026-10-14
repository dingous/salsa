export const COOKIE_NAME='avatar_appearance_v1';
export const DEFAULT_APPEARANCE={version:1,presentation:'professional',background:'studio',framing:'bust',mirror:true,colors:{}};
export function sanitizeAppearance(value){
  const data=value&&typeof value==='object'?value:{};
  const presentation=data.presentation==='corporate'?'corporate':'professional';
  const colors={};for(const key of ['skin','hair','eyes','outfit'])if(/^#[0-9a-f]{6}$/i.test(data.colors?.[key]||''))colors[key]=data.colors[key].toUpperCase();
  return {version:1,presentation,background:['studio','corporate','green','dark'].includes(data.background)?data.background:presentation==='corporate'?'corporate':'studio',framing:data.framing==='full'?'full':'bust',mirror:typeof data.mirror==='boolean'?data.mirror:true,colors};
}
export function readAppearance(cookie=document.cookie){
  try{const entry=cookie.split(';').map(s=>s.trim()).find(s=>s.startsWith(COOKIE_NAME+'='));if(!entry)return null;const value=JSON.parse(decodeURIComponent(entry.slice(COOKIE_NAME.length+1)));return value.version===1?sanitizeAppearance(value):null;}catch{return null;}
}
export function writeAppearance(value,doc=document,loc=location){
  const encoded=encodeURIComponent(JSON.stringify(sanitizeAppearance(value)));
  const path=loc.pathname.startsWith('/Games/Avatar')?'/Games/Avatar/':'/';
  try{doc.cookie=`${COOKIE_NAME}=${encoded}; Max-Age=15552000; Path=${path}; SameSite=Lax${loc.protocol==='https:'?'; Secure':''}`;return JSON.stringify(readAppearance(doc.cookie))===JSON.stringify(sanitizeAppearance(value));}catch{return false;}
}
function modelStore(mode,action){
  return new Promise((resolve,reject)=>{
    if(!globalThis.indexedDB){reject(new Error('Armazenamento de avatar indisponível.'));return;}
    const request=indexedDB.open('avatar-studio-guest',1);let finished=false;
    const finish=(error,value)=>{if(finished)return;finished=true;clearTimeout(timer);error?reject(error):resolve(value);};
    const timer=setTimeout(()=>finish(new Error('O armazenamento local não respondeu.')),5000);
    request.onupgradeneeded=()=>{if(!request.result.objectStoreNames.contains('models'))request.result.createObjectStore('models');};
    request.onerror=()=>finish(request.error);request.onblocked=()=>finish(new Error('Feche outra aba do Avatar para liberar o armazenamento.'));
    request.onsuccess=()=>{
      const db=request.result;if(finished){db.close();return;}
      try{const tx=db.transaction('models',mode),operation=action(tx.objectStore('models'));let value;
        operation.onsuccess=()=>{value=operation.result;};
        tx.oncomplete=()=>{db.close();finish(null,value);};tx.onerror=tx.onabort=()=>{db.close();finish(tx.error||new Error('Falha ao guardar o avatar.'));};
      }catch(error){db.close();finish(error);}
    };
  });
}
export const loadGuestModel=()=>modelStore('readonly',store=>store.get('selected'));
export const saveGuestModel=file=>modelStore('readwrite',store=>store.put(file,'selected'));
export const clearGuestModel=()=>modelStore('readwrite',store=>store.delete('selected'));
