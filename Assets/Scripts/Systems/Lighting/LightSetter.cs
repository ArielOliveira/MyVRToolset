using UnityEngine;

[ExecuteAlways,
 RequireComponent(typeof(Light))]
public class LightSetter : MonoBehaviour {
    [SerializeField] private Texture2D powLut, sunZenithGrad, viewZenithGrad, sunViewGrad;
    [SerializeField, Range(0.1f, 0.4f)] private float sunRadius;
    private Light _light;

    private static int 
        _POW_LUT_ID = Shader.PropertyToID("_POW_LUT"),
        _SUN_DIR_ID = Shader.PropertyToID("_SUN_DIR"),
        _SUN_RADIUS_ID = Shader.PropertyToID("_SUN_RADIUS"),
        _SUN_ZENITH_GRAD_ID = Shader.PropertyToID("_SUN_ZENITH_GRAD"),
        _VIEW_ZENITH_GRAD_ID = Shader.PropertyToID("_VIEW_ZENITH_GRAD"),
        _SUN_VIEW_GRAD_ID = Shader.PropertyToID("_SUN_VIEW_GRAD");
    


    private void Start() {
        Shader.SetGlobalTexture(_POW_LUT_ID, powLut);       
        Shader.SetGlobalTexture(_SUN_ZENITH_GRAD_ID, sunZenithGrad);
        Shader.SetGlobalTexture(_VIEW_ZENITH_GRAD_ID, viewZenithGrad);
        Shader.SetGlobalTexture(_SUN_VIEW_GRAD_ID, sunViewGrad);
    }

    private void Update() {
        if (_light == null) _light = GetComponent<Light>();

        if (_light == null) return;

        Shader.SetGlobalVector(_SUN_DIR_ID, -transform.forward);
        Shader.SetGlobalFloat(_SUN_RADIUS_ID, sunRadius);
    }
}
