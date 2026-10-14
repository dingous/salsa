using System;
using System.Collections.Generic;
using UnityEngine;

namespace WebcamAvatar
{
    /// <summary>Customization of the actual skinned MakeHuman model and its fitted accessories.</summary>
    public sealed class CorporateAppearance : MonoBehaviour
    {
        readonly Dictionary<Material,Shader> shaders=new Dictionary<Material,Shader>();
        SkinnedMeshRenderer[] meshes;
        public bool IsCorporateHuman {get;private set;}
        public void Bind(GameObject model)
        {
            meshes=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            shaders.Clear();IsCorporateHuman=false;
            foreach(var mesh in meshes) {
                if(mesh.sharedMesh.GetBlendShapeIndex("identity_faceWidth")>=0)IsCorporateHuman=true;
                foreach(var material in mesh.materials)if(!shaders.ContainsKey(material))shaders[material]=material.shader;
            }
        }
        public void Apply(AvatarProfile profile)
        {
            if(!IsCorporateHuman)return;
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
                }
            }
            ApplyIdentity(profile);
        }
        public void ApplyIdentity(AvatarProfile profile)
        {
            if(!IsCorporateHuman || meshes==null)return;
            foreach(var mesh in meshes) {
                Set(mesh,"identity_faceWidth",Mathf.Clamp((profile.faceWidth-1)/.12f,-1,1));
                Set(mesh,"identity_jawWidth",Mathf.Clamp((profile.jawWidth-1)/.12f,-1,1));
                Set(mesh,"identity_stylized",profile.style==0?1:0);
            }
        }
        static void Set(SkinnedMeshRenderer mesh,string name,float value)
        {
            int index=mesh.sharedMesh.GetBlendShapeIndex(name);
            if(index>=0)mesh.SetBlendShapeWeight(index,value*100);
        }
    }
}
