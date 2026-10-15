package br.com.dingous.avatar;
import android.app.*;
import android.content.*;
import android.widget.*;
import org.json.*;
import android.speech.tts.*;
import java.util.*;
public final class DingousNative {
 public static void start(Activity a,boolean voice,boolean camera){
  Intent i=new Intent(a,DingousService.class).putExtra("voice",voice).putExtra("camera",camera);
  a.startForegroundService(i);
 }
 public static void stop(Activity a){a.stopService(new Intent(a,DingousService.class));}
 public static String state(Activity a){return a.getSharedPreferences("dingous",0).getString("state","{}");}
 public static boolean ask(Activity a,String question){DingousService service=DingousService.instance;return service!=null&&service.ask(question);}
 public static void preview(Activity a){if(DingousService.instance!=null)DingousService.instance.previewVoice();}
 public static void chooseVoice(Activity a){a.runOnUiThread(()->{
  TextToSpeech[] engine=new TextToSpeech[1];
  engine[0]=new TextToSpeech(a,result->a.runOnUiThread(()->{
   if(result!=TextToSpeech.SUCCESS){Toast.makeText(a,"Motor TTS indisponível",Toast.LENGTH_LONG).show();if(engine[0]!=null)engine[0].shutdown();return;}
   TextToSpeech t=engine[0];ArrayList<Voice> available=new ArrayList<>();
   if(t.getVoices()!=null)for(Voice v:t.getVoices())if(!v.isNetworkConnectionRequired()&&(v.getFeatures()==null||!v.getFeatures().contains("notInstalled")))available.add(v);
   available.sort(Comparator.comparing(v->v.getLocale().toLanguageTag()+v.getName()));
   String saved=a.getSharedPreferences("dingous",0).getString("ttsVoice","");int current=0;
   String[] labels=new String[available.size()+1];labels[0]="Padrão local pt-BR";
   for(int i=0;i<available.size();i++){Voice v=available.get(i);labels[i+1]=v.getLocale().toLanguageTag()+" · "+v.getName();if(v.getName().equals(saved))current=i+1;}
   int[] selected={current};
   AlertDialog dialog=new AlertDialog.Builder(a).setTitle("Voz TTS · Android · offline").setSingleChoiceItems(labels,current,(d,index)->{
    selected[0]=index;if(index>0)t.setVoice(available.get(index-1));else t.setLanguage(new Locale("pt","BR"));
    t.speak("Olá, esta é uma amostra da voz escolhida.",TextToSpeech.QUEUE_FLUSH,null,"voice-choice");
   }).setPositiveButton("Salvar",(d,w)->{String name=selected[0]==0?"":available.get(selected[0]-1).getName();a.getSharedPreferences("dingous",0).edit().putString("ttsVoice",name).apply();if(DingousService.instance!=null)DingousService.instance.applyVoicePreference();}).setNegativeButton("Cancelar",null).create();
   dialog.setOnDismissListener(d->{t.stop();t.shutdown();});dialog.show();
  }));
 });}
 public static void installVoice(Activity a){a.runOnUiThread(()->{try{a.startActivity(new Intent(android.speech.tts.TextToSpeech.Engine.ACTION_INSTALL_TTS_DATA));}catch(ActivityNotFoundException e){Toast.makeText(a,"Instale um motor de voz pt-BR offline nas configurações do Android.",Toast.LENGTH_LONG).show();}});}
 public static void pair(Activity a){a.runOnUiThread(()->{
  LinearLayout layout=new LinearLayout(a);layout.setOrientation(1);
  EditText url=new EditText(a),token=new EditText(a);url.setHint("URL da ponte HTTPS ou loopback ADB");url.setInputType(android.text.InputType.TYPE_CLASS_TEXT|android.text.InputType.TYPE_TEXT_VARIATION_URI);url.setSingleLine(true);url.setText(a.getSharedPreferences("dingous",0).getString("url","http://127.0.0.1:8767"));token.setHint("Código secreto de pareamento");token.setInputType(129);token.setSingleLine(true);layout.addView(url);layout.addView(token);
  new AlertDialog.Builder(a).setTitle("IA local · parear computador").setView(layout).setPositiveButton("Salvar",(d,w)->configure(a,url.getText().toString().trim(),token.getText().toString().trim())).setNegativeButton("Cancelar",null).show();
 });}
 public static void configure(Activity a,String url,String token){
  try{java.net.URI u=new java.net.URI(url);boolean loop="127.0.0.1".equals(u.getHost())||"localhost".equals(u.getHost());if(u.getUserInfo()!=null||!("https".equals(u.getScheme())||("http".equals(u.getScheme())&&loop))||token.length()<32)throw new Exception("Use HTTPS e código válido; loopback HTTP somente para ADB.");
   a.getSharedPreferences("dingous",0).edit().putString("url",url).putString("token",token).apply();
  }catch(Exception e){Toast.makeText(a,e.getMessage(),Toast.LENGTH_LONG).show();}
 }
}
