using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AntEyeSimple : MonoBehaviour
{
    [Header("复眼效果参数")]
    [Tooltip("切片数量（固定10×10=100）")] public int gridSize = 10;
    [Tooltip("小眼边缘模糊")][Range(0f, 0.5f)] public float blur = 0.1f;
    [Tooltip("网格线强度")][Range(0f, 0.3f)] public float gridLine = 0.05f;
    [Tooltip("颜色偏移")][Range(0f, 0.1f)] public float colorShift = 0.03f;

    // 内置材质（自动创建，无需外部文件）
    private Material antEyeMat;

    void Start()
    {
        // 创建内置Shader材质，无需手动创建Shader文件
        antEyeMat = new Material(Shader.Find("Hidden/AntEyeShader"));
        if (!antEyeMat)
        {
            Debug.LogError("创建材质失败，使用默认材质");
            antEyeMat = new Material(Shader.Find("Unlit/Texture"));
        }
    }

    // 渲染时应用复眼效果
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!antEyeMat)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // 传递参数到Shader
        antEyeMat.SetInt("_GridSize", gridSize);
        antEyeMat.SetFloat("_Blur", blur);
        antEyeMat.SetFloat("_GridLine", gridLine);
        antEyeMat.SetFloat("_ColorShift", colorShift);
        antEyeMat.SetVector("_ScreenSize", new Vector2(Screen.width, Screen.height));

        // 应用效果到屏幕
        Graphics.Blit(source, destination, antEyeMat);
    }

    // 销毁时清理材质
    void OnDestroy()
    {
        if (antEyeMat) Destroy(antEyeMat);
    }
}