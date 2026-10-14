using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace WebcamAvatar
{
    public class VirtualCameraOutput : MonoBehaviour
    {
        public RenderTexture source;
        public Camera sourceCamera;
        public bool Sending {get;private set;}
        public string Error {get;private set;}
        Thread worker;
        TcpClient client;
        readonly object gate=new object();
        readonly AutoResetEvent ready=new AutoResetEvent(false);
        byte[] pending;
        volatile bool running;
        Texture2D readback;
        Coroutine capture;
        public void Begin()
        {
            if(running) return;
            if(source==null) throw new InvalidOperationException("Prévia indisponível.");
            Error=null; running=true;
            readback=new Texture2D(source.width,source.height,TextureFormat.RGB24,false);
            worker=new Thread(SendLoop) {IsBackground=true,Name="Avatar virtual camera"}; worker.Start();
            capture=StartCoroutine(Capture());
        }
        IEnumerator Capture()
        {
            float next=0;
            while(running) {
                yield return null;
                if(Time.unscaledTime<next || !Sending) continue;
                next=Time.unscaledTime+1f/30;
                var previous=RenderTexture.active;
                var target=sourceCamera!=null?sourceCamera.targetTexture:null;
                RenderTexture encoded=null;
                try {
                    // Explicit isolated render also works when the native window is hidden/minimized.
                    if(sourceCamera!=null){sourceCamera.targetTexture=source;sourceCamera.Render();}
                    if(QualitySettings.activeColorSpace==ColorSpace.Linear && !source.sRGB) {
                        // JPEG/virtual cameras expect sRGB bytes, whereas the studio render target is linear.
                        encoded=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                        bool write=GL.sRGBWrite;
                        try {GL.sRGBWrite=true;Graphics.Blit(source,encoded);}finally {GL.sRGBWrite=write;}
                    }
                    RenderTexture.active=encoded!=null?encoded:source; readback.ReadPixels(new Rect(0,0,source.width,source.height),0,0,false); readback.Apply(false);
                    byte[] jpg=readback.EncodeToJPG(85);
                    lock(gate) pending=jpg;
                    ready.Set();
                } finally {if(sourceCamera!=null)sourceCamera.targetTexture=target;RenderTexture.active=previous;if(encoded!=null)RenderTexture.ReleaseTemporary(encoded);}
            }
        }
        void SendLoop()
        {
            try {
                for(int attempt=0;attempt<30 && running;attempt++) {
                    try {client=new TcpClient();client.Connect("127.0.0.1",5060); break;}
                    catch(SocketException) {client?.Close();client=null;Thread.Sleep(100);}
                }
                if(!running) return;
                if(client==null) throw new SocketException();
                client.NoDelay=true;client.SendTimeout=1500; Sending=true;
                using(var stream=client.GetStream()) while(running) {
                    ready.WaitOne(100);
                    byte[] bytes;lock(gate) {bytes=pending;pending=null;}
                    if(bytes==null) continue;
                    int n=bytes.Length; byte[] header={(byte)(n>>24),(byte)(n>>16),(byte)(n>>8),(byte)n};
                    stream.Write(header,0,4);stream.Write(bytes,0,n);
                }
            } catch(Exception) {if(running) Error="A saída virtual foi desconectada. Pare a transmissão e tente novamente.";}
            finally {Sending=false;client?.Close();}
        }
        public void Stop()
        {
            running=false;ready.Set();client?.Close();
            if(capture!=null) {StopCoroutine(capture);capture=null;}
            worker?.Join(400);worker=null;
            lock(gate) pending=null;
            if(readback!=null) {Destroy(readback);readback=null;}
            Sending=false;
        }
        void OnDestroy() {Stop();ready.Dispose();}
    }
}
