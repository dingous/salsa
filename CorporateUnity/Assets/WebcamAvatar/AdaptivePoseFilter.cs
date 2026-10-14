using UnityEngine;

namespace WebcamAvatar
{
    /// <summary>One Euro filtering in meters, reset after tracking loss.</summary>
    public sealed class AdaptivePoseFilter
    {
        readonly Vector3[] value=new Vector3[33],velocity=new Vector3[33];
        readonly bool[] initialized=new bool[33];
        float previous=-10;
        static float Alpha(float cutoff,float dt)=>1/(1+1/(2*Mathf.PI*cutoff*dt));
        public void Reset(){System.Array.Clear(initialized,0,initialized.Length);previous=-10;}
        public Point[] Process(Point[] points,float now)
        {
            if(points==null || points.Length!=33){Reset();return points;}
            float elapsed=now-previous;
            if(elapsed>.4f)Reset();
            float dt=Mathf.Clamp(elapsed,1f/120,.15f);previous=now;
            var result=new Point[33];
            for(int i=0;i<33;i++) {
                var point=points[i];Vector3 raw=new Vector3(point.x,point.y,point.z);
                if(point.confidence<.55f){initialized[i]=false;result[i]=point;continue;}
                if(!initialized[i]){value[i]=raw;velocity[i]=Vector3.zero;initialized[i]=true;}
                else {
                    raw=value[i]+Vector3.ClampMagnitude(raw-value[i],3*dt+.025f);
                    velocity[i]=Vector3.Lerp(velocity[i],(raw-value[i])/dt,Alpha(1,dt));
                    value[i]=Vector3.Lerp(value[i],raw,Alpha(1.2f+velocity[i].magnitude*.65f,dt));
                }
                result[i]=new Point{x=value[i].x,y=value[i].y,z=value[i].z,confidence=point.confidence};
            }
            return result;
        }
    }
}
