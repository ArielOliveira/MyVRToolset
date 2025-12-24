using System.IO;
using UnityEngine;
using UnityEditor;
using Codice.Client.Common;

namespace Arielado.Graphics {
    [CustomEditor(typeof(LightSetter))]
    public class LightSetterEditor : Editor {
        SerializedProperty computeShader;

        private void OnEnable() {
            computeShader = serializedObject.FindProperty("computeShader");
        }

        public override void OnInspectorGUI(){
            base.OnInspectorGUI();

            if (GUILayout.Button("BakePowLUT"))
                BakePowLUT();

            if (GUILayout.Button("BakeHorizonLUT"))
                BakeHorizonLUT();

            if (GUILayout.Button("LoadTextures"))
                ((LightSetter)target).LoadTextures();
        }

        private void BakeHorizonLUT() {
            if (computeShader == null || computeShader.objectReferenceValue == null) return;

            ComputeShader cs = computeShader.objectReferenceValue as ComputeShader;

            RenderTexture rt = new RenderTexture(512, 512, 0);
            rt.enableRandomWrite = true;
            rt.Create();

            cs.SetFloat("lutSize", 512);
            cs.SetTexture(1, "Result", rt);
            cs.Dispatch(1, rt.width / 8, rt.height / 8, 1);

            RenderTexture current = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(512, 512, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            tex.Apply();

            RenderTexture.active = current;

            byte[] bytes = ImageConversion.EncodeToPNG(tex);
            DestroyImmediate(tex);

            File.WriteAllBytes(Paths.GetPersistentDir(Paths.LUT_TEXTURES + "HORIZON_LUT.png"), bytes);
        }

        private void BakePowLUT() {
            if (computeShader == null || computeShader.objectReferenceValue == null) return;

            ComputeShader cs = computeShader.objectReferenceValue as ComputeShader;
            
            RenderTexture rt = new RenderTexture(512, 512, 0);
            rt.enableRandomWrite = true;
            rt.Create();

            cs.SetFloat("lutSize", 512);
            cs.SetTexture(0, "Result", rt);
            cs.Dispatch(0, rt.width / 8, rt.height / 8, 1);

            RenderTexture current = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(512, 512, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
            tex.Apply();

            RenderTexture.active = current;

            byte[] bytes = ImageConversion.EncodeToPNG(tex);
            DestroyImmediate(tex);

            File.WriteAllBytes(Paths.GetPersistentDir(Paths.LUT_TEXTURES + "POW_LUT.png"), bytes);
        } 
    }
}