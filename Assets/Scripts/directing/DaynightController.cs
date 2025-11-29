using UnityEngine;

public class DaynightController : MonoBehaviour
{
    [Header("Target Light")]
    public Light directionalLight; // 제어할 태양 조명

    [Header("Angle Settings")]
    [Tooltip("게임 시작 시 해의 각도 (오후)")]
    public Vector3 startSunAngle = new Vector3(50f, -30f, 0f);
    
    [Tooltip("게임 끝날 때 해의 각도 (해질녘)")]
    public Vector3 endSunAngle = new Vector3(0f, -30f, 0f);

    [Header("Color & Time Settings")]
    [Tooltip("시간 흐름에 따른 태양광 색상 변화")]
    public Gradient sunColorGradient;
    
    [Tooltip("노을이 지는 데 걸리는 총 시간 (초)")]
    public float dayCycleDuration = 120f;

    private float timeElapsed = 0f;
    private bool isRunning = true; // 시간 흐름 제어 플래그

    public void Initialize()
    {
        // 시작 시 초기 각도와 색상 설정
        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(startSunAngle);
            directionalLight.color = sunColorGradient.Evaluate(0f);
        }
    }

    void Update()
    {
        // 멈춰있거나 라이트가 없으면 실행 안 함
        if (GameManager.Instance.isGameOver) return;
        
        if (!isRunning || directionalLight == null) return;

        HandleSunset();
    }

    private void HandleSunset()
    {
        // 시간 경과
        timeElapsed += Time.deltaTime;

        // 진행률 (0.0 ~ 1.0)
        float percentage = timeElapsed / dayCycleDuration;
        percentage = Mathf.Clamp01(percentage);

        // 1. 회전 (각도 변경)
        Quaternion startRot = Quaternion.Euler(startSunAngle);
        Quaternion endRot = Quaternion.Euler(endSunAngle);
        directionalLight.transform.rotation = Quaternion.Lerp(startRot, endRot, percentage);

        // 2. 색상 변경
        directionalLight.color = sunColorGradient.Evaluate(percentage);
        
        // (선택사항) 시간이 다 되면 멈추고 싶으면 아래 주석 해제
        // if (percentage >= 1.0f) isRunning = false; 
    }

    // 외부(GameManager 등)에서 시간을 멈추고 싶을 때 호출할 함수
    public void StopTime()
    {
        isRunning = false;
    }
}