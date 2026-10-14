using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UniVRM10;

namespace WebcamAvatar
{
    /// <summary>Customization of the actual skinned MakeHuman model and its fitted accessories.</summary>
    public sealed class CorporateAppearance : MonoBehaviour
    {
        readonly Dictionary<Material,Texture> fabricTextures=new Dictionary<Material,Texture>();
        readonly Dictionary<Material,Shader> shaders=new Dictionary<Material,Shader>();
        SkinnedMeshRenderer[] meshes;
        Texture2D logo;string loadedLogo;Transform chest;
        public bool IsCorporateHuman {get;private set;}
        public void Bind(GameObject model)
        {
            meshes=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var instance=model.GetComponent<Vrm10Instance>();chest=instance!=null?instance.Runtime.ControlRig.GetBoneTransform(HumanBodyBones.Neck):null;
            shaders.Clear();fabricTextures.Clear();IsCorporateHuman=false;
            foreach(var mesh in meshes) {
                if(mesh.sharedMesh.GetBlendShapeIndex("identity_faceWidth")>=0)IsCorporateHuman=true;
                foreach(var material in mesh.materials)if(!shaders.ContainsKey(material)){shaders[material]=material.shader;fabricTextures[material]=material.HasProperty("_MainTex")?material.GetTexture("_MainTex"):null;}
            }
        }
        public void Apply(AvatarProfile profile)
        {
            if(!IsCorporateHuman)return;
            if(loadedLogo!=profile.shirtLogoPath){
                if(logo!=null)Destroy(logo);logo=null;loadedLogo=profile.shirtLogoPath;
                if(!string.IsNullOrEmpty(loadedLogo)&&File.Exists(loadedLogo)){logo=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!logo.LoadImage(File.ReadAllBytes(loadedLogo))){Destroy(logo);logo=null;}else logo.wrapMode=TextureWrapMode.Clamp;}
            }
            foreach(var mesh in meshes) {
                string name=mesh.name.ToLowerInvariant();
                if(name.Contains("male_elegantsuit01"))mesh.enabled=profile.clothingStyle==0;
                for(int i=1;i<=6;i++)if(name.Contains("male_casualsuit"+i.ToString("00")))mesh.enabled=profile.clothingStyle==i;
                if(name.Contains("short02"))mesh.enabled=profile.hairStyle==0;
                else if(name.Contains("short04"))mesh.enabled=profile.hairStyle==1;
                else if(name.Contains("long01"))mesh.enabled=profile.hairStyle==2;
                else if(name.Contains("beard"))mesh.enabled=profile.beard;
                else if(name.Contains("glasses"))mesh.enabled=profile.glasses;
                foreach(var material in mesh.materials) {
                    if(!shaders.TryGetValue(material,out var original))continue;
                    // Preserve textures and alpha: switching only the shader retains their properties.
                    material.shader=profile.style==0?Shader.Find("CorporateAvatar/TexturedToon"):original;
                    if(material.HasProperty("_MainTex")&&fabricTextures.TryGetValue(material,out var fabric))material.SetTexture("_MainTex",fabric);
                    if(name.Contains("male_casualsuit06")&&(profile.shirtPattern!=0||logo!=null)){
                        material.shader=Shader.Find("CorporateAvatar/ShirtPrint");
                        var bounds=mesh.sharedMesh.bounds;float height=bounds.size.y;
                        SetLogoRectangle(material);
                        material.SetTexture("_MainTex",Texture2D.whiteTexture);
                        material.SetFloat("_Pattern",profile.shirtPattern);material.SetFloat("_HasLogo",logo!=null?1:0);material.SetTexture("_Logo",logo);
                    }
                }
            }
            ApplyIdentity(profile);
        }
        public void ApplyIdentity(AvatarProfile profile)
        {
            if(!IsCorporateHuman || meshes==null)return;
            foreach(var mesh in meshes) {
                if(mesh.enabled)foreach(var material in mesh.sharedMaterials)if(material.shader.name=="CorporateAvatar/ShirtPrint")SetLogoRectangle(material);
                Set(mesh,"identity_faceWidth",Mathf.Clamp((profile.faceWidth-1)/.12f,-1,1));
                Set(mesh,"identity_jawWidth",Mathf.Clamp((profile.jawWidth-1)/.12f,-1,1));
                Set(mesh,"identity_stylized",profile.style==0?1:0);
            }
        }
        void SetLogoRectangle(Material material){
            var center=chest!=null?chest.position-Vector3.up*.18f:Vector3.up*1.3f;
            float aspect=logo!=null?(float)logo.width/logo.height:1;
            material.SetVector("_LogoRect",new Vector4(center.x,center.y,.16f*Mathf.Min(aspect,2),.16f/Mathf.Max(1,aspect/2)));
        }
        void OnDestroy(){if(logo!=null)Destroy(logo);}
        static void Set(SkinnedMeshRenderer mesh,string name,float value)
        {
            int index=mesh.sharedMesh.GetBlendShapeIndex(name);
            if(index>=0)mesh.SetBlendShapeWeight(index,value*100);
        }
    }
}
