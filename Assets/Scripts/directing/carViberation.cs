using UnityEngine;

public class CarVibration : MonoBehaviour
{
    [Header("기본 진동 설정 (엔진/노면)")]
    public float positionShakeAmount = 0.02f; // 위아래 흔들림 강도
    public float rotationShakeAmount = 0.3f;  // 회전 흔들림 강도
    public float shakeSpeed = 15.0f;          // 진동 속도 (높을수록 엔진이 빨리 도는 느낌)

    [Header("랜덤 덜컹거림 (방지턱 효과)")]
    public bool enableRandomBumps = true;
    public float bumpIntervalMin = 2.0f;      // 최소 덜컹 주기 (초)
    public float bumpIntervalMax = 7.0f;      // 최대 덜컹 주기 (초)
    public float bumpForce = 0.1f;            // 덜컹거리는 강도
    public float bumpDuration = 0.3f;         // 덜컹거림이 지속되는 시간

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    
    // 노이즈 계산을 위한 오프셋 (매번 다른 패턴을 위해)
    private float noiseOffsetPos;
    private float noiseOffsetRot;

    // 덜컹거림 타이머
    private float bumpTimer;
    private float currentBumpOffset = 0f;

    void Start()
    {
        // 시작 위치와 회전값을 저장해둡니다 (이 기준점에서 흔들기 위해)
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        noiseOffsetPos = Random.Range(0f, 100f);
        noiseOffsetRot = Random.Range(0f, 100f);

        bumpTimer = Random.Range(bumpIntervalMin, bumpIntervalMax);
    }

    void Update()
    {
        // 1. 기본 진동 계산 (Perlin Noise)
        // Time.time에 속도를 곱해 노이즈 그래프를 이동시킵니다.
        float noiseY = (Mathf.PerlinNoise(Time.time * shakeSpeed, noiseOffsetPos) - 0.5f) * 2f; // -1 ~ 1 사이 값
        float noiseRotZ = (Mathf.PerlinNoise(Time.time * shakeSpeed, noiseOffsetRot) - 0.5f) * 2f; 

        // 2. 랜덤 덜컹거림 처리
        HandleRandomBump();

        // 3. 최종 위치 적용
        // Y축(위아래)으로 주로 흔들리고, 덜컹거림(currentBumpOffset)을 더합니다.
        Vector3 newPos = initialPosition;
        newPos.y += (noiseY * positionShakeAmount) + currentBumpOffset;
        
        // 4. 최종 회전 적용
        // Z축(좌우 롤링)을 살짝 섞어주면 더 리얼합니다.
        Quaternion newRot = initialRotation * Quaternion.Euler(0, 0, noiseRotZ * rotationShakeAmount);

        // 로컬 좌표에 적용 (부모가 움직여도 내부 진동 유지)
        transform.localPosition = newPos;
        transform.localRotation = newRot;
    }

    void HandleRandomBump()
    {
        if (!enableRandomBumps) return;

        bumpTimer -= Time.deltaTime;

        // 덜컹거릴 시간이 되면
        if (bumpTimer <= 0)
        {
            StartCoroutine(DoBump());
            bumpTimer = Random.Range(bumpIntervalMin, bumpIntervalMax); // 다음 주기 랜덤 설정
        }
    }

    // 부드럽게 튀어올랐다 내려오는 코루틴
    System.Collections.IEnumerator DoBump()
    {
        float elapsed = 0f;

        while (elapsed < bumpDuration)
        {
            elapsed += Time.deltaTime;
            // 0에서 1로 갔다가 다시 0으로 돌아오는 Sin 곡선 (덜컹!)
            float progress = elapsed / bumpDuration;
            currentBumpOffset = Mathf.Sin(progress * Mathf.PI) * bumpForce;
            
            yield return null;
        }

        currentBumpOffset = 0f;
    }
}