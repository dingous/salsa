using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;
namespace WebcamAvatar {
    public partial class StudioUI {
        void ShowFramework(int option){
            if(advancedPanel!=null){Destroy(advancedPanel);advancedPanel=null;}
            if(presetPanel!=null){Destroy(presetPanel);presetPanel=null;}
            bool returnToAgent=AgentFullScreen;if(presentation)SetPresentation(false);
            advancedPanel=Panel("Framework de avatar",responsive.Content,116,256,370,558,panel).gameObject;
            Label(option==4?"CORPORATIVO 4 · METAHUMAN":option==3?"CORPORATIVO 3 · AVATURN":"CORPORATIVO 2",advancedPanel.transform,24,22,322,32,18,white,FontStyle.Bold);
            if(option==2){
                Label("Real-Time Full Body Tracking",advancedPanel.transform,24,68,322,54,19,white);
                Label("Pacote do fornecedor não instalado. O GitHub contém somente documentação. Os modelos atuais continuam disponíveis.",advancedPanel.transform,24,138,322,100,15,muted);
                Button("Abrir página do fornecedor",advancedPanel.transform,24,266,322,44,()=>Application.OpenURL("https://assetstore.unity.com/packages/tools/ai-ml-integration/real-time-full-body-tracking-system-350552"));
            }else if(option==4){
                Label("Importe um MetaHuman convertido para VRM, com corpo Humanoid, dedos e blendshapes ARKit. O personagem MetaHuman não está incluído.",advancedPanel.transform,24,68,322,92,14,muted);
                Button("Guia de exportação",advancedPanel.transform,24,174,156,38,()=>Application.OpenURL("https://dev.epicgames.com/documentation/metahuman/metahuman-creator-export-tool-in-unreal-engine"));
                Button("Analisar DNA (.dna)",advancedPanel.transform,190,174,156,38,()=>{if(busy)return;try{var path=PhotoPicker.Pick(dna:true);if(path!=null)StartCoroutine(InspectMetaHumanDna(path));}catch(Exception e){SetStatus(e.Message,true);}});
                Button("Importar MetaHuman preparado .vrm",advancedPanel.transform,24,224,322,40,()=>{if(busy)return;try{var path=PhotoPicker.Pick(true);if(path!=null)StartCoroutine(ImportMetaHumanFile(path));}catch(Exception e){SetStatus(e.Message+" Use o link VRM abaixo no Android.",true);}},true);
                var download=Input("Link HTTPS do MetaHuman preparado .vrm",advancedPanel.transform,24,278,322,38);
                Button("Carregar MetaHuman pelo link",advancedPanel.transform,24,326,322,38,()=>{if(!busy)StartCoroutine(DownloadAvaturn(download.text,true));});
                Label("Windows: o DNA pode ser analisado pelo OpenRigLogic local. Isso não carrega a aparência. Para renderizar, use VRM preparado com malha, materiais e expressões vinculadas.",advancedPanel.transform,24,382,322,104,13,muted);
            }else{
                Label("Criação online: fotos processadas pelo Avaturn.\nEscolha T2 para olhos e boca animados.\nA demonstração T1 tem rosto estático.",advancedPanel.transform,24,64,322,72,14,muted);
                Button("Usar demonstração Avaturn · T1",advancedPanel.transform,24,138,322,32,()=>{
                    if(busy)return;avatar.Profile.presetId=null;avatar.Profile.style=1;avatar.Profile.vrmColors=false;
                    string demoPath=Path.Combine(Application.streamingAssetsPath,"Avatars","AvaturnBase.glb");
                    StartCoroutine(LoadVrmRoutine(demoPath));
                });
                var creator=Input("URL do projeto Avaturn",advancedPanel.transform,24,178,322,32);creator.text=PlayerPrefs.GetString("DingousAvaturnCreator","https://demo.avaturn.dev");
                Button("Criar avatar no Avaturn ↗",advancedPanel.transform,24,220,322,34,()=>{
                    if(!Uri.TryCreate(creator.text.Trim(),UriKind.Absolute,out var url)||url.Scheme!="https"||!url.Host.EndsWith(".avaturn.dev",StringComparison.OrdinalIgnoreCase)||!string.IsNullOrEmpty(url.UserInfo)){SetStatus("Use a URL HTTPS do projeto em avaturn.dev.",true);return;}
                    PlayerPrefs.SetString("DingousAvaturnCreator",url.AbsoluteUri);PlayerPrefs.Save();Application.OpenURL(url.AbsoluteUri);SetStatus("Criador online aberto. Exporte seu GLB e importe aqui. O projeto demo tem limitações.");
                },true);
                Button("Importar Avaturn .glb / .vrm",advancedPanel.transform,24,264,322,34,()=>{if(busy)return;try{var path=PhotoPicker.Pick(glb:true);if(path!=null)StartCoroutine(ImportAvaturnFile(path));}catch(Exception e){SetStatus(e.Message+" Use o link GLB abaixo no Android.",true);}});
                var download=Input("Cole o link HTTPS do GLB exportado",advancedPanel.transform,24,306,322,38);
                Button("Carregar GLB pelo link",advancedPanel.transform,24,354,322,38,()=>{if(!busy)StartCoroutine(DownloadAvaturn(download.text));});
                Label("Use um avatar animável para olhos e boca. Modelos sem blendshapes terão somente corpo e mãos. Roupa/cabelo são escolhidos no Avaturn.",advancedPanel.transform,24,408,322,74,13,muted);
            }
            Button("Voltar",advancedPanel.transform,24,508,322,34,()=>{Destroy(advancedPanel);advancedPanel=null;if(returnToAgent)SetPresentation(true);});responsive.LayoutAdvanced();
        }
        public IEnumerator ImportAvaturnFile(string path)=>ImportFrameworkFile(path,false);
        [Serializable] class MetaHumanDnaReport {public int rawControls,joints,meshes,vertices;public bool hasGeometry;}
        public IEnumerator InspectMetaHumanDna(string path){
            if(busy)yield break;
            busy=true;string message="O analisador não retornou um resultado.",error=null;LocalJob job=null;
            try{job=new LocalJob("metahuman_dna.py","--file",path);}catch(Exception e){error=e.Message;}
            if(job!=null){using(job){
                float deadline=Time.realtimeSinceStartup+30;
                while(!job.Finished&&Time.realtimeSinceStartup<deadline)yield return null;
                if(job.Finished)job.Process.WaitForExit(); // Flush redirected output before parsing the completed job.
                if(!job.Finished||job.ExitCode!=0)error=job.FriendlyError("Falha ao analisar DNA localmente.");
                else while(job.Output.TryDequeue(out var line))if(line.StartsWith("METAHUMAN_DNA|")){
                    try{var data=JsonUtility.FromJson<MetaHumanDnaReport>(line.Substring(14));message="OpenRigLogic: "+data.rawControls+" controles, "+data.joints+" ossos, "+data.vertices+" vértices. "+(data.hasGeometry?"Dados geométricos presentes; prepare o VRM para renderizar.":"Sem geometria: este DNA não contém um personagem renderizável.");}catch(Exception e){error=e.Message;}
                }
            }}
            busy=false;SetStatus(error??message,error!=null);
        }
        public IEnumerator ImportMetaHumanFile(string path)=>ImportFrameworkFile(path,true);
        IEnumerator ImportFrameworkFile(string path,bool metaHuman){
            if(busy)yield break;
            string cached=null,error=null;
            try{
                var source=new FileInfo(path);if(!source.Exists||source.Length>150*1024*1024)throw new IOException("Arquivo ausente ou acima de 150 MB.");
                string extension=source.Extension.ToLowerInvariant();if((metaHuman&&extension!=".vrm")||(!metaHuman&&extension!=".glb"&&extension!=".vrm"))throw new IOException(metaHuman?"Use um MetaHuman convertido para VRM. DNA/FBX bruto não são suportados.":"Use GLB ou VRM exportado do Avaturn.");
                using(var hash=SHA256.Create())using(var input=source.OpenRead()){string id=BitConverter.ToString(hash.ComputeHash(input)).Replace("-","").ToLowerInvariant();string folder=Path.Combine(Application.persistentDataPath,metaHuman?"MetaHuman":"Avaturn");Directory.CreateDirectory(folder);cached=Path.Combine(folder,id+extension);if(!string.Equals(source.FullName,cached,StringComparison.OrdinalIgnoreCase))File.Copy(source.FullName,cached,true);}
            }catch(Exception e){error=e.Message;}
            if(error!=null){SetStatus(error,true);yield break;}
            yield return LoadVrmRoutine(cached);
            if(Vrm.Active&&Vrm.SourcePath==cached){avatar.Profile.presetId=null;avatar.Profile.style=1;avatar.Profile.vrmColors=false;Vrm.ApplyAppearance();SetVrmPresentation(true);SyncAppearance();RefreshModelStatus();SetStatus(metaHuman?(Vrm.HasFacialBindings?"Corporativo 4 · MetaHuman preparado ativo. Salve para guardar.":"MetaHuman preparado carregado: corpo e mãos. Sem blendshapes, não há expressões faciais."):!Vrm.HasFacialBindings?"Avaturn carregado: corpo e mãos. Este GLB não possui expressões faciais; exporte um avatar animável.":"Corporativo 3 · Avaturn ativo: mesmo rig nos modos Webcam e Agente. Salve para guardar.");}
        }
        IEnumerator DownloadAvaturn(string value,bool metaHuman=false){
            if(!Uri.TryCreate(value.Trim(),UriKind.Absolute,out var url)||url.Scheme!="https"||!string.IsNullOrEmpty(url.UserInfo)){SetStatus(metaHuman?"Informe o link HTTPS do MetaHuman preparado .vrm.":"Informe o link HTTPS do GLB exportado.",true);yield break;}
            busy=true;SetStatus(metaHuman?"Baixando MetaHuman preparado…":"Baixando seu avatar Avaturn…");string path=Path.Combine(Application.temporaryCachePath,metaHuman?"metahuman-download.vrm":"avaturn-download.glb");
            using(var request=UnityWebRequest.Get(url.AbsoluteUri)){
                request.timeout=90;var operation=request.SendWebRequest();while(!operation.isDone){if(request.downloadedBytes>150UL*1024*1024){request.Abort();break;}yield return null;}
                busy=false;if(request.result!=UnityWebRequest.Result.Success||request.downloadedBytes>150UL*1024*1024){SetStatus("Falha ao baixar avatar ou limite de 150 MB excedido.",true);yield break;}
                string error=null;try{File.WriteAllBytes(path,request.downloadHandler.data);}catch(Exception e){error=e.Message;}if(error!=null){SetStatus(error,true);yield break;}
            }
            yield return ImportFrameworkFile(path,metaHuman);
        }
    }
}
