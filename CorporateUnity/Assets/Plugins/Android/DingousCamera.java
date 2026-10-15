package br.com.dingous.avatar;
import android.content.*;
import android.graphics.*;
import android.hardware.camera2.*;
import android.media.*;
import android.os.*;
import android.view.*;
import com.google.mediapipe.framework.image.*;
import com.google.mediapipe.tasks.core.*;
import com.google.mediapipe.tasks.components.containers.*;
import com.google.mediapipe.tasks.vision.core.*;
import com.google.mediapipe.tasks.vision.facelandmarker.*;
import com.google.mediapipe.tasks.vision.poselandmarker.*;
import com.google.mediapipe.tasks.vision.handlandmarker.*;
import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.*;
import java.util.*;

final class DingousCamera {
 final DingousService service;volatile long frames=0;private volatile boolean running=true;
 private HandlerThread thread;private Handler handler;private CameraDevice device;private ImageReader reader;private CameraCaptureSession session;
 private FaceLandmarker face;private PoseLandmarker pose;private HandLandmarker hands;private DatagramSocket udp;private int rotation,sensorRotation;private boolean frontCamera;
 DingousCamera(DingousService service){this.service=service;}
 void start(){thread=new HandlerThread("Dingous-Camera");thread.start();handler=new Handler(thread.getLooper());handler.post(()->{try{
  File models=service.models();
  face=FaceLandmarker.createFromOptions(service,FaceLandmarker.FaceLandmarkerOptions.builder().setBaseOptions(base(new File(models,"face.task"))).setNumFaces(3).setOutputFaceBlendshapes(true).setOutputFacialTransformationMatrixes(true).setRunningMode(RunningMode.VIDEO).build());
  pose=PoseLandmarker.createFromOptions(service,PoseLandmarker.PoseLandmarkerOptions.builder().setBaseOptions(base(new File(models,"pose.task"))).setRunningMode(RunningMode.VIDEO).build());
  hands=HandLandmarker.createFromOptions(service,HandLandmarker.HandLandmarkerOptions.builder().setBaseOptions(base(new File(models,"hand.task"))).setNumHands(2).setRunningMode(RunningMode.VIDEO).build());udp=new DatagramSocket();
  CameraManager manager=service.getSystemService(CameraManager.class);String selected=null;for(String id:manager.getCameraIdList()){if(selected==null)selected=id;if(manager.getCameraCharacteristics(id).get(CameraCharacteristics.LENS_FACING)==CameraCharacteristics.LENS_FACING_FRONT){selected=id;break;}}
  if(selected==null)throw new IOException("Nenhuma câmera disponível");CameraCharacteristics c=manager.getCameraCharacteristics(selected);int sensor=c.get(CameraCharacteristics.SENSOR_ORIENTATION);sensorRotation=sensor;frontCamera=c.get(CameraCharacteristics.LENS_FACING)==CameraCharacteristics.LENS_FACING_FRONT;int display=((WindowManager)service.getSystemService(Context.WINDOW_SERVICE)).getDefaultDisplay().getRotation()*90;rotation=c.get(CameraCharacteristics.LENS_FACING)==CameraCharacteristics.LENS_FACING_FRONT?(sensor+display)%360:(sensor-display+360)%360;
  reader=ImageReader.newInstance(640,480,ImageFormat.YUV_420_888,2);reader.setOnImageAvailableListener(this::image,handler);
  manager.openCamera(selected,new CameraDevice.StateCallback(){public void onOpened(CameraDevice camera){if(!running){camera.close();return;}device=camera;try{camera.createCaptureSession(Collections.singletonList(reader.getSurface()),new CameraCaptureSession.StateCallback(){public void onConfigured(CameraCaptureSession s){if(!running){s.close();return;}session=s;try{CaptureRequest.Builder request=device.createCaptureRequest(CameraDevice.TEMPLATE_PREVIEW);request.addTarget(reader.getSurface());request.set(CaptureRequest.CONTROL_AF_MODE,CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_VIDEO);s.setRepeatingRequest(request.build(),null,handler);service.setStatus("Câmera MediaPipe local ativa");}catch(Exception e){service.setStatus("Câmera: "+e.getMessage());}}public void onConfigureFailed(CameraCaptureSession s){service.setStatus("Configuração de câmera falhou");}},handler);}catch(Exception e){service.setStatus("Câmera: "+e.getMessage());}}public void onDisconnected(CameraDevice camera){camera.close();service.observedFaces(-1);}public void onError(CameraDevice camera,int error){camera.close();service.setStatus("Erro de câmera "+error);}},handler);
 }catch(Exception e){service.setStatus("MediaPipe: "+e.getMessage());}});}
 private BaseOptions base(File file){return BaseOptions.builder().setModelAssetPath(file.getPath()).build();}
 private long last=0;
 private void image(ImageReader source){try(Image image=source.acquireLatestImage()){if(image==null||!running)return;long now=SystemClock.elapsedRealtime();if(now-last<150)return;last=now;Bitmap bitmap=bitmap(image);int display=((WindowManager)service.getSystemService(Context.WINDOW_SERVICE)).getDefaultDisplay().getRotation()*90;rotation=frontCamera?(sensorRotation+display)%360:(sensorRotation-display+360)%360;Matrix m=new Matrix();m.postRotate(rotation);Bitmap rotated=Bitmap.createBitmap(bitmap,0,0,bitmap.getWidth(),bitmap.getHeight(),m,false);
  try(MPImage input=new BitmapImageBuilder(rotated).build()){
   FaceLandmarkerResult f=face.detectForVideo(input,now);PoseLandmarkerResult p=pose.detectForVideo(input,now);HandLandmarkerResult h=hands.detectForVideo(input,now);service.observedFaces(f.faceLandmarks().size());
   JSONObject packet=new JSONObject().put("face",!f.faceLandmarks().isEmpty()).put("pose",!p.worldLandmarks().isEmpty()).put("shapes",new JSONArray()).put("points",new JSONArray()).put("rotation",new JSONArray()).put("imagePose",new JSONArray()).put("leftHand",new JSONArray()).put("rightHand",new JSONArray());
   if(f.faceBlendshapes().isPresent()&&!f.faceBlendshapes().get().isEmpty()){JSONArray shapes=new JSONArray();for(Category category:f.faceBlendshapes().get().get(0))shapes.put(new JSONObject().put("name",category.categoryName()).put("value",category.score()));packet.put("shapes",shapes);}
   if(f.facialTransformationMatrixes().isPresent()&&!f.facialTransformationMatrixes().get().isEmpty()){float[] transform=f.facialTransformationMatrixes().get().get(0);JSONArray r=new JSONArray();for(int row=0;row<3;row++)for(int col=0;col<3;col++)r.put(transform[col*4+row]);packet.put("rotation",r);}
   if(!p.worldLandmarks().isEmpty()){JSONArray points=new JSONArray(),imagePose=new JSONArray();for(int j=0;j<33;j++){NormalizedLandmark normalized=p.landmarks().get(0).get(j);Landmark world=p.worldLandmarks().get(0).get(j);float conf=normalized.x()>=0&&normalized.x()<=1&&normalized.y()>=0&&normalized.y()<=1?world.visibility().orElse(0f):0;points.put(point(world.x(),world.y(),world.z(),conf));imagePose.put(point(normalized.x(),normalized.y(),normalized.z(),conf));}packet.put("points",points).put("imagePose",imagePose);}
   for(int i=0;i<h.worldLandmarks().size();i++){NormalizedLandmark wrist=h.landmarks().get(i).get(0);boolean left=!h.handedness().get(i).get(0).categoryName().equals("Left");if(!p.landmarks().isEmpty()){NormalizedLandmark l=p.landmarks().get(0).get(15),r=p.landmarks().get(0).get(16);if(l.visibility().orElse(0f)>.5&&r.visibility().orElse(0f)>.5)left=distance(wrist,l)<distance(wrist,r);else if(l.visibility().orElse(0f)>.5&&l.x()>=0&&l.x()<=1&&l.y()>=0&&l.y()<=1&&distance(wrist,l)<.0225f)left=true;else if(r.visibility().orElse(0f)>.5&&r.x()>=0&&r.x()<=1&&r.y()>=0&&r.y()<=1&&distance(wrist,r)<.0225f)left=false;}JSONArray hand=new JSONArray();for(Landmark landmark:h.worldLandmarks().get(i))hand.put(point(landmark.x(),landmark.y(),landmark.z(),h.handedness().get(i).get(0).score()));packet.put(left?"leftHand":"rightHand",hand);}
   byte[] data=packet.toString().getBytes("UTF-8");udp.send(new DatagramPacket(data,data.length,InetAddress.getByName("127.0.0.1"),5059));frames++;if(frames%30==0)android.util.Log.i("Dingous","TRACKING frames="+frames+" faces="+f.faceLandmarks().size()+" pose="+!p.worldLandmarks().isEmpty()+" hands="+h.worldLandmarks().size());
  }finally{if(rotated!=bitmap)rotated.recycle();bitmap.recycle();}
 }catch(Exception e){if(running)service.setStatus("Tracking: "+e.getMessage());}}
 private float distance(NormalizedLandmark a,NormalizedLandmark b){return (a.x()-b.x())*(a.x()-b.x())+(a.y()-b.y())*(a.y()-b.y());}
 private JSONObject point(float x,float y,float z,float confidence)throws JSONException{return new JSONObject().put("x",x).put("y",y).put("z",z).put("confidence",confidence);}
 private Bitmap bitmap(Image image){int w=image.getWidth(),h=image.getHeight();Image.Plane[] planes=image.getPlanes();ByteBuffer y=planes[0].getBuffer(),u=planes[1].getBuffer(),v=planes[2].getBuffer();int[] pixels=new int[w*h];for(int row=0;row<h;row++)for(int col=0;col<w;col++){int yy=(y.get(row*planes[0].getRowStride()+col*planes[0].getPixelStride())&255)-16;int uu=(u.get((row/2)*planes[1].getRowStride()+(col/2)*planes[1].getPixelStride())&255)-128;int vv=(v.get((row/2)*planes[2].getRowStride()+(col/2)*planes[2].getPixelStride())&255)-128;int r=Math.max(0,Math.min(255,(298*yy+409*vv+128)>>8)),g=Math.max(0,Math.min(255,(298*yy-100*uu-208*vv+128)>>8)),b=Math.max(0,Math.min(255,(298*yy+516*uu+128)>>8));pixels[row*w+col]=0xff000000|(r<<16)|(g<<8)|b;}return Bitmap.createBitmap(pixels,w,h,Bitmap.Config.ARGB_8888);}
 void close(){running=false;if(session!=null)session.close();if(device!=null)device.close();if(reader!=null)reader.close();if(handler!=null)handler.post(()->{if(face!=null)face.close();if(pose!=null)pose.close();if(hands!=null)hands.close();if(udp!=null)udp.close();thread.quitSafely();});service.observedFaces(-1);}
}
