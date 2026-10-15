// Device preferences are presentation choices, never a replacement for privacy policy.
export function initTtsVoices(select,preview,message){
 const engine=globalThis.speechSynthesis,key='dingous.tts.web.voice.v1';
 let chosen='';try{chosen=localStorage.getItem(key)||'';}catch{}
 function localVoices(){return engine?.getVoices().filter(v=>v.localService)||[];}
 function refresh(){
  select.replaceChildren(new Option('Padrão do admin / pt-BR',''));
  for(const v of localVoices().sort((a,b)=>a.lang.localeCompare(b.lang)||a.name.localeCompare(b.name)))select.add(new Option(`${v.name} (${v.lang})`,v.voiceURI));
  select.value=chosen;
  if(chosen&&!Array.from(select.options).some(o=>o.value===chosen)){
   select.add(new Option('Voz salva indisponível neste navegador',chosen));select.value=chosen;
  }
  message.textContent=engine?(localVoices().length?'Voz deste navegador. Somente vozes locais.':'Nenhuma voz local disponível. Instale uma voz no sistema e reabra o navegador.'):'Síntese de voz não suportada neste navegador.';
  select.disabled=preview.disabled=!engine;
 }
 select.onchange=()=>{chosen=select.value;try{localStorage.setItem(key,chosen);}catch{message.textContent='Preferência ativa nesta sessão; armazenamento indisponível.';}};
 function configure(utterance,settings={}){
  const available=localVoices();
  utterance.voice=(chosen?available.find(v=>v.voiceURI===chosen):available.find(v=>v.name===settings.voiceName)||available.find(v=>v.lang.toLowerCase()==='pt-br'))||null;
  if(!utterance.voice){message.textContent='A voz escolhida não está disponível. Escolha outra voz local.';return false;}
  utterance.lang=utterance.voice.lang;
  utterance.rate=Math.max(.7,Math.min(1.3,1+(Number(settings.voiceRate)||0)*.08));
  utterance.volume=Math.max(0,Math.min(1,(settings.voiceVolume??85)/100));return true;
 }
 preview.onclick=()=>{const speech=new SpeechSynthesisUtterance('Olá, esta é uma amostra da voz escolhida.');if(configure(speech)){engine.cancel();engine.speak(speech);}};
 engine?.addEventListener('voiceschanged',refresh);refresh();
 return {configure};
}
