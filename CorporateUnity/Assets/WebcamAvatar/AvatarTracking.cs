using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace WebcamAvatar
{
    [Serializable] public class Shape { public string name; public float value; }
    [Serializable] public class Point { public float x, y, z, confidence; }
    [Serializable] public class TrackingFrame
    {
        public bool face, pose;
        public Shape[] shapes;
        public Point[] points;
        public float[] rotation;
        public Point[] leftHand, rightHand;
        public Point[] imagePose;
    }

    public class AvatarTracking : MonoBehaviour
    {
        public int port = 5059;
        public float smoothing = 12;
        public bool mirror = true;
        public bool showDiagnostics = false;
        public float expressionGain = 1.15f;
        public bool Connected => Time.unscaledTime - received < .8f;
        public bool FaceDetected => Connected && frame != null && frame.face;
        public bool PoseDetected => Connected && frame != null && frame.pose;
        public string Error => error;
        public Quaternion HeadRotation { get; private set; } = Quaternion.identity;
        public Point[] PosePoints => PoseDetected && frame.points != null && frame.points.Length == 33 ? frame.points : null;
        public Point[] ImagePosePoints=>PoseDetected&&frame.imagePose!=null&&frame.imagePose.Length==33?frame.imagePose:null;
        public int FrameSequence {get;private set;}
        public bool HandsDetected => GetHand(true)!=null || GetHand(false)!=null;
        public bool FullBodyDetected {
            get {var p=PosePoints;if(p==null)return false;foreach(int i in new[]{11,12,13,14,15,16,23,24,25,26,27,28,29,30,31,32})if(p[i].confidence<.55f)return false;return true;}
        }
        public bool HandComplete(bool left){var p=GetHand(left);if(p==null)return false;foreach(var point in p)if(point.confidence<.5f)return false;return true;}
        public Point[] GetHand(bool left) {
            if(!Connected || frame==null)return null;
            if(mirror)left=!left;
            var hand=left?frame.leftHand:frame.rightHand;
            return hand!=null && hand.Length==21 && hand[0].confidence>.5f ? hand : null;
        }
        Quaternion neutral = Quaternion.identity;
        Quaternion rawHead = Quaternion.identity;
        Vector2 bodyReference;
        bool hasBodyReference;
        bool BodyCenter(out Vector2 center) {
            center=Vector2.zero;
            var p=Connected && frame!=null?frame.imagePose:null;
            if(p==null || p.Length!=33 || p[23].confidence<.55f || p[24].confidence<.55f)return false;
            foreach(int i in new[]{23,24})if(p[i].x<0 || p[i].x>1 || p[i].y<0 || p[i].y>1)return false;
            center=new Vector2((p[23].x+p[24].x)*.5f,(p[23].y+p[24].y)*.5f);return true;
        }
        public Vector2 BodyOffset {
            get {if(!BodyCenter(out var center))return Vector2.zero;if(!hasBodyReference){bodyReference=center;hasBodyReference=true;}var d=center-bodyReference;return new Vector2(Mathf.Clamp(d.x*(mirror?-1:1),-.4f,.4f),Mathf.Clamp(-d.y,-.4f,.4f));}
        }
        readonly Dictionary<string, float> smoothed = new Dictionary<string, float>();
        public float GetExpression(string name) {
            if(mirror){if(name.EndsWith("Left"))name=name.Substring(0,name.Length-4)+"Right";else if(name.EndsWith("Right"))name=name.Substring(0,name.Length-5)+"Left";}
            return smoothed.TryGetValue(name,out var value)?value:0;
        }
        public void Calibrate() {if(FaceDetected)neutral=rawHead;if(BodyCenter(out var center)){bodyReference=center;hasBodyReference=true;}}
        [Tooltip("Opcional: avatar importado com rig Humanoid e blendshapes MediaPipe/ARKit.")]
        public Animator humanoid;
        public SkinnedMeshRenderer[] facialMeshes;
        UdpClient socket;
        TrackingFrame frame;
        readonly AdaptivePoseFilter poseFilter=new AdaptivePoseFilter();
        readonly AdaptivePoseFilter leftHandFilter=new AdaptivePoseFilter(21,1.5f,3f),rightHandFilter=new AdaptivePoseFilter(21,1.5f,3f);
        float received = -10;
        string error;
        readonly Dictionary<string, float> expressions = new Dictionary<string, float>();
        readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        readonly Dictionary<Transform, Vector3> restDirections = new Dictionary<Transform, Vector3>();
        Transform head, leftEye, rightEye, mouth, leftBrow, rightBrow;
        readonly List<Transform> joints = new List<Transform>();
        readonly List<Transform> segments = new List<Transform>();
        static readonly int[,] links = {{11,12},{11,13},{13,15},{12,14},{14,16},{11,23},{12,24},{23,24},{23,25},{25,27},{24,26},{26,28}};
        Material skin, suit, dark, accent;

        void Start()
        {
            // Keep synthetic smoke packets isolated from an open user session.
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--corporate-smoke")>=0)port=15059;
            try { socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, port)); socket.Client.Blocking = false; }
            catch (Exception e) { error = e.Message; }
            if (humanoid != null && humanoid.isHuman) CacheRig();
            else if (GetComponent<CartoonAvatar>() == null) CreateDemo();
        }
        Material Mat(Color color)
        {
            var m = new Material(Shader.Find("Standard")); m.color = color; return m;
        }
        Transform Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(go.GetComponent<Collider>()); return go.transform;
        }
        void CreateDemo()
        {
            skin = Mat(new Color(.95f,.68f,.45f)); suit = Mat(new Color(.12f,.62f,.72f));
            dark = Mat(new Color(.025f,.035f,.06f)); accent = Mat(new Color(.9f,.95f,1));
            head = new GameObject("Cabeça").transform; head.SetParent(transform); head.position = new Vector3(0,1.85f,0);
            Part("Rosto", PrimitiveType.Sphere, head, Vector3.zero, new Vector3(.52f,.62f,.45f), skin);
            leftEye = Part("Olho esquerdo", PrimitiveType.Sphere, head, new Vector3(-.11f,.06f,-.21f), new Vector3(.07f,.09f,.035f), dark);
            rightEye = Part("Olho direito", PrimitiveType.Sphere, head, new Vector3(.11f,.06f,-.21f), new Vector3(.07f,.09f,.035f), dark);
            mouth = Part("Boca", PrimitiveType.Sphere, head, new Vector3(0,-.13f,-.215f), new Vector3(.14f,.025f,.035f), dark);
            leftBrow = Part("Sobrancelha esquerda", PrimitiveType.Cube, head, new Vector3(-.11f,.15f,-.205f), new Vector3(.1f,.018f,.025f), dark);
            rightBrow = Part("Sobrancelha direita", PrimitiveType.Cube, head, new Vector3(.11f,.15f,-.205f), new Vector3(.1f,.018f,.025f), dark);
            for (int i=0;i<33;i++) joints.Add(Part("Articulação " + i, PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one*.1f, skin));
            for (int i=0;i<links.GetLength(0);i++) segments.Add(Part("Membro " + i, PrimitiveType.Cylinder, transform, Vector3.zero, Vector3.one, suit));
            SetDemoPose(null, 1);
        }
        Vector3 Position(int index, Point[] points)
        {
            if (points == null || points.Length != 33)
            {
                switch(index) {
                    case 11:return new Vector3(-.32f,1.5f,0); case 12:return new Vector3(.32f,1.5f,0);
                    case 13:return new Vector3(-.5f,1.15f,0); case 14:return new Vector3(.5f,1.15f,0);
                    case 15:return new Vector3(-.55f,.85f,0); case 16:return new Vector3(.55f,.85f,0);
                    case 23:return new Vector3(-.2f,.9f,0); case 24:return new Vector3(.2f,.9f,0);
                    case 25:return new Vector3(-.2f,.5f,0); case 26:return new Vector3(.2f,.5f,0);
                    case 27:return new Vector3(-.2f,.1f,0); case 28:return new Vector3(.2f,.1f,0);
                    default:return Vector3.zero;
                }
            }
            var center = new Vector3((points[23].x+points[24].x)*.5f,(points[23].y+points[24].y)*.5f,(points[23].z+points[24].z)*.5f);
            var p = points[index]; var v = new Vector3(p.x,-p.y,p.z) - new Vector3(center.x,-center.y,center.z);
            if (mirror) v.x = -v.x;
            return v + Vector3.up*.95f;
        }
        void SetDemoPose(Point[] points, float t)
        {
            for(int i=0;i<joints.Count;i++) joints[i].gameObject.SetActive(false);
            for(int i=0;i<segments.Count;i++)
            {
                int a=links[i,0], b=links[i,1];
                bool visible=points==null || (points[a].confidence>.45f && points[b].confidence>.45f);
                segments[i].gameObject.SetActive(visible);
                if(!visible) continue;
                Vector3 pa=Position(a,points), pb=Position(b,points);
                joints[a].gameObject.SetActive(true); joints[b].gameObject.SetActive(true);
                joints[a].position=Vector3.Lerp(joints[a].position,pa,t); joints[b].position=Vector3.Lerp(joints[b].position,pb,t);
                var segment=segments[i]; segment.position=Vector3.Lerp(segment.position,(pa+pb)*.5f,t);
                segment.rotation=Quaternion.Slerp(segment.rotation,Quaternion.FromToRotation(Vector3.up,pb-pa),t);
                segment.localScale=new Vector3(.09f,Vector3.Distance(pa,pb)*.5f,.09f);
            }
            head.position=Vector3.Lerp(head.position,(Position(11,points)+Position(12,points))*.5f+Vector3.up*.35f,t);
        }
        float Expression(string name) => expressions.TryGetValue(name,out var value) ? value : 0;
        void Update()
        {
            if(socket!=null) try {
                // Bounded drain: always prefer newest frame without blocking Unity.
                TrackingFrame newest=null;
                for(int i=0;i<64 && socket.Available>0;i++) {
                    IPEndPoint endpoint=null; var data=socket.Receive(ref endpoint);
                    var next=JsonUtility.FromJson<TrackingFrame>(Encoding.UTF8.GetString(data));
                    if(Valid(next))newest=next;
                }
                if(newest!=null){
                    if(newest.pose)newest.points=poseFilter.Process(newest.points,Time.unscaledTime);else poseFilter.Reset();
                    newest.leftHand=leftHandFilter.Process(newest.leftHand,Time.unscaledTime);
                    newest.rightHand=rightHandFilter.Process(newest.rightHand,Time.unscaledTime);
                    frame=newest;FrameSequence++;received=Time.unscaledTime;error=null;
                }
            } catch(SocketException e) { if(e.SocketErrorCode!=SocketError.WouldBlock) error=e.Message; }
              catch(Exception e) { error=e.Message; }
            float t=1-Mathf.Exp(-smoothing*Time.unscaledDeltaTime);
            bool live=Time.unscaledTime-received<.6f;
            expressions.Clear();
            if(live && frame.face && frame.shapes!=null) foreach(var s in frame.shapes) expressions[s.name]=Mathf.Clamp01(s.value);
            var points=live && frame.pose && frame.points.Length==33 ? frame.points : null;
            Quaternion rotation=Quaternion.identity;
            if(live && frame.face && frame.rotation!=null && frame.rotation.Length==9) {
                var r=frame.rotation;
                // Conjugate MediaPipe rotation by Y reflection to Unity coordinates.
                var forward=new Vector3(r[2],-r[5],r[8]); var up=new Vector3(-r[1],r[4],-r[7]);
                if(mirror) { forward.x=-forward.x; up.x=-up.x; }
                if(forward.sqrMagnitude>.1f && up.sqrMagnitude>.1f) rotation=Quaternion.LookRotation(forward,up);
            }
            rawHead = rotation;
            HeadRotation = Quaternion.Slerp(HeadRotation, live && frame.face ? Quaternion.Inverse(neutral) * rotation : Quaternion.identity, t);
            // Keep values between frames so expressive geometry never snaps on packet boundaries.
            var keys = new List<string>(smoothed.Keys);
            foreach(var s in expressions) if(!smoothed.ContainsKey(s.Key)) { smoothed[s.Key]=0; keys.Add(s.Key); }
            foreach(var key in keys){
                float target=Mathf.Clamp01(Expression(key)*expressionGain);
                if(key.StartsWith("eyeBlink")){
                    target=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.65f,target));
                    float rate=target>smoothed[key]?80:35;
                    smoothed[key]=Mathf.Lerp(smoothed[key],target,1-Mathf.Exp(-rate*Time.unscaledDeltaTime));
                }else smoothed[key]=Mathf.Lerp(smoothed[key],target,t);
            }
            if(head!=null) {
                SetDemoPose(points,t); head.localRotation=Quaternion.Slerp(head.localRotation,rotation,t);
                leftEye.localScale=new Vector3(.07f,Mathf.Lerp(.09f,.006f,Expression("eyeBlinkLeft")),.035f);
                rightEye.localScale=new Vector3(.07f,Mathf.Lerp(.09f,.006f,Expression("eyeBlinkRight")),.035f);
                float smile=(Expression("mouthSmileLeft")+Expression("mouthSmileRight"))*.5f;
                mouth.localScale=new Vector3(.14f+smile*.11f,.025f+Expression("jawOpen")*.16f,.035f);
                leftBrow.localPosition=new Vector3(-.11f,.15f+Expression("browInnerUp")*.07f-Expression("browDownLeft")*.04f,-.205f);
                rightBrow.localPosition=new Vector3(.11f,.15f+Expression("browInnerUp")*.07f-Expression("browDownRight")*.04f,-.205f);
            }
            if(humanoid!=null && humanoid.isHuman) UpdateRig(points,rotation,t);
            if(facialMeshes!=null) foreach(var renderer in facialMeshes) {
                if(renderer==null || renderer.sharedMesh==null) continue;
                for(int i=0;i<renderer.sharedMesh.blendShapeCount;i++) {
                    string name=renderer.sharedMesh.GetBlendShapeName(i); int dot=name.LastIndexOf('.'); if(dot>=0) name=name.Substring(dot+1);
                    if(string.IsNullOrEmpty(name)) continue;
                    name=char.ToLowerInvariant(name[0])+name.Substring(1);
                    renderer.SetBlendShapeWeight(i,Mathf.Lerp(renderer.GetBlendShapeWeight(i),Expression(name)*100,t));
                }
            }
        }
        static readonly HumanBodyBones[] bones={HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg};
        static readonly HumanBodyBones[] ends={HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
        static readonly int[,] rigLinks={{11,13},{13,15},{12,14},{14,16},{23,25},{25,27},{24,26},{26,28}};
        void CacheRig() {
            humanoid.enabled=false;
            for(int i=0;i<bones.Length;i++) { var b=humanoid.GetBoneTransform(bones[i]); var e=humanoid.GetBoneTransform(ends[i]); if(b!=null && e!=null) { rest[b]=b.localRotation; restDirections[b]=b.InverseTransformDirection(e.position-b.position).normalized; } }
            var h=humanoid.GetBoneTransform(HumanBodyBones.Head); if(h!=null) rest[h]=h.localRotation;
        }
        void UpdateRig(Point[] points,Quaternion rotation,float t) {
            for(int i=0;i<bones.Length;i++) {
                var b=humanoid.GetBoneTransform(bones[i]); if(b==null || !restDirections.ContainsKey(b)) continue;
                int a=rigLinks[i,0], e=rigLinks[i,1];
                if(points==null || points[a].confidence<.5f || points[e].confidence<.5f) { b.localRotation=Quaternion.Slerp(b.localRotation,rest[b],t); continue; }
                Vector3 direction=Position(e,points)-Position(a,points);
                if(direction.sqrMagnitude<.0001f) continue;
                Quaternion target=Quaternion.FromToRotation(b.TransformDirection(restDirections[b]),direction)*b.rotation;
                b.rotation=Quaternion.Slerp(b.rotation,target,t);
            }
            var h=humanoid.GetBoneTransform(HumanBodyBones.Head); if(h!=null) h.localRotation=Quaternion.Slerp(h.localRotation,rest[h]*rotation,t);
        }
        void OnGUI() {
            if(!showDiagnostics) return;
            GUI.Box(new Rect(16,16,420,132),"WEBCAM AVATAR • MediaPipe gratuito");
            GUI.Label(new Rect(30,45,390,24),Time.unscaledTime-received<.6f ? $"Conectado | Rosto: {frame.face} | Pose: {frame.pose}" : "Aguardando Tracker/start.ps1 • UDP localhost:5059");
            mirror=GUI.Toggle(new Rect(30,73,200,24),mirror,"Espelhar movimentos");
            GUI.Label(new Rect(30,102,390,32),error ?? "Sorria, pisque, abra a boca e movimente os braços.");
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Valid(TrackingFrame value) {
            if(value==null || value.points==null || (value.points.Length!=0 && value.points.Length!=33) || value.shapes==null || value.shapes.Length>64 || value.rotation==null || (value.rotation.Length!=0 && value.rotation.Length!=9)) return false;
            foreach(var p in value.points) if(p==null || !Finite(p.x) || !Finite(p.y) || !Finite(p.z) || !Finite(p.confidence)) return false;
            foreach(var s in value.shapes) if(s==null || string.IsNullOrEmpty(s.name) || !Finite(s.value)) return false;
            foreach(var r in value.rotation) if(!Finite(r)) return false;
            if(value.imagePose!=null){if(value.imagePose.Length!=0 && value.imagePose.Length!=33)return false;foreach(var p in value.imagePose)if(p==null || !Finite(p.x) || !Finite(p.y) || !Finite(p.z) || !Finite(p.confidence))return false;}
            foreach(var hand in new[]{value.leftHand,value.rightHand}) {
                if(hand==null)continue;
                if(hand.Length!=0 && hand.Length!=21)return false;
                foreach(var p in hand)if(p==null || !Finite(p.x) || !Finite(p.y) || !Finite(p.z) || !Finite(p.confidence))return false;
            }
            return true;
        }
        void OnDestroy() { socket?.Close(); foreach(var m in new[]{skin,suit,dark,accent}) if(m!=null) Destroy(m); }
    }
}
