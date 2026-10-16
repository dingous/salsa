using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WebcamAvatar.Editor
{
    public static class CorporateBuild
    {
        public static void Android()
        {
            CreateDemo.CreateStudio();
            EditorUserBuildSettings.buildAppBundle=false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"br.com.dingous.avatar");
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.X86_64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait=true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=true;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;
            PlayerSettings.allowedAutorotateToLandscapeRight=true;
            var playerSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            playerSettings.FindProperty("useCustomMainGradleTemplate").boolValue=true;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            var toon=Shader.Find("CorporateAvatar/TexturedToon");
            if(toon==null)throw new Exception("Corporate toon shader missing");
            bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==toon)found=true;
            if(!found){int index=shaders.arraySize;shaders.InsertArrayElementAtIndex(index);shaders.GetArrayElementAtIndex(index).objectReferenceValue=toon;}
            graphics.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Build/Android");
            var result=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Build/Android/DingousAvatar.apk",BuildTarget.Android,BuildOptions.Development);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Android build failed");
            Debug.Log("DINGOUS_ANDROID_BUILD_PASS");
        }
        public static void Windows()
        {
            CreateDemo.CreateStudio();
            DingousBranding.Apply();
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            var toon=Shader.Find("CorporateAvatar/TexturedToon");
            if(toon==null)throw new Exception("Corporate toon shader missing");
            bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==toon)found=true;
            if(!found){int index=shaders.arraySize;shaders.InsertArrayElementAtIndex(index);shaders.GetArrayElementAtIndex(index).objectReferenceValue=toon;}
            graphics.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Build");
            var development=Array.IndexOf(Environment.GetCommandLineArgs(),"--development-build")>=0;
            var result=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Build/CorporateAvatarStudio.exe",BuildTarget.StandaloneWindows64,development?BuildOptions.Development:BuildOptions.None);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Corporate build failed");
            Debug.Log("CORPORATE_BUILD_PASS");
        }
    }
}
