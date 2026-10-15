using System;
using System.Collections;
using System.IO;
using CrazyMinnow.SALSA;
using UnityEngine;

namespace WebcamAvatar {
    // SALSA writes a private control mesh; UniVRM remains the sole writer of the visible face.
    // This prevents SALSA and the facial-expression rig overwriting one another every frame.
    public sealed class SpeechLipSync : MonoBehaviour {
        public AvatarTracking tracking;
        public AudioSource Source {get;private set;}
        public Salsa Engine {get;private set;}
        public bool Playing=>Source!=null&&Source.isPlaying;
        public float Jaw=>Playing?Mathf.Clamp(proxy.GetBlendShapeWeight(0)*.0028f+proxy.GetBlendShapeWeight(1)*.0048f+proxy.GetBlendShapeWeight(2)*.0072f,0,.72f):0;
        public float Round=>Playing?Mathf.Clamp01(proxy.GetBlendShapeWeight(1)*.0018f):0;
        SkinnedMeshRenderer proxy;Mesh mesh;Coroutine playback;AudioClip clip;string activeFile;float peakJaw;
        public float PeakJaw=>peakJaw;
        void Awake(){
            if(FindAnyObjectByType<AudioListener>()==null)gameObject.AddComponent<AudioListener>();
            Source=gameObject.AddComponent<AudioSource>();Source.playOnAwake=false;Source.loop=false;Source.spatialBlend=0;
            var control=new GameObject("SALSA face controls");control.transform.SetParent(transform,false);
            proxy=control.AddComponent<SkinnedMeshRenderer>();proxy.enabled=false;
            mesh=new Mesh{name="SALSA expression bridge"};mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up};mesh.triangles=new[]{0,1,2};
            var empty=new Vector3[3];foreach(string name in new[]{"soft","medium","open"})mesh.AddBlendShapeFrame(name,100,empty,empty,empty);proxy.sharedMesh=mesh;
            Engine=control.AddComponent<Salsa>();Engine.audioSrc=Source;Engine.queueProcessor=control.AddComponent<QueueProcessor>();
            Engine.autoAdjustAnalysis=true;Engine.autoAdjustMicrophone=false;Engine.audioUpdateDelay=.06f;
            Engine.loCutoff=.012f;Engine.hiCutoff=.65f;Engine.useAdvDyn=true;Engine.advDynPrimaryBias=.4f;
            Engine.useAdvDynJitter=false;Engine.advDynSecondaryMix=0;
            Engine.visemes.Clear();
            foreach(string name in new[]{"soft","medium","open"}){
                int i=Engine.visemes.Count;var expression=new LipsyncExpression(name,new InspectorControllerHelperData(),0);
                var data=expression.expData;data.components[0].name=name;data.components[0].durationOn=.055f;data.components[0].durationOff=.09f;
                data.controllerVars[0].smr=proxy;data.controllerVars[0].blendIndex=i;data.controllerVars[0].minShape=0;data.controllerVars[0].maxShape=1;
                Engine.visemes.Add(expression);
            }
            Engine.DistributeTriggers(LerpEasings.EasingType.SquaredIn);Engine.UpdateExpressionControllers();
        }
        public void PlayFile(string path){
            var full=Path.GetFullPath(path);var root=Path.GetFullPath(LocalJob.SpeechRoot)+Path.DirectorySeparatorChar;
            if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!full.EndsWith(".wav",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Áudio fora da pasta privada de fala.");
            Stop();activeFile=full;playback=StartCoroutine(Play(full));
        }
        IEnumerator Play(string path){
            byte[] bytes=null;try{var info=new FileInfo(path);if(info.Length>32000000)throw new InvalidDataException();bytes=File.ReadAllBytes(path);clip=DecodeWave(bytes);}
            catch(Exception){Complete("error");yield break;}
            Source.clip=clip;peakJaw=0;Source.Play();
            yield return null;
            while(Source.isPlaying){peakJaw=Mathf.Max(peakJaw,Jaw);yield return null;}
            Complete("ok");playback=null;
        }
        public void Stop(){if(playback!=null){StopCoroutine(playback);playback=null;}if(Source!=null)Source.Stop();Complete("cancelled");}
        void Complete(string result){
            if(Engine!=null)Engine.TurnOffAll();
            if(Source!=null){Source.Stop();Source.clip=null;}
            if(clip!=null){Destroy(clip);clip=null;}
            if(proxy!=null)for(int i=0;i<3;i++)proxy.SetBlendShapeWeight(i,0);
            if(!string.IsNullOrEmpty(activeFile)){try{File.WriteAllText(Path.ChangeExtension(activeFile,".done"),result);File.Delete(activeFile);}catch(IOException){}activeFile=null;}
        }
        public static AudioClip DecodeWave(byte[] bytes){
            if(bytes.Length<44||System.Text.Encoding.ASCII.GetString(bytes,0,4)!="RIFF"||System.Text.Encoding.ASCII.GetString(bytes,8,4)!="WAVE")throw new InvalidDataException("WAV inválido.");
            int channels=0,rate=0,bits=0,format=0,start=0,count=0;
            for(int p=12;p<=bytes.Length-8;){int size=BitConverter.ToInt32(bytes,p+4);if(size<0||size>bytes.Length-p-8)throw new InvalidDataException();string id=System.Text.Encoding.ASCII.GetString(bytes,p,4);
                if(id=="fmt "&&size>=16){format=BitConverter.ToUInt16(bytes,p+8);channels=BitConverter.ToUInt16(bytes,p+10);rate=BitConverter.ToInt32(bytes,p+12);bits=BitConverter.ToUInt16(bytes,p+22);}
                if(id=="data"){start=p+8;count=size;}
                p+=8+size+(size&1);
            }
            if(format!=1||channels<1||channels>2||rate<8000||rate>96000||bits!=16||count<2||count%(channels*2)!=0||count/(channels*2f*rate)>300)throw new InvalidDataException("Use áudio PCM16 de até cinco minutos.");
            var samples=new float[count/2];for(int i=0;i<samples.Length;i++)samples[i]=BitConverter.ToInt16(bytes,start+i*2)/32768f;
            var result=AudioClip.Create("Dingous speech",samples.Length/channels,channels,rate,false);result.SetData(samples,0);return result;
        }
        void OnDisable(){Stop();}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
