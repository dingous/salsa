using UnityEditor;
using UnityEngine;

namespace WebcamAvatar.Editor {
    [InitializeOnLoad]
    public static class DingousBranding {
        static DingousBranding(){EditorApplication.delayCall+=Apply;}
        public static void Apply(){
            const string path="Assets/Branding/DingousAvatar.png";
            var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(icon==null)return;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon});
            var sizes=PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
            var icons=new Texture2D[sizes.Length];
            for(int i=0;i<icons.Length;i++)icons[i]=icon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,icons);
        }
    }
}
