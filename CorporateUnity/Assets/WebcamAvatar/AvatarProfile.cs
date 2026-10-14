using System;
using System.IO;
using UnityEngine;

namespace WebcamAvatar
{
    [Serializable]
    public class AvatarProfile
    {
        public int schemaVersion = 1;
        public string name = "Meu avatar";
        public string skin = "#DFA17B", hair = "#382C35", eyes = "#477A72", outfit = "#7279EC";
        public int hairStyle;
        public int clothingStyle;
        public int shirtPattern;
        public string shirtLogoPath, backgroundPath;
        public int style; // 0: cartoon; 1: realistic. Older profiles remain cartoon.
        public bool IsRealistic => style == 1;
        public float EffectiveEyeSize => eyeSize * (IsRealistic ? .78f : 1f);
        public bool glasses;
        public bool beard;
        public int environment;
        public float faceWidth = 1, eyeSpacing = 1, noseSize = 1, mouthWidth = 1, jawWidth = 1;
        public float cheekVolume = 1, lipFullness = 1, eyeSize = 1, hairVolume = 1;
        public string source = "manual";
        public string vrmSource;
        public bool vrmColors;
        public bool corporate;
        public static Color ColorOf(string hex, Color fallback) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        public void Sanitize()
        {
            if(schemaVersion != 1) throw new InvalidDataException("Formato de avatar não suportado.");
            name = string.IsNullOrWhiteSpace(name) ? "Meu avatar" : name.Substring(0, Math.Min(name.Length, 48));
            faceWidth = Clamp(faceWidth,.83f,1.18f); eyeSpacing = Clamp(eyeSpacing,.85f,1.15f);
            noseSize = Clamp(noseSize,.8f,1.22f); mouthWidth = Clamp(mouthWidth,.8f,1.2f); jawWidth = Clamp(jawWidth,.84f,1.16f);
            cheekVolume=Detail(cheekVolume);lipFullness=Detail(lipFullness);eyeSize=Detail(eyeSize);hairVolume=Detail(hairVolume);
            hairStyle = Mathf.Clamp(hairStyle,0,3);
            clothingStyle = Mathf.Clamp(clothingStyle,0,6);
            style = Mathf.Clamp(style,0,1);
            environment=Mathf.Clamp(environment,0,5);
            shirtPattern=Mathf.Clamp(shirtPattern,0,3);
            skin=ValidColor(skin,"#DFA17B"); hair=ValidColor(hair,"#382C35"); eyes=ValidColor(eyes,"#477A72"); outfit=ValidColor(outfit,"#7279EC");
        }
        static float Clamp(float value,float min,float max) => float.IsNaN(value) || float.IsInfinity(value) ? 1 : Mathf.Clamp(value,min,max);
        static string ValidColor(string value,string fallback) => ColorUtility.TryParseHtmlString(value,out _) ? value : fallback;
        static float Detail(float value)=>value==0?1:Clamp(value,.7f,1.35f);
    }
}
