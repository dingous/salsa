using System;
using System.Collections.Generic;
using UnityEngine;

namespace WebcamAvatar
{
    [Serializable] class FacePoint {public float x,y;}
    [Serializable] class FaceTopology {public FacePoint[] points;public int[] triangles,contour;public string[] shapes;}

    /// <summary>Original sculpted surface with real facial blendshapes, lips and eyelid openings.</summary>
    public class AdvancedFace : MonoBehaviour
    {
        public SkinnedMeshRenderer Renderer {get;private set;}
        public int ShapeCount => Renderer!=null ? Renderer.sharedMesh.blendShapeCount : 0;
        Mesh mesh;
        AvatarProfile profile;
        FaceTopology topology;
        public Vector3 PointOnFace(float x,float y)=>Surface(x,y);
        public void Build(AvatarProfile value,Material skin)
        {
            profile=value;
            var data=Resources.Load<TextAsset>("AdvancedFaceTopology");
            if(data==null)throw new InvalidOperationException("Topologia facial avançada não encontrada.");
            topology=JsonUtility.FromJson<FaceTopology>(data.text);
            var vertices=new List<Vector3>();var colors=new List<Color>();
            foreach(var point in topology.points){vertices.Add(Surface(point.x,point.y));colors.Add(VertexColor(point.x,point.y));}
            var triangles=new List<int>(topology.triangles);
            // Connect the face outline to the back of the skull. The seam shares the same vertices.
            int count=topology.contour.Length;int previousStart=-1;
            for(int ring=1;ring<=20;ring++) {
                int start=vertices.Count;float t=(float)ring/20;
                for(int i=0;i<count;i++) {
                    var boundary=vertices[topology.contour[i]];
                    float radial=Mathf.Cos(t*Mathf.PI*.5f);
                    vertices.Add(new Vector3(boundary.x*radial,boundary.y*radial,.02f+Mathf.Sin(t*Mathf.PI*.5f)*.25f));colors.Add(Color.white);
                }
                for(int i=0;i<count;i++) {
                    int next=(i+1)%count,a=ring==1?topology.contour[i]:previousStart+i,b=ring==1?topology.contour[next]:previousStart+next,c=start+i,d=start+next;
                    triangles.AddRange(new[]{a,b,c,b,d,c});
                }
                previousStart=start;
            }
            mesh=new Mesh{name="Advanced cartoon face / 52 blendshapes"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            Vector3[] rest=vertices.ToArray();var zero=new Vector3[rest.Length];var baseNormals=mesh.normals;
            foreach(string name in topology.shapes) {
                var delta=new Vector3[rest.Length];
                for(int i=0;i<topology.points.Length;i++) delta[i]=Deform(name,topology.points[i].x,topology.points[i].y);
                // The skull also follows lateral jaw motion, with a small falloff at the rear.
                if(name.StartsWith("jaw"))for(int i=topology.points.Length;i<rest.Length;i++) {
                    float jaw=Mathf.SmoothStep(0,1,Mathf.Clamp01((-rest[i].y-.04f)/.22f));
                    if(name=="jawOpen")delta[i]=Vector3.down*.065f*jaw;
                    if(name=="jawLeft")delta[i]=Vector3.left*.012f*jaw;
                    if(name=="jawRight")delta[i]=Vector3.right*.012f*jaw;
                }
                var posed=new Vector3[rest.Length];for(int i=0;i<posed.Length;i++)posed[i]=rest[i]+delta[i];
                mesh.vertices=posed;mesh.RecalculateNormals();var movedNormals=mesh.normals;
                var normalDelta=new Vector3[rest.Length];for(int i=0;i<normalDelta.Length;i++)normalDelta[i]=movedNormals[i]-baseNormals[i];
                mesh.AddBlendShapeFrame(name,100,delta,normalDelta,zero);
            }
            mesh.vertices=rest;mesh.normals=baseNormals;mesh.RecalculateBounds();
            Renderer=gameObject.AddComponent<SkinnedMeshRenderer>();Renderer.sharedMesh=mesh;Renderer.sharedMaterial=skin;Renderer.updateWhenOffscreen=true;
            Renderer.localBounds=new Bounds(Vector3.zero,new Vector3(.9f,1.2f,.9f));
        }
        static float G(float x,float y,float cx,float cy,float sx,float sy)=>Mathf.Exp(-((x-cx)*(x-cx)/(sx*sx)+(y-cy)*(y-cy)/(sy*sy))*2);
        Vector3 Surface(float x,float y)
        {
            float rx=.278f*profile.faceWidth,ry=.36f;
            float xx=x*rx;float yy=y*ry;
            float eyeMask=Mathf.Max(G(x,y,-.43f,.20f,.40f,.29f),G(x,y,.43f,.20f,.40f,.29f));
            xx+=(x-Mathf.Sign(x)*.43f)*rx*(profile.EffectiveEyeSize-1)*eyeMask;
            yy+=(y-.20f)*ry*(profile.EffectiveEyeSize-1)*eyeMask;
            float depth=-.052f-.218f*Mathf.Sqrt(Mathf.Max(0,1-x*x-.72f*y*y));
            depth-=G(x,y,0,.10f,.15f,.33f)*.036f*profile.noseSize;
            depth-=G(x,y,0,-.12f,.22f,.13f)*.071f*profile.noseSize;
            depth-=G(x,y,-.14f,-.16f,.12f,.08f)*.024f*profile.noseSize;
            depth-=G(x,y,.14f,-.16f,.12f,.08f)*.024f*profile.noseSize;
            depth-= (G(x,y,-.6f,-.17f,.31f,.34f)+G(x,y,.6f,-.17f,.31f,.34f))*.018f*profile.cheekVolume;
            float lipRadius=Mathf.Sqrt(x*x/(.285f*.285f)+(y+.43f)*(y+.43f)/(.046f*.046f));
            if(lipRadius>.7f && lipRadius<2.5f)depth-=Mathf.Exp(-Mathf.Pow((lipRadius-1.35f)/.6f,2))*.010f*profile.lipFullness;
            // Preserve photo-based spacing and mouth proportions through local deformations.
            xx+=Mathf.Sign(x)*(profile.eyeSpacing-1)*.12f*G(x,y,Mathf.Sign(x)*.43f,.20f,.4f,.32f);
            xx+=x*(profile.mouthWidth-1)*rx*G(x,y,0,-.43f,.44f,.20f);
            xx*=Mathf.Lerp(profile.jawWidth,1,Mathf.Clamp01((y+.8f)/.65f));
            return new Vector3(xx,yy,depth);
        }
        Color VertexColor(float x,float y)
        {
            float lip=Mathf.Sqrt(x*x/(.285f*.285f)+(y+.43f)*(y+.43f)/(.046f*.046f));
            float weight=lip>.6f && lip<2.2f ? Mathf.Exp(-Mathf.Pow((lip-1.25f)/.6f,2)) : 0;
            Color c=Color.Lerp(Color.white,new Color(.79f,.49f,.48f),weight*.40f);
            float blush=G(x,y,-.61f,-.20f,.28f,.18f)+G(x,y,.61f,-.20f,.28f,.18f);
            c=Color.Lerp(c,new Color(1,.80f,.79f),blush*.18f);
            for(int side=-1;side<=1;side+=2) {
                float eyeRadius=Mathf.Sqrt(Mathf.Pow((x-side*.43f)/.265f,2)+Mathf.Pow((y-.20f)/.125f,2));
                if(y>.2f && eyeRadius>1 && eyeRadius<1.12f)c=Color.Lerp(c,new Color(.40f,.32f,.30f),.6f);
            }
            return c;
        }
        Vector3 Deform(string name,float x,float y)
        {
            float side=name.EndsWith("Left")?-1:name.EndsWith("Right")?1:0;
            float sideMask=side==0?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((x*side+.04f)/.22f));
            float eye=G(x,y,side*.43f,.20f,.43f,.33f);
            float eyeRadius=Mathf.Sqrt(Mathf.Pow((x-side*.43f)/.265f,2)+Mathf.Pow((y-.20f)/.125f,2));
            float closure=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((eyeRadius-1.05f)/1.40f));
            float brow=G(x,y,side*.43f,.52f,.45f,.30f);
            float mouth=G(x,y,0,-.43f,.55f,.30f);
            float corner=G(x,y,side*.27f,-.43f,.25f,.22f);
            float jaw=Mathf.SmoothStep(0,1,Mathf.Clamp01((-y-.18f)/.65f));
            var d=Vector3.zero;
            if(name.StartsWith("eyeBlink")){float size=1+(profile.EffectiveEyeSize-1)*Mathf.Max(G(x,y,-.43f,.20f,.40f,.29f),G(x,y,.43f,.20f,.40f,.29f));d.y=-(y-.20f)*.36f*closure*size;}
            else if(name.StartsWith("eyeSquint"))d.y=-(y-.20f)*.20f*eye;
            else if(name.StartsWith("eyeWide"))d.y=(y-.20f)*.17f*eye;
            else if(name.StartsWith("eyeLook")) {
                if(name.Contains("Up"))d.y=.002f*eye;
                if(name.Contains("Down"))d.y=-.002f*eye;
                if(name.Contains("In"))d.x=-side*.002f*eye;
                if(name.Contains("Out"))d.x=side*.002f*eye;
            }
            else if(name.StartsWith("browDown"))d.y=-.026f*brow;
            else if(name.StartsWith("browOuterUp"))d.y=.035f*brow;
            else if(name=="browInnerUp")d.y=.032f*(G(x,y,-.25f,.5f,.3f,.25f)+G(x,y,.25f,.5f,.3f,.25f));
            else if(name=="jawOpen"){float lowerLip=Mathf.SmoothStep(0,1,Mathf.Clamp01((-.43f-y)/.028f))*mouth;d.y=-.065f*jaw-.065f*lowerLip;d.z=.012f*jaw;}
            else if(name=="jawLeft")d.x=-.025f*jaw;
            else if(name=="jawRight")d.x=.025f*jaw;
            else if(name=="jawForward")d.z=-.022f*jaw;
            else if(name.StartsWith("mouthSmile")){d.y=.033f*corner;d.x=side*.018f*corner;}
            else if(name.StartsWith("mouthFrown"))d.y=-.024f*corner;
            else if(name.StartsWith("mouthStretch"))d.x=side*.027f*corner;
            else if(name.StartsWith("mouthDimple"))d.z=.015f*corner;
            else if(name=="mouthPucker"){d.x=-x*.08f*mouth;d.z=-.024f*mouth;d.y=(y+.43f)*.06f*mouth;}
            else if(name=="mouthFunnel"){d.x=-x*.055f*mouth;d.z=-.018f*mouth;d.y=(y+.43f)*.16f*mouth;}
            else if(name=="mouthLeft")d.x=-.024f*mouth;
            else if(name=="mouthRight")d.x=.024f*mouth;
            else if(name=="mouthClose")d.y=(y<-.43f?.01f:-.007f)*mouth;
            else if(name.StartsWith("mouthUpperUp"))d.y=.026f*mouth*sideMask*Mathf.Clamp01((y+.44f)/.09f);
            else if(name.StartsWith("mouthLowerDown"))d.y=-.035f*mouth*sideMask*Mathf.Clamp01((-.42f-y)/.09f);
            else if(name.StartsWith("mouthPress")){d.y=-(y+.43f)*.15f*mouth*sideMask;d.z=.006f*mouth*sideMask;}
            else if(name=="mouthRollUpper" || name=="mouthRollLower"){float mask=name.EndsWith("Upper")?Mathf.Clamp01((y+.43f)/.05f):Mathf.Clamp01((-.43f-y)/.05f);d.z=.011f*mouth*mask;}
            else if(name=="mouthShrugUpper")d.y=.015f*mouth*Mathf.Clamp01((y+.43f)/.05f);
            else if(name=="mouthShrugLower")d.y=.017f*mouth*Mathf.Clamp01((-.43f-y)/.05f);
            else if(name=="cheekPuff"){float cheeks=G(x,y,-.6f,-.13f,.36f,.35f)+G(x,y,.6f,-.13f,.36f,.35f);d.z=-.026f*cheeks;d.x=Mathf.Sign(x)*.012f*cheeks;}
            else if(name.StartsWith("cheekSquint"))d.y=.017f*G(x,y,side*.62f,-.02f,.30f,.25f);
            else if(name.StartsWith("noseSneer")){d.y=.012f*G(x,y,side*.16f,-.11f,.20f,.15f);d.z=-.006f*G(x,y,side*.15f,-.12f,.20f,.16f);}
            else if(name=="tongueOut")d.y=-.002f*mouth;
            return d;
        }
        public void Animate(Func<string,float> expression)
        {
            for(int i=0;i<topology.shapes.Length;i++) {
                string name=topology.shapes[i];float weight=expression(name);
                if(name=="jawOpen")weight*=1-expression("mouthClose")*.85f;
                Renderer.SetBlendShapeWeight(i,Mathf.Clamp01(weight)*100);
            }
        }
        void OnDestroy(){if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
