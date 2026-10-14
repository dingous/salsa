const vision=import('./vendor/mediapipe/package/vision_bundle.mjs');

let face,pose,hands;
const point=(p,confidence=1)=>({x:p.x,y:p.y,z:p.z,confidence});
self.onmessage=async({data})=>{
  try {
    if(data.type==='init') {
      const {FaceLandmarker,PoseLandmarker,HandLandmarker,FilesetResolver}=await vision;
      const files=await FilesetResolver.forVisionTasks(new URL('vendor/mediapipe/package/wasm/',self.location.href).href);
      const options=name=>({baseOptions:{modelAssetPath:new URL('assets/'+name+'.task',self.location.href).href,delegate:'CPU'},runningMode:'VIDEO'});
      face=await FaceLandmarker.createFromOptions(files,{...options('face'),numFaces:1,outputFaceBlendshapes:true,outputFacialTransformationMatrixes:true});
      const quality=data.poseQuality==='heavy'?'heavy':'full';
      pose=await PoseLandmarker.createFromOptions(files,{...options('pose_'+quality),numPoses:1,minPoseDetectionConfidence:.6,minPosePresenceConfidence:.6,minTrackingConfidence:.6});
      hands=await HandLandmarker.createFromOptions(files,{...options('hand'),numHands:2});
      postMessage({type:'ready'});
    }else if(data.type==='frame') {
      try {
        const f=face.detectForVideo(data.image,data.time),p=pose.detectForVideo(data.image,data.time),h=hands.detectForVideo(data.image,data.time);
        const imagePose=p.landmarks?.[0]||[],frame={face:!!f.faceLandmarks?.length,pose:!!p.worldLandmarks?.length,shapes:[],points:[],imagePose:[],rotation:[],leftHand:[],rightHand:[]};
        frame.shapes=(f.faceBlendshapes?.[0]?.categories||[]).map(s=>({name:s.categoryName,value:s.score}));
        const m=f.facialTransformationMatrixes?.[0]?.data;
        if(m)frame.rotation=[m[0],m[4],m[8],m[1],m[5],m[9],m[2],m[6],m[10]];
        frame.imagePose=imagePose.map(p=>point(p,p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1?p.visibility??0:0));
        frame.points=(p.worldLandmarks?.[0]||[]).map((p,i)=>point(p,imagePose[i]?.x>=0&&imagePose[i]?.x<=1&&imagePose[i]?.y>=0&&imagePose[i]?.y<=1?p.visibility??0:0));
        for(let i=0;i<(h.landmarks?.length||0);i++) {
          const wrist=h.landmarks[i][0],l=imagePose[15],r=imagePose[16],category=h.handedness[i][0];
          // Unmirrored input: MediaPipe's selfie handedness is reversed.
          let side=category.categoryName==='Left'?'rightHand':'leftHand';
          if(l?.visibility>.5&&r?.visibility>.5)side=(wrist.x-l.x)**2+(wrist.y-l.y)**2<(wrist.x-r.x)**2+(wrist.y-r.y)**2?'leftHand':'rightHand';
          frame[side]=h.worldLandmarks[i].map((p,j)=>point(p,h.landmarks[i][j].x>=0&&h.landmarks[i][j].x<=1&&h.landmarks[i][j].y>=0&&h.landmarks[i][j].y<=1?category.score:0));
        }
        postMessage({type:'result',frame});
      }finally{data.image.close();}
    }
  }catch(error){postMessage({type:'error',error:error.message});}
};
