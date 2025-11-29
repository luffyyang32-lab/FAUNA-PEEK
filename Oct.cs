using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Oct : MonoBehaviour
{
    [Header("章鱼视觉核心参数")]
    [Tooltip("低光环境亮度系数（0.1=极暗，1=正常光）")]
    [Range(0.1f, 2f)] public float lowLightIntensity = 0.3f;
    [Tooltip("对焦距离范围（最小/最大）")]
    public Vector2 focusRange = new Vector2(0.5f, 20f);
    [Tooltip("广角视野角度（默认170°，接近章鱼实际视野）")]
    [Range(60f, 179f)] public float wideAngleFOV = 170f;
    [Tooltip("对焦平滑速度")]
    public float focusSmoothSpeed = 5f;

    private Camera mainCamera;
    private float targetFocusDistance;
    private float currentExposure;
    // 新增：用于模拟曝光的材质和Shader
    private Material exposureMaterial;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("未找到Camera组件！");
            enabled = false;
            return;
        }

        mainCamera.fieldOfView = wideAngleFOV;
        currentExposure = lowLightIntensity;
        targetFocusDistance = focusRange.y / 2;

        // 初始化曝光材质（使用内置Shader模拟亮度调整）
        exposureMaterial = new Material(Shader.Find("Hidden/ExposureAdjust"));
        if (exposureMaterial == null)
        {
            Debug.LogWarning("未找到曝光调整Shader，将使用基础亮度模拟");
        }
    }

    void Update()
    {
        UpdateDynamicFocus();
        UpdateLowLightAdaptation();
    }

    void UpdateDynamicFocus()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, focusRange.y))
        {
            targetFocusDistance = Mathf.Clamp(hit.distance, focusRange.x, focusRange.y);
        }
        else
        {
            targetFocusDistance = Mathf.Lerp(targetFocusDistance, focusRange.y / 2, Time.deltaTime * 2f);
        }

        mainCamera.focalLength = Mathf.Lerp(mainCamera.focalLength, CalculateFocalLength(targetFocusDistance), Time.deltaTime * focusSmoothSpeed);
    }

    float CalculateFocalLength(float distance)
    {
        distance = Mathf.Clamp(distance, focusRange.x, focusRange.y);
        return distance / (2 * Mathf.Tan(wideAngleFOV * Mathf.Deg2Rad / 2));
    }

    void UpdateLowLightAdaptation()
    {
        currentExposure = Mathf.Lerp(currentExposure, lowLightIntensity, Time.deltaTime);
        mainCamera.backgroundColor = new Color(currentExposure * 0.05f, currentExposure * 0.1f, currentExposure * 0.2f);
        mainCamera.allowHDR = true;

        // 修正：移除直接访问exposure的代码，改用后期处理模拟
        if (exposureMaterial != null)
        {
            exposureMaterial.SetFloat("_Exposure", currentExposure);
        }
    }

    // 新增：通过OnRenderImage实现后期曝光调整
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (exposureMaterial != null)
        {
            Graphics.Blit(source, destination, exposureMaterial);
        }
        else
        {
            // 无Shader时的备选方案：直接调整亮度
            Graphics.Blit(source, destination);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * targetFocusDistance);
        Gizmos.DrawWireSphere(transform.position + transform.forward * targetFocusDistance, 0.1f);
    }

    // 清理材质资源
    void OnDestroy()
    {
        if (exposureMaterial != null)
        {
            Destroy(exposureMaterial);
        }
    }
}