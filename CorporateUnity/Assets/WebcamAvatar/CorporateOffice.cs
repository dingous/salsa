using System.Collections.Generic;
using UnityEngine;

namespace WebcamAvatar
{
    /// <summary>Original, local 3D office set; contains no downloaded background photograph.</summary>
    public sealed class CorporateOffice : MonoBehaviour
    {
        GameObject root;
        readonly List<Material> materials=new List<Material>();
        Material Mat(Color color){var m=new Material(Shader.Find("Standard"));m.color=color;materials.Add(m);return m;}
        void Box(string name,Vector3 position,Vector3 size,Material material) {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root.transform,false);
            obj.transform.position=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
            Destroy(obj.GetComponent<Collider>());
        }
        public void Show(bool visible) {
            if(root==null)Build();root.SetActive(visible);
        }
        void Build() {
            root=new GameObject("Corporate office set");
            Material wall=Mat(new Color(.68f,.73f,.76f)),wood=Mat(new Color(.27f,.24f,.22f)),metal=Mat(new Color(.20f,.26f,.29f)),glass=Mat(new Color(.55f,.72f,.80f));
            Box("Office wall",new Vector3(0,1.5f,1.85f),new Vector3(7,4,.05f),wall);
            Box("Daylight window",new Vector3(-1.15f,1.7f,1.70f),new Vector3(1.65f,1.9f,.05f),glass);
            for(int i=0;i<3;i++)Box("Window mullion",new Vector3(-1.95f+i*.8f,1.7f,1.60f),new Vector3(.025f,1.95f,.025f),metal);
            Box("Window crossbar",new Vector3(-1.15f,1.68f,1.60f),new Vector3(1.68f,.025f,.025f),metal);
            for(int i=0;i<3;i++)Box("Shelf",new Vector3(1.15f,1.25f+i*.40f,1.58f),new Vector3(1,.045f,.18f),wood);
            var books=Mat(new Color(.35f,.42f,.47f));
            for(int i=0;i<6;i++)Box("Book",new Vector3(.78f+i*.065f,1.42f,1.53f),new Vector3(.045f,.27f,.10f),books);
            Box("Desk",new Vector3(0,.68f,1.1f),new Vector3(3.2f,.07f,.6f),wood);
            Box("Monitor",new Vector3(1.0f,1.0f,.9f),new Vector3(.65f,.40f,.06f),metal);
            Box("Monitor stand",new Vector3(1.0f,.78f,1.0f),new Vector3(.08f,.16f,.05f),metal);
        }
        void OnDestroy(){if(root!=null)Destroy(root);foreach(var material in materials)Destroy(material);}
    }
}
