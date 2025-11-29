using UnityEngine;

[RequireComponent(typeof(Camera))]
[AddComponentMenu("视觉效果/水母纯感光视觉")]
public class JellyfishVision : MonoBehaviour
{
    [Header("🪼 水母感光参数")]
    [Tooltip("明暗对比度（值越大，亮部越亮/暗部越暗）")]
    [Range(1f, 5f)] public float lightContrast = 2.5f;

    [Tooltip("全局弥散模糊强度（模拟无清晰视觉）")]
    [Range(0.1f, 3f)] public float blurIntensity = 1.8f;

    [Tooltip("感光灵敏度（值越大，对弱光越敏感）")]
    [Range(0.5f, 3f)] public float lightSensitivity = 1.5f;

    [Tooltip("视觉分辨率（值越小，越模糊低清）")]
    [Range(0.1f, 1f)] public float visionResolution = 0.4f;

    private Camera mainCamera;
    private Material jellyfishMaterial;
    private RenderTexture tempRT; // 低分辨率临时纹理

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("未找到Camera组件！");
            enabled = false;
            return;
        }

        // 加载水母视觉Shader
        Shader jellyShader = Shader.Find("Custom/JellyfishVision");
        if (jellyShader != null && jellyShader.isSupported)
        {
            jellyfishMaterial = new Material(jellyShader);
            jellyfishMaterial.hideFlags = HideFlags.DontSave;
        }
        else
        {
            Debug.LogWarning("未找到JellyfishVision Shader，效果失效！");
            enabled = false;
        }

        // 初始化低分辨率纹理（模拟低清感光）
        int rtWidth = Mathf.RoundToInt(Screen.width * visionResolution);
        int rtHeight = Mathf.RoundToInt(Screen.height * visionResolution);
        tempRT = new RenderTexture(rtWidth, rtHeight, 0, RenderTextureFormat.R8); // 仅灰度通道
        tempRT.hideFlags = HideFlags.DontSave;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (jellyfishMaterial == null || tempRT == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // 更新临时纹理分辨率（适配窗口大小变化）
        UpdateTempRTResolution();

        // 1. 缩放到低分辨率（模拟低清感光）
        Graphics.Blit(source, tempRT);

        // 2. 传递参数给Shader
        jellyfishMaterial.SetFloat("_LightContrast", lightContrast);
        jellyfishMaterial.SetFloat("_BlurIntensity", blurIntensity * 0.01f); // 缩放模糊值
        jellyfishMaterial.SetFloat("_LightSensitivity", lightSensitivity);

        // 3. 应用水母视觉效果（纯明暗感知）
        Graphics.Blit(tempRT, destination, jellyfishMaterial);
    }

    // 动态更新临时纹理分辨率
    void UpdateTempRTResolution()
    {
        int targetWidth = Mathf.RoundToInt(Screen.width * visionResolution);
        int targetHeight = Mathf.RoundToInt(Screen.height * visionResolution);

        if (tempRT.width != targetWidth || tempRT.height != targetHeight)
        {
            DestroyImmediate(tempRT);
            tempRT = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.R8);
            tempRT.hideFlags = HideFlags.DontSave;
        }
    }

    // 清理资源
    void OnDestroy()
    {
        if (jellyfishMaterial != null) DestroyImmediate(jellyfishMaterial);
        if (tempRT != null) DestroyImmediate(tempRT);
    }

    // 编辑器参数验证
    void OnValidate()
    {
        lightContrast = Mathf.Clamp(lightContrast, 1f, 5f);
        blurIntensity = Mathf.Clamp(blurIntensity, 0.1f, 3f);
        lightSensitivity = Mathf.Clamp(lightSensitivity, 0.5f, 3f);
        visionResolution = Mathf.Clamp(visionResolution, 0.1f, 1f);
    }
}