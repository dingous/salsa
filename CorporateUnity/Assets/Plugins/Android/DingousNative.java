package br.com.dingous.avatar;
import android.app.*;
import android.content.*;
import android.widget.*;
import org.json.*;
public final class DingousNative {
 public static void start(Activity a,boolean voice,boolean camera){
  Intent i=new Intent(a,DingousService.class).putExtra("voice",voice).putExtra("camera",camera);
  a.startForegroundService(i);
 }
 public static void stop(Activity a){a.stopService(new Intent(a,DingousService.class));}
 public static String state(Activity a){return a.getSharedPreferences("dingous",0).getString("state","{}");}
 public static boolean ask(Activity a,String question){DingousService service=DingousService.instance;return service!=null&&service.ask(question);}
 public static void preview(Activity a){if(DingousService.instance!=null)DingousService.instance.previewVoice();}
 public static void pair(Activity a){a.runOnUiThread(()->{
  LinearLayout layout=new LinearLayout(a);layout.setOrientation(1);
  EditText url=new EditText(a),token=new EditText(a);url.setHint("URL da ponte HTTPS ou http://127.0.0.1:8767 (ADB)");token.setHint("Código secreto de pareamento");token.setInputType(129);layout.addView(url);layout.addView(token);
  new AlertDialog.Builder(a).setTitle("IA local · parear computador").setView(layout).setPositiveButton("Salvar",(d,w)->configure(a,url.getText().toString().trim(),token.getText().toString().trim())).setNegativeButton("Cancelar",null).show();
 });}
 public static void configure(Activity a,String url,String token){
  try{java.net.URI u=new java.net.URI(url);boolean loop="127.0.0.1".equals(u.getHost())||"localhost".equals(u.getHost());if(u.getUserInfo()!=null||!("https".equals(u.getScheme())||("http".equals(u.getScheme())&&loop))||token.length()<32)throw new Exception("Use HTTPS e código válido; loopback HTTP somente para ADB.");
   a.getSharedPreferences("dingous",0).edit().putString("url",url).putString("token",token).apply();
  }catch(Exception e){Toast.makeText(a,e.getMessage(),Toast.LENGTH_LONG).show();}
 }
}
