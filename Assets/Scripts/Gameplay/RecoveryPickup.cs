using System.Collections;
using UnityEngine;

/// <summary>
/// RunningAction에서 생성되는 프리팹에 붙여 사용합니다.
/// 플레이어 콜라이더와 닿으면 GameManager.GetTired(int)로 집중도를 회복(증가)시킵니다.
/// 기본 동작: 한 번 회복하면 오브젝트를 삭제하거나 쿨다운 후 재사용할 수 있습니다.
/// 또한 일정한 속도로 이동하고 시간이 지나면 자동으로 사라집니다.
/// </summary>
public class RecoveryPickup : MonoBehaviour
{
    [Header("회복 설정")]
    [Tooltip("회복할 집중도 양 (GameManager.GetTired에 전달되는 정수).")]
    [SerializeField] private int healAmount = 10;

    [Tooltip("플레이어가 닿으면 이 오브젝트를 삭제할지 여부.")]
    [SerializeField] private bool destroyOnPickup = true;

    [Tooltip("destroyOnPickup이 false일 때 재사용 가능한 경우의 쿨다운 시간(초). 0이면 즉시 재사용 가능).")]
    [SerializeField] private float reuseCooldown = 0f;

    [Header("탐지 설정")]
    [Tooltip("플레이어 오브젝트의 태그(기본값: Player). 태그가 없으면 PlayerCtrl 컴포넌트로도 판별합니다.)")]
    [SerializeField] private string playerTag = "Player";

    [Header("시각/음향")]
    [Tooltip("이펙트(파티클)를 인스펙터에서 할당하면 회복 시 생성됩니다.)")]
    [SerializeField] private ParticleSystem pickupVfx;

    [Tooltip("회복 시 재생할 효과음 이름 (SoundManager의 SFX 폴더에서 로드). 비워두면 재생하지 않습니다.")]
    [SerializeField] private string pickupSfxName = "pickup";

    [Header("이동 설정")]
    [Tooltip("오브젝트가 이동할 방향 벡터 (정규화됨).")]
    [SerializeField] private Vector3 moveDirection = Vector3.forward;

    [Tooltip("이동 속도 (초당 유닛 수).")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("생명주기")]
    [Tooltip("오브젝트가 자동으로 삭제되는 시간(초). 0이면 자동 삭제 안 함.")]
    [SerializeField] private float lifespan = 10f;

    private Collider pickupCollider;
    private bool onCooldown = false;
    private float spawnTime = 0f;
    private bool isPickedUp = false;

    private void Awake()
    {
        spawnTime = Time.time;
        
        // 콜라이더 설정
        pickupCollider = GetComponent<Collider>();
        if (pickupCollider == null)
        {
            Debug.LogWarning("[RecoveryPickup] 콜라이더가 없어서 SphereCollider를 생성합니다.", this);
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            pickupCollider = sc;
        }
        else
        {
            // 기존 콜라이더를 트리거로 설정
            pickupCollider.isTrigger = true;
            Debug.Log($"[RecoveryPickup] 콜라이더를 트리거로 설정: {pickupCollider.GetType().Name}", this);
        }

        // 리지드바디 설정 (트리거 콜리전이 작동하려면 필요)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // 물리 시뮬레이션 제외 (수동으로 이동)
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // moveDirection 정규화 (0이 아닌 경우만)
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            moveDirection.Normalize();
        }

