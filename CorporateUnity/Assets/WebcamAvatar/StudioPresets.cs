using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace WebcamAvatar {
    public partial class StudioUI {
        GameObject presetPanel;RectTransform presetItems;bool presetReturnToAgent;string presetFilter="";
        void ShowPresets(){
            if(busy){SetStatus("Aguarde o carregamento do avatar.");return;}
            if(presetPanel!=null)return;
            presetReturnToAgent=AgentFullScreen;if(presentation)SetPresentation(false);
            presetPanel=Panel("Presets de avatares",responsive.Content,116,256,370,558,panel).gameObject;
            Label("GALERIA · 12 AVATARES",presetPanel.transform,24,22,322,28,19,white,FontStyle.Bold);
            Label("Escolha um preset e veja a prévia ao lado.",presetPanel.transform,24,60,322,28,13,muted);
            string[] labels={"Todos","Masculinos","Femininos"},filters={"","masculino","feminino"};
            for(int i=0;i<labels.Length;i++){int selected=i;Button(labels[i],presetPanel.transform,24+i*110,100,102,32,()=>{presetFilter=filters[selected];RefreshPresets();});}
            var viewport=Rect("Presets viewport",presetPanel.transform,24,144,322,308);viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;
            presetItems=Rect("Presets",viewport,0,0,322,308);scroll.content=presetItems;
            Label("Cartoon ou Corporativo no seletor do topo.\nSalvar avatar guarda sua versão na biblioteca.",presetPanel.transform,24,464,322,40,12,muted);
            Button("Concluir escolha",presetPanel.transform,24,514,322,32,()=>{Destroy(presetPanel);presetPanel=null;if(presetReturnToAgent)SetPresentation(true);},true);
            RefreshPresets();responsive.LayoutAdvanced();
        }
        void RefreshPresets(){
            foreach(Transform child in presetItems)Destroy(child.gameObject);
            int row=0;foreach(var preset in AvatarPresetCatalog.Load()){
                if(presetFilter!=""&&preset.category!=presetFilter)continue;
                var selected=preset;
                var button=Button(preset.name+"\n"+preset.description,presetItems,0,row*82,322,74,()=>{if(!busy)StartCoroutine(ApplyPreset(selected.id));},avatar.Profile.presetId==preset.id);
                button.GetComponentInChildren<Text>().fontSize=14;row++;
            }
            presetItems.sizeDelta=new Vector2(322,Mathf.Max(308,row*82));presetItems.anchoredPosition=Vector2.zero;
        }
        public IEnumerator ApplyPreset(string id){
            if(busy)yield break;
            var preset=Array.Find(AvatarPresetCatalog.Load(),p=>p.id==id);
            if(preset==null){SetStatus("Preset não encontrado.",true);yield break;}
            var profile=preset.Apply(avatar.Profile);
            string path=Path.Combine(Application.streamingAssetsPath,"Avatars",preset.model);
            bool sameModel=Vrm.Active&&(Application.platform==RuntimePlatform.Android?Path.GetFileName(Vrm.SourcePath)==preset.model:string.Equals(Vrm.SourcePath,Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase));
            if(!sameModel){yield return LoadVrmRoutine(path);if(!Vrm.Active||Path.GetFileName(Vrm.SourcePath)!=preset.model)yield break;}
            profile.vrmSource=path;avatar.Rebuild(profile);Vrm.UseVrm();Vrm.ApplyAppearance();
            nameField.text=previewName.text=profile.name;SetVrmPresentation(profile.corporate);FrameVrm(false);SyncAppearance();RefreshModelStatus();
            if(presetPanel!=null)RefreshPresets();SetStatus(profile.name+" selecionado. Personalize em Aparência ou salve na biblioteca.");
        }
    }
}
