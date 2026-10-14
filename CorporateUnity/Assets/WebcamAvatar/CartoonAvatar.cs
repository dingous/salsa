using System.Collections.Generic;
using UnityEngine;

namespace WebcamAvatar
{
    /// <summary>Editable procedural character. Photo-derived proportions, fully local geometry.</summary>
    public class CartoonAvatar : MonoBehaviour
    {
        public AvatarProfile Profile { get; private set; } = new AvatarProfile();
        public bool previewAnimation;
        public int previewExpressionPreset;
        public bool freeze;
        bool externalModel;
        public void UseExternalModel(bool value) {externalModel=value;if(model!=null)model.gameObject.SetActive(!value);}
        public float turnLimit = 40;
        public float bodyMotion = .65f;
        AvatarTracking tracking;
        Transform model, head, torso, mouth, topLip, bottomLip, teeth;
        Transform[] eyes = new Transform[2], irises = new Transform[2], brows = new Transform[2], lids = new Transform[2];
        Transform[] upperArms = new Transform[2], foreArms = new Transform[2];
        readonly List<Material> materials = new List<Material>();
        readonly List<Mesh> meshes = new List<Mesh>();
        float time;
        AdvancedFace advancedFace;
        Transform[] handRoots=new Transform[2];
        Transform[,] fingerJoints=new Transform[2,15];
        Transform lowerTeeth,tongue;
        Transform[] nostrils=new Transform[2];
        readonly List<Transform> flowingHair=new List<Transform>();
        Vector3 hairTilt,hairVelocity;
        Quaternion previousHead=Quaternion.identity;
        Transform hips,spine,neck;
        Transform[] upperLegs=new Transform[2],lowerLegs=new Transform[2],feet=new Transform[2];
        public int FacialBlendshapeCount => advancedFace!=null ? advancedFace.ShapeCount : 0;

