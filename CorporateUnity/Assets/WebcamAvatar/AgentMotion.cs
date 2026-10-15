using System;
using UnityEngine;
namespace WebcamAvatar
{
    [DefaultExecutionOrder(-100)]
    public sealed class AgentMotion : MonoBehaviour
    {
        public AvatarTracking tracking;string action="nod";float started=-20;
        public void Respond(string question,string answer){
            if(!tracking.AgentMode || string.IsNullOrWhiteSpace(answer))return;
            string text=(question??"").ToLowerInvariant();
            bool raise=(text.Contains("levante")||text.Contains("erga"))&&(text.Contains("braço")||text.Contains("braco"));
            action=text.Contains("acen")?"wave":text.Contains("pisq")?"wink":text.Contains("sorr")?"smile":raise?"raise":"nod";
            started=Time.unscaledTime;
        }
        static Point P(float x,float y,float z=0)=>new Point{x=x,y=y,z=z,confidence=1};
        void Update(){
            float age=Time.unscaledTime-started;if(!tracking.AgentMode||age>3)return;
            float phase=Mathf.Clamp01(age/3),pulse=Mathf.Sin(phase*Mathf.PI);pulse*=pulse;
            var points=new Point[33];for(int i=0;i<33;i++)points[i]=P(0,0);points[0]=P(0,-.8f);
            foreach(int side in new[]{0,1}){
                float sign=side==0?1:-1;int shoulder=11+side,elbow=13+side,wrist=15+side;
                points[shoulder]=P(sign*.2f,-.5f);points[elbow]=P(sign*.32f,-.2f,-.08f);points[wrist]=P(sign*.3f,.02f,-.18f);
                points[23+side]=P(sign*.16f,0);points[25+side]=P(sign*.16f,.4f);points[27+side]=P(sign*.16f,.8f);
                if(action=="raise" || action=="wave"&&side==1){points[elbow]=P(sign*(.32f+.12f*pulse),-.2f-.4f*pulse,-.08f);points[wrist]=P(sign*(.3f+.12f*pulse+.04f*Mathf.Sin(age*7)*pulse),.02f-.85f*pulse,-.18f);}
            }
            float angle=action=="nod"?.15f*Mathf.Sin(age*5)*pulse:0;
            var shapes=action=="wink"?new[]{new Shape{name="eyeBlinkLeft",value=pulse}}:new[]{new Shape{name="mouthSmileLeft",value=.3f*pulse},new Shape{name="mouthSmileRight",value=.3f*pulse}};
            tracking.ApplyAgentFrame(new TrackingFrame{source="agent",face=true,pose=true,points=points,shapes=shapes,rotation=new[]{1f,0,0,0,Mathf.Cos(angle),-Mathf.Sin(angle),0,Mathf.Sin(angle),Mathf.Cos(angle)}});
        }
    }
}
