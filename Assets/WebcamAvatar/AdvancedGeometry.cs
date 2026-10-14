using System.Collections.Generic;
using UnityEngine;

namespace WebcamAvatar
{
    public static class AdvancedGeometry
    {
        public static Mesh Tube(IList<Vector3> path,IList<float> radii,int sides=12,float flatten=1)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            Vector3 normal=Vector3.right;
            for(int i=0;i<path.Count;i++) {
                Vector3 tangent=(path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(i-1,0)]).normalized;
                if(tangent.sqrMagnitude<.1f)tangent=Vector3.down;
                normal=Vector3.ProjectOnPlane(normal,tangent).normalized;
                if(normal.sqrMagnitude<.1f)normal=Vector3.Cross(tangent,Vector3.forward).normalized;
                Vector3 binormal=Vector3.Cross(tangent,normal).normalized;
                for(int j=0;j<=sides;j++) {
                    float angle=(float)j/sides*Mathf.PI*2;
                    vertices.Add(path[i]+(normal*Mathf.Cos(angle)+binormal*Mathf.Sin(angle)*flatten)*radii[i]);
                    uv.Add(new Vector2((float)j/sides,(float)i/(path.Count-1)));
                }
            }
            for(int i=0;i<path.Count-1;i++)for(int j=0;j<sides;j++) {
                int a=i*(sides+1)+j,b=a+sides+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});
            }
            int first=vertices.Count;vertices.Add(path[0]);uv.Add(Vector2.zero);
            int last=vertices.Count;vertices.Add(path[path.Count-1]);uv.Add(Vector2.one);
            for(int j=0;j<sides;j++){triangles.AddRange(new[]{first,j+1,j});int a=(path.Count-1)*(sides+1)+j;triangles.AddRange(new[]{last,a,a+1});}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Lock(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float width)
        {
            var path=new List<Vector3>();var radii=new List<float>();
            for(int i=0;i<=24;i++){float t=i/24f,u=1-t;path.Add(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);radii.Add(width*Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Lerp(.07f,.998f,t)),.65f));}
            return Tube(path,radii,16,.42f);
        }
        public static Mesh Loft(float[] height,float[] width,float[] depth,int slices=48)
        {
            var v=new List<Vector3>();var tri=new List<int>();var uv=new List<Vector2>();
            for(int i=0;i<height.Length;i++)for(int j=0;j<=slices;j++) {
                float angle=(float)j/slices*Mathf.PI*2;
                v.Add(new Vector3(Mathf.Cos(angle)*width[i],height[i],Mathf.Sin(angle)*depth[i]));uv.Add(new Vector2((float)j/slices,(float)i/(height.Length-1)));
            }
            for(int i=0;i<height.Length-1;i++)for(int j=0;j<slices;j++){int a=i*(slices+1)+j,b=a+slices+1;tri.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
