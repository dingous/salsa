using System;
using UnityEngine;
namespace WebcamAvatar {
    [Serializable] public class AvatarPreset {
        public string id,name,category,description,model,skin,hair,eyes,outfit;
        public int hairStyle,clothingStyle;public bool glasses,beard;
        public float faceWidth=1,jawWidth=1;
        public AvatarProfile Apply(AvatarProfile current){
            var p=JsonUtility.FromJson<AvatarProfile>(JsonUtility.ToJson(current));
            p.presetId=id;p.name=name;p.source="preset:"+id;p.skin=skin;p.hair=hair;p.eyes=eyes;p.outfit=outfit;
            p.hairStyle=hairStyle;p.clothingStyle=clothingStyle;p.glasses=glasses;p.beard=beard;
            p.faceWidth=faceWidth;p.jawWidth=jawWidth;p.vrmColors=true;
            p.eyeSpacing=p.noseSize=p.mouthWidth=p.cheekVolume=p.lipFullness=p.eyeSize=p.hairVolume=1;
            p.Sanitize();return p;
        }
    }
    [Serializable] public class AvatarPresetCatalog {
        public int version;public AvatarPreset[] presets;
        public static AvatarPreset[] Load(){
            var data=Resources.Load<TextAsset>("AvatarPresets");
            if(data==null)throw new InvalidOperationException("Catálogo de presets ausente.");
            var catalog=JsonUtility.FromJson<AvatarPresetCatalog>(data.text);
            if(catalog.version!=1||catalog.presets==null)throw new InvalidOperationException("Catálogo de presets inválido.");
            return catalog.presets;
        }
    }
}
