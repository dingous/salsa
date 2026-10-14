using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WebcamAvatar.Editor
{
    public static class CorporateBuild
    {
        public static void Windows()
        {
            CreateDemo.CreateStudio();
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            var toon=Shader.Find("CorporateAvatar/TexturedToon");
            if(toon==null)throw new Exception("Corporate toon shader missing");
            bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==toon)found=true;
            if(!found){int index=shaders.arraySize;shaders.InsertArrayElementAtIndex(index);shaders.GetArrayElementAtIndex(index).objectReferenceValue=toon;}
            graphics.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Build");
            var result=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Build/CorporateAvatarStudio.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Corporate build failed");
            Debug.Log("CORPORATE_BUILD_PASS");
        }
    }
}