        void Start() { tracking=GetComponent<AvatarTracking>(); if(model==null) Rebuild(Profile); }
        Material Material(Color color, float gloss=.22f)
        {
            var m=new Material(Shader.Find(Profile.IsRealistic ? "AvatarStudio/Realistic" : "AvatarStudio/Cartoon")); m.color=color; m.SetFloat("_Glossiness",gloss); materials.Add(m); return m;
        }
        Transform Node(string name, Transform parent, Vector3 position)
        {
            var t=new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=position; return t;
        }
        Transform Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var t=GameObject.CreatePrimitive(type).transform; t.name=name; t.SetParent(parent,false); t.localPosition=position; t.localScale=scale;
            t.GetComponent<Renderer>().sharedMaterial=material;
            var collider=t.GetComponent<Collider>(); if(Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            return t;
        }
        Transform MeshPart(string name,Transform parent,Mesh mesh,Material material)
        {
            meshes.Add(mesh);mesh.name=name;var node=Node(name,parent,Vector3.zero);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return node;
        }
        public void Rebuild(AvatarProfile profile)
        {
            profile.Sanitize(); Profile=profile;
            if(model!=null) { model.gameObject.SetActive(false); Release(model.gameObject); }
            foreach(var m in materials) Release(m); materials.Clear();
            foreach(var m in meshes) Release(m); meshes.Clear();
            flowingHair.Clear();
            model=Node("Avatar avançado",transform,Vector3.zero);
            Color skinColor=AvatarProfile.ColorOf(profile.skin,new Color(.85f,.6f,.45f));
            Color hairColor=AvatarProfile.ColorOf(profile.hair,new Color(.16f,.1f,.12f));
            var skin=Material(skinColor,.25f); var hairMat=Material(hairColor,.18f);
            var outfit=Material(AvatarProfile.ColorOf(profile.outfit,new Color(.4f,.4f,.9f)),.1f);
            var knit=Material(outfit.color*.78f,.1f); var whites=Material(new Color(.97f,.98f,1),.4f);
            var irisMat=Material(AvatarProfile.ColorOf(profile.eyes,new Color(.2f,.45f,.4f)),.55f);
            var black=Material(new Color(.025f,.022f,.04f),.3f);
            var lips=Material(Color.Lerp(skinColor,new Color(.55f,.17f,.19f),.42f),.2f);
            var inside=Material(new Color(.17f,.035f,.075f),0);
            torso=Node("Tronco",model,new Vector3(0,.97f,0));
            MeshPart("Roupa modelada",torso,AdvancedGeometry.Loft(new[]{-.56f,-.40f,-.17f,.10f,.29f,.38f,.44f,.46f},new[]{.18f,.23f,.27f,.30f,.34f,.31f,.19f,.115f},new[]{.125f,.15f,.17f,.175f,.17f,.145f,.11f,.10f}),outfit);
            Shape("Barra",PrimitiveType.Sphere,torso,new Vector3(0,-.44f,0),new Vector3(.45f,.14f,.32f),knit);
            Shape("Gola",PrimitiveType.Cylinder,torso,new Vector3(0,.43f,0),new Vector3(.25f,.055f,.24f),knit);
            Tube("Costura central",torso,new[]{new Vector3(0,.37f,-.155f),new Vector3(0,.14f,-.18f),new Vector3(0,-.15f,-.173f),new Vector3(0,-.43f,-.15f)},.0025f,knit);
            for(int side=0;side<2;side++) {
                float sign=side==0?-1:1;
                Tube("Costura do ombro",torso,new[]{new Vector3(sign*.10f,.44f,-.055f),new Vector3(sign*.20f,.405f,-.085f),new Vector3(sign*.30f,.33f,-.105f)},.003f,knit);
                var pocket=Node("Bolso",torso,new Vector3(sign*.17f,-.06f,-.170f));
                Tube("Acabamento",pocket,new[]{new Vector3(-.07f,.035f,-.004f),new Vector3(0,.028f,-.007f),new Vector3(.07f,.035f,-.004f)},.004f,knit);
            }
            neck=Shape("Pescoço",PrimitiveType.Capsule,model,new Vector3(0,1.48f,0),new Vector3(.19f,.18f,.19f),skin);
            for(int side=0;side<2;side++) {
                float x=side==0 ? -.34f : .34f;
                upperArms[side]=Node("Ombro " + side,model,new Vector3(x,1.36f,0));
                Shape("Manga",PrimitiveType.Capsule,upperArms[side],new Vector3(0,-.17f,0),new Vector3(.17f,.20f,.19f),outfit);
                foreArms[side]=Node("Cotovelo " + side,upperArms[side],new Vector3(0,-.33f,0));
                Shape("Antebraço",PrimitiveType.Capsule,foreArms[side],new Vector3(0,-.13f,0),new Vector3(.14f,.17f,.15f),outfit);
                Shape("Punho",PrimitiveType.Cylinder,foreArms[side],new Vector3(0,-.27f,0),new Vector3(.15f,.035f,.16f),knit);
                BuildHand(side,skin);
            }
            head=Node("Cabeça expressiva",model,new Vector3(0,1.86f,0));
            if(profile.IsRealistic)head.localScale=new Vector3(.82f,.90f,.86f);
            var faceNode=Node("Malha facial avançada",head,Vector3.zero);advancedFace=faceNode.gameObject.AddComponent<AdvancedFace>();advancedFace.Build(profile,skin);
            for(int side=0;side<2;side++) {
                float sign=side==0?-1:1;
                var point=advancedFace.PointOnFace(sign*.105f,-.178f)+Vector3.back*.002f;
                nostrils[side]=Shape("Narina",PrimitiveType.Sphere,head,point,new Vector3(.021f,.007f,.003f),Material(skinColor*.52f));
            }
            for(int side=0;side<2;side++) {
                float sign=side==0 ? -1 : 1;
                Shape("Orelha",PrimitiveType.Sphere,head,new Vector3(sign*.283f*profile.faceWidth,-.008f,.015f),new Vector3(.12f,.19f,.10f),skin);
                Shape("Interior da orelha",PrimitiveType.Sphere,head,new Vector3(sign*.305f*profile.faceWidth,-.01f,-.034f),new Vector3(.052f,.108f,.025f),lips);
                var pivot=Node("Olho " + side,head,new Vector3(sign*(.1195f*profile.faceWidth+.12f*(profile.eyeSpacing-1)),.072f,-.186f));
                eyes[side]=Shape("Esclera",PrimitiveType.Sphere,pivot,Vector3.zero,new Vector3(.158f,.154f,.083f),whites);
                irises[side]=Node("Olhar",pivot,new Vector3(0,0,-.043f));
                if(profile.IsRealistic)irises[side].localScale=new Vector3(.76f,.76f,1);
                Shape("Íris",PrimitiveType.Sphere,irises[side],Vector3.zero,new Vector3(.067f,.076f,.012f),irisMat);
                Shape("Anel da íris",PrimitiveType.Sphere,irises[side],new Vector3(0,0,.003f),new Vector3(.074f,.083f,.01f),black);
                Shape("Pupila",PrimitiveType.Sphere,irises[side],new Vector3(0,0,-.008f),new Vector3(.027f,.041f,.006f),black);
                Shape("Brilho",PrimitiveType.Sphere,irises[side],new Vector3(-.012f,.017f,-.012f),new Vector3(.012f,.014f,.004f),whites);
                Shape("Brilho secundário",PrimitiveType.Sphere,irises[side],new Vector3(.011f,-.012f,-.011f),new Vector3(.005f,.006f,.003f),whites);
                lids[side]=Node("Pálpebra controlada pela malha",pivot,Vector3.zero);
                lids[side].gameObject.SetActive(false);
                brows[side]=Node("Sobrancelha",head,new Vector3(sign*.12f*profile.eyeSpacing,.182f,-.245f));
                Tube("Arco",brows[side],new[]{new Vector3(-.065f,-.011f,.015f),new Vector3(-.035f,.011f,-.003f),new Vector3(.008f,.017f,-.006f),new Vector3(.062f,-.002f,.015f)},.010f,Material(hairColor*.66f));
            }
            mouth=Shape("Cavidade oral",PrimitiveType.Sphere,head,new Vector3(0,-.177f,-.195f),new Vector3(.24f*profile.mouthWidth,.25f,.07f),inside);
            teeth=Node("Arcada superior",head,new Vector3(0,-.159f,-.243f));
            lowerTeeth=Node("Arcada inferior",head,new Vector3(0,-.172f,-.241f));
            for(int i=0;i<8;i++) {
                float x=(i-3.5f)*.016f;
                Shape("Dente superior",PrimitiveType.Cube,teeth,new Vector3(x,0,Mathf.Abs(x)*.18f),new Vector3(.014f,.024f,.008f),whites);
                Shape("Dente inferior",PrimitiveType.Cube,lowerTeeth,new Vector3(x,0,Mathf.Abs(x)*.18f),new Vector3(.014f,.019f,.008f),whites);
            }
            tongue=Shape("Língua",PrimitiveType.Sphere,head,new Vector3(0,-.210f,-.218f),new Vector3(.082f,.033f,.055f),lips);
            teeth.gameObject.SetActive(false);
            lowerTeeth.gameObject.SetActive(false);tongue.gameObject.SetActive(false);
            if(profile.hairStyle!=3) Hair(head,hairMat,profile.hairStyle);
            if(profile.glasses) Glasses(head,black);
            BuildLowerBody(knit,black);
            spine=Node("Coluna e tronco",hips,Vector3.zero);
            foreach(var node in new[]{torso,head,neck,upperArms[0],upperArms[1]})node.SetParent(spine,true);
            model.gameObject.SetActive(!externalModel);
        }
        void BuildLowerBody(Material pants,Material shoes)
        {
            hips=Node("Quadril",model,new Vector3(0,.53f,0));
            Shape("Calça",PrimitiveType.Sphere,hips,Vector3.zero,new Vector3(.46f,.26f,.32f),pants);
            for(int side=0;side<2;side++) {
                upperLegs[side]=Node("Coxa "+side,hips,new Vector3(side==0?-.13f:.13f,-.045f,0));
                Shape("Coxa",PrimitiveType.Capsule,upperLegs[side],new Vector3(0,-.19f,0),new Vector3(.205f,.235f,.225f),pants);
                lowerLegs[side]=Node("Joelho "+side,upperLegs[side],new Vector3(0,-.39f,0));
                Shape("Canela",PrimitiveType.Capsule,lowerLegs[side],new Vector3(0,-.175f,0),new Vector3(.16f,.205f,.185f),pants);
                feet[side]=Node("Tornozelo "+side,lowerLegs[side],new Vector3(0,-.36f,0));
                Shape("Sapato",PrimitiveType.Sphere,feet[side],new Vector3(0,-.055f,-.045f),new Vector3(.19f,.135f,.32f),shoes);
            }
        }
        Vector3 PosePosition(Point p)=>new Vector3(tracking!=null && tracking.mirror?-p.x:p.x,-p.y,p.z);
        static Quaternion Limited(Quaternion q,float pitch,float yaw,float roll)
        {
            var a=q.eulerAngles;return Quaternion.Euler(Mathf.Clamp(Mathf.DeltaAngle(0,a.x),-pitch,pitch),Mathf.Clamp(Mathf.DeltaAngle(0,a.y),-yaw,yaw),Mathf.Clamp(Mathf.DeltaAngle(0,a.z),-roll,roll));
        }
        static bool Visible(Point[] p,params int[] indices)
        {
            if(p==null)return false;foreach(int i in indices)if(p[i].confidence<.55f)return false;return true;
        }
        void PoseBody()
        {
            var p=tracking!=null && !previewAnimation?tracking.PosePoints:null;
            int hl=tracking!=null && !tracking.mirror?24:23,hr=hl==23?24:23,sl=hl==23?11:12,sr=sl==11?12:11;
            Quaternion hipTarget=Quaternion.identity,bodyTarget=Quaternion.identity;
            if(Visible(p,hl,hr)) {
                var right=PosePosition(p[hr])-PosePosition(p[hl]);var up=Vector3.ProjectOnPlane(Vector3.up,right).normalized;
                var forward=Vector3.Cross(right,up);
                if(forward.sqrMagnitude>.001f)hipTarget=Limited(Quaternion.LookRotation(forward,up),15,50,18);
                if(Visible(p,sl,sr)) {
                    up=((PosePosition(p[sl])+PosePosition(p[sr]))-(PosePosition(p[hl])+PosePosition(p[hr]))).normalized;
                    right=PosePosition(p[sr])-PosePosition(p[sl]);forward=Vector3.Cross(right,up);
                    if(forward.sqrMagnitude>.001f)bodyTarget=Limited(Quaternion.LookRotation(forward,up),35,60,35);
                }
            }
            float t=1-Mathf.Exp(-10*Time.unscaledDeltaTime);
            hips.localRotation=Quaternion.Slerp(hips.localRotation,hipTarget,t);
            spine.localRotation=Quaternion.Slerp(spine.localRotation,Quaternion.Inverse(hipTarget)*bodyTarget,t);
            torso.localRotation=Quaternion.identity;
            for(int side=0;side<2;side++) {
                int source=tracking!=null && !tracking.mirror?1-side:side;
                int hip=source==0?23:24,knee=source==0?25:26,ankle=source==0?27:28,toe=source==0?31:32;
                Quaternion thigh=Quaternion.identity,calf=Quaternion.identity,foot=Quaternion.identity;
                if(Visible(p,hip,knee)) {
                    var direction=PosePosition(p[knee])-PosePosition(p[hip]);
                    if(direction.sqrMagnitude>.001f)thigh=Quaternion.Inverse(hipTarget)*Quaternion.FromToRotation(Vector3.down,direction);
                    if(Visible(p,knee,ankle)) {
                        direction=PosePosition(p[ankle])-PosePosition(p[knee]);
                        if(direction.sqrMagnitude>.001f)calf=Quaternion.Inverse(hipTarget*thigh)*Quaternion.FromToRotation(Vector3.down,direction);
                    }
                    if(Visible(p,ankle,toe)) {
                        direction=PosePosition(p[toe])-PosePosition(p[ankle]);
                        if(direction.sqrMagnitude>.0001f && Vector3.Cross(direction,Vector3.up).sqrMagnitude>.00001f)foot=Quaternion.Inverse(hipTarget*thigh*calf)*Quaternion.LookRotation(-direction,Vector3.up);
                    }
                }
                upperLegs[side].localRotation=Quaternion.Slerp(upperLegs[side].localRotation,thigh,t);
                lowerLegs[side].localRotation=Quaternion.Slerp(lowerLegs[side].localRotation,calf,t);
                feet[side].localRotation=Quaternion.Slerp(feet[side].localRotation,foot,t);
            }
        }
        void Tube(string name, Transform parent, Vector3[] path,float radius,Material mat)
        {
            var radii=new float[path.Length];for(int i=0;i<radii.Length;i++)radii[i]=radius;
            MeshPart(name,parent,AdvancedGeometry.Tube(path,radii),mat);
        }
        void Hair(Transform parent,Material mat,int style)
        {
            // Open cap: covers the crown and back without intersecting the forehead.
            int rings=18, slices=48; var v=new List<Vector3>(); var tri=new List<int>();
            for(int r=0;r<=rings;r++) for(int s=0;s<=slices;s++) {
                float phi=s*Mathf.PI*2/slices;
                float front=(1-Mathf.Cos(phi))*.5f; // z=-1 faces camera.
                float end=Mathf.Lerp(1.9f,.83f,Mathf.Pow(front,2));
                float theta=(float)r/rings*end;
                v.Add(new Vector3(Mathf.Sin(theta)*Mathf.Sin(phi)*.304f*Profile.faceWidth,Mathf.Cos(theta)*.379f,Mathf.Sin(theta)*Mathf.Cos(phi)*.282f));
            }
            for(int r=0;r<rings;r++) for(int s=0;s<slices;s++) { int a=r*(slices+1)+s,b=a+slices+1; tri.AddRange(new[]{a,b,a+1,a+1,b,b+1}); }
            var mesh=new Mesh {name="Coiffure"}; mesh.SetVertices(v); mesh.SetTriangles(tri,0); mesh.RecalculateNormals(); meshes.Add(mesh);
            var cap=Node("Chevelure",parent,Vector3.zero); cap.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh; cap.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
            mat.SetFloat("_Fiber",1);
            cap.localScale=new Vector3(1+(Profile.hairVolume-1)*.1f,1+(Profile.hairVolume-1)*.35f,1+(Profile.hairVolume-1)*.1f);cap.localPosition=Vector3.back*.058f;
            Shape("Volume da franja",PrimitiveType.Sphere,parent,new Vector3(-.025f,.316f,-.185f),new Vector3(.445f,.155f,.23f),mat).localRotation=Quaternion.Euler(0,0,-12);
            for(int i=0;i<11;i++) {
                float t=i/10f,x=Mathf.Lerp(-.21f,.24f,t)*Profile.faceWidth;
                var root=new Vector3(x+.015f,.325f,.035f);
                var tip=new Vector3(x-.045f,.215f+Mathf.Sin(t*Mathf.PI)*.042f,-.278f+Mathf.Abs(x)*.025f);
                var lockMesh=AdvancedGeometry.Lock(root,new Vector3(x-.025f,.435f,-.105f),new Vector3(x-.085f,.32f,-.290f),tip,.046f*Profile.hairVolume);
                MeshPart("Mecha frontal "+i,parent,lockMesh,mat);
            }
            if(style>0) for(int side=0;side<2;side++)for(int i=0;i<12;i++) {
                float sign=side==0?-1:1,t=i/11f;
                Vector3 root=new Vector3(sign*.27f*Profile.faceWidth,.24f,Mathf.Lerp(-.10f,.20f,t));
                Vector3 tip=new Vector3(sign*(.26f+Mathf.Sin(t*Mathf.PI)*.04f)*Profile.faceWidth,style==1?-.28f:-.59f,Mathf.Lerp(-.025f,.19f,t));
                var strand=MeshPart("Mecha lateral",parent,AdvancedGeometry.Lock(root,root+new Vector3(sign*.08f,-.13f,0),tip+new Vector3(sign*.025f,.13f,0),tip,.037f*Profile.hairVolume),mat);
                flowingHair.Add(strand);
            }
        }
        void Glasses(Transform parent,Material mat)
        {
            for(int side=0;side<2;side++) {
                var ring=Node("Armação",parent,new Vector3((side==0?-1:1)*.13f*Profile.eyeSpacing,.06f,-.291f));
                var path=new Vector3[33]; for(int j=0;j<path.Length;j++) { float a=j*Mathf.PI*2/32; path[j]=new Vector3(Mathf.Cos(a)*.093f,Mathf.Sin(a)*.100f,0); }
                Tube("Aro",ring,path,.006f,mat);
            }
            Tube("Ponte dos óculos",parent,new[]{new Vector3(-.035f,.073f,-.298f),new Vector3(0,.089f,-.304f),new Vector3(.035f,.073f,-.298f)},.006f,mat);
        }
        float Expression(string name) => tracking!=null && !previewAnimation ? tracking.GetExpression(name) : Preview(name);
        float Preview(string name)
        {
            if(!previewAnimation) return 0;
            int preset=previewExpressionPreset==0?1+(int)(time/4)%4:previewExpressionPreset;
            float blinkPhase=time%4.4f;
            if(name.StartsWith("eyeBlink"))return blinkPhase>4.1f?Mathf.Sin((blinkPhase-4.1f)/.3f*Mathf.PI):0;
            if(name.StartsWith("mouthSmile"))return preset==1?.70f:preset==4?.22f:0;
            if(name=="jawOpen")return preset==2?.48f:preset==4?Mathf.Max(0,Mathf.Sin(time*4.3f))*.55f:0;
            if(name=="browInnerUp")return preset==2?.72f:preset==1?.15f:0;
            if(name.StartsWith("eyeWide"))return preset==2?.55f:0;
            if(name.StartsWith("browDown"))return preset==3?.66f:0;
            if(name.StartsWith("mouthFrown"))return preset==3?.40f:0;
            if(name.StartsWith("cheekSquint"))return preset==1?.25f:0;
            if(name=="mouthPucker")return preset==4?Mathf.Max(0,Mathf.Sin(time*2.1f))*.32f:0;
            if(name.StartsWith("eyeLook")) {
                float gaze=Mathf.Sin(time*.65f)*.20f;float sign=name.EndsWith("Left")?-1:1;
                if(name.Contains("Out"))return Mathf.Max(0,gaze*sign);
                if(name.Contains("In"))return Mathf.Max(0,-gaze*sign);
            }
            return 0;
        }
        void LateUpdate()
        {
            if(model==null || freeze || externalModel) return;
            time+=Time.unscaledDeltaTime;
            Quaternion target=tracking!=null ? tracking.HeadRotation : Quaternion.identity;
            if(previewAnimation) target=Quaternion.Euler(Mathf.Sin(time*.65f)*5,Mathf.Sin(time*.48f)*12,Mathf.Sin(time*.73f)*3);
            var angles=target.eulerAngles;
            float Signed(float value)=>Mathf.DeltaAngle(0,value);
            target=Quaternion.Euler(Mathf.Clamp(Signed(angles.x),-25,25),Mathf.Clamp(Signed(angles.y),-turnLimit,turnLimit),Mathf.Clamp(Signed(angles.z),-20,20));
            PoseBody();
            if(previewAnimation || (tracking!=null && tracking.FaceDetected))head.rotation=transform.rotation*target;
            else head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.identity,1-Mathf.Exp(-10*Time.unscaledDeltaTime));
            float smile=(Expression("mouthSmileLeft")+Expression("mouthSmileRight"))*.5f;
            float open=Expression("jawOpen"); float pucker=Expression("mouthPucker");
            advancedFace.Animate(Expression);
            teeth.gameObject.SetActive(open>.10f);lowerTeeth.gameObject.SetActive(open>.30f);tongue.gameObject.SetActive(open>.3f);
            lowerTeeth.localPosition=new Vector3(0,-.173f-open*.066f,-.241f);
            tongue.localPosition=new Vector3(0,-.210f-open*.045f,-.218f-Expression("tongueOut")*.04f);
            for(int side=0;side<2;side++) {
                string suffix=side==0?"Left":"Right";
                float blink=Expression("eyeBlink"+suffix); float wide=Expression("eyeWide"+suffix);
                eyes[side].gameObject.SetActive(blink<.985f);irises[side].gameObject.SetActive(blink<.985f);
                float eyeHeight=Mathf.Lerp(.154f+wide*.015f,.010f,blink);
                eyes[side].localScale=new Vector3(.158f,eyeHeight,.083f)*Profile.EffectiveEyeSize;
                irises[side].localScale=new Vector3(1,Mathf.Lerp(1,.025f,blink),1);
                float gazeX=(Expression("eyeLookOut"+suffix)-Expression("eyeLookIn"+suffix))*(side==0?-1:1)*.022f;
                float gazeY=(Expression("eyeLookUp"+suffix)-Expression("eyeLookDown"+suffix))*.023f;
                irises[side].localPosition=new Vector3(gazeX,gazeY,-.043f);
                lids[side].gameObject.SetActive(false);
                brows[side].localPosition=new Vector3((side==0?-1:1)*.12f*Profile.eyeSpacing,.182f+Expression("browInnerUp")*.030f+Expression("browOuterUp"+suffix)*.02f-Expression("browDown"+suffix)*.027f,-.245f);
                brows[side].localRotation=Quaternion.Euler(0,0,(side==0?-1:1)*Expression("browDown"+suffix)*13);
                nostrils[side].localPosition=advancedFace.PointOnFace((side==0?-1:1)*.105f,-.178f)+new Vector3(0,Expression("noseSneer"+suffix)*.008f,-.002f);
            }
            float breath=previewAnimation || (tracking!=null && tracking.FaceDetected) ? Mathf.Sin(time*1.6f)*.003f : 0;
            Vector2 offset=tracking!=null && !previewAnimation?tracking.BodyOffset:Vector2.zero;
            model.localPosition=Vector3.Lerp(model.localPosition,new Vector3(offset.x*1.5f,offset.y*1.2f+breath,0),1-Mathf.Exp(-10*Time.unscaledDeltaTime));
            Vector3 delta=(Quaternion.Inverse(previousHead)*head.rotation).eulerAngles;
            Vector3 hairTarget=new Vector3(Mathf.Clamp(-Signed(delta.x)*.6f,-4,4),0,Mathf.Clamp(-Signed(delta.z)*.6f,-5,5));
            hairTilt=Vector3.SmoothDamp(hairTilt,hairTarget,ref hairVelocity,.16f,100,Time.unscaledDeltaTime);previousHead=head.rotation;
            foreach(var strand in flowingHair)strand.localRotation=Quaternion.Euler(hairTilt);
            PoseArms();
            PoseHands();
        }
        void BuildHand(int side,Material skin)
        {
            var palm=Node("Pulso articulado",foreArms[side],new Vector3(0,-.30f,-.008f));handRoots[side]=palm;
            Shape("Palma",PrimitiveType.Sphere,palm,new Vector3(0,-.043f,0),new Vector3(.102f,.115f,.052f),skin);
            float[] lengths={.042f,.054f,.060f,.055f,.043f};
            for(int finger=0;finger<5;finger++) {
                float x=finger==0?(side==0?.049f:-.049f):(finger-2.5f)*.025f*(side==0?-1:1);
                var parent=palm;Vector3 basePosition=new Vector3(x,finger==0?-.035f:-.09f,0);
                for(int joint=0;joint<3;joint++) {
                    var bone=Node("Dedo "+finger+" / "+joint,parent,joint==0?basePosition:new Vector3(0,-lengths[finger]*(joint==1?1:.72f),0));fingerJoints[side,finger*3+joint]=bone;
                    float length=lengths[finger]*(joint==0?1:joint==1?.72f:.58f);
                    float radius=(finger==0?.012f:.010f)*(1-joint*.15f);
                    MeshPart("Falange",bone,AdvancedGeometry.Tube(new[]{Vector3.zero,new Vector3(0,-length*.25f,0),new Vector3(0,-length*.80f,0),new Vector3(0,-length,0)},new[]{radius*.8f,radius,radius*.86f,radius*.50f},12),skin);
                    Shape("Articulação",PrimitiveType.Sphere,bone,Vector3.zero,Vector3.one*radius*1.75f,skin);
                    if(finger==0 && joint==0)bone.localRotation=Quaternion.Euler(0,0,side==0?42:-42);
                    parent=bone;
                }
            }
        }
        void PoseHands()
        {
            float t=1-Mathf.Exp(-15*Time.unscaledDeltaTime);
            for(int side=0;side<2;side++) {
                Point[] points=tracking!=null && !previewAnimation ? tracking.GetHand(side==0) : null;
                Vector3 P(int i)=>new Vector3(tracking!=null && tracking.mirror?-points[i].x:points[i].x,-points[i].y,points[i].z);
                Vector3 right=handRoots[side].right;
                Quaternion wrist=Quaternion.identity;
                Quaternion wristWorld=handRoots[side].rotation;
                if(points!=null) {
                    var direction=P(9)-P(0);var across=(P(5)-P(17))*(side==0?1:-1);
                    if(points[5].confidence>.5f && points[9].confidence>.5f && points[17].confidence>.5f && direction.sqrMagnitude>.00001f && across.sqrMagnitude>.00001f) {
                        right=Vector3.ProjectOnPlane(across,direction).normalized;
                        var up=-direction.normalized;var forward=Vector3.Cross(right,up).normalized;
                        if(forward.sqrMagnitude>.5f){wristWorld=Quaternion.LookRotation(forward,up);wrist=Quaternion.Inverse(foreArms[side].rotation)*wristWorld;}
                    }
                }
                handRoots[side].localRotation=Quaternion.Slerp(handRoots[side].localRotation,wrist,t);
                for(int finger=0;finger<5;finger++)for(int joint=0;joint<3;joint++) {
                    float curl=previewAnimation?(side==1?8:20):16;
                    float spread=finger==0 && joint==0?(side==0?42:-42):0;
                    if(points!=null) {
                        int start=finger==0?1:1+finger*4;int center=start+joint;
                        int before=joint==0?0:center-1,after=center+1;
                        Vector3 a=P(center)-P(before),b=P(after)-P(center);
                        if(points[before].confidence>.5f && points[center].confidence>.5f && points[after].confidence>.5f && a.sqrMagnitude>.0000001f && b.sqrMagnitude>.0000001f)curl=Mathf.Clamp(Vector3.SignedAngle(a,b,right),-95,95);
                        if(joint==0 && points[center].confidence>.5f && points[after].confidence>.5f && b.sqrMagnitude>.0000001f){var local=Quaternion.Inverse(wristWorld)*b;spread=Mathf.Clamp(Mathf.Atan2(local.x,-local.y)*Mathf.Rad2Deg,finger==0?-75:-30,finger==0?75:30);}
                    }
                    var bone=fingerJoints[side,finger*3+joint];bone.localRotation=Quaternion.Slerp(bone.localRotation,Quaternion.Euler(curl,0,spread),t);
                }
            }
        }
        void PoseArms()
        {
            var points=tracking!=null && !previewAnimation ? tracking.PosePoints : null;
            float t=1-Mathf.Exp(-10*Time.unscaledDeltaTime);
            for(int side=0;side<2;side++) {
                int sourceSide=tracking!=null && !tracking.mirror?1-side:side;
                int shoulder=sourceSide==0?11:12, elbow=sourceSide==0?13:14, wrist=sourceSide==0?15:16;
                Vector3 Convert(Point p)=>new Vector3(tracking.mirror?-p.x:p.x,-p.y,p.z);
                Quaternion up=Quaternion.Euler(0,0,side==0?-8:8), lower=Quaternion.Euler(-12,0,0);
                if(points!=null && points[shoulder].confidence>.55f && points[elbow].confidence>.55f) {
                    Vector3 delta=Convert(points[elbow])-Convert(points[shoulder]);
                    if(delta.sqrMagnitude>.001f) up=Quaternion.FromToRotation(Vector3.down,delta);
                    if(points[wrist].confidence>.55f) { delta=Convert(points[wrist])-Convert(points[elbow]); if(delta.sqrMagnitude>.001f) lower=Quaternion.Inverse(up)*Quaternion.FromToRotation(Vector3.down,delta); }
                }
                if(previewAnimation && side==1) { up=Quaternion.Euler(0,0,85); lower=Quaternion.Euler(0,0,45+Mathf.Sin(time*2.2f)*16); }
                upperArms[side].rotation=Quaternion.Slerp(upperArms[side].rotation,transform.rotation*up,t);
                foreArms[side].localRotation=Quaternion.Slerp(foreArms[side].localRotation,lower,t);
            }
        }
        static void Release(Object value) { if(value==null) return; if(Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
        void OnDestroy() { foreach(var m in materials) Release(m); foreach(var m in meshes) Release(m); }
    }
}
