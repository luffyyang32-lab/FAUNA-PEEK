using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PureButterflyEye : MonoBehaviour
{
    [Header("纯复眼切片效果")]
    [Tooltip("复眼切片数量（10×10=100个）")]
    [Range(8, 15)] public int gridSize = 10;

    [Tooltip("切片网格线强度（0=无，0.01=淡，0.02=明显）")]
    [Range(0f, 0.02f)] public float gridLineIntensity = 0.01f;

    private Material pureEyeMat;

    void Start()
    {
        // 加载纯复眼切片Shader
        pureEyeMat = new Material(Shader.Find("Hidden/PureButterflyEyeShader"));
        if (pureEyeMat == null)
        {
            Debug.LogWarning("Shader未找到，使用默认材质（无复眼效果）");
            pureEyeMat = new Material(Shader.Find("Unlit/Texture"));
        }

        // 相机恢复默认设置（无任何视野修改）
        Camera mainCam = GetComponent<Camera>();
        mainCam.fieldOfView = 60f; // 标准相机视野，完全无变形
        mainCam.nearClipPlane = 0.1f;
        mainCam.farClipPlane = 1000f;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (pureEyeMat == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // 仅传递切片数量和网格线参数（无任何色彩/畸变参数）
        pureEyeMat.SetInt("_GridSize", gridSize);
        pureEyeMat.SetFloat("_GridLineIntensity", gridLineIntensity);

        // 仅绘制复眼切片，不修改任何画面色彩/畸变
        Graphics.Blit(source, destination, pureEyeMat);
    }

    void OnDestroy()
    {
        if (pureEyeMat != null) Destroy(pureEyeMat);
    }

    void OnValidate()
    {
        if (Application.isPlaying && pureEyeMat != null)
        {
            pureEyeMat.SetFloat("_GridLineIntensity", gridLineIntensity);
        }
    }
}