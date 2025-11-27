using UnityEngine;
using UnityEngine.Rendering; // 볼륨(Volume) 제어용
using UnityEngine.Rendering.Universal; // URP 효과(Vignette) 제어용

public class VignetteController : MonoBehaviour
{
    [Header("Volume Settings")]
    public Volume globalVolume; // 씬에 있는 Global Volume을 연결하세요.

    [Header("Vignette Intensity")]
    [Range(0f, 1f)]
    public float minIntensity = 0.2f; // 집중력 100%일 때 (평소)
    
    [Range(0f, 1f)]
    public float maxIntensity = 0.65f; // 집중력 0%일 때 (게임오버 직전)

    [Header("Smoothness")]
    public float smoothSpeed = 5f; // 값이 부드럽게 변하는 속도

    private Vignette vignette; // 제어할 비네팅 효과 변수

    void Start()
    {
        // Volume에서 Vignette 효과를 찾아 가져옵니다.
        if (globalVolume != null && globalVolume.profile.TryGet(out vignette))
        {
            Debug.Log("Vignette 효과를 찾았습니다.");
        }
        else
        {
            Debug.LogError("Global Volume에 Vignette가 없거나 연결되지 않았습니다!");
        }
    }

    void Update()
    {
        if (vignette == null) return;

        // 1. GameManager에서 현재 집중력 비율 가져오기 (1.0 = 가득 참, 0.0 = 바닥남)
        float ratio = GameManager.Instance.ConcentrationRatio;

        // 2. 비율에 따른 목표 강도 계산 (Mathf.Lerp 사용)
        // ratio가 1이면 minIntensity(약함), ratio가 0이면 maxIntensity(강함)가 됨
        float targetIntensity = Mathf.Lerp(maxIntensity, minIntensity, ratio);

        // 3. 현재 값에서 목표 값으로 부드럽게 변경 (눈이 피로하지 않게)
        float currentIntensity = (float)vignette.intensity;
        vignette.intensity.value = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothSpeed);
    }
}