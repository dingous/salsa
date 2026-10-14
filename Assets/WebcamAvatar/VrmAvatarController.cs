using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniVRM10;
using UnityEngine;

namespace WebcamAvatar
{
    [DefaultExecutionOrder(12000)]
    public sealed class VrmAvatarController : MonoBehaviour
    {
        public CartoonAvatar settings;
        public AvatarTracking tracking;
        public Vrm10Instance Instance {get;private set;}
        public string SourcePath {get;private set;}
        public bool Active=>Instance!=null && Instance.gameObject.activeSelf;
        public bool Loading {get;private set;}
        public int ExpressionCount=>Instance==null?0:Instance.Runtime.Expression.ExpressionKeys.Count;
        public string ModelName=>Instance==null?"":Instance.Vrm.Meta.Name;
        readonly Dictionary<HumanBodyBones,Transform> bones=new Dictionary<HumanBodyBones,Transform>();
        readonly Dictionary<HumanBodyBones,Quaternion> localRest=new Dictionary<HumanBodyBones,Quaternion>(),worldRest=new Dictionary<HumanBodyBones,Quaternion>();
        readonly Dictionary<HumanBodyBones,Vector3> directions=new Dictionary<HumanBodyBones,Vector3>();
        readonly Dictionary<HumanBodyBones,Quaternion> palmRest=new Dictionary<HumanBodyBones,Quaternion>();
        readonly Dictionary<Material,Color> originalColors=new Dictionary<Material,Color>();
        public void ApplyAppearance() {
            if(Instance==null)return;
            foreach(var pair in originalColors) {
                var material=pair.Key;string name=material.name.ToLowerInvariant();Color color=pair.Value;
                if(settings.Profile.vrmColors) {
                    string hex=name.Contains("hair")?settings.Profile.hair:name.Contains("eye")&&!name.Contains("trans")?settings.Profile.eyes:name.Contains("huku")||name.Contains("cloth")||name.Contains("outfit")?settings.Profile.outfit:name.Contains("body_bake")||name.Contains("skin")?settings.Profile.skin:null;
                    if(hex!=null)color=AvatarProfile.ColorOf(hex,color);
                }
                if(material.HasProperty("_Color"))material.SetColor("_Color",color);
                if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
            }
        }
        public void ResetAppearance(){settings.Profile.vrmColors=false;ApplyAppearance();}
        Vector3 origin;
        Quaternion facing;
        float clock;
        public static string DefaultPath=>Path.Combine(Application.streamingAssetsPath,"Avatars","Seed-san.vrm");
        public async Task Load(string path) {
            if(Loading)throw new InvalidOperationException("Um modelo já está sendo carregado.");
            Loading=true;Vrm10Instance candidate=null;
            try {
                var file=new FileInfo(path);
                if(!file.Exists || !path.EndsWith(".vrm",StringComparison.OrdinalIgnoreCase))throw new IOException("Selecione um arquivo .vrm válido.");
                if(file.Length>150*1024*1024)throw new IOException("O modelo deve ter até 150 MB.");
                candidate=await Vrm10.LoadPathAsync(path,canLoadVrm0X:true,showMeshes:false);
                if(candidate==null)throw new IOException("O arquivo não contém um avatar VRM compatível.");
                candidate.UpdateType=Vrm10Instance.UpdateTypes.None;
                var rig=candidate.Runtime.ControlRig;
                if(rig==null || rig.GetBoneTransform(HumanBodyBones.Head)==null)throw new IOException("O modelo não possui rig humanoide válido.");
                // VRM faces +Z. Our studio camera is on -Z.
                candidate.transform.rotation=Quaternion.Euler(0,180,0);
                candidate.transform.position=Vector3.zero;
                var renderers=candidate.GetComponentsInChildren<Renderer>(true);
                var bounds=new Bounds();bool first=true;
                foreach(var renderer in renderers){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
                if(first || bounds.size.y<.1f)throw new IOException("O modelo não contém malhas visíveis.");
                float scale=2.15f/bounds.size.y;candidate.transform.localScale*=scale;
                candidate.transform.position=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale-.30f,-bounds.center.z*scale);
                if(Instance!=null)Destroy(Instance.gameObject);
                Instance=candidate;candidate=null;SourcePath=Path.GetFullPath(path);origin=Instance.transform.position;facing=Instance.transform.rotation;
                bones.Clear();localRest.Clear();worldRest.Clear();directions.Clear();palmRest.Clear();
                foreach(HumanBodyBones key in Enum.GetValues(typeof(HumanBodyBones))) {
                    if(key==HumanBodyBones.LastBone)continue;
                    var bone=rig.GetBoneTransform(key);if(bone==null)continue;
                    bones[key]=bone;localRest[key]=bone.localRotation;worldRest[key]=bone.rotation;
                    directions[key]=bone.childCount>0?bone.GetChild(0).position-bone.position:bone.position-bone.parent.position;
                }
                foreach(bool left in new[]{true,false}) {
                    var wrist=left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand;
                    var index=left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal;
                    var middle=left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal;
                    var little=left?HumanBodyBones.LeftLittleProximal:HumanBodyBones.RightLittleProximal;
                    if(bones.ContainsKey(wrist) && bones.ContainsKey(index) && bones.ContainsKey(middle) && bones.ContainsKey(little)) {
                        Vector3 up=bones[middle].position-bones[wrist].position,across=bones[little].position-bones[index].position;
                        Vector3 normal=Vector3.Cross(across,up);if(!left)normal=-normal;
                        if(normal.sqrMagnitude>.000001f)palmRest[wrist]=Quaternion.LookRotation(normal,up);
                    }
                }
                originalColors.Clear();
                foreach(var renderer in Instance.GetComponentsInChildren<Renderer>(true)) {
                    renderer.enabled=!(Path.GetFullPath(path)==Path.GetFullPath(DefaultPath)&&renderer.name.StartsWith("robo_arm",StringComparison.OrdinalIgnoreCase));
                    foreach(var material in renderer.materials)if(!originalColors.ContainsKey(material))originalColors[material]=material.HasProperty("_Color")?material.GetColor("_Color"):material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):Color.white;
                }
                ApplyAppearance();
                settings.UseExternalModel(true);
            } finally {if(candidate!=null)Destroy(candidate.gameObject);Loading=false;}
        }
        public void UseProcedural() {if(Instance!=null)Instance.gameObject.SetActive(false);settings.UseExternalModel(false);}
        public void UseVrm() {if(Instance==null)return;Instance.gameObject.SetActive(true);settings.UseExternalModel(true);}
        Vector3 P(Point p)=>new Vector3(tracking.mirror?-p.x:p.x,-p.y,p.z);
        static bool Valid(Point[] p,params int[] indices) {if(p==null)return false;foreach(int i in indices)if(i>=p.Length || p[i]==null || p[i].confidence<.55f)return false;return true;}
        void Rotate(HumanBodyBones key,Quaternion target,float t,bool world=false) {
            if(!bones.TryGetValue(key,out var bone))return;
            if(world)bone.rotation=Quaternion.Slerp(bone.rotation,target,t);else bone.localRotation=Quaternion.Slerp(bone.localRotation,target,t);
        }
        void Aim(HumanBodyBones key,Vector3 direction,float t) {
            if(direction.sqrMagnitude<.000001f || !directions.TryGetValue(key,out var rest) || rest.sqrMagnitude<.000001f)return;
            Rotate(key,Quaternion.FromToRotation(rest,direction)*worldRest[key],t,true);
        }
        void Segment(HumanBodyBones key,Point[] p,int a,int b,float t,Vector3 fallback) {
            Aim(key,Valid(p,a,b)?P(p[b])-P(p[a]):fallback,t);
        }
        float E(string name) {
            if(!settings.previewAnimation)return tracking.GetExpression(name);
            int preset=settings.previewExpressionPreset==0?1+(int)(clock/4)%4:settings.previewExpressionPreset;
            if(name.StartsWith("eyeBlink"))return clock%4.4f>4.1f?Mathf.Sin((clock%4.4f-4.1f)/.3f*Mathf.PI):0;
            if(name.StartsWith("mouthSmile"))return preset==1?.8f:0;
            if(name=="jawOpen")return preset==2?.5f:preset==4?Mathf.Max(0,Mathf.Sin(clock*4))*.7f:0;
            if(name=="browInnerUp")return preset==2?.6f:0;
            if(name.StartsWith("browDown"))return preset==3?.7f:0;
            if(name.StartsWith("mouthFrown"))return preset==3?.4f:0;
            if(name=="mouthPucker")return preset==4?Mathf.Max(0,Mathf.Sin(clock*2))*.3f:0;
            return 0;
        }
        void Face() {
            var expression=Instance.Runtime.Expression;
            bool separateBlink=false;foreach(var key in expression.ExpressionKeys)if(key.Preset==ExpressionPreset.blinkLeft)separateBlink=true;
            foreach(var key in expression.ExpressionKeys) {
                float value=0;
                switch(key.Preset) {
                    case ExpressionPreset.happy:value=(E("mouthSmileLeft")+E("mouthSmileRight"))*.5f;break;
                    case ExpressionPreset.angry:value=(E("browDownLeft")+E("browDownRight"))*.5f;break;
                    case ExpressionPreset.sad:value=(E("mouthFrownLeft")+E("mouthFrownRight"))*.5f;break;
                    case ExpressionPreset.surprised:value=E("browInnerUp")*E("jawOpen");break;
                    case ExpressionPreset.aa:value=E("jawOpen")*(1-E("mouthPucker"))*(1-E("mouthClose"));break;
                    case ExpressionPreset.ou:value=E("mouthPucker");break;
                    case ExpressionPreset.oh:value=E("mouthFunnel");break;
                    case ExpressionPreset.ee:value=(E("mouthStretchLeft")+E("mouthStretchRight"))*.5f;break;
                    case ExpressionPreset.ih:value=(E("mouthPressLeft")+E("mouthPressRight"))*.25f;break;
                    case ExpressionPreset.blink:value=separateBlink?0:(E("eyeBlinkLeft")+E("eyeBlinkRight"))*.5f;break;
                    case ExpressionPreset.blinkLeft:value=E("eyeBlinkLeft");break;
                    case ExpressionPreset.blinkRight:value=E("eyeBlinkRight");break;
                    case ExpressionPreset.custom:value=E(key.Name);break;
                }
                if(settings.Profile.corporate&&(key.Preset==ExpressionPreset.happy||key.Preset==ExpressionPreset.angry||key.Preset==ExpressionPreset.sad||key.Preset==ExpressionPreset.surprised))value*=.75f;
                expression.SetWeight(key,Mathf.Clamp01(value));
            }
            float yaw=(E("eyeLookOutLeft")+E("eyeLookInRight")-E("eyeLookInLeft")-E("eyeLookOutRight"))*12;
            float pitch=(E("eyeLookUpLeft")+E("eyeLookUpRight")-E("eyeLookDownLeft")-E("eyeLookDownRight"))*8;
            Instance.Runtime.LookAt.SetYawPitchManually(yaw,pitch);
        }
        void LateUpdate() {
            if(!Active || settings.freeze)return;clock+=Time.unscaledDeltaTime;
            float t=1-Mathf.Exp(-tracking.smoothing*Time.unscaledDeltaTime);
            var p=settings.previewAnimation?null:tracking.PosePoints;
            if(Valid(p,11,12,23,24)) {
                Vector3 up=(P(p[11])+P(p[12])-P(p[23])-P(p[24]))*.5f;
                Vector3 right=P(p[12])-P(p[11]);if(tracking.mirror)right=-right;
                Vector3 forward=Vector3.Cross(right,up);
                if(forward.sqrMagnitude>.00001f) {
                    var torsoRotation=Quaternion.LookRotation(forward.normalized,up.normalized)*facing;
                    Rotate(HumanBodyBones.Spine,torsoRotation,t,true);
                    Vector3 hipAcross=P(p[24])-P(p[23]);if(tracking.mirror)hipAcross=-hipAcross;
                    Vector3 hipForward=Vector3.Cross(hipAcross,Vector3.up);
                    if(hipForward.sqrMagnitude>.00001f)Rotate(HumanBodyBones.Hips,Quaternion.LookRotation(hipForward,Vector3.up)*facing,t,true);
                }
            }else {
                if(localRest.TryGetValue(HumanBodyBones.Hips,out var hipRest))Rotate(HumanBodyBones.Hips,hipRest,t);
                if(localRest.TryGetValue(HumanBodyBones.Spine,out var spineRest))Rotate(HumanBodyBones.Spine,spineRest,t);
            }
            for(int side=0;side<2;side++) {
                bool left=side==0;int source=tracking.mirror?side:1-side;int s=source==0?11:12,e=source==0?13:14,w=source==0?15:16,h=source==0?23:24,k=source==0?25:26,a=source==0?27:28,toe=source==0?31:32;
                Segment(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm,p,s,e,t,new Vector3(left?-.12f:.12f,-1,0));
                Segment(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm,p,e,w,t,new Vector3(0,-1,-.1f));
                Segment(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg,p,h,k,t,Vector3.down);
                Segment(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg,p,k,a,t,Vector3.down);
                Segment(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot,p,a,toe,t,new Vector3(0,-.1f,-1));
                Hands(left,t);
            }
            if(bones.ContainsKey(HumanBodyBones.Head)) {
                var rotation=settings.previewAnimation?Quaternion.Euler(Mathf.Sin(clock*.6f)*5,Mathf.Sin(clock*.5f)*12,0):tracking.HeadRotation;
                Rotate(HumanBodyBones.Head,rotation*worldRest[HumanBodyBones.Head],t,true);
            }
            Vector2 offset=settings.previewAnimation?Vector2.zero:tracking.BodyOffset;
            Instance.transform.position=Vector3.Lerp(Instance.transform.position,origin+new Vector3(offset.x*1.5f,offset.y*1.2f,0),t);
            Face();Instance.Runtime.Process();
        }
        void Hands(bool left,float t) {
            var hand=settings.previewAnimation?null:tracking.GetHand(left);
            var wrist=left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand;
            if(Valid(hand,0,5,9,17) && bones.ContainsKey(wrist)) {
                Vector3 up=P(hand[9])-P(hand[0]),across=P(hand[17])-P(hand[5]);
                Vector3 normal=Vector3.Cross(across,up);if(!left)normal=-normal;
                if(normal.sqrMagnitude>.000001f && palmRest.TryGetValue(wrist,out var basis))Rotate(wrist,Quaternion.LookRotation(normal,up)*Quaternion.Inverse(basis)*worldRest[wrist],t,true);
            }else if(localRest.TryGetValue(wrist,out var rest))Rotate(wrist,rest,t);
            HumanBodyBones[] starts=left?new[]{HumanBodyBones.LeftThumbProximal,HumanBodyBones.LeftIndexProximal,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftRingProximal,HumanBodyBones.LeftLittleProximal}:new[]{HumanBodyBones.RightThumbProximal,HumanBodyBones.RightIndexProximal,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightRingProximal,HumanBodyBones.RightLittleProximal};
            for(int finger=0;finger<5;finger++)for(int joint=0;joint<3;joint++) {
                var key=(HumanBodyBones)((int)starts[finger]+joint);int index=1+finger*4+joint;
                if(Valid(hand,index,index+1))Aim(key,P(hand[index+1])-P(hand[index]),t);
                else if(localRest.TryGetValue(key,out var fingerRest))Rotate(key,fingerRest,t);
            }
        }
        void OnDestroy(){if(Instance!=null)Destroy(Instance.gameObject);}
    }
}
