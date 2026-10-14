using UnityEngine;

namespace WebcamAvatar
{
    /// <summary>One Euro filtering in meters, reset after tracking loss.</summary>
    public sealed class AdaptivePoseFilter
    {
        readonly Vector3[] value,velocity,rawPrevious;
        readonly bool[] initialized;
        readonly float minCutoff,beta;
        Point[] lastResult;float lastValid=-10;
        public AdaptivePoseFilter(int count=33,float cutoff=1.2f,float speedGain=.65f){value=new Vector3[count];velocity=new Vector3[count];rawPrevious=new Vector3[count];initialized=new bool[count];minCutoff=cutoff;beta=speedGain;}
        float previous=-10;
        static float Alpha(float cutoff,float dt)=>1/(1+1/(2*Mathf.PI*cutoff*dt));
        public void Reset(){System.Array.Clear(initialized,0,initialized.Length);previous=-10;lastResult=null;lastValid=-10;}
        public Point[] Process(Point[] points,float now)
        {
            if(points==null || points.Length!=value.Length){if(now-lastValid<.15f)return lastResult;Reset();return points;}
            float elapsed=now-previous;
            if(elapsed>.4f)Reset();
            float dt=Mathf.Clamp(elapsed,1f/120,.15f);previous=now;
            var result=new Point[value.Length];
            lastValid=now;
            for(int i=0;i<value.Length;i++) {
                var point=points[i];Vector3 raw=new Vector3(point.x,point.y,point.z);
                if(point.confidence<.55f){initialized[i]=false;result[i]=point;continue;}
                if(!initialized[i]){value[i]=raw;rawPrevious[i]=raw;velocity[i]=Vector3.zero;initialized[i]=true;}
                else {
                    raw=value[i]+Vector3.ClampMagnitude(raw-value[i],3*dt+.025f);
                    velocity[i]=Vector3.Lerp(velocity[i],(raw-rawPrevious[i])/dt,Alpha(1,dt));
                    rawPrevious[i]=raw;
                    value[i]=Vector3.Lerp(value[i],raw,Alpha(minCutoff+velocity[i].magnitude*beta,dt));
                }
                result[i]=new Point{x=value[i].x,y=value[i].y,z=value[i].z,confidence=point.confidence};
            }
            lastResult=result;return result;
        }
    }
}