        Debug.Log("[RecoveryPickup] 초기화 완료", this);
    }

    private void Update()
    {
        // 이미 픽업되었거나 일시정지 중이면 이동하지 않음
        if (isPickedUp) return;

        // GameManager 체크
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;

        // 일정한 속도로 이동
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        // 생명주기 확인: 스폰된 지 lifespan 초가 지났으면 삭제
        if (lifespan > 0f && Time.time - spawnTime >= lifespan)
        {
            Debug.Log("[RecoveryPickup] 생명주기 만료로 삭제됩니다.", this);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[RecoveryPickup] OnTriggerEnter called with: {other.gameObject.name}, Tag: {other.tag}", this);
        
        if (onCooldown)
        {
            Debug.Log("[RecoveryPickup] 쿨다운 중입니다.", this);
            return;
        }

        if (!IsPlayerCollider(other))
        {
            Debug.Log($"[RecoveryPickup] 플레이어가 아닙니다: {other.gameObject.name}", this);
            return;
        }

        Debug.Log("[RecoveryPickup] 플레이어와 충돌! 픽업 적용 중...", this);
        ApplyPickup();
    }

    private void OnTriggerStay(Collider other)
    {
        // 이미 픽업되었거나 쿨다운 중이면 무시
        if (isPickedUp || onCooldown) return;

        // 플레이어와의 충돌이 지속되는 경우 (OnTriggerEnter를 놓친 경우 대비)
        Debug.Log($"[RecoveryPickup] OnTriggerStay called with: {other.gameObject.name}", this);
        if (IsPlayerCollider(other))
        {
            Debug.Log("[RecoveryPickup] OnTriggerStay에서 플레이어 감지! 픽업 적용...", this);
            ApplyPickup();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 트리거가 아닌 콜리전으로 들어오는 경우(안전장치)
        Debug.Log($"[RecoveryPickup] OnCollisionEnter called with: {collision.gameObject.name}", this);
        
        if (onCooldown) return;
        if (!IsPlayerCollider(collision.collider)) return;

        Debug.Log("[RecoveryPickup] 충돌을 통해 픽업 적용 중...", this);
        ApplyPickup();
    }

    private void OnCollisionStay(Collision collision)
    {
        // 이미 픽업되었거나 쿨다운 중이면 무시
        if (isPickedUp || onCooldown) return;

        Debug.Log($"[RecoveryPickup] OnCollisionStay called with: {collision.gameObject.name}", this);
        if (IsPlayerCollider(collision.collider))
        {
            Debug.Log("[RecoveryPickup] OnCollisionStay에서 플레이어 감지! 픽업 적용...", this);
            ApplyPickup();
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            Debug.Log("[RecoveryPickup] Collider가 null입니다.", this);
            return false;
        }

        // 루트 오브젝트 찾기
        GameObject rootObject = GetRootGameObject(other.gameObject);
        
        // 1. 태그 검사 (대소문자 무시) - 현재 오브젝트와 루트 모두 확인
        if (!string.IsNullOrEmpty(playerTag))
        {
            if (other.gameObject.CompareTag(playerTag))
            {
                Debug.Log($"[RecoveryPickup] 현재 오브젝트의 태그 일치: {playerTag} ({other.gameObject.name})", this);
                return true;
            }
            if (rootObject.CompareTag(playerTag))
            {
                Debug.Log($"[RecoveryPickup] 루트 오브젝트의 태그 일치: {playerTag} ({rootObject.name})", this);
                return true;
            }
        }

        // 2. 현재 오브젝트에서 PlayerCtrl 검색
        PlayerCtrl pc = other.GetComponent<PlayerCtrl>();
        if (pc != null)
        {
            Debug.Log("[RecoveryPickup] 현재 오브젝트에서 PlayerCtrl 발견", this);
            return true;
        }

        // 3. 부모 오브젝트에서 PlayerCtrl 검색
        pc = other.GetComponentInParent<PlayerCtrl>();
        if (pc != null)
        {
            Debug.Log("[RecoveryPickup] 부모 오브젝트에서 PlayerCtrl 발견", this);
            return true;
        }

        // 4. 루트 오브젝트에서 PlayerCtrl 검색
        pc = rootObject.GetComponent<PlayerCtrl>();
        if (pc != null)
        {
            Debug.Log("[RecoveryPickup] 루트 오브젝트에서 PlayerCtrl 발견", this);
            return true;
        }

        // 5. 자식 오브젝트에서 PlayerCtrl 검색
        pc = other.GetComponentInChildren<PlayerCtrl>();
        if (pc != null)
        {
            Debug.Log("[RecoveryPickup] 자식 오브젝트에서 PlayerCtrl 발견", this);
            return true;
        }

        Debug.Log($"[RecoveryPickup] 플레이어 판별 실패: {other.gameObject.name}, Tag: {other.tag}, Root: {rootObject.name}", this);
        return false;
    }

    private GameObject GetRootGameObject(GameObject obj)
    {
        if (obj.transform.parent == null)
        {
            return obj;
        }
        return GetRootGameObject(obj.transform.parent.gameObject);
    }

    private void ApplyPickup()
    {
        // 이미 픽업된 경우 중복 처리 방지
        if (isPickedUp)
        {
            Debug.Log("[RecoveryPickup] 이미 픽업 처리됨.", this);
            return;
        }
        isPickedUp = true;

        Debug.Log($"[RecoveryPickup] 픽업 적용 시작 - 회복량: {healAmount}", this);

        // GameManager에 접근해서 집중도(피로도)를 회복시킵니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GetTired(healAmount);
            Debug.Log($"[RecoveryPickup] 집중도 +{healAmount} 적용됨", this);
        }
        else
        {
            Debug.LogWarning("RecoveryPickup: GameManager.Instance가 없습니다. 집중도 회복을 적용할 수 없습니다.", this);
        }

        // 시각 효과
        if (pickupVfx != null)
        {
            Debug.Log("[RecoveryPickup] VFX 생성", this);
            Instantiate(pickupVfx, transform.position, Quaternion.identity);
        }

        // 음향 효과 (SoundManager 사용)
        if (!string.IsNullOrEmpty(pickupSfxName) && SoundManager.Instance != null)
        {
            Debug.Log($"[RecoveryPickup] SFX 재생: {pickupSfxName}", this);
            SoundManager.Instance.PlaySFX(pickupSfxName);
        }

        if (destroyOnPickup)
        {
            Debug.Log("[RecoveryPickup] 프리팹 삭제", this);
            Destroy(gameObject);
        }
        else
        {
            if (reuseCooldown > 0f)
            {
                Debug.Log($"[RecoveryPickup] 쿨다운 시작: {reuseCooldown}초", this);
                StartCoroutine(CooldownCoroutine());
            }
        }
    }

    private IEnumerator CooldownCoroutine()
    {
        onCooldown = true;
        // Disable collider to prevent repeated triggers during cooldown
        if (pickupCollider != null) pickupCollider.enabled = false;

        yield return new WaitForSeconds(reuseCooldown);

        if (pickupCollider != null) pickupCollider.enabled = true;
        onCooldown = false;
    }

    // 인스펙터에서 동적으로 설정할 수 있도록 공개 메서드
    public void SetHealAmount(int amount) => healAmount = amount;
    public void SetDestroyOnPickup(bool destroy) => destroyOnPickup = destroy;
    public void SetReuseCooldown(float seconds) => reuseCooldown = seconds;
}

