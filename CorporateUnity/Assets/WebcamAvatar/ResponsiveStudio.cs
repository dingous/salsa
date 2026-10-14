using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WebcamAvatar
{
    /// <summary>Reflows the studio without rebuilding controls or losing input state.</summary>
    public sealed class ResponsiveStudio : MonoBehaviour
    {
        Canvas canvas;
        RectTransform viewport,content;
        ScrollRect scroll;
        readonly List<RectTransform> nodes=new List<RectTransform>();
        readonly Dictionary<RectTransform,Vector2> original=new Dictionary<RectTransform,Vector2>();
        Vector2 lastSize;
        Rect lastSafe;
        public bool Suspended {get;set;}
        static void Place(RectTransform r,float x,float y,float w,float h) {
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);
        }
        public void Initialize(Canvas owner) {
            canvas=owner;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            foreach(Transform child in canvas.transform) {
                var r=child as RectTransform;if(r==null)continue;
                if(r.name=="Fundo")continue;
                if(r.name=="Sidebar" || r.name=="Logo" || r.name=="LOCAL" || r.name=="01" || r.name=="02" || r.name=="03") {r.gameObject.SetActive(false);continue;}
                nodes.Add(r);original[r]=r.anchoredPosition;
            }
            viewport=new GameObject("Responsive viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect)).GetComponent<RectTransform>();
            viewport.SetParent(canvas.transform,false);viewport.GetComponent<Image>().color=Color.clear;
            content=new GameObject("Studio content",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);
            foreach(var r in nodes)r.SetParent(content,false);
            scroll=viewport.GetComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=36;
            ApplyScreen();
        }
        public Transform Content=>content;
        void LateUpdate() {
            if(canvas==null || Suspended)return;
            if(lastSize!=new Vector2(Screen.width,Screen.height) || lastSafe!=Screen.safeArea)ApplyScreen();
        }
        public void ApplyScreen() {
            lastSize=new Vector2(Screen.width,Screen.height);lastSafe=Screen.safeArea;
            float scale=Mathf.Clamp(Screen.width/1440f,.82f,1.3f);
            if(Screen.width<390)scale=Screen.width/390f;
            canvas.GetComponent<CanvasScaler>().scaleFactor=scale;canvas.scaleFactor=scale;
            var safe=Screen.safeArea;
            Place(viewport,safe.x/scale,(Screen.height-safe.yMax)/scale,safe.width/scale,safe.height/scale);
            Layout(safe.width/scale,safe.height/scale);
        }
        public void Layout(float width,float height) {
            bool narrow=width<1100;float margin=width<600?16:32;float inner=width-2*margin;
            float header=narrow?248:216;
            float leftScale=Mathf.Min(1,inner/370f);
            float cardWidth=narrow?inner:inner-394;
            float previewY=narrow?header:header;
            float imageHeight=Mathf.Clamp((cardWidth-32)*9/16,170,440);
            float previewHeight=imageHeight+(cardWidth<650?392:246);
            float pageY=narrow?previewY+previewHeight+24:header;
            float footerY=Mathf.Max(pageY+558*leftScale,previewY+previewHeight)+20;
            foreach(var r in nodes) {
                var p=original[r];
                if(r.name.StartsWith("Página ")) {r.localScale=Vector3.one*leftScale;Place(r,narrow?margin+(inner-370*leftScale)/2:margin,pageY,370,558);}
                else if(r.name=="Prévia") {Place(r,narrow?margin:margin+394,previewY,cardWidth,previewHeight);LayoutPreview(r,cardWidth,imageHeight);}
                else if(r.name=="CORPORATE") {Place(r,margin,20,narrow?170:230,34);r.GetComponent<Text>().fontSize=narrow?22:25;}
                else if(r.name=="AVATAR STUDIO · UNITY")Place(r,margin,56,230,20);
                else if(r.name=="Local badge") {r.gameObject.SetActive(width>=600);Place(r,width-margin-104,24,104,30);}
                else if(r.name=="Criar avatar" || r.name=="Aparência" || r.name=="Transmissão" || r.name=="Modelo VRM") {
                    int i=r.name=="Criar avatar"?0:r.name=="Aparência"?1:r.name=="Transmissão"?2:3;
                    float navWidth=narrow?(inner-24)/4:124;Place(r,narrow?margin+i*(navWidth+8):margin+260+i*132,narrow?94:24,navWidth,48);FitLabel(r);
                    r.GetComponentInChildren<Text>().fontSize=width<600?12:14;
                }
                else if(r.GetComponent<Dropdown>()!=null)Place(r,narrow?width-margin-190:width-margin-220, narrow?20:82,narrow?190:220,40);
                else if(Mathf.Abs(p.y+128)<1) {Place(r,margin,narrow?154:110,inner,48);r.GetComponent<Text>().fontSize=width<600?26:38;}
                else if(Mathf.Abs(p.y+192)<1)Place(r,margin,narrow?204:166,inner,42);
                else Place(r,margin,footerY,inner,64);
            }
            Place(content,0,0,width,Mathf.Max(height,footerY+96));
            scroll.StopMovement();scroll.verticalNormalizedPosition=1;
            foreach(var dropdown in content.GetComponentsInChildren<Dropdown>(true))FitDropdown(dropdown);
            LayoutAdvanced();
        }
        static void FitDropdown(Dropdown dropdown) {
            float w=((RectTransform)dropdown.transform).sizeDelta.x;
            var caption=dropdown.captionText.rectTransform;caption.sizeDelta=new Vector2(w-48,caption.sizeDelta.y);
            var arrow=dropdown.transform.Find("v") as RectTransform;if(arrow!=null)arrow.anchoredPosition=new Vector2(w-30,arrow.anchoredPosition.y);
            var template=dropdown.template;template.sizeDelta=new Vector2(w,template.sizeDelta.y);
            foreach(var r in template.GetComponentsInChildren<RectTransform>(true)) {
                if(r==template)continue;
                float inset=r.GetComponent<Text>()!=null?32:8;
                r.sizeDelta=new Vector2(w-inset,r.sizeDelta.y);
            }
        }
        static void FitLabel(RectTransform r) {
            if(r.childCount>0 && r.GetChild(0).GetComponent<Text>()!=null)Place((RectTransform)r.GetChild(0),12,0,r.sizeDelta.x-24,r.sizeDelta.y);
        }
        static void LayoutPreview(RectTransform card,float width,float imageHeight) {
            bool compact=width<650;int dropdown=0;
            foreach(Transform child in card) {
                var r=(RectTransform)child;
                if(r.name=="PRÉVIA DO AVATAR")Place(r,16,16,width-32,22);
                else if(r.name=="PRÉVIA")Place(r,16,40,width-32,22);
                else if(r.name=="Preview frame") {
                    Place(r,16,70,width-32,imageHeight);
                    var image=(RectTransform)r.GetChild(0);float h=(width-32)*9/16;Place(image,0,(imageHeight-h)/2,width-32,h);
                }
                else if(r.GetComponent<Text>()!=null) {
                    bool title=r.GetComponent<Text>().fontSize==23;Place(r,16,70+imageHeight+(title?12:50),width-32,title?32:24);
                }
                else if(r.GetComponent<Toggle>()!=null)Place(r,16,70+imageHeight+80,190,32);
                else if(r.name=="Calibrar" || r.name=="Tela limpa") {Place(r,r.name=="Calibrar"?16+(compact?0:210):16+(compact?(width-40)/2+8:324),70+imageHeight+(compact?122:80),compact?(width-40)/2:104,40);FitLabel(r);}
                else if(r.GetComponent<Dropdown>()!=null) {
                    if(r.sizeDelta.x==328 || r.name=="Library responsive") {r.name="Library responsive";Place(r,16,70+imageHeight+(compact?218:124),compact?width-32:Mathf.Max(180,width-472),36);}
                    else {Place(r,compact?16+dropdown*((width-40)/2+8):width-440+dropdown*216,70+imageHeight+(compact?174:124),compact?(width-40)/2:200,36);dropdown++;}
                }
                else if(r.name=="Salvar avatar") {Place(r,compact?16:width-224,70+imageHeight+(compact?262:80),compact?width-32:208,44);FitLabel(r);}
            }
        }
        public void LayoutAdvanced() {
            if(content==null)return;
            var panel=(content.Find("Ajustes avançados")??content.Find("Laboratório Dingous")) as RectTransform;if(panel==null)return;
            var page=nodes.Find(r=>r.name=="Página 0");panel.localScale=page.localScale;Place(panel,page.anchoredPosition.x,-page.anchoredPosition.y,370,558);panel.SetAsLastSibling();
        }
    }
}
