using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WebcamAvatar
{
    /// <summary>Local avatar creator, appearance editor and camera broadcast control.</summary>
    public class StudioUI : MonoBehaviour
    {
        public CartoonAvatar avatar;
        public AvatarTracking tracking;
        public Camera avatarCamera;
        public RenderTexture output;
        public Renderer backgroundRenderer;
        Font font;
        Canvas canvas;
        ResponsiveStudio responsive;
        GameObject[] pages=new GameObject[4];
        Button[] navigation=new Button[4];
        public VrmAvatarController Vrm {get;private set;}
        Text modelStatus,rigStatus;
        Text status, trackingStatus, previewName, photoCaption, cameraStatus;
        Button generateButton, saveButton, trackButton, virtualButton;
        InputField nameField;
        RawImage referenceImage, avatarImage;
        GameObject photoPlaceholder;
        Texture2D reference;
        string selectedPhoto, draft;
        bool busy, presentation;
        LocalJob generator, trackerJob, cameraJob;
        VirtualCameraOutput broadcaster;
        string[] savedFolders=Array.Empty<string>();
        Dropdown library;
        Dropdown hairDropdown;
        Dropdown styleDropdown;
        Toggle glassesToggle, animationToggle;
        int cameraIndex;
        Material backdrop;
        GameObject advancedPanel;
        bool rebuildPending;
        float lastDetailChange;
        Sprite rounded;
        Texture2D roundTexture;
        readonly List<Texture2D> uiTextures=new List<Texture2D>();
        readonly Color background=new Color(.043f,.052f,.079f), panel=new Color(.075f,.086f,.126f), field=new Color(.105f,.117f,.165f);
        readonly Color white=new Color(.93f,.94f,.99f), muted=new Color(.56f,.60f,.70f), purple=new Color(.53f,.48f,1), green=new Color(.33f,.86f,.69f);
#if UNITY_EDITOR
        public string validationStorage;
#endif
        string Storage {
            get {
#if UNITY_EDITOR
                if(!string.IsNullOrEmpty(validationStorage))return validationStorage;
#endif
                return Application.persistentDataPath;
            }
        }

        void Start()
        {
            Application.runInBackground=true; Application.targetFrameRate=60;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Directory.CreateDirectory(Path.Combine(Storage,"Avatars")); Directory.CreateDirectory(Path.Combine(Storage,"Drafts"));
            broadcaster=gameObject.AddComponent<VirtualCameraOutput>(); broadcaster.source=output;
            Vrm=gameObject.AddComponent<VrmAvatarController>();Vrm.settings=avatar;Vrm.tracking=tracking;
            MakeRoundedSprite(); Build(); RefreshLibrary();
            int savedIndex=Array.IndexOf(savedFolders,PlayerPrefs.GetString("LastAvatar",""));
            if(savedIndex>=0 && !string.IsNullOrEmpty(JsonUtility.FromJson<AvatarProfile>(File.ReadAllText(Path.Combine(savedFolders[savedIndex],"avatar.json"))).vrmSource))LoadSaved(savedIndex+1);
            else StartCoroutine(LoadVrmRoutine(VrmAvatarController.DefaultPath));
        }
        void MakeRoundedSprite()
        {
            roundTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);
            var pixels=new Color[4096];
            for(int y=0;y<64;y++) for(int x=0;x<64;x++) {
                float dx=Mathf.Max(12-x,x-51),dy=Mathf.Max(12-y,y-51);
                float dist=Mathf.Sqrt(Mathf.Max(dx,0)*Mathf.Max(dx,0)+Mathf.Max(dy,0)*Mathf.Max(dy,0));
                pixels[y*64+x]=new Color(1,1,1,Mathf.Clamp01(12.5f-dist));
            }
            roundTexture.SetPixels(pixels);roundTexture.Apply();
            rounded=Sprite.Create(roundTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(14,14,14,14));
        }
        RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color,bool round=true)
        {
            var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;
            if(round){image.sprite=rounded;image.type=Image.Type.Sliced;}return image;
        }
        Text Label(string content,Transform parent,float x,float y,float w,float h,int size,Color color,FontStyle style=FontStyle.Normal)
        {
            var text=Rect(content,parent,x,y,w,h).gameObject.AddComponent<Text>();text.text=content;text.font=font;text.fontSize=size;text.color=color;text.fontStyle=style;
            text.raycastTarget=false;text.verticalOverflow=VerticalWrapMode.Truncate;text.horizontalOverflow=HorizontalWrapMode.Wrap;return text;
        }
        Button Button(string content,Transform parent,float x,float y,float w,float h,Action action,bool primary=false)
        {
            var image=Panel(content,parent,x,y,w,h,primary?purple:field);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.14f,1.14f,1.14f);colors.pressedColor=new Color(.82f,.82f,.87f);colors.disabledColor=new Color(.45f,.45f,.5f,.8f);button.colors=colors;
            var label=Label(content,image.transform,12,0,w-24,h,14,white,FontStyle.Bold);label.alignment=TextAnchor.MiddleCenter;
            button.onClick.AddListener(()=>action());return button;
        }
        InputField Input(string placeholder,Transform parent,float x,float y,float w,float h)
        {
            var image=Panel("Campo",parent,x,y,w,h,field);image.raycastTarget=true;
            var input=image.gameObject.AddComponent<InputField>();input.targetGraphic=image;
            var text=Label("",image.transform,14,0,w-28,h,15,white);text.alignment=TextAnchor.MiddleLeft;
            var hint=Label(placeholder,image.transform,14,0,w-28,h,15,muted);hint.alignment=TextAnchor.MiddleLeft;
            input.textComponent=text;input.placeholder=hint;input.characterLimit=48;return input;
        }
        Toggle Toggle(string label,Transform parent,float x,float y,float w,bool value,Action<bool> action)
        {
            var rect=Rect(label,parent,x,y,w,32);var toggle=rect.gameObject.AddComponent<Toggle>();
            var box=Panel("Checkbox",rect,0,6,20,20,field);box.raycastTarget=true;
            var mark=Panel("Ativo",box.transform,4,4,12,12,purple);toggle.targetGraphic=box;toggle.graphic=mark;
            Label(label,rect,32,0,w-32,32,14,white).alignment=TextAnchor.MiddleLeft;
            toggle.isOn=value;toggle.onValueChanged.AddListener(v=>action(v));return toggle;
        }
        Slider Slider(string label,Transform parent,float x,float y,float w,float min,float max,float value,Action<float> action)
        {
            var rect=Rect(label,parent,x,y,w,66);Label(label,rect,0,0,w,24,14,muted);
            var sliderRect=Rect("Ajuste",rect,0,32,w,24);var slider=sliderRect.gameObject.AddComponent<Slider>();
            Panel("Trilho",sliderRect,0,9,w,5,field);
            var fillArea=Rect("Fill area",sliderRect,0,9,w,5);var fill=Panel("Fill",fillArea,0,0,w,5,purple);
            fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
            var handleArea=Rect("Handle area",sliderRect,8,0,w-16,24);
            var handle=Panel("Handle",handleArea,0,4,16,16,white);handle.raycastTarget=true;
            handle.rectTransform.pivot=new Vector2(.5f,.5f);
            handle.rectTransform.sizeDelta=new Vector2(16,-8);handle.rectTransform.anchoredPosition=Vector2.zero;
            slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=min;slider.maxValue=max;slider.value=value;
            slider.onValueChanged.AddListener(v=>action(v));return slider;
        }
        Dropdown Dropdown(string[] options,Transform parent,float x,float y,float w,float h,Action<int> action)
        {
            var image=Panel("Seleção",parent,x,y,w,h,field);image.raycastTarget=true;
            var drop=image.gameObject.AddComponent<Dropdown>();drop.targetGraphic=image;
            var caption=Label("",image.transform,14,0,w-48,h,14,white);caption.alignment=TextAnchor.MiddleLeft;
            Label("v",image.transform,w-30,0,20,h,16,muted).alignment=TextAnchor.MiddleCenter;
            var template=Panel("Template",image.transform,0,h+4,w,Mathf.Min(230,options.Length*36+8),field);
            template.rectTransform.anchorMin=template.rectTransform.anchorMax=new Vector2(0,0);template.rectTransform.anchoredPosition=new Vector2(0,-4);
            var scroll=template.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;
            var viewport=Rect("Viewport",template.transform,4,4,w-8,template.rectTransform.sizeDelta.y-8);viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("Content",viewport,0,0,w-8,options.Length*36);
            var item=Rect("Item",content,0,0,w-8,36);var itemToggle=item.gameObject.AddComponent<Toggle>();
            var itemBG=Panel("Item background",item,0,0,w-8,36,field);itemBG.raycastTarget=true;
            var check=Panel("Selected",item,2,10,4,16,purple);itemToggle.targetGraphic=itemBG;itemToggle.graphic=check;
            var itemText=Label("Option",item,14,0,w-28,36,14,white);itemText.alignment=TextAnchor.MiddleLeft;
            scroll.viewport=viewport;scroll.content=content;scroll.movementType=ScrollRect.MovementType.Clamped;
            drop.template=template.rectTransform;drop.captionText=caption;drop.itemText=itemText;
            drop.ClearOptions();drop.AddOptions(new List<string>(options));template.gameObject.SetActive(false);drop.onValueChanged.AddListener(v=>action(v));return drop;
        }
        void Build()
        {
            var root=new GameObject("Avatar Studio UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1440,900);scale.matchWidthOrHeight=.5f;
            if(FindAnyObjectByType<EventSystem>()==null)new GameObject("Event System",typeof(EventSystem),typeof(StandaloneInputModule));
            var backgroundRect=Panel("Fundo",root.transform,0,0,1440,900,background,false).rectTransform;
            backgroundRect.anchorMin=Vector2.zero;backgroundRect.anchorMax=Vector2.one;backgroundRect.offsetMin=backgroundRect.offsetMax=Vector2.zero;
            Panel("Sidebar",root.transform,0,0,84,900,new Color(.055f,.065f,.097f),false);
            var logo=Panel("Logo",root.transform,20,28,44,44,purple);Label("S",logo.transform,0,0,44,44,26,white,FontStyle.Bold).alignment=TextAnchor.MiddleCenter;
            for(int i=0;i<3;i++){int index=i;Button(new[]{"01","02","03"}[i],root.transform,20,142+i*64,44,44,()=>ShowPage(index));}
            Label("LOCAL",root.transform,16,838,60,24,10,muted).alignment=TextAnchor.MiddleCenter;
            Label("SALSA",root.transform,116,31,140,34,25,white,FontStyle.Bold);
            Label("AVATAR STUDIO",root.transform,116,68,180,20,11,muted,FontStyle.Bold);
            var local=Panel("Local badge",root.transform,1220,40,104,30,new Color(.085f,.18f,.16f));Label("100% LOCAL",local.transform,0,0,104,30,10,green,FontStyle.Bold).alignment=TextAnchor.MiddleCenter;
            for(int i=0;i<4;i++){int index=i;navigation[i]=Button(new[]{"Criar avatar","Aparência","Transmissão","Modelo VRM"}[i],root.transform,400+i*132,40,124,40,()=>ShowPage(index));}
            styleDropdown=Dropdown(new[]{"Estilo: Cartoon","Estilo: Realístico"},root.transform,982,40,220,40,i=>Modify(p=>p.style=i));
            styleDropdown.SetValueWithoutNotify(avatar.Profile.style);
            Label("Crie sua versão digital.",root.transform,116,128,1040,54,38,white,FontStyle.Bold);
            Label("Uma foto. Um personagem. Suas expressões em tempo real.",root.transform,118,192,1060,30,17,muted);
            for(int i=0;i<4;i++) pages[i]=Panel("Página "+i,root.transform,116,256,370,558,panel).gameObject;
            BuildCreator(pages[0].transform);BuildAppearance(pages[1].transform);BuildBroadcast(pages[2].transform);
            BuildVrm(pages[3].transform);
            BuildPreview(root.transform);
            status=Label("Escolha uma foto frontal para começar. A prévia já pode ser explorada.",root.transform,116,834,1180,48,14,muted);
            ShowPage(0);
            responsive=root.AddComponent<ResponsiveStudio>();responsive.Initialize(canvas);
        }
        void BuildCreator(Transform parent)
        {
            Label("01  /  FOTO DE REFERÊNCIA",parent,24,24,322,22,11,purple,FontStyle.Bold);
            Label("O começo é você.",parent,24,60,322,32,24,white,FontStyle.Bold);
            var frame=Panel("Área da foto",parent,24,108,322,192,field);
            var raw=Rect("Foto",frame.transform,4,4,314,184).gameObject.AddComponent<RawImage>();raw.color=Color.white;raw.raycastTarget=false;referenceImage=raw;raw.gameObject.SetActive(false);
            var placeholder=Label("+",frame.transform,0,24,322,54,40,purple);placeholder.alignment=TextAnchor.MiddleCenter;photoPlaceholder=placeholder.gameObject;
            photoCaption=Label("Um retrato frontal, com boa luz",frame.transform,20,96,282,50,14,muted);photoCaption.alignment=TextAnchor.MiddleCenter;
            Button("Escolher foto",parent,24,314,322,42,SelectPhoto);
            Label("PNG ou JPEG · até 25 MB · um rosto",parent,24,364,322,24,12,muted);
            nameField=Input("Nome do avatar",parent,24,400,322,44);nameField.text="Meu avatar";
            generateButton=Button("Gerar meu avatar  →",parent,24,460,322,48,Generate,true);generateButton.interactable=false;
            Label("Gerado neste computador. Sem enviar sua foto.",parent,24,520,322,24,11,muted);
        }
        void BuildVrm(Transform parent)
        {
            Label("04  /  UNIVRM 0.131.3",parent,24,24,322,24,11,purple,FontStyle.Bold);
            Label("Seu avatar profissional.",parent,24,60,322,58,24,white,FontStyle.Bold);
            modelStatus=Label("Carregando o modelo VRM…",parent,24,130,322,100,15,white);
            Button("Importar arquivo .vrm",parent,24,244,322,46,SelectVrm,true);
            Button("Usar Seed-san (demonstração)",parent,24,304,322,42,()=>{if(!busy)StartCoroutine(LoadVrmRoutine(VrmAvatarController.DefaultPath));});
            Button("Usar gerador procedural",parent,24,360,322,42,()=>{if(busy)return;Vrm.UseProcedural();avatar.Profile.vrmSource=null;RefreshModelStatus();SetStatus("Gerador procedural selecionado. Gere e personalize pela foto.");});
            Label("Importe seu modelo cartoon ou realístico. Rosto, dedos e corpo usam o rig do arquivo VRM.",parent,24,425,322,58,13,muted);
            Label("Seed-san © VirtualCast, Inc.\nVRM Public License 1.0 · crédito obrigatório",parent,24,500,322,44,11,muted);
        }
        void SelectVrm()
        {
            if(busy)return;
            try{string path=PhotoPicker.Pick(true);if(path!=null)StartCoroutine(LoadVrmRoutine(path));}
            catch(Exception e){SetStatus("Não foi possível selecionar o modelo: "+e.Message,true);}
        }
        public IEnumerator LoadVrmRoutine(string path)
        {
            if(busy)yield break;busy=true;SetStatus("Carregando malhas, texturas, expressões e rig do avatar VRM…");
            var job=Vrm.Load(path);while(!job.IsCompleted)yield return null;
            busy=false;
            if(job.IsFaulted)SetStatus("Não foi possível carregar o VRM: "+job.Exception.GetBaseException().Message,true);
            else {
                avatar.Profile.vrmSource=Path.GetFullPath(path)==Path.GetFullPath(VrmAvatarController.DefaultPath)?"builtin:Seed-san":path;
                avatar.Profile.name=Vrm.ModelName;nameField.text=previewName.text=Vrm.ModelName;
                FrameVrm(false);SetStatus("Avatar VRM ativo. Inicie o rastreamento ou explore a prévia animada.");
            }
            RefreshModelStatus();
        }
        void RefreshModelStatus()
        {
            bool active=Vrm.Active;
            modelStatus.text=active?$"{Vrm.ModelName}\n{Vrm.ExpressionCount} expressões VRM\nRig Humanoid · física de cabelo\nCréditos: {string.Join(", ",Vrm.Instance.Vrm.Meta.Authors)}":"Gerador procedural ativo";
            rigStatus.text=active?$"UNIVRM  /  {Vrm.ExpressionCount} EXPRESSÕES":"AVATAR PRO  /  RIG FACIAL 52";
            styleDropdown.interactable=!active;
            SetButtonLabel(generateButton,active?"Gerar pela foto (procedural)":"Gerar meu avatar  →");
        }
        void FrameVrm(bool full)
        {
            if(!Vrm.Active)return;
            if(full){avatarCamera.orthographicSize=1.3f;avatarCamera.transform.position=new Vector3(0,.78f,-5);}
            else if(Vrm.Instance.TryGetBoneTransform(HumanBodyBones.Head,out var head)) {
                avatarCamera.orthographicSize=.66f;avatarCamera.transform.position=new Vector3(0,head.position.y-.20f,-5);
            }
            if(backgroundRenderer!=null)backgroundRenderer.transform.localScale=new Vector3(9,6,1);
        }
        void Swatches(string label,Transform parent,float y,string[] colors,Action<string> action)
        {
            Label(label,parent,24,y,322,20,13,muted);
            for(int i=0;i<colors.Length;i++) {
                string color=colors[i];var b=Button("",parent,24+i*46,y+30,34,34,()=>action(color));b.GetComponent<Image>().color=AvatarProfile.ColorOf(color,white);
            }
        }
        void BuildAppearance(Transform parent)
        {
            Label("02  /  SEU ESTILO",parent,24,24,322,22,11,purple,FontStyle.Bold);
            Label("Faça do seu jeito.",parent,24,60,322,32,24,white,FontStyle.Bold);
            Swatches("Pele",parent,112,new[]{"#F7D2B5","#E9B38B","#D8956C","#B77750","#925A3A","#613A2B","#3C251F"},c=>Modify(p=>p.skin=c));
            Swatches("Cabelo",parent,195,new[]{"#241F29","#503328","#916249","#D7AB62","#C35438","#C9C7D9","#9B75DF"},c=>Modify(p=>p.hair=c));
            Swatches("Olhos",parent,278,new[]{"#382A24","#7C5430","#417E68","#438DB3","#747889","#AD78C6"},c=>Modify(p=>p.eyes=c));
            Swatches("Roupa",parent,361,new[]{"#7279EC","#45A496","#D17D7D","#E4B65F","#F0EEE8","#353B50"},c=>Modify(p=>p.outfit=c));
            hairDropdown=Dropdown(new[]{"Cabelo curto","Cabelo médio","Cabelo longo","Sem cabelo"},parent,24,450,210,40,i=>Modify(p=>p.hairStyle=i));
            glassesToggle=Toggle("Óculos",parent,247,454,100,false,v=>Modify(p=>p.glasses=v));
            Button("Ajustes avançados do rosto",parent,24,508,322,34,ShowAdvanced,true);
        }
        void BuildBroadcast(Transform parent)
        {
            Label("03  /  CÂMERA VIRTUAL",parent,24,24,322,22,11,purple,FontStyle.Bold);
            Label("Entre em cena.",parent,24,60,322,32,24,white,FontStyle.Bold);
            Label("Câmera de rastreamento",parent,24,116,322,24,14,muted);
            Dropdown(new[]{"Webcam 0 (padrão)","Webcam 1","Webcam 2","Webcam 3"},parent,24,151,322,40,i=>cameraIndex=i);
            trackButton=Button("Iniciar rastreamento",parent,24,205,322,44,ToggleTracking,true);
            virtualButton=Button("Ativar câmera virtual",parent,24,261,322,44,ToggleCamera);
            cameraStatus=Label("Saída: 1280 × 720 · 30 fps",parent,24,316,322,30,13,muted);
            Toggle("Espelhar movimentos",parent,24,359,196,true,v=>tracking.mirror=v);
            Toggle("Fundo verde",parent,221,359,135,false,SetGreenScreen);
            Slider("Intensidade das expressões",parent,24,403,322,.5f,2,tracking.expressionGain,v=>tracking.expressionGain=v);
            Button("Como usar nas chamadas",parent,24,480,322,38,ShowCameraHelp);
            Label("A transmissão mostra apenas o personagem.",parent,24,528,322,20,11,muted);
        }
        void BuildPreview(Transform root)
        {
            var card=Panel("Prévia",root,510,256,814,558,panel);
            Label("PRÉVIA DO AVATAR",card.transform,24,20,360,22,11,muted,FontStyle.Bold);
            trackingStatus=Label("PRÉVIA",card.transform,560,20,230,22,11,green,FontStyle.Bold);trackingStatus.alignment=TextAnchor.MiddleRight;
            var clip=Rect("Preview frame",card.transform,16,60,782,366);clip.gameObject.AddComponent<RectMask2D>();
            avatarImage=Rect("Camera image",clip,0,0,782,440).gameObject.AddComponent<RawImage>();avatarImage.texture=output;avatarImage.raycastTarget=false;
            avatarImage.rectTransform.anchoredPosition=new Vector2(0,37); // Cover: crop height, preserve 16:9 pixels.
            previewName=Label("Seu personagem",card.transform,28,440,270,32,23,white,FontStyle.Bold);
            rigStatus=Label("AVATAR PRO  /  RIG FACIAL 52",card.transform,28,478,310,24,11,muted);
            animationToggle=Toggle("Prévia animada",card.transform,362,445,190,false,v=>avatar.previewAnimation=v);
            Button("Calibrar",card.transform,572,445,104,36,()=>{if(!tracking.FaceDetected && !tracking.PoseDetected){SetStatus("Inicie o rastreamento e enquadre rosto ou corpo antes de calibrar.");return;}tracking.Calibrate();SetStatus("Posição neutra calibrada para as partes detectadas.");});
            Button("Tela limpa",card.transform,688,445,104,36,()=>SetPresentation(true));
            Dropdown(new[]{"Demo automática","Sorriso","Surpresa","Seriedade","Fala"},card.transform,362,483,200,28,i=>{avatar.previewExpressionPreset=i;avatar.previewAnimation=true;animationToggle.SetIsOnWithoutNotify(true);});
            Dropdown(new[]{"Busto","Corpo inteiro"},card.transform,362,517,200,28,SetFraming);
            library=Dropdown(new[]{"Biblioteca de avatares"},card.transform,28,514,328,32,LoadSaved);
            saveButton=Button("Salvar avatar",card.transform,590,510,200,36,Save,true);
        }
        void ShowPage(int index)
        {
            if(advancedPanel!=null){Destroy(advancedPanel);advancedPanel=null;}
            for(int i=0;i<pages.Length;i++){pages[i].SetActive(i==index);navigation[i].GetComponent<Image>().color=i==index?purple:field;}
        }
        void ShowAdvanced()
        {
            if(Vrm.Active){SetStatus("Os detalhes deste rosto vêm do arquivo VRM. Selecione o gerador procedural em Modelo VRM para usar estes ajustes.");return;}
            if(busy){SetStatus("Aguarde a geração terminar para ajustar o rosto.");return;}
            if(advancedPanel!=null)return;
            string original=JsonUtility.ToJson(avatar.Profile);
            advancedPanel=Panel("Ajustes avançados",responsive.Content,116,256,370,558,panel).gameObject;
            Label("AJUSTES AVANÇADOS",advancedPanel.transform,24,18,322,24,11,purple,FontStyle.Bold);
            Label("Sua identidade, em detalhes.",advancedPanel.transform,24,48,322,30,20,white,FontStyle.Bold);
            void Detail(string label,int row,float min,float max,float value,Action<AvatarProfile,float> change)
            {
                var slider=Slider(label,advancedPanel.transform,24,84+row*46,322,min,max,value,v=>{change(avatar.Profile,v);rebuildPending=true;lastDetailChange=Time.unscaledTime;});
                slider.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-22);
                var text=slider.transform.parent.GetChild(0).GetComponent<Text>();text.fontSize=12;text.text=label;
            }
            var p=avatar.Profile;
            Detail("Largura do rosto",0,.83f,1.18f,p.faceWidth,(a,v)=>a.faceWidth=v);
            Detail("Tamanho dos olhos",1,.7f,1.35f,p.eyeSize,(a,v)=>a.eyeSize=v);
            Detail("Distância entre os olhos",2,.85f,1.15f,p.eyeSpacing,(a,v)=>a.eyeSpacing=v);
            Detail("Formato do nariz",3,.8f,1.22f,p.noseSize,(a,v)=>a.noseSize=v);
            Detail("Volume dos lábios",4,.7f,1.35f,p.lipFullness,(a,v)=>a.lipFullness=v);
            Detail("Largura da boca",5,.8f,1.2f,p.mouthWidth,(a,v)=>a.mouthWidth=v);
            Detail("Formato do maxilar",6,.84f,1.16f,p.jawWidth,(a,v)=>a.jawWidth=v);
            Detail("Volume das bochechas",7,.7f,1.35f,p.cheekVolume,(a,v)=>a.cheekVolume=v);
            Detail("Volume do cabelo",8,.7f,1.35f,p.hairVolume,(a,v)=>a.hairVolume=v);
            Button("Desfazer",advancedPanel.transform,24,512,108,30,()=>{rebuildPending=false;avatar.Rebuild(JsonUtility.FromJson<AvatarProfile>(original));Destroy(advancedPanel);advancedPanel=null;});
            Button("Concluir ajustes",advancedPanel.transform,144,512,202,30,()=>{if(rebuildPending){avatar.Rebuild(avatar.Profile);rebuildPending=false;}Destroy(advancedPanel);advancedPanel=null;SetStatus("Proporções atualizadas. Salve o avatar para guardar esta versão.");},true);
            responsive.LayoutAdvanced();
        }
        public void SetStatus(string message,bool isError=false) {status.text=message;status.color=isError?new Color(1,.58f,.57f):muted;}
        void Modify(Action<AvatarProfile> change)
        {
            if(Vrm.Active){SyncAppearance();SetStatus("Este modelo VRM tem aparência própria. Importe um cartoon ou realístico em Modelo VRM, ou selecione o gerador procedural para editar pela foto.");return;}
            if(busy){SyncAppearance();SetStatus("Aguarde a geração terminar para editar a aparência.");return;}
            change(avatar.Profile);avatar.Rebuild(avatar.Profile);SetStatus("Aparência atualizada. Salve para guardar esta versão.");
        }
        void SelectPhoto()
        {
            if(busy)return;
            try {string file=PhotoPicker.Pick();if(file!=null)StartCoroutine(ImportPhoto(file));}
            catch(Exception e){SetStatus(e.Message,true);}
        }
        IEnumerator ImportPhoto(string path)
        {
            busy=true;generateButton.interactable=false;SetButtonLabel(generateButton,"Preparando foto…");SetStatus("Preparando sua foto…");
            string thumbnail=Path.Combine(Storage,"Drafts",Guid.NewGuid().ToString("N")+".png");
            LocalJob job=null;
            try{job=new LocalJob("photo_preview.py","--input",path,"--output",thumbnail);generator=job;}
            catch(Exception e){busy=false;generateButton.interactable=selectedPhoto!=null;SetButtonLabel(generateButton,"Gerar meu avatar  →");SetStatus(e.Message,true);yield break;}
            float started=Time.realtimeSinceStartup;
            while(!job.Finished && Time.realtimeSinceStartup-started<30)yield return null;
            if(!job.Finished || job.ExitCode!=0){SetStatus(job.Finished?job.FriendlyError("Não foi possível abrir essa foto."):"A leitura da foto demorou demais. Tente outra imagem.",true);}
            else {
                try{SetReference(thumbnail);selectedPhoto=path;photoCaption.text="";SetStatus("Foto pronta. Gere seu personagem ou altere o nome abaixo.");}
                catch(Exception e){SetStatus("Não foi possível carregar a foto: "+e.Message,true);}
            }
            job.Dispose();generator=null;if(File.Exists(thumbnail))File.Delete(thumbnail);busy=false;generateButton.interactable=selectedPhoto!=null;SetButtonLabel(generateButton,"Gerar meu avatar  →");
        }
        void SetReference(string path)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            if(!texture.LoadImage(File.ReadAllBytes(path))){Destroy(texture);throw new InvalidDataException("Imagem inválida.");}
            if(reference!=null)Destroy(reference);reference=texture;referenceImage.texture=texture;
            var rect=referenceImage.rectTransform;
            float aspect=(float)texture.width/texture.height;
            float height=Mathf.Min(184,314/aspect),width=height*aspect;
            rect.sizeDelta=new Vector2(width,height);rect.anchoredPosition=new Vector2((322-width)/2,-(192-height)/2);
            referenceImage.gameObject.SetActive(true);
            photoPlaceholder.SetActive(false);
        }
        void Generate(){if(!busy && selectedPhoto!=null)StartCoroutine(GenerateRoutine());}
        IEnumerator GenerateRoutine()
        {
            if(Vrm.Active){Vrm.UseProcedural();RefreshModelStatus();SetFraming(0);}
            int selectedStyle=avatar.Profile.style;
            busy=true;generateButton.interactable=saveButton.interactable=false;SetButtonLabel(generateButton,"Gerando seu avatar…");
            SetStatus("Lendo proporções e cores do rosto. Seu avatar está sendo criado…");
            string directory=Path.Combine(Storage,"Drafts",Guid.NewGuid().ToString("N"));LocalJob job=null;
            try{job=new LocalJob("generate_avatar.py","--input",selectedPhoto,"--output",directory,"--name",nameField.text);generator=job;}
            catch(Exception e){busy=false;generateButton.interactable=saveButton.interactable=true;SetButtonLabel(generateButton,"Gerar meu avatar  →");SetStatus(e.Message,true);yield break;}
            float started=Time.realtimeSinceStartup;
            while(!job.Finished && Time.realtimeSinceStartup-started<90)yield return null;
            if(!job.Finished || job.ExitCode!=0)SetStatus(job.Finished?job.FriendlyError("Não foi possível gerar. Use um retrato frontal."):"A geração demorou demais. Confira a conexão para baixar os modelos na primeira execução.",true);
            else try {
                var profile=JsonUtility.FromJson<AvatarProfile>(File.ReadAllText(Path.Combine(directory,"avatar.json")));profile.Sanitize();
                profile.style=selectedStyle;
                avatar.Rebuild(profile);SyncAppearance();draft=directory;previewName.text=profile.name;nameField.text=profile.name;SetReference(Path.Combine(directory,"reference.png"));
                SetStatus("Avatar criado! Ajuste cabelo, óculos e cores em Aparência, depois salve na biblioteca.");
            } catch(Exception e){SetStatus("Não foi possível montar o avatar: "+e.Message,true);}
            job.Dispose();generator=null;busy=false;generateButton.interactable=saveButton.interactable=true;SetButtonLabel(generateButton,"Gerar meu avatar  →");
        }
        void Save()
        {
            if(busy){SetStatus("Aguarde a geração terminar antes de salvar.");return;}
            try {
                avatar.Profile.name=string.IsNullOrWhiteSpace(nameField.text)?"Meu avatar":nameField.text;avatar.Profile.Sanitize();
                string folder=Path.Combine(Storage,"Avatars",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
                if(Vrm.Active){File.Copy(Vrm.SourcePath,Path.Combine(folder,"model.vrm"));avatar.Profile.vrmSource="model.vrm";}
                else avatar.Profile.vrmSource=null;
                File.WriteAllText(Path.Combine(folder,"avatar.json"),JsonUtility.ToJson(avatar.Profile,true));
                if(reference!=null)File.WriteAllBytes(Path.Combine(folder,"reference.png"),reference.EncodeToPNG());
                previewName.text=avatar.Profile.name;RefreshLibrary();PlayerPrefs.SetString("LastAvatar",folder);PlayerPrefs.Save();
                SetStatus("Avatar salvo na sua biblioteca local.");
            } catch(Exception e){SetStatus("Não foi possível salvar: "+e.Message,true);}
        }
        void RefreshLibrary()
        {
            var folders=Directory.GetDirectories(Path.Combine(Storage,"Avatars"));Array.Sort(folders,(a,b)=>Directory.GetCreationTimeUtc(b).CompareTo(Directory.GetCreationTimeUtc(a)));
            var names=new List<string>{"Biblioteca de avatares"};var valid=new List<string>();
            foreach(var folder in folders)try{var p=JsonUtility.FromJson<AvatarProfile>(File.ReadAllText(Path.Combine(folder,"avatar.json")));p.Sanitize();names.Add(p.name);valid.Add(folder);}catch(Exception){ }
            savedFolders=valid.ToArray();library.ClearOptions();library.AddOptions(names);library.SetValueWithoutNotify(0);library.RefreshShownValue();
        }
        void LoadSaved(int index)
        {
            if(busy || index<1 || index>savedFolders.Length)return;
            try {
                string folder=savedFolders[index-1];var p=JsonUtility.FromJson<AvatarProfile>(File.ReadAllText(Path.Combine(folder,"avatar.json")));p.Sanitize();avatar.Rebuild(p);
                Vrm.UseProcedural();
                if(!string.IsNullOrEmpty(p.vrmSource))StartCoroutine(LoadVrmRoutine(p.vrmSource=="builtin:Seed-san"?VrmAvatarController.DefaultPath:Path.Combine(folder,p.vrmSource)));
                else {RefreshModelStatus();SetFraming(0);}
                previewName.text=nameField.text=p.name;draft=folder;SyncAppearance();
                string thumb=Path.Combine(folder,"reference.png");if(File.Exists(thumb)){SetReference(thumb);photoCaption.text="";}
                else {referenceImage.gameObject.SetActive(false);photoPlaceholder.SetActive(true);photoCaption.text="Avatar criado manualmente";if(reference!=null){Destroy(reference);reference=null;}}
                SetStatus("Avatar carregado. Inicie o rastreamento para acompanhar suas expressões.");
            }catch(Exception e){SetStatus("Não foi possível carregar: "+e.Message,true);}
        }
        void SetButtonLabel(Button button,string value)=>button.GetComponentInChildren<Text>().text=value;
        void ToggleTracking()
        {
            if(trackerJob!=null){trackerJob.Dispose();trackerJob=null;SetButtonLabel(trackButton,"Iniciar rastreamento");SetStatus("Rastreamento encerrado. A webcam foi liberada.");return;}
            try{avatar.previewAnimation=false;animationToggle.SetIsOnWithoutNotify(false);trackerJob=new LocalJob("tracker.py","--camera",cameraIndex.ToString(),"--no-preview","--hands");SetButtonLabel(trackButton,"Parar rastreamento");SetStatus("Abrindo a webcam para rosto, braços e dedos. Aguarde o primeiro rastreamento…");}
            catch(Exception e){SetStatus(e.Message,true);}
        }
        void ToggleCamera()
        {
            if(cameraJob!=null){StopCamera();SetStatus("Câmera virtual desativada.");return;}
            try{cameraJob=new LocalJob("virtual_camera.py");SetButtonLabel(virtualButton,"Desativar câmera virtual");SetStatus("Preparando a câmera virtual…");}
            catch(Exception e){SetStatus(e.Message,true);}
        }
        void StopCamera(){broadcaster.Stop();cameraJob?.Dispose();cameraJob=null;SetButtonLabel(virtualButton,"Ativar câmera virtual");cameraStatus.text="Saída: 1280 × 720 · 30 fps";}
        void ShowCameraHelp()
        {
            SetStatus("Instale o OBS Studio uma vez. Com a saída virtual do OBS desligada, ative aqui e selecione ‘OBS Virtual Camera’ na chamada. A webcam real é usada somente para rastrear.");
            Application.OpenURL("https://obsproject.com/kb/virtual-camera-guide");
        }
        void SyncAppearance(){styleDropdown.SetValueWithoutNotify(avatar.Profile.style);hairDropdown.SetValueWithoutNotify(avatar.Profile.hairStyle);glassesToggle.SetIsOnWithoutNotify(avatar.Profile.glasses);}
        void SetGreenScreen(bool value)
        {
            if(backgroundRenderer==null)return;
            if(backdrop==null)backdrop=backgroundRenderer.material;
            backdrop.SetColor("_Top",value?Color.green:new Color(.15f,.17f,.29f));
            backdrop.SetColor("_Bottom",value?Color.green:new Color(.04f,.05f,.09f));
            backdrop.SetColor("_Glow",value?Color.black:new Color(.24f,.20f,.43f));
        }
        void SetFraming(int value)
        {
            if(Vrm.Active){FrameVrm(value==1);return;}
            bool full=value==1;avatarCamera.orthographicSize=full?1.70f:.94f;
            avatarCamera.transform.position=new Vector3(0,full?.90f:1.54f,-5);
            if(backgroundRenderer!=null)backgroundRenderer.transform.localScale=new Vector3(9,6,1);
        }
        public IEnumerator CaptureInterface(string path,int width=1440,int height=900)
        {
            responsive.Suspended=true;
            responsive.Layout(width,height);
            var responsiveViewport=(RectTransform)responsive.Content.parent;responsiveViewport.anchoredPosition=Vector2.zero;responsiveViewport.sizeDelta=new Vector2(width,height);
            avatarCamera.Render();
            var captureObject=new GameObject("UI capture camera");var camera=captureObject.AddComponent<Camera>();
            var target=new RenderTexture(width,height,24);target.Create();camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=background;camera.cullingMask=1<<31;camera.orthographic=true;camera.orthographicSize=height/2f;
            var nodes=canvas.GetComponentsInChildren<Transform>(true);var layers=new int[nodes.Length];
            for(int i=0;i<nodes.Length;i++){layers[i]=nodes[i].gameObject.layer;nodes[i].gameObject.layer=31;}
            int mask=avatarCamera.cullingMask;avatarCamera.cullingMask &= ~(1<<31);
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.enabled=false;canvas.scaleFactor=1;
            yield return null;Canvas.ForceUpdateCanvases();
            foreach(var text in canvas.GetComponentsInChildren<Text>(true)){text.cachedTextGenerator.Invalidate();text.SetAllDirty();}
            Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=previous;
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;avatarCamera.cullingMask=mask;
            scaler.enabled=true;
            for(int i=0;i<nodes.Length;i++)if(nodes[i]!=null)nodes[i].gameObject.layer=layers[i];
            Destroy(texture);target.Release();Destroy(target);Destroy(captureObject);
            responsive.Suspended=false;responsive.ApplyScreen();
        }
        void SetPresentation(bool value)
        {
            presentation=value;canvas.gameObject.SetActive(!value);avatarCamera.targetTexture=value?null:output;
            avatarCamera.rect=new Rect(0,0,1,1);SetStatus("Tela limpa: Esc ou F11 para voltar ao Studio.");
        }
        void Update()
        {
            if(canvas==null)return;
            if(rebuildPending && Time.unscaledTime-lastDetailChange>.18f){avatar.Rebuild(avatar.Profile);rebuildPending=false;}
            if(UnityEngine.Input.GetKeyDown(KeyCode.F11))SetPresentation(!presentation);
            if(presentation && UnityEngine.Input.GetKeyDown(KeyCode.Escape))SetPresentation(false);
            // Keep the isolated render target alive even in presentation mode.
            if(presentation){var target=avatarCamera.targetTexture;avatarCamera.targetTexture=output;avatarCamera.Render();avatarCamera.targetTexture=target;}
            int hands=(tracking.GetHand(true)!=null?1:0)+(tracking.GetHand(false)!=null?1:0);
            bool partialHand=(tracking.GetHand(true)!=null && !tracking.HandComplete(true)) || (tracking.GetHand(false)!=null && !tracking.HandComplete(false));
            trackingStatus.text=avatar.freeze?"AVATAR PAUSADO":avatar.previewAnimation?"PRÉVIA ANIMADA":tracking.Connected?$"{(tracking.FaceDetected?"ROSTO":"SEM ROSTO")} • {hands} MÃOS{(partialHand?"*":"")} • {(tracking.FullBodyDetected?"CORPO":"PARCIAL")}":"PRÉVIA • WEBCAM DESLIGADA";
            if(trackerJob!=null && trackerJob.Finished){string message=trackerJob.FriendlyError("O rastreamento foi encerrado.");trackerJob.Dispose();trackerJob=null;SetButtonLabel(trackButton,"Iniciar rastreamento");SetStatus(message,true);}
            if(cameraJob!=null){
                while(cameraJob.Output.TryDequeue(out string line))if(line.StartsWith("READY|")){broadcaster.Begin();cameraStatus.text="Selecione OBS Virtual Camera na chamada";SetStatus("Câmera virtual pronta. O vídeo contém apenas o avatar.");}
                if(cameraJob.Finished){string error=cameraJob.FriendlyError("A câmera virtual foi encerrada.");StopCamera();SetStatus(error,true);}
                else if(broadcaster.Error!=null){string error=broadcaster.Error;StopCamera();SetStatus(error,true);}
            }
        }
        void OnDestroy()
        {
            generator?.Dispose();trackerJob?.Dispose();broadcaster?.Stop();cameraJob?.Dispose();
            if(reference!=null)Destroy(reference);if(rounded!=null)Destroy(rounded);if(roundTexture!=null)Destroy(roundTexture);
            if(backdrop!=null)Destroy(backdrop);
            foreach(var texture in uiTextures)Destroy(texture);
        }
    }
}
