using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UniJSON;

namespace WebcamAvatar {
    // GLB geometry, skin weights, materials and original asset metadata stay intact.
    // Only humanoid/expression bindings are added for the shared UniVRM runtime.
    public static class AvaturnGlb {
        public static Dictionary<string,string> BoneNames(){
            var names=new Dictionary<string,string>{{"hips","Hips"},{"spine","Spine"},{"chest","Spine1"},{"upperChest","Spine2"},{"neck","Neck"},{"head","Head"}};
            foreach(var side in new[]{"left","right"}){
                string prefix=char.ToUpperInvariant(side[0])+side.Substring(1);
                foreach(var pair in new[]{"Shoulder:Shoulder","UpperArm:Arm","LowerArm:ForeArm","Hand:Hand","UpperLeg:UpLeg","LowerLeg:Leg","Foot:Foot","Toes:ToeBase"}){var p=pair.Split(':');names[side+p[0]]=prefix+p[1];}
                foreach(var finger in new[]{"Thumb","Index","Middle","Ring","Little"}){
                    var joints=finger=="Thumb"?new[]{"Metacarpal","Proximal","Distal"}:new[]{"Proximal","Intermediate","Distal"};
                    for(int j=0;j<3;j++)names[side+finger+joints[j]]=prefix+"Hand"+(finger=="Little"?"Pinky":finger)+(j+1);
                }
            }return names;
        }
        static string Quote(string value){var f=new JsonFormatter();f.Value(value);return f.ToString();}
        static string Map(IEnumerable<KeyValuePair<string,string>> pairs)=>"{"+string.Join(",",pairs.Select(p=>Quote(p.Key)+":"+p.Value))+"}";
        static Dictionary<string,string> Fields(JsonNode node)=>node.ObjectItems().ToDictionary(p=>p.Key.GetString(),p=>p.Value.Value.ToString());
        public static byte[] Convert(byte[] data){
            if(data==null||data.Length<28||data.Length>150*1024*1024||BitConverter.ToUInt32(data,0)!=0x46546c67||BitConverter.ToUInt32(data,4)!=2||BitConverter.ToUInt32(data,8)!=data.Length||BitConverter.ToUInt32(data,16)!=0x4e4f534a)throw new InvalidDataException("GLB inválido ou acima de 150 MB.");
            int length=checked((int)BitConverter.ToUInt32(data,12));if(length<2||length>data.Length-20)throw new InvalidDataException("Cabeçalho GLB inválido.");
            var root=JsonParser.Parse(Encoding.UTF8.GetString(data,20,length));
            if(root.TryGet("extensions",out var existing)&&existing.ContainsKey("VRMC_vrm"))return data;
            foreach(string section in new[]{"buffers","images"})if(root.TryGet(section,out var entries))foreach(var item in entries.ArrayItems())if(item.TryGet("uri",out var uri)&&!uri.GetString().StartsWith("data:",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Exporte um GLB com texturas e buffers incorporados.");
            var nodes=root["nodes"].ArrayItems().ToArray();var lookup=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            for(int i=0;i<nodes.Length;i++)if(nodes[i].TryGet("name",out var name)){string key=name.GetString().Split(':').Last();if(!lookup.ContainsKey(key))lookup[key]=i;}
            var human=new Dictionary<string,string>();foreach(var bone in BoneNames()){
                if(lookup.TryGetValue(bone.Value,out int index))human[bone.Key]="{\"node\":"+index+"}";
                else if(bone.Key!="upperChest"&&bone.Key!="leftToes"&&bone.Key!="rightToes")throw new InvalidDataException("Osso Avaturn ausente: "+bone.Value);
            }
            var custom=new Dictionary<string,List<string>>();var meshes=root["meshes"].ArrayItems().ToArray();
            for(int n=0;n<nodes.Length;n++)if(nodes[n].TryGet("mesh",out var meshIndex)){
                var mesh=meshes[meshIndex.GetInt32()];if(!mesh.TryGet("extras",out var extras)||!extras.TryGet("targetNames",out var targets))continue;
                int index=0;foreach(var target in targets.ArrayItems()){
                    string key=target.GetString();if(!string.IsNullOrEmpty(key)){key=char.ToLowerInvariant(key[0])+key.Substring(1);if(!custom.ContainsKey(key))custom[key]=new List<string>();custom[key].Add("{\"node\":"+n+",\"index\":"+index+",\"weight\":1}");}index++;
                }
            }
            if(!custom.ContainsKey("jawOpen")&&custom.ContainsKey("mouthOpen"))custom["jawOpen"]=custom["mouthOpen"];
            var expressions=custom.ToDictionary(p=>p.Key,p=>"{\"isBinary\":false,\"morphTargetBinds\":["+string.Join(",",p.Value)+"]}");
            var preset=new Dictionary<string,string>();if(expressions.TryGetValue("jawOpen",out var jaw))preset["aa"]=jaw;
            foreach(var side in new[]{"Left","Right"})if(expressions.TryGetValue("eyeBlink"+side,out var blink))preset["blink"+side]=blink;
            string meta="{\"name\":\"Avaturn · Meu avatar\",\"version\":\"1.0\",\"authors\":[\"Avaturn / proprietário do avatar\"],\"licenseUrl\":\"https://vrm.dev/licenses/1.0/\",\"otherLicenseUrl\":\"https://avaturn.me/terms\",\"avatarPermission\":\"onlyAuthor\",\"commercialUsage\":\"personalNonProfit\",\"creditNotation\":\"required\",\"allowRedistribution\":false,\"modification\":\"allowModification\",\"copyrightInformation\":\"Arquivo importado pelo usuário. Direitos e metadados originais preservados; consulte os termos do fornecedor.\"}";
            string vrm="{\"specVersion\":\"1.0\",\"meta\":"+meta+",\"humanoid\":{\"humanBones\":"+Map(human)+"},\"expressions\":{\"preset\":"+Map(preset)+",\"custom\":"+Map(expressions)+"}}";
            var fields=Fields(root);var extensions=existing.IsMap()?Fields(existing):new Dictionary<string,string>();extensions["VRMC_vrm"]=vrm;fields["extensions"]=Map(extensions);
            var used=root.TryGet("extensionsUsed",out var list)?list.ArrayItems().Select(v=>v.GetString()).ToList():new List<string>();if(!used.Contains("VRMC_vrm"))used.Add("VRMC_vrm");fields["extensionsUsed"]="["+string.Join(",",used.Select(Quote))+"]";
            byte[] json=Encoding.UTF8.GetBytes(Map(fields));int padded=(json.Length+3)&~3;int tail=20+length;
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream)){
                writer.Write(0x46546c67u);writer.Write(2u);writer.Write(checked(20+padded+data.Length-tail));writer.Write(padded);writer.Write(0x4e4f534au);writer.Write(json);for(int i=json.Length;i<padded;i++)writer.Write((byte)32);writer.Write(data,tail,data.Length-tail);return stream.ToArray();
            }
        }
    }
}
