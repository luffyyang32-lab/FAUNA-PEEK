using UnityEngine;

[RequireComponent(typeof(Camera))]
public class EagleWideAngleCamera : MonoBehaviour
{
    [Header("广角镜头设置")]
    [Range(60f, 110f)] public float wideAngleFOV = 90f;       // 无色散最优FOV
    [Range(0f, 0.15f)] public float wideAngleDistortion = 0.03f; // 广角畸变强度

    [Header("鹰视角运动（相对Player）")]
    public Vector3 offsetFromPlayer = new Vector3(0, 10f, -5f); // 相对Player的偏移
    public float headBobSpeed = 0.5f;                          // 头部摆动速度
    public Vector2 headBobRange = new Vector2(3f, 2f);         // 摆动幅度（子物体建议减小）
    public float scanRotationSpeed = 8f;                       // 扫描旋转速度

    [Header("消色散优化")]
    [Range(0.1f, 0.8f)] public float focusRadius = 0.3f;       // 中心清晰区域
    [Range(0f, 0.05f)] public float edgeBlur = 0.01f;          // 边缘模糊
    [Range(0f, 0.02f)] public float chromaticAberration = 0.005f; // 色差校正

    private Camera eagleCamera;
    private Vector3 originalLocalPos; // 相对Player的初始偏移
    private Material distortionMaterial;

    // Shader参数ID缓存
    private readonly int _DistortionAmount = Shader.PropertyToID("_DistortionAmount");
    private readonly int _FocusRadius = Shader.PropertyToID("_FocusRadius");
    private readonly int _EdgeBlur = Shader.PropertyToID("_EdgeBlur");
    private readonly int _ChromaticAberration = Shader.PropertyToID("_ChromaticAberration");

    private void Awake()
    {
        // 初始化相机核心参数
        eagleCamera = GetComponent<Camera>();
        // 记录相对Player的初始本地位置（优先用自定义offset）
        originalLocalPos = offsetFromPlayer;
        transform.localPosition = originalLocalPos;

        // 无色散相机设置
        eagleCamera.fieldOfView = wideAngleFOV;
        eagleCamera.nearClipPlane = 1f;          // 避免近景拉伸色散
        eagleCamera.farClipPlane = 2000f;
        eagleCamera.allowMSAA = true;            // 开启抗锯齿

        // 初始化消色散材质
        InitDistortionMaterial();
    }

    private void Update()
    {
        // 核心逻辑：相对Player的头部摆动 + 扫描旋转
        UpdateHeadBob();
        UpdateScanRotation();

        // 更新消色散参数
        if (distortionMaterial != null)
        {
            distortionMaterial.SetFloat(_DistortionAmount, wideAngleDistortion);
            distortionMaterial.SetFloat(_FocusRadius, focusRadius);
            distortionMaterial.SetFloat(_EdgeBlur, edgeBlur);
            distortionMaterial.SetFloat(_ChromaticAberration, chromaticAberration);
        }
    }

    /// <summary>模拟鹰头部轻微摆动（相对Player，无抖动）</summary>
    private void UpdateHeadBob()
    {
        // 基于时间的平滑摆动（子物体幅度减小，避免脱离Player）
        float x = Mathf.Sin(Time.time * headBobSpeed) * headBobRange.x * 0.01f;
        float y = Mathf.Cos(Time.time * headBobSpeed * 0.8f) * headBobRange.y * 0.01f;
        float z = Mathf.Sin(Time.time * headBobSpeed * 0.6f) * headBobRange.y * 0.005f;

        // 平滑插值，避免突变
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            originalLocalPos + new Vector3(x, y, z),
            Time.deltaTime * 5f
        );
    }

    /// <summary>缓慢扫描旋转（相对Player，模拟鹰巡视）</summary>
    private void UpdateScanRotation()
    {
        // 修复：使用正确的Rotate方法 + Space.Self（局部空间旋转）
        transform.Rotate(0f, scanRotationSpeed * Time.deltaTime * 0.1f, 0f, Space.Self);
    }

    /// <summary>初始化消色散Shader材质</summary>
    private void InitDistortionMaterial()
    {
        Shader shader = Shader.Find("Custom/WideAngleDistortion_NoChromatic");
        if (shader != null)
        {
            distortionMaterial = new Material(shader);
            eagleCamera.depthTextureMode = DepthTextureMode.Depth;
        }
        else
        {
            Debug.LogWarning("请创建Shader：Custom/WideAngleDistortion_NoChromatic");
        }
    }

    /// <summary>应用无色散后处理</summary>
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (distortionMaterial != null && wideAngleDistortion > 0f)
        {
            Graphics.Blit(source, destination, distortionMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    // 清理材质
    private void OnDestroy()
    {
        if (distortionMaterial != null) Destroy(distortionMaterial);
    }

    // 编辑器实时校验参数
    private void OnValidate()
    {
        if (eagleCamera != null) eagleCamera.fieldOfView = wideAngleFOV;
        chromaticAberration = Mathf.Clamp(chromaticAberration, 0f, 0.02f);
        edgeBlur = Mathf.Clamp(edgeBlur, 0f, 0.05f);
        wideAngleDistortion = Mathf.Clamp(wideAngleDistortion, 0f, 0.15f);
    }
}