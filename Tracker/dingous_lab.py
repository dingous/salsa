"""Dingous motion laboratory: constrained local AI plans, reproducible playback."""
import argparse,json,math,os,socket,time,urllib.request,urllib.error
from pathlib import Path
from local_ai import ensure_local_ai,model_name
ACTIONS={'neutral','smile','surprise','wink_left','wink_right','nod','wave_left','wave_right','raise_arms','open_hands','close_hands'}
DEMO={'version':1,'steps':[{'action':a,'duration':2.0,'intensity':.8} for a in ('smile','wink_left','wink_right','nod','wave_left','wave_right','close_hands','open_hands','neutral')]}
def validate(plan):
    if not isinstance(plan,dict) or type(plan.get('version')) is not int or plan.get('version')!=1: raise ValueError('Plano de IA inválido: versão.')
    steps=plan.get('steps')
    if not isinstance(steps,list) or not 1<=len(steps)<=12: raise ValueError('Use de 1 a 12 ações.')
    clean=[]
    for step in steps:
        if not isinstance(step,dict) or not isinstance(step.get('action'),str) or step.get('action') not in ACTIONS: raise ValueError('Ação de IA não suportada.')
        d=step.get('duration',2);v=step.get('intensity',.8)
        if isinstance(d,bool) or isinstance(v,bool) or not isinstance(d,(int,float)) or not isinstance(v,(int,float)) or not math.isfinite(d) or not math.isfinite(v):raise ValueError('Valores numéricos inválidos.')
        clean.append(dict(action=step['action'],duration=max(.5,min(5,float(d))),intensity=max(0,min(1,float(v)))))
    return dict(version=1,steps=clean)
def generate(prompt):
    if not isinstance(prompt,str) or not 1<=len(prompt.strip())<=1000: raise ValueError('Descreva o teste em até 1000 caracteres.')
    instruction='Generate only a JSON avatar animation plan with version:1 and steps:[{action,duration,intensity}]. Up to 12 steps, duration 0.5 to 5 seconds, intensity 0 to 1. Allowed actions: '+','.join(sorted(ACTIONS))+'. User request: '+prompt
    body=json.dumps(dict(model=model_name(),prompt=instruction,stream=False,format='json',think=False,keep_alive=0,options=dict(num_ctx=2048,num_predict=768,temperature=.2))).encode()
    request=urllib.request.Request('http://127.0.0.1:11434/api/generate',data=body,headers={'Content-Type':'application/json'})
    try:
        with urllib.request.urlopen(request,timeout=90) as response: result=json.loads(response.read(256000))
    except (OSError,urllib.error.URLError) as e: raise RuntimeError('IA local indisponível. Instale/inicie Ollama e baixe o modelo qwen3.5:4b, ou configure DINGOUS_AI_MODEL.') from e
    return validate(json.loads(result.get('response','{}')))
def point(x,y,z=0):return dict(x=x,y=y,z=z,confidence=1)
def frame(action,phase,intensity):
    # MediaPipe coordinates, anatomical left/right; model/mirror mapping stays in the rig.
    p=[point(0,0) for _ in range(33)];p[0]=point(0,-.8)
    for s,e,w,h,k,a,side in ((11,13,15,23,25,27,1),(12,14,16,24,26,28,-1)):
        p[s]=point(side*.2,-.5);p[e]=point(side*.32,-.20,-.08);p[w]=point(side*.3,.02,-.18);p[h]=point(side*.16,0);p[k]=point(side*.16,.4);p[a]=point(side*.16,.8)
    pulse=math.sin(math.pi*phase)**2*intensity
    if action=='raise_arms' or action.startswith('wave_'):
        for s,e,w,side in ((11,13,15,1),(12,14,16,-1)):
            if action=='raise_arms' or (action=='wave_left' and side==1) or (action=='wave_right' and side==-1):
                p[e]=point(side*(.32+.12*pulse),-.20-.40*pulse,-.08);p[w]=point(side*(.3+.12*pulse+.04*math.sin(phase*math.pi*6)*pulse),.02-.85*pulse,-.18)
    shapes=[]
    for name in {'smile':['mouthSmileLeft','mouthSmileRight'],'surprise':['browInnerUp','eyeWideLeft','eyeWideRight','jawOpen'],'wink_left':['eyeBlinkLeft'],'wink_right':['eyeBlinkRight']}.get(action,[]):shapes.append(dict(name=name,value=pulse))
    angle=.25*math.sin(phase*math.pi*4)*pulse if action=='nod' else 0
    hands=[]
    for side in (1,-1):
        hand=[point(0,0)];curl=pulse if action=='close_hands' else 0
        for f in range(5):
            for j in range(4): hand.append(point(side*(f-2)*.018,-.02*(j+1)*(1-curl),-.018*j*curl))
        hands.append(hand)
    return dict(source='agent',face=True,pose=True,shapes=shapes,points=p,imagePose=[],rotation=[1,0,0,0,math.cos(angle),-math.sin(angle),0,math.sin(angle),math.cos(angle)],leftHand=hands[0],rightHand=hands[1])
def frames(plan,fps=30):
    for step in validate(plan)['steps']:
        count=max(2,round(step['duration']*fps))
        for i in range(count):yield frame(step['action'],i/(count-1),step['intensity'])
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--generate-only',action='store_true');parser.add_argument('--plan');parser.add_argument('--prompt',default='');parser.add_argument('--demo',action='store_true');parser.add_argument('--port',type=int,default=5059);parser.add_argument('--output',required=True);args=parser.parse_args()
    if not args.demo and not args.plan:ensure_local_ai()
    plan=validate(json.loads(Path(args.plan).read_text(encoding='utf-8'))) if args.plan else validate(DEMO) if args.demo else generate(args.prompt)
    Path(args.output).parent.mkdir(parents=True,exist_ok=True);Path(args.output).write_text(json.dumps(plan,ensure_ascii=False,indent=2),encoding='utf-8')
    print('READY|Demonstração programada' if args.demo else 'READY|Sequência gerada por IA local',flush=True)
    if args.generate_only:return
    with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as udp:
        start=time.monotonic()
        for i,packet in enumerate(frames(plan)):
            udp.sendto(json.dumps(packet,allow_nan=False).encode(),('127.0.0.1',args.port));time.sleep(max(0,start+(i+1)/30-time.monotonic()))
if __name__=='__main__':main()
