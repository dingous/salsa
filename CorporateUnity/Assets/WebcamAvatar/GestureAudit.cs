using System;
using System.IO;
using UnityEngine;

namespace WebcamAvatar
{
    [Serializable] public sealed class GestureAudit
    {
        public int samples,poseSamples,leftHandSamples,rightHandSamples;
        public float[] headDistance={99,99},avatarHeadDistance={99,99};
        public float[] armMin={99,99},armMax={-99,-99};
        public float[] imageArmMin={99,99},imageArmMax={-99,-99},avatarArmMin={99,99},avatarArmMax={-99,-99};
        public float[] shoulderSpanMin={99,99},shoulderSpanMax={0,0};
        public int[] lowConfidenceWrists={0,0},outOfFrameWrists={0,0},aboveShoulderImageSamples={0,0};
        int lastSequence=-1;
        public float[] curlMin={180,180},curlMax={0,0},avatarCurlMin={180,180},avatarCurlMax={0,0};
        static bool Valid(Point[] p,int i)=>p!=null&&i<p.Length&&p[i]!=null&&p[i].confidence>=.55f;
        static Vector3 Position(Point p)=>new Vector3(p.x,p.y,p.z);
        public void Sample(StudioUI ui)
        {
            if(lastSequence==ui.tracking.FrameSequence)return;
            lastSequence=ui.tracking.FrameSequence;
            samples++;var p=ui.tracking.PosePoints;var image=ui.tracking.ImagePosePoints;var rig=ui.Vrm.Instance.Runtime.ControlRig;
            if(p!=null)poseSamples++;
            for(int side=0;side<2;side++) {
                bool left=side==0;int source=ui.tracking.mirror?1-side:side,s=source==0?11:12,w=source==0?15:16;
                var handBone=rig.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
                var headBone=rig.GetBoneTransform(HumanBodyBones.Head);
                if(p!=null&&!Valid(p,w))lowConfidenceWrists[side]++;
                if(image!=null&&image[w]!=null&&(image[w].x<0||image[w].x>1||image[w].y<0||image[w].y>1))outOfFrameWrists[side]++;
                if(Valid(image,s)&&Valid(image,w)){
                    float height=image[s].y-image[w].y;imageArmMin[side]=Mathf.Min(imageArmMin[side],height);imageArmMax[side]=Mathf.Max(imageArmMax[side],height);
                    if(height>.05f)aboveShoulderImageSamples[side]++;
                }
                if(Valid(p,w)&&Valid(p,0)){
                    float distance=Vector3.Distance(Position(p[w]),Position(p[0]));
                    headDistance[side]=Mathf.Min(headDistance[side],distance);
                    if(distance<.23f)avatarHeadDistance[side]=Mathf.Min(avatarHeadDistance[side],Vector3.Distance(handBone.position,headBone.position));
                }
                if(Valid(p,s)&&Valid(p,w)){
                    float height=p[s].y-p[w].y;armMin[side]=Mathf.Min(armMin[side],height);armMax[side]=Mathf.Max(armMax[side],height);
                    var upperBone=rig.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                    float modelHeight=handBone.position.y-upperBone.position.y;avatarArmMin[side]=Mathf.Min(avatarArmMin[side],modelHeight);avatarArmMax[side]=Mathf.Max(avatarArmMax[side],modelHeight);
                    if(Valid(p,11)&&Valid(p,12)){float span=Vector3.Distance(Position(p[11]),Position(p[12]));shoulderSpanMin[side]=Mathf.Min(shoulderSpanMin[side],span);shoulderSpanMax[side]=Mathf.Max(shoulderSpanMax[side],span);}
                }
                var hand=ui.tracking.GetHand(left);if(hand==null)continue;
                if(left)leftHandSamples++;else rightHandSamples++;
                float curl=0,modelCurl=0;int count=0;
                for(int f=1;f<5;f++){
                    int i=1+f*4;if(!Valid(hand,i)||!Valid(hand,i+1)||!Valid(hand,i+2))continue;
                    Vector3 a=Position(hand[i+1])-Position(hand[i]),b=Position(hand[i+2])-Position(hand[i+1]);
                    if(a.sqrMagnitude<1e-8f||b.sqrMagnitude<1e-8f)continue;
                    var first=(HumanBodyBones)((int)(left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal)+(f-1)*3);
                    Transform boneA=rig.GetBoneTransform(first),boneB=rig.GetBoneTransform((HumanBodyBones)((int)first+1)),boneC=rig.GetBoneTransform((HumanBodyBones)((int)first+2));
                    if(boneA==null||boneB==null||boneC==null)continue;
                    curl+=Vector3.Angle(a,b);modelCurl+=Vector3.Angle(boneB.position-boneA.position,boneC.position-boneB.position);count++;
                }
                if(count>0){curl/=count;modelCurl/=count;curlMin[side]=Mathf.Min(curlMin[side],curl);curlMax[side]=Mathf.Max(curlMax[side],curl);avatarCurlMin[side]=Mathf.Min(avatarCurlMin[side],modelCurl);avatarCurlMax[side]=Mathf.Max(avatarCurlMax[side],modelCurl);}
            }
        }
        public bool Passed=>poseSamples>30&&leftHandSamples>20&&rightHandSamples>20&&SidePassed(0)&&SidePassed(1);
        bool SidePassed(int i)=>headDistance[i]<.23f&&avatarHeadDistance[i]<.3f&&armMax[i]-armMin[i]>.25f&&curlMax[i]-curlMin[i]>20&&avatarCurlMax[i]-avatarCurlMin[i]>15;
        public void Save(string path)=>File.WriteAllText(path,JsonUtility.ToJson(this,true));
    }
}
