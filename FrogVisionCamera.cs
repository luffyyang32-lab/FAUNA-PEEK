using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FrogVisionCamera : MonoBehaviour
{
    [Header("青蛙视觉参数")]
    [Tooltip("水平视野角度（青蛙约180°）")]
    [Range(90, 180)] public float horizontalFOV = 180f;
    [Tooltip("垂直视野角度（青蛙约90°）")]
    [Range(60, 120)] public float verticalFOV = 90f;
    [Tooltip("视觉距离（青蛙有效视觉范围）")]
    public float visionDistance = 8f;
    [Tooltip("静态物体模糊强度")]
    [Range(0, 5)] public float staticBlurIntensity = 2f;
    [Tooltip("青蛙视觉色偏强度")]
    [Range(0, 1)] public float colorTintIntensity = 0.8f;
    [Tooltip("视野外暗化强度")]
    [Range(0, 1)] public float outOfFOVDarken = 0.9f;

    private Camera frogCamera;
    private RenderTexture motionTexture; // 运动检测纹理
    private Material visionMaterial;    // 视觉效果材质

    // Shader参数ID（优化性能）
    private int _HorizontalFOV_ID;
    private int _VerticalFOV_ID;
    private int _VisionDistance_ID;
    private int _StaticBlurIntensity_ID;
    private int _ColorTintIntensity_ID;
    private int _OutOfFOVDarken_ID;
    private int _MotionTexture_ID;
    private int _CameraForward_ID;
    private int _CameraPosition_ID;

    void Awake()
    {
        frogCamera = GetComponent<Camera>();
        InitShaderIDs();
        InitVisionMaterial();
        InitMotionDetection();
    }

    void Start()
    {
        // 设置摄像机基础参数（匹配青蛙视野）
        frogCamera.fieldOfView = verticalFOV;
        frogCamera.aspect = horizontalFOV / verticalFOV; // 宽高比匹配视野角度
        frogCamera.nearClipPlane = 0.1f;
        frogCamera.farClipPlane = visionDistance;
    }

    void Update()
    {
        // 实时更新Shader参数
        UpdateShaderParameters();
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // 应用青蛙视觉效果
        if (visionMaterial != null)
        {
            Graphics.Blit(src, dest, visionMaterial);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }

    void OnDestroy()
    {
        // 释放资源
        if (motionTexture != null)
        {
            Destroy(motionTexture);
        }
        if (visionMaterial != null)
        {
            Destroy(visionMaterial);
        }
    }

    /// <summary>
    /// 初始化Shader参数ID（避免字符串查找）
    /// </summary>
    void InitShaderIDs()
    {
        _HorizontalFOV_ID = Shader.PropertyToID("_HorizontalFOV");
        _VerticalFOV_ID = Shader.PropertyToID("_VerticalFOV");
        _VisionDistance_ID = Shader.PropertyToID("_VisionDistance");
        _StaticBlurIntensity_ID = Shader.PropertyToID("_StaticBlurIntensity");
        _ColorTintIntensity_ID = Shader.PropertyToID("_ColorTintIntensity");
        _OutOfFOVDarken_ID = Shader.PropertyToID("_OutOfFOVDarken");
        _MotionTexture_ID = Shader.PropertyToID("_MotionTexture");
        _CameraForward_ID = Shader.PropertyToID("_CameraForward");
        _CameraPosition_ID = Shader.PropertyToID("_CameraPosition");
    }

    /// <summary>
    /// 初始化视觉效果材质
    /// </summary>
    void InitVisionMaterial()
    {
        Shader visionShader = Shader.Find("Custom/FrogVision");
        if (visionShader != null)
        {
            visionMaterial = new Material(visionShader);
            visionMaterial.hideFlags = HideFlags.DontSave;
        }
        else
        {
            Debug.LogError("未找到FrogVision Shader！请确认Shader已创建并命名为Custom/FrogVision");
        }
    }

    /// <summary>
    /// 初始化运动检测纹理（用于区分动态/静态物体）
    /// </summary>
    void InitMotionDetection()
    {
        // 创建运动检测纹理（和屏幕分辨率一致）
        motionTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8);
        motionTexture.name = "FrogVision_MotionTexture";
        motionTexture.Create();

        // 启用摄像机运动向量渲染
        frogCamera.depthTextureMode |= DepthTextureMode.MotionVectors;
    }

    /// <summary>
    /// 更新Shader参数
    /// </summary>
    void UpdateShaderParameters()
    {
        if (visionMaterial == null) return;

        visionMaterial.SetFloat(_HorizontalFOV_ID, horizontalFOV * Mathf.Deg2Rad);
        visionMaterial.SetFloat(_VerticalFOV_ID, verticalFOV * Mathf.Deg2Rad);
        visionMaterial.SetFloat(_VisionDistance_ID, visionDistance);
        visionMaterial.SetFloat(_StaticBlurIntensity_ID, staticBlurIntensity);
        visionMaterial.SetFloat(_ColorTintIntensity_ID, colorTintIntensity);
        visionMaterial.SetFloat(_OutOfFOVDarken_ID, outOfFOVDarken);
        visionMaterial.SetTexture(_MotionTexture_ID, motionTexture);
        visionMaterial.SetVector(_CameraForward_ID, transform.forward);
        visionMaterial.SetVector(_CameraPosition_ID, transform.position);
    }

    /// <summary>
    /// Gizmos绘制视野范围（编辑器可视化）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // 绘制视野扇形
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Vector3 forward = transform.forward;
        float halfHorizontal = horizontalFOV / 2 * Mathf.Deg2Rad;

        // 绘制水平视野边界
        int segments = 20;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfHorizontal, halfHorizontal, (float)i / segments);
            Vector3 dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            dir = transform.TransformDirection(dir);
            Gizmos.DrawLine(transform.position, transform.position + dir * visionDistance);
        }

        // 绘制垂直视野边界
        float halfVertical = verticalFOV / 2 * Mathf.Deg2Rad;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfVertical, halfVertical, (float)i / segments);
            Vector3 dir = new Vector3(0, Mathf.Sin(angle), Mathf.Cos(angle));
            dir = transform.TransformDirection(dir);
            Gizmos.DrawLine(transform.position, transform.position + dir * visionDistance);
        }

        // 绘制视野范围球体
        Gizmos.DrawSphere(transform.position, visionDistance);
    }
}