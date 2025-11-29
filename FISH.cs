using UnityEngine;

/// <summary>
/// 骆驼视角：屏幕中间垂直黑条（仅核心功能，无多余逻辑）
/// 挂载到相机 → 仅实现中间黑条 + 修复AudioListener重复报错
/// </summary>
[RequireComponent(typeof(Camera))]
public class fish : MonoBehaviour
{
    [Header("黑条设置")]
    [Range(0.005f, 0.05f)] public float blackBarWidth = 0.015f; // 黑条宽度（屏幕比例）
    public Color blackBarColor = Color.black;                   // 黑条颜色（默认纯黑）

    private Camera cam;
    private Material visionMaterial;
    private static bool audioListenerFixed = false; // 仅修复一次AudioListener

    void Awake()
    {
        cam = GetComponent<Camera>();
        FixAudioListener(); // 仅修复骆驼视角相机相关的AudioListener
        InitShaderMaterial();
    }

    void OnDestroy()
    {
        if (visionMaterial != null) Destroy(visionMaterial);
    }

    /// <summary>
    /// 仅修复重复AudioListener（只保留当前相机的）
    /// </summary>
    void FixAudioListener()
    {
        if (audioListenerFixed) return;

        // 获取当前相机的AudioListener
        AudioListener camListener = cam.GetComponent<AudioListener>();
        if (camListener == null) return;

        // 禁用其他所有AudioListener，仅保留当前相机的
        foreach (AudioListener listener in FindObjectsOfType<AudioListener>())
        {
            if (listener != camListener) listener.enabled = false;
        }
        audioListenerFixed = true;
    }

    /// <summary>
    /// 初始化黑条Shader（修复Queue报错）
    /// </summary>
    void InitShaderMaterial()
    {
        Shader shader = Shader.Find("Custom/CamelVisionShader");
        if (shader == null)
        {
            Debug.LogError("未找到CamelVisionShader，请检查命名！");
            enabled = false;
            return;
        }
        visionMaterial = new Material(shader);
        visionMaterial.hideFlags = HideFlags.DontSave;
    }

    /// <summary>
    /// 相机后处理：仅绘制屏幕中间垂直黑条
    /// </summary>
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (visionMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // 传递黑条参数到Shader
        visionMaterial.SetFloat("_BlackBarWidth", blackBarWidth);
        visionMaterial.SetColor("_BlackBarColor", blackBarColor);

        // 仅应用中间黑条效果
        Graphics.Blit(source, destination, visionMaterial);
    }
}