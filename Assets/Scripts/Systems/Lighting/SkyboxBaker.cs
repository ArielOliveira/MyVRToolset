
using System.IO;
using Arielado;
using UnityEngine;

[ExecuteAlways]
public class SkyboxBaker : MonoBehaviour {
    [SerializeField] private ComputeShader computeShader;

    private int _POW_LUT_ID  = Shader.PropertyToID("_POW_LUT");
    private RenderTexture rt;
    private void Execute() {
        if (computeShader == null) return;

        if (rt == null) {
            rt = new RenderTexture(512, 1, 0);
            rt.enableRandomWrite = true;
            rt.Create();
        }

        computeShader.SetTexture(0, "Result", rt);
        computeShader.Dispatch(0, rt.width / 8, 1, 1);

        RenderTexture current = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(512, 1, TextureFormat.RHalf, false);
        tex.ReadPixels(new Rect(0, 0, 512, 1), 0, 0);
        tex.Apply();

        RenderTexture.active = current;

        byte[] bytes = ImageConversion.EncodeToPNG(tex);
        DestroyImmediate(tex);

        File.WriteAllBytes(Paths.GetPersistentDir(Paths.LUT_TEXTURES + "POW_LUT.png"), bytes);
    } 
   

    private void OnDisable() {
        if (rt != null)
            rt.DiscardContents();

        rt = null;
    }

    private void Update() {
        Execute();
    }
}
