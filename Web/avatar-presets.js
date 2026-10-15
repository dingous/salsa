export function validatePresets(catalog){
 if(catalog?.version!==1||!Array.isArray(catalog.presets))throw new Error('Catálogo de avatares inválido.');
 const ids=new Set();
 for(const p of catalog.presets){
  if(!/^[a-z0-9-]+$/.test(p.id)||ids.has(p.id)||!['masculino','feminino'].includes(p.category)||!['CorporateHuman.vrm','CorporateWoman.vrm'].includes(p.model))throw new Error('Preset inválido.');
  for(const key of ['skin','hair','eyes','outfit'])if(!/^#[0-9a-f]{6}$/i.test(p[key]))throw new Error('Cor do preset inválida.');
  ids.add(p.id);
 }
 return catalog.presets;
}
export function presetState(p,style='realistic'){
 return {builtin:p.model==='CorporateWoman.vrm'?'woman':'makehuman',human:{style,presetId:p.id,hairStyle:p.hairStyle,clothingStyle:p.clothingStyle,faceWidth:p.faceWidth,jawWidth:p.jawWidth,beard:p.beard,glasses:p.glasses},colors:{skin:p.skin,hair:p.hair,eyes:p.eyes,outfit:p.outfit}};
}
