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
        readonly Dictionary<HumanBodyBones,HumanBodyBones> limbEnds=new Dictionary<HumanBodyBones,HumanBodyBones>();
        readonly Dictionary<HumanBodyBones,Quaternion> palmRest=new Dictionary<HumanBodyBones,Quaternion>();
        readonly Dictionary<HumanBodyBones,Vector3> trackedDirections=new Dictionary<HumanBodyBones,Vector3>();
        readonly Dictionary<HumanBodyBones,float> lastSeen=new Dictionary<HumanBodyBones,float>();
        readonly Dictionary<Material,Color> originalColors=new Dictionary<Material,Color>();
        public CorporateAppearance Appearance {get;private set;}
        public readonly Vector3[] WristTargets=new Vector3[2];
        public readonly float[] ImmediateAlignment=new float[2];
        public Vector2 ArmAlignment(bool left){var a=left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm;var b=left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm;var c=left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand;return new Vector2(Vector3.Dot((bones[b].position-bones[a].position).normalized,trackedDirections[a]),Vector3.Dot((bones[c].position-bones[b].position).normalized,trackedDirections[b]));}
        public void ApplyAppearance() {
            if(Instance==null)return;
            if(Appearance!=null)Appearance.Apply(settings.Profile);
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
        bool snapPose;
        public static string DefaultPath=>Path.Combine(Application.streamingAssetsPath,"Avatars","CorporateHuman.vrm");
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
                limbEnds.Clear();
                trackedDirections.Clear();lastSeen.Clear();
                foreach(HumanBodyBones key in Enum.GetValues(typeof(HumanBodyBones))) {
                    if(key==HumanBodyBones.LastBone)continue;
                    var bone=rig.GetBoneTransform(key);if(bone==null)continue;
                    bones[key]=bone;localRest[key]=bone.localRotation;worldRest[key]=bone.rotation;
                    directions[key]=bone.childCount>0?bone.GetChild(0).position-bone.position:bone.position-bone.parent.position;
                }
                foreach(bool left in new[]{true,false}) {
                    var chain=left?new[]{HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes}:new[]{HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,HumanBodyBones.RightToes};
                    foreach(int link in new[]{0,1,3,4,5})if(bones.ContainsKey(chain[link])&&bones.ContainsKey(chain[link+1])){directions[chain[link]]=bones[chain[link+1]].position-bones[chain[link]].position;limbEnds[chain[link]]=chain[link+1];}
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
                Appearance=Instance.gameObject.AddComponent<CorporateAppearance>();
                Appearance.Bind(Instance.gameObject);
                foreach(var renderer in Instance.GetComponentsInChildren<Renderer>(true)) {
                    renderer.enabled=true;
                    foreach(var material in renderer.materials)if(!originalColors.ContainsKey(material))originalColors[material]=material.HasProperty("_Color")?material.GetColor("_Color"):material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):Color.white;
                }
                ApplyAppearance();
                settings.UseExternalModel(true);
                snapPose=true;LateUpdate();
            } finally {if(candidate!=null)Destroy(candidate.gameObject);Loading=false;}
        }
        public void UseProcedural() {if(Instance!=null)Instance.gameObject.SetActive(false);settings.UseExternalModel(false);}
        public void UseVrm() {if(Instance==null)return;Instance.gameObject.SetActive(true);settings.UseExternalModel(true);}
        Vector3 P(Point p)=>new Vector3(tracking.mirror?-p.x:p.x,-p.y,p.z);
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static bool Valid(Point[] p,params int[] indices) {if(p==null)return false;foreach(int i in indices)if(i>=p.Length || p[i]==null || !Finite(p[i].confidence)||p[i].confidence<.55f||!Finite(p[i].x)||!Finite(p[i].y)||!Finite(p[i].z))return false;return true;}
        void Rotate(HumanBodyBones key,Quaternion target,float t,bool world=false) {
            if(!bones.TryGetValue(key,out var bone))return;
            // A child forearm must compensate shoulder rotation as well as
            // its own flexion; using the upper-arm cap for both loses reach.
            string name=key.ToString();
            float step=snapPose?3600:(name.Contains("LowerArm")?720:name.Contains("UpperArm")?360:480)*Time.unscaledDeltaTime;
            if(world)bone.rotation=Quaternion.RotateTowards(bone.rotation,Quaternion.Slerp(bone.rotation,target,t),step);
            else bone.localRotation=Quaternion.RotateTowards(bone.localRotation,Quaternion.Slerp(bone.localRotation,target,t),step);
        }
        void Aim(HumanBodyBones key,Vector3 direction,float t) {
            if(direction.sqrMagnitude<.000001f)return;
            if(limbEnds.TryGetValue(key,out var end)){
                var actual=bones[end].position-bones[key].position;
                // Directions/landmarks are already filtered. A second rotation
                // low-pass makes the forearm chase a moving upper-arm frame.
                // Preserve the angular speed cap while following that target.
                if(actual.sqrMagnitude>.000001f){
                    var parent=bones[key].parent;
                    var from=parent.InverseTransformVector(actual);var to=parent.InverseTransformVector(direction);
                    Rotate(key,Quaternion.FromToRotation(from,to)*bones[key].localRotation,1);
                }
                return;
            }
            if(!directions.TryGetValue(key,out var rest)||rest.sqrMagnitude<.000001f)return;
            Rotate(key,Quaternion.FromToRotation(rest,direction)*worldRest[key],t,true);
        }
        void Segment(HumanBodyBones key,Point[] p,int a,int b,float t,Vector3 fallback) {
            Aim(key,Valid(p,a,b)?P(p[b])-P(p[a]):fallback,t);
        }
        Vector3 FilterDirection(HumanBodyBones key,Vector3 target,bool visible) {
            if(target.sqrMagnitude<.000001f)visible=false;
            if(visible)lastSeen[key]=clock;
            if(!trackedDirections.TryGetValue(key,out var previous)||snapPose)previous=target.normalized;
            if(!visible&&lastSeen.TryGetValue(key,out var seen)&&clock-seen<.2f)return previous;
            var value=Vector3.Slerp(previous,target.normalized,snapPose?1:1-Mathf.Exp(-(visible?18:4)*Time.unscaledDeltaTime)).normalized;
            trackedDirections[key]=value;return value;
        }
        void SeatedArm(bool left,Point[] p,int s,int e,int w,float t) {
            var upper=left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm;
            var lower=left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm;
            var torso=bones.ContainsKey(HumanBodyBones.Chest)?HumanBodyBones.Chest:HumanBodyBones.Spine;
            var delta=bones.TryGetValue(torso,out var chest)?chest.rotation*Quaternion.Inverse(worldRest[torso]):Quaternion.identity;
            var relaxedUpper=delta*new Vector3(left?.12f:-.12f,-1,-.08f).normalized;
            if(Valid(p,11,12,s,w)&&bones.ContainsKey(upper)&&bones.ContainsKey(lower)) {
                var wrist=left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand;
                var other=left?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm;
                if(bones.ContainsKey(wrist)&&bones.ContainsKey(other)) {
                    float observedWidth=Vector3.Distance(P(p[11]),P(p[12]));
                    float modelWidth=Vector3.Distance(bones[upper].position,bones[other].position);
                    float lengthA=Vector3.Distance(bones[upper].position,bones[lower].position),lengthB=Vector3.Distance(bones[lower].position,bones[wrist].position);
                    if(observedWidth>.12f&&lengthA>.01f&&lengthB>.01f) {
                        float scale=Mathf.Clamp(modelWidth/observedWidth,.4f,3f);
                        Vector3 pole=Valid(p,s,e)?P(p[e])-P(p[s]):trackedDirections.TryGetValue(upper,out var lastUpper)?lastUpper:new Vector3(left?.4f:-.4f,.2f,-.15f);
                        Vector3 target=(P(p[w])-P(p[s]))*scale;
                        // Near-face gestures need a shared head anchor, because
                        // human and avatar neck/shoulder proportions differ.
                        if(Valid(p,0)&&bones.ContainsKey(HumanBodyBones.Head)) {
                            float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.23f,Vector3.Distance(P(p[w]),P(p[0]))));
                            var headTarget=bones[HumanBodyBones.Head].position-bones[upper].position+(P(p[w])-P(p[0]))*scale;
                            target=Vector3.Lerp(target,headTarget,blend);
                        }
                        float minimum=Mathf.Sqrt(lengthA*lengthA+lengthB*lengthB+2*lengthA*lengthB*Mathf.Cos(150*Mathf.Deg2Rad));
                        // Wrist direction becomes unstable at the shoulder.
                        // The visible elbow provides the stable bend direction.
                        if(target.magnitude<minimum)target=pole.normalized*minimum;
                        target=target.normalized*Mathf.Clamp(target.magnitude,minimum,lengthA+lengthB-.0001f);
                        WristTargets[left?0:1]=bones[upper].position+target;
                        SolveArm(target,pole,lengthA,lengthB,out var ikUpper,out var ikLower);
                        // Filter landmarks, not each half of a solved chain:
                        // independent direction filters break endpoint reach.
                        trackedDirections[upper]=ikUpper;lastSeen[upper]=clock;Aim(upper,ikUpper,t);
                        var actualUpper=(bones[lower].position-bones[upper].position).normalized;
                        var actualLower=(WristTargets[left?0:1]-bones[lower].position).normalized;
                        actualLower=Vector3.RotateTowards(actualUpper,actualLower,150*Mathf.Deg2Rad,0).normalized;
                        trackedDirections[lower]=actualLower;lastSeen[lower]=clock;Aim(lower,actualLower,t);ImmediateAlignment[left?0:1]=ArmAlignment(left).y;return;
                    }
                }
            }
            var up=FilterDirection(upper,Valid(p,s,e)?P(p[e])-P(p[s]):relaxedUpper,Valid(p,s,e));
            Aim(upper,up,t);
            var relaxedLower=Quaternion.FromToRotation(relaxedUpper,up)*(delta*new Vector3(left?.06f:-.06f,-.25f,-1).normalized);
            var down=FilterDirection(lower,Valid(p,e,w)?P(p[w])-P(p[e]):relaxedLower,Valid(p,e,w));
            down=Vector3.RotateTowards(up,down,150*Mathf.Deg2Rad,0).normalized;
            trackedDirections[lower]=down;Aim(lower,down,t);
        }
        public static void SolveArm(Vector3 target,Vector3 pole,float upperLength,float lowerLength,out Vector3 upper,out Vector3 lower) {
            float min=Mathf.Sqrt(upperLength*upperLength+lowerLength*lowerLength+2*upperLength*lowerLength*Mathf.Cos(150*Mathf.Deg2Rad));
            float distance=Mathf.Clamp(target.magnitude,min,upperLength+lowerLength-.0001f);
            Vector3 axis=target.sqrMagnitude>.000001f?target.normalized:Vector3.down;
            var bend=Vector3.ProjectOnPlane(pole,axis);
            if(bend.sqrMagnitude<.000001f)bend=Vector3.ProjectOnPlane(Vector3.back,axis);
            if(bend.sqrMagnitude<.000001f)bend=Vector3.ProjectOnPlane(Vector3.right,axis);
            float along=(upperLength*upperLength-lowerLength*lowerLength+distance*distance)/(2*distance);
            var elbow=axis*along+bend.normalized*Mathf.Sqrt(Mathf.Max(0,upperLength*upperLength-along*along));
            upper=elbow.normalized;lower=(axis*distance-elbow).normalized;
        }
        void LimitedSegment(HumanBodyBones key,Point[] p,int a,int b,int c,float maximum,float t,Vector3 fallback) {
            if(!Valid(p,a,b,c)){Aim(key,fallback,t);return;}
            Vector3 upper=P(p[b])-P(p[a]),lower=P(p[c])-P(p[b]);
            if(upper.sqrMagnitude<.000001f || lower.sqrMagnitude<.000001f)return;
            float angle=Vector3.Angle(upper,lower);
            if(angle>maximum)lower=Vector3.RotateTowards(upper.normalized,lower.normalized,maximum*Mathf.Deg2Rad,0)*lower.magnitude;
            Aim(key,lower,t);
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
            float t=snapPose?1:1-Mathf.Exp(-tracking.smoothing*Time.unscaledDeltaTime);
            var p=settings.previewAnimation?null:tracking.PosePoints;
            if(Valid(p,11,12,23,24)) {
                Vector3 up=(P(p[11])+P(p[12])-P(p[23])-P(p[24]))*.5f;
                Vector3 right=P(p[12])-P(p[11]);if(!tracking.mirror)right=-right;
                Vector3 forward=Vector3.Cross(right,up);
                if(forward.sqrMagnitude>.00001f) {
                    var torsoRotation=Quaternion.LookRotation(forward.normalized,up.normalized)*facing;
                    Rotate(HumanBodyBones.Spine,torsoRotation,t,true);
                    Vector3 hipAcross=P(p[24])-P(p[23]);if(!tracking.mirror)hipAcross=-hipAcross;
                    Vector3 hipForward=Vector3.Cross(hipAcross,Vector3.up);
                    if(hipForward.sqrMagnitude>.00001f)Rotate(HumanBodyBones.Hips,Quaternion.LookRotation(hipForward,Vector3.up)*facing,t,true);
                }
            }else {
                if(localRest.TryGetValue(HumanBodyBones.Hips,out var hipRest))Rotate(HumanBodyBones.Hips,hipRest,t);
                if(localRest.TryGetValue(HumanBodyBones.Spine,out var spineRest))Rotate(HumanBodyBones.Spine,spineRest,t);
            }
            for(int side=0;side<2;side++) {
                bool left=side==0;int source=tracking.mirror?1-side:side;int s=source==0?11:12,e=source==0?13:14,w=source==0?15:16,h=source==0?23:24,k=source==0?25:26,a=source==0?27:28,toe=source==0?31:32;
                SeatedArm(left,p,s,e,w,t);
                Segment(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg,p,h,k,t,Vector3.down);
                LimitedSegment(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg,p,h,k,a,160,t,Vector3.down);
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
            if(Appearance!=null)Appearance.ApplyIdentity(settings.Profile);
            snapPose=false;
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
                if(Valid(hand,0,5,9,17,index,index+1)&&bones.ContainsKey(wrist)&&directions.TryGetValue(key,out var axis)) {
                    var target=FilterDirection(key,P(hand[index+1])-P(hand[index]),true);
                    var palmDelta=bones[wrist].rotation*Quaternion.Inverse(worldRest[wrist]);
                    if(target.sqrMagnitude>.000001f&&axis.sqrMagnitude>.000001f)Rotate(key,Quaternion.FromToRotation(palmDelta*axis,target)*palmDelta*worldRest[key],t,true);
                }
                else if(localRest.TryGetValue(key,out var fingerRest))Rotate(key,fingerRest,t);
            }
        }
        void OnDestroy(){if(Instance!=null)Destroy(Instance.gameObject);}
    }
}
