package br.com.dingous.avatar;
import android.app.*;
import android.content.*;
import android.content.pm.ServiceInfo;
import android.media.*;
import android.os.*;
import android.speech.tts.*;
import org.json.*;
import org.vosk.*;
import java.io.*;
import java.net.*;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.*;

public class DingousService extends Service {
 public static volatile DingousService instance;
 private volatile boolean running,listen,cameraRequested,ttsReady;
 private volatile int faces=-1;
 private volatile long faceTime=0,activateUntil=0;
 private final AtomicBoolean busy=new AtomicBoolean(false);
 private final AtomicLong audioChunks=new AtomicLong();
 private static final Object MODEL_LOCK=new Object();
 private final ExecutorService jobs=Executors.newSingleThreadExecutor();
 private TextToSpeech tts;private AudioRecord recorder;private Thread audioThread;private DingousCamera camera;
 private String status="Preparando serviços locais",answer="",question="";private volatile boolean asrReady,callDetected;
 public void onCreate(){super.onCreate();instance=this;running=true;
  getSystemService(NotificationManager.class).createNotificationChannel(new NotificationChannel("dingous", "Dingous · microfone e câmera",NotificationManager.IMPORTANCE_LOW));
  tts=new TextToSpeech(this,result->{if(result!=TextToSpeech.SUCCESS){setStatus("Voz Android indisponível");return;}Voice selected=null;for(Voice v:tts.getVoices())if(v.getLocale().getLanguage().equals("pt")&&!v.isNetworkConnectionRequired()&&!v.getFeatures().contains("notInstalled")){if(selected==null||v.getLocale().getCountry().equals("BR"))selected=v;}
   if(selected==null){setStatus("Instale uma voz pt-BR local no Android");return;}tts.setVoice(selected);tts.setSpeechRate(.92f);tts.setPitch(.98f);ttsReady=true;applyVoicePreference();setStatus("Voz local disponível");
   tts.setOnUtteranceProgressListener(new UtteranceProgressListener(){public void onStart(String id){setStatus("Voz local reproduzindo");}public void onDone(String id){setStatus("Voz concluída · escuta retomada");}public void onError(String id){setStatus("Falha na voz local; verifique os dados instalados");}});
  });
 }
 public void applyVoicePreference(){
  if(!ttsReady||tts==null||tts.getVoices()==null)return;
  String saved=getSharedPreferences("dingous",0).getString("ttsVoice","");Voice fallback=null;
  for(Voice v:tts.getVoices())if(!v.isNetworkConnectionRequired()&&(v.getFeatures()==null||!v.getFeatures().contains("notInstalled"))){if(v.getName().equals(saved)){tts.setVoice(v);return;}if(v.getLocale().getLanguage().equals("pt")&&(fallback==null||v.getLocale().getCountry().equals("BR")))fallback=v;}
  if(fallback!=null)tts.setVoice(fallback);
 }
 public int onStartCommand(Intent intent,int flags,int startId){
  if(intent!=null&&"stop".equals(intent.getAction())){stopSelf();return START_NOT_STICKY;}
  if(intent!=null){listen=intent.getBooleanExtra("voice",false);cameraRequested=intent.getBooleanExtra("camera",false);}
  int types=(listen?ServiceInfo.FOREGROUND_SERVICE_TYPE_MICROPHONE:0)|(cameraRequested?ServiceInfo.FOREGROUND_SERVICE_TYPE_CAMERA:0);
  Intent stop=new Intent(this,DingousService.class).setAction("stop");
  Intent launch=getPackageManager().getLaunchIntentForPackage(getPackageName());
  PendingIntent open=launch==null?null:PendingIntent.getActivity(this,1,launch,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);
  Notification note=new Notification.Builder(this,"dingous").setSmallIcon(android.R.drawable.ic_btn_speak_now).setContentTitle("Dingous ativo").setContentText(listen?"Escuta local: diga Dingous · toque para abrir":"Câmera local para avatar").setContentIntent(open).setOngoing(true).addAction(new Notification.Action.Builder(null,"Parar",PendingIntent.getService(this,0,stop,PendingIntent.FLAG_IMMUTABLE)).build()).build();
  if(android.os.Build.VERSION.SDK_INT>=29&&types!=0)startForeground(4101,note,types);else startForeground(4101,note);
  if(cameraRequested&&camera==null){camera=new DingousCamera(this);camera.start();}else if(!cameraRequested&&camera!=null){camera.close();camera=null;faces=-1;}
  if((audioThread==null||!audioThread.isAlive())&&listen){audioThread=new Thread(this::audioLoop,"Dingous-ASR");audioThread.start();}
  publish();return START_NOT_STICKY;
 }
 public IBinder onBind(Intent intent){return null;}
 private File copyModels()throws Exception{ synchronized(MODEL_LOCK){
  File root=new File(getFilesDir(),"AndroidModels"),ready=new File(root,".ready-v1");
  if(!ready.exists()){copyTree("AndroidModels",root);new FileOutputStream(ready).close();}return root;}
 }
 private void copyTree(String path,File target)throws Exception{String[] list=getAssets().list(path);if(list!=null&&list.length>0){target.mkdirs();for(String name:list)copyTree(path+"/"+name,new File(target,name));}else{target.getParentFile().mkdirs();try(InputStream input=getAssets().open(path);OutputStream out=new FileOutputStream(target)){byte[] b=new byte[65536];int n;while((n=input.read(b))>0){if(!running)throw new IOException("Serviço encerrado");out.write(b,0,n);}}}}
 public File models()throws Exception{return copyModels();}
 private void audioLoop(){
  try{setStatus("Preparando Vosk português offline");File models=copyModels();try(Model model=new Model(new File(models,"vosk-model-small-pt-0.3").getPath());Recognizer recognizer=new Recognizer(model,16000)){
   asrReady=true;int size=Math.max(8192,AudioRecord.getMinBufferSize(16000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT));
   recorder=new AudioRecord(MediaRecorder.AudioSource.VOICE_RECOGNITION,16000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT,size);
   if(recorder.getState()!=AudioRecord.STATE_INITIALIZED)throw new IOException("Microfone não inicializou");byte[] buffer=new byte[3200];setStatus("Microfone local pronto · diga Dingous");
   while(running){
    if(!listen||busy.get()||(tts!=null&&tts.isSpeaking())||inCall()){if(recorder.getRecordingState()==AudioRecord.RECORDSTATE_RECORDING)recorder.stop();recognizer.reset();Thread.sleep(150);continue;}
    if(recorder.getRecordingState()!=AudioRecord.RECORDSTATE_RECORDING)recorder.startRecording();
    int n=recorder.read(buffer,0,buffer.length);if(n<0)throw new IOException("Falha no microfone: "+n);audioChunks.incrementAndGet();
    if(recognizer.acceptWaveForm(buffer,n)){String text=new JSONObject(recognizer.getResult()).optString("text","");if(!text.isEmpty())handleSpeech(text);}if(audioChunks.get()%10==0)publish();
   }
  }}catch(Exception e){setStatus("ASR: "+e.getMessage());}finally{if(recorder!=null){try{recorder.stop();}catch(Exception ignored){}recorder.release();recorder=null;}asrReady=false;publish();}
 }
 private void handleSpeech(String text){
  java.util.regex.Matcher m=java.util.regex.Pattern.compile("\\b(?:dingous|dingus|dingos|dingo|din go)\\b",java.util.regex.Pattern.CASE_INSENSITIVE).matcher(text);
  if(m.find()){String command=text.substring(m.end()).trim();if(command.isEmpty()){activateUntil=System.currentTimeMillis()+8000;setStatus("Dingous ativado · faça sua pergunta");}else{activateUntil=0;ask(command);}}
  else if(System.currentTimeMillis()<activateUntil){activateUntil=0;ask(text);}
 }
 public boolean inCall(){AudioManager a=(AudioManager)getSystemService(AUDIO_SERVICE);int mode=a.getMode();callDetected=mode==AudioManager.MODE_IN_CALL||mode==AudioManager.MODE_IN_COMMUNICATION;return callDetected;}
 public void observedFaces(int count){faces=count;faceTime=System.currentTimeMillis();if(count>1&&tts!=null)tts.stop();
  if(listen&&count==1&&ttsReady&&!inCall()&&!busy.get()){
   String day=new java.text.SimpleDateFormat("yyyy-MM-dd",Locale.ROOT).format(new Date());
   if(!day.equals(getSharedPreferences("dingous",0).getString("greeting-day",""))){
    getSharedPreferences("dingous",0).edit().putString("greeting-day",day).apply();
    int hour=Calendar.getInstance().get(Calendar.HOUR_OF_DAY);
    String hello=hour<12?"Bom dia":hour<18?"Boa tarde":"Boa noite";
    new Handler(Looper.getMainLooper()).post(()->{if(listen&&faces==1&&!inCall())tts.speak(hello+". Estou por aqui. Diga Dingous quando precisar de mim.",TextToSpeech.QUEUE_ADD,null,"dingous-greeting");});
   }
  }
  publish();}
 public boolean ask(String text){if(text==null||text.trim().isEmpty()||text.length()>2000||!busy.compareAndSet(false,true))return false;question=text;setStatus("IA local pensando · microfone pausado");jobs.execute(()->{
  HttpURLConnection connection=null;
  try{android.content.SharedPreferences prefs=getSharedPreferences("dingous",0);String url=prefs.getString("url",""),token=prefs.getString("token","");if(url.isEmpty()||token.isEmpty())throw new IOException("Pareie a ponte de IA local primeiro");
   connection=(HttpURLConnection)new URL(url+"/ask").openConnection();connection.setConnectTimeout(10000);connection.setReadTimeout(120000);connection.setRequestMethod("POST");connection.setDoOutput(true);connection.setRequestProperty("Content-Type","application/json");connection.setRequestProperty("Authorization","Bearer "+token);
   byte[] data=new JSONObject().put("question",text).toString().getBytes("UTF-8");connection.setFixedLengthStreamingMode(data.length);try(OutputStream out=connection.getOutputStream()){out.write(data);}
   int code=connection.getResponseCode();InputStream input=code==200?connection.getInputStream():connection.getErrorStream();ByteArrayOutputStream result=new ByteArrayOutputStream();byte[] b=new byte[4096];int n;while((n=input.read(b))>0){if(result.size()+n>65536)throw new IOException("Resposta muito grande");result.write(b,0,n);}input.close();JSONObject reply=new JSONObject(result.toString("UTF-8"));if(code!=200)throw new IOException(reply.optString("error","HTTP "+code));answer=reply.getString("answer");
   boolean environment=faces==1&&System.currentTimeMillis()-faceTime<2000&&!inCall();
   if(reply.optBoolean("speechAllowed",false)&&environment&&ttsReady)tts.speak(answer,TextToSpeech.QUEUE_FLUSH,null,"dingous-answer");
   setStatus(reply.optBoolean("speechAllowed",false)&&environment?"Resposta recebida":"Privacidade ativa · resposta somente em texto");
  }catch(Exception e){setStatus("IA: "+e.getMessage());}finally{if(connection!=null)connection.disconnect();busy.set(false);publish();}
 });return true;}
 public void previewVoice(){if(!ttsReady){setStatus("Voz pt-BR local ainda indisponível");return;}if(inCall()||faces>1){setStatus("Voz bloqueada: ligação ou múltiplas pessoas");return;}tts.speak("Olá, eu sou o Dingous. Este é um teste de voz local, sem informações pessoais.",TextToSpeech.QUEUE_FLUSH,null,"dingous-preview");}
 public void setStatus(String value){status=value;android.util.Log.i("Dingous","STATE "+value);publish();}
 private void publish(){if(instance!=this)return;try{getSharedPreferences("dingous",0).edit().putString("state",new JSONObject().put("status",status).put("question",question).put("answer",answer).put("listening",listen).put("tracking",cameraRequested).put("asrReady",asrReady).put("voiceReady",ttsReady).put("speaking",tts!=null&&tts.isSpeaking()).put("audioChunks",audioChunks.get()).put("faces",faces).put("inCall",callDetected).put("trackingFrames",camera==null?0:camera.frames).toString()).apply();}catch(Exception ignored){}}
 public void onDestroy(){running=false;listen=false;if(camera!=null)camera.close();jobs.shutdownNow();if(tts!=null){tts.stop();tts.shutdown();}instance=null;getSharedPreferences("dingous",0).edit().remove("state").apply();super.onDestroy();}
}
