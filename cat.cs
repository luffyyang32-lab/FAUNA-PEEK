using UnityEngine;

[RequireComponent(typeof(Camera))]
public class cat : MonoBehaviour
{
    [Header("🐱 猫视角光圈+滤镜参数")]
    [Tooltip("蓝绿色滤镜强度（0-1）")]
    [Range(0f, 1f)] public float filterIntensity = 0.8f;

    [Tooltip("光圈半径（第一视角建议0.6-0.8）")]
    [Range(0f, 1f)] public float apertureRadius = 0.7f;

    [Tooltip("光圈边缘模糊度")]
    [Range(0.01f, 0.5f)] public float apertureSmooth = 0.15f;

    [Tooltip("光圈中心亮度（1=正常，>1更亮）")]
    [Range(1f, 2f)] public float centerBrightness = 1.2f;

    private Material catMaterial;

    void Awake()
    {
        // 加载自定义Shader（后续创建）
        Shader catShader = Shader.Find("Custom/CatApertureFilter");
        if (catShader != null)
        {
            catMaterial = new Material(catShader);
            catMaterial.hideFlags = HideFlags.DontSave;
        }
        else
        {
            Debug.LogError("找不到CatApertureFilter Shader！请先创建Shader文件");
            enabled = false;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (catMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // 传递参数给Shader
        catMaterial.SetFloat("_FilterIntensity", filterIntensity);
        catMaterial.SetFloat("_ApertureRadius", apertureRadius);
        catMaterial.SetFloat("_ApertureSmooth", apertureSmooth);
        catMaterial.SetFloat("_CenterBrightness", centerBrightness);

        // 应用后处理
        Graphics.Blit(source, destination, catMaterial);
    }

    void OnDestroy()
    {
        if (catMaterial != null) DestroyImmediate(catMaterial);
    }
}