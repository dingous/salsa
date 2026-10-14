using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WebcamAvatar.Editor
{
    public static class CreateDemo
    {
        [MenuItem("Webcam Avatar/Criar Avatar Studio")]
        public static void CreateStudio()
        {
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var included=graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach(string shaderName in new[]{"VRM10/MToon10","UniGLTF/UniUnlit","Standard"}) {
                var shader=Shader.Find(shaderName);if(shader==null)throw new System.InvalidOperationException("Shader VRM ausente: "+shaderName);
                bool found=false;for(int i=0;i<included.arraySize;i++)if(included.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
                if(!found){int index=included.arraySize;included.InsertArrayElementAtIndex(index);included.GetArrayElementAtIndex(index).objectReferenceValue=shader;}
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var character=new GameObject("Cartoon avatar");
            var cartoon=character.AddComponent<CartoonAvatar>();var tracking=character.AddComponent<AvatarTracking>();
            var cameraGO=new GameObject("Avatar Output Camera");cameraGO.tag="MainCamera";
            var camera=cameraGO.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.94f;
            camera.transform.position=new Vector3(0,1.54f,-5);camera.transform.rotation=Quaternion.identity;
            camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.06f,.10f);
            System.IO.Directory.CreateDirectory("Assets/Studio");
            var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Studio/AvatarOutput.renderTexture");
            if(texture==null){texture=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){name="AvatarOutput",antiAliasing=4};AssetDatabase.CreateAsset(texture,"Assets/Studio/AvatarOutput.renderTexture");}
            camera.targetTexture=texture;
            var backdrop=GameObject.CreatePrimitive(PrimitiveType.Quad);backdrop.name="Studio backdrop";
            backdrop.transform.position=new Vector3(0,1.54f,2);backdrop.transform.localScale=new Vector3(6,3.5f,1);Object.DestroyImmediate(backdrop.GetComponent<Collider>());
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Studio/Backdrop.mat");
            if(material==null){material=new Material(Shader.Find("AvatarStudio/Background"));AssetDatabase.CreateAsset(material,"Assets/Studio/Backdrop.mat");}
            backdrop.GetComponent<Renderer>().sharedMaterial=material;
            var key=new GameObject("Soft key light").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.05f;key.color=new Color(1,.92f,.86f);key.transform.rotation=Quaternion.Euler(20,-28,0);
            var fill=new GameObject("Cool fill light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.color=new Color(.73f,.80f,1);fill.transform.rotation=Quaternion.Euler(-8,32,0);
            var rim=new GameObject("Rim light").AddComponent<Light>();rim.type=LightType.Point;rim.intensity=2;rim.range=5;rim.color=new Color(.58f,.50f,1);rim.transform.position=new Vector3(.8f,2.1f,.7f);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.50f,.50f,.57f);
            var ui=new GameObject("Avatar Studio").AddComponent<StudioUI>();ui.avatar=cartoon;ui.tracking=tracking;ui.avatarCamera=camera;ui.output=texture;ui.backgroundRenderer=backdrop.GetComponent<Renderer>();
            System.IO.Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/AvatarStudio.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/AvatarStudio.unity",true)};
            PlayerSettings.companyName="Salsa";PlayerSettings.productName="Avatar Studio";PlayerSettings.runInBackground=true;
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;PlayerSettings.visibleInBackground=true;
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Webcam Avatar/Criar cena de demonstração")]
        public static void Create()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var avatar=new GameObject("Avatar - Rastreamento webcam"); avatar.AddComponent<AvatarTracking>();
            var camera=new GameObject("Main Camera"); camera.tag="MainCamera";
            var c=camera.AddComponent<Camera>(); camera.transform.position=new Vector3(0,1.2f,-4.5f);
            camera.transform.LookAt(new Vector3(0,1.2f,0)); c.backgroundColor=new Color(.025f,.045f,.08f); c.clearFlags=CameraClearFlags.SolidColor;
            var light=new GameObject("Luz principal").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.25f; light.transform.rotation=Quaternion.Euler(35,-30,0);
            RenderSettings.ambientLight=new Color(.5f,.5f,.55f);
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/WebcamAvatar.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/WebcamAvatar.unity",true)};
        }
    }
}
