import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {validatePresets,presetState} from '../Web/avatar-presets.js';
const catalog=JSON.parse(await readFile(new URL('../Web/assets/avatar-presets.json',import.meta.url),'utf8'));
const presets=validatePresets(catalog);assert.equal(presets.length,12);
for(const category of ['masculino','feminino'])assert.equal(presets.filter(p=>p.category===category).length,6);
for(const p of presets){const state=presetState(p,'stylized');assert.equal(state.human.style,'stylized');assert.equal(state.human.presetId,p.id);assert.equal(state.builtin,p.category==='feminino'?'woman':'makehuman');}
assert.throws(()=>validatePresets({...catalog,presets:[presets[0],presets[0]]}));
assert.throws(()=>validatePresets({...catalog,presets:[{...presets[0],model:'../outside.vrm'}]}));
console.log('AVATAR_PRESETS_WEB=PASS');
