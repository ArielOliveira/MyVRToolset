using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[ExecuteAlways,
 RequireComponent(typeof(Light))]
public class LightSetter : MonoBehaviour {
    private const float MIN_RADIUS = 0.1f;
    private const float MAX_RADIUS = 0.4f;

    [SerializeField] private Texture2D powLut, sunZenithGrad, viewZenithGrad, sunViewGrad;
    [SerializeField] private Cubemap nightSky;
    [SerializeField] private Transform moon;
    [SerializeField] private Vector3 moonRotation;
    [SerializeField, Range(MIN_RADIUS, MAX_RADIUS)] private float sunRadius = 0.1f;
    [SerializeField, Range(MIN_RADIUS, MAX_RADIUS)] private float moonRadius = 0.1f;
    // Workaround: radius in the moon mesh and skybox mask don't match
    [SerializeField] private Vector2 moonDistanceResize = new Vector2(2.65f, 2.85f);
    [SerializeField, Range(2f, 500f)] private float moonDistance = 2f;
    [SerializeField, Range(0.01f, 0.5f)] private float debugCircleRadius, debugLineSize;
    private Light _light;

    private static int 
        _POW_LUT_ID = Shader.PropertyToID("_POW_LUT"),
        _SUN_DIR_ID = Shader.PropertyToID("_SUN_DIR"),
        _MOON_DIR_ID = Shader.PropertyToID("_MOON_DIR"),
        _SUN_RADIUS_ID = Shader.PropertyToID("_SUN_RADIUS"),
        _MOON_RADIUS_ID = Shader.PropertyToID("_MOON_RADIUS"),
        _SUN_ZENITH_GRAD_ID = Shader.PropertyToID("_SUN_ZENITH_GRAD"),
        _VIEW_ZENITH_GRAD_ID = Shader.PropertyToID("_VIEW_ZENITH_GRAD"),
        _SUN_VIEW_GRAD_ID = Shader.PropertyToID("_SUN_VIEW_GRAD"),
        _NIGHT_SKY_ID = Shader.PropertyToID("_NIGHT_SKY");
    

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        Quaternion rot = Quaternion.Euler(moonRotation);

        Vector3 moonDir = rot * Vector3.forward;
        Vector3 moonUp = rot * Vector3.up;
        Vector3 moonRight = rot * Vector3.right;
        
        Handles.color = Color.blue;
        Handles.DrawWireDisc(transform.position, moonDir, debugCircleRadius);

        Handles.DrawLine(transform.position + moonUp * debugCircleRadius, transform.position + moonUp * debugCircleRadius + moonDir * debugLineSize, 0.1f);
        Handles.DrawLine(transform.position - moonUp * debugCircleRadius, transform.position - moonUp * debugCircleRadius + moonDir * debugLineSize, 0.1f);
        Handles.DrawLine(transform.position + moonRight * debugCircleRadius, transform.position + moonRight * debugCircleRadius + moonDir * debugLineSize, 0.1f);
        Handles.DrawLine(transform.position - moonRight * debugCircleRadius, transform.position - moonRight * debugCircleRadius + moonDir * debugLineSize, 0.1f);
    }
    #endif

    private void Start() {
        Shader.SetGlobalTexture(_POW_LUT_ID, powLut);       
        Shader.SetGlobalTexture(_SUN_ZENITH_GRAD_ID, sunZenithGrad);
        Shader.SetGlobalTexture(_VIEW_ZENITH_GRAD_ID, viewZenithGrad);
        Shader.SetGlobalTexture(_SUN_VIEW_GRAD_ID, sunViewGrad);
        Shader.SetGlobalTexture(_NIGHT_SKY_ID, nightSky);
    }

    private void OnEnable() {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable() {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void Update() {
        if (_light == null) _light = GetComponent<Light>();

        if (_light == null) return;

        Shader.SetGlobalVector(_SUN_DIR_ID, -transform.forward);
        Shader.SetGlobalFloat(_SUN_RADIUS_ID, sunRadius);
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera) {
        UpdateMoon(camera);
    }

    private void UpdateMoon(Camera camera) {
        if (moon == null) return;

        Vector3 camPos = camera.transform.position;

        Quaternion rot = Quaternion.Euler(moonRotation);

        Vector3 moonDir = rot * -Vector3.forward;
        Vector3 moonPos = camPos + moonDir * moonDistance;

        moon.position = moonPos;

        float radiusRange = (Mathf.Abs(moonRadius - MIN_RADIUS)) / MAX_RADIUS;

        float factor = Mathf.Lerp(moonDistanceResize.x, moonDistanceResize.y, radiusRange);
        //float radiusPerDistanceAdjust = Mathf.Lerp(moonDistanceResize.x, moonDistanceResize.y, )

        float scale = (moonRadius * factor * moonDistance);

        moon.localScale = Vector3.one * scale;

        Shader.SetGlobalVector(_MOON_DIR_ID, moonDir);
        Shader.SetGlobalFloat(_MOON_RADIUS_ID, moonRadius);
    }
}
