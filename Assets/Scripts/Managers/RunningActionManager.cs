using UnityEngine;
using DG.Tweening;

public class RunningActionManager : SingletonBehaviour<RunningActionManager>
{
    public bool IsRunningAction { get; private set; }

    // 종료 중 플래그: DOTween이 완료될 때까지 End 호출을 무시
    private bool isEnding = false;

    // 시작 중 플래그: Start 트윈이 완료되기 전에는 End가 큐되도록 한다
    private bool isStarting = false;
    private bool pendingEnd = false;

    // 내부에 저장할 레퍼런스들
    private CameraDirector cameraDirector;
    private PlayerCtrl playerCtrl;

    private RectTransform rightHandTransform;
    private TransformData rightHandStartTransform;
    private TransformData rightHandTargetTransform;

    private RectTransform leftHandTransform;
    private TransformData leftHandStartTransform;
    private TransformData leftHandTargetTransform;

    // 트윈 지속시간들 (기본값은 기존 GameManager와 동일하게 설정)
    private float initialHandTweenDuration = 0.5f;
    private float handMoveDuration = 1f;

    // Tween 핸들 저장 (중복 실행 제어용)
    private Sequence rightHandSeq;
    private Sequence leftHandSeq;

    [Header("JumpSetting")]
    [SerializeField] private float jumpHeight = 100f; // 조정 가능한 값
    [SerializeField] private float jumpDuration = 0.3f; // 올라가는 시간
    [SerializeField] private float fallDuration = 0.3f;
    
    // 점프 중 플래그: 점프가 완료될 때까지 추가 점프를 받지 않음
    private bool isJumping = false;
    
    // Prefab 생성 관련 필드
    [Header("Prefab Spawning")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    private float spawnTimer;
    private bool isSpawning;
    private Coroutine spawnCoroutine;

    private void KillAndClearSequences()
    {
        if (rightHandSeq != null)
        {
            rightHandSeq.Kill(true);
            rightHandSeq = null;
        }
        if (leftHandSeq != null)
        {
            leftHandSeq.Kill(true);
            leftHandSeq = null;
        }
    }

    public void StartRunningAction(CameraDirector cameraDir, PlayerCtrl player,
        RectTransform rightHand, TransformData rightStart, TransformData rightTarget,
        RectTransform leftHand, TransformData leftStart, TransformData leftTarget)
    {
        // 이미 시작 중이거나 실행 중이면 무시
        if (IsRunningAction || isStarting) return;

        // 초기화: 종료 플래그 리셋
        isEnding = false;
        pendingEnd = false;
        isStarting = true; // 시작 중 표시
        isJumping = false; // 점프 플래그 리셋

        // 저장
        cameraDirector = cameraDir;
        playerCtrl = player;

        rightHandTransform = rightHand;
        rightHandStartTransform = rightStart;
        rightHandTargetTransform = rightTarget;

        leftHandTransform = leftHand;
        leftHandStartTransform = leftStart;
        leftHandTargetTransform = leftTarget;

        // 전역 일시정지 플래그 설정
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = true;
        }

        // 기존 트윈 있으면 제거
        KillAndClearSequences();

        // 오른손을 초기 위치로 이동시키는 Sequence
        if (rightHandTransform != null)
        {
            rightHandSeq = DOTween.Sequence();
            rightHandSeq
                .Join(rightHandTransform.DOAnchorPos(rightHandStartTransform.position, initialHandTweenDuration).SetEase(Ease.OutCirc))
                .Join(rightHandTransform.DORotate(rightHandStartTransform.rotation, initialHandTweenDuration).SetEase(Ease.OutCirc))
                .Join(rightHandTransform.DOScale(rightHandStartTransform.scale, initialHandTweenDuration).SetEase(Ease.OutCirc))
                .OnComplete(() =>
                {
                    // 왼손을 타겟으로 이동 (시퀀스로 관리)
                    if (leftHandTransform != null)
                    {
                        leftHandSeq = DOTween.Sequence();
                        leftHandSeq
                            .Append(leftHandTransform.DOAnchorPos(leftHandTargetTransform.position, handMoveDuration).SetEase(Ease.OutCirc))
                            .Join(leftHandTransform.DORotate(leftHandTargetTransform.rotation, handMoveDuration).SetEase(Ease.OutCirc))
                            .Join(leftHandTransform.DOScale(leftHandTargetTransform.scale, handMoveDuration).SetEase(Ease.OutCirc))
                            .OnComplete(() =>
                            {
                                OnStartSequenceComplete();
                            });
                    }
                    else
                    {
                        OnStartSequenceComplete();
                    }
                });
        }
        else
        {
            // 오른손 트랜스폼이 없으면 왼손만 시퀀스로 처리
            if (leftHandTransform != null)
            {
                leftHandSeq = DOTween.Sequence();
                leftHandSeq
                    .Append(leftHandTransform.DOAnchorPos(leftHandTargetTransform.position, handMoveDuration).SetEase(Ease.OutCirc))
                    .Join(leftHandTransform.DORotate(leftHandTargetTransform.rotation, handMoveDuration).SetEase(Ease.OutCirc))
                    .Join(leftHandTransform.DOScale(leftHandTargetTransform.scale, handMoveDuration).SetEase(Ease.OutCirc))
                    .OnComplete(() =>
                    {
                        OnStartSequenceComplete();
                    });
            }
            else
            {
                // 둘 다 없으면 즉시 완료
                OnStartSequenceComplete();
            }
        }
    }

    private void OnStartSequenceComplete()
    {
        IsRunningAction = true;

        // UI 활성화
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetRunningActionCanvasActive(true);
        }

        // 카메라 포커스
        cameraDirector?.FocusRunningAction();

        // 게임 일시정지 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = false;
        }

        // 시작 완료 플래그 해제
        isStarting = false;

        // 만약 Start 중에 End 호출 요청이 있었으면 지금 처리
        if (pendingEnd)
        {
            pendingEnd = false;
            EndRunningAction();
        }

        // Prefab 생성 시작
        StartSpawning();
    }

    public void EndRunningAction()
    {
        // 만약 Start가 아직 완료되지 않았다면 End 요청을 큐합니다.
        if (isStarting)
        {
            pendingEnd = true;
            return;
        }

        // 이미 종료 중이면 무시
        if (isEnding) return;

        if (!IsRunningAction) return;

        // 종료 시작 플래그 설정
        isEnding = true;

        // 일시정지
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = true;
        }

        // UI 비활성화
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetRunningActionCanvasActive(false);
        }

        // 기존 트윈 있으면 제거(종료 트윈 시작 전에 안전하게 정리)
        KillAndClearSequences();

        // 왼손을 시작 위치로 이동 (시퀀스 사용)
        if (leftHandTransform != null)
        {
            leftHandSeq = DOTween.Sequence();
            leftHandSeq
                .Append(leftHandTransform.DOAnchorPos(leftHandStartTransform.position, initialHandTweenDuration).SetEase(Ease.OutCirc))
                .Join(leftHandTransform.DORotate(leftHandStartTransform.rotation, initialHandTweenDuration).SetEase(Ease.OutCirc))
                .Join(leftHandTransform.DOScale(leftHandStartTransform.scale, initialHandTweenDuration).SetEase(Ease.OutCirc))
                .OnComplete(() =>
                {
                    // 오른손을 타겟으로 복귀
                    if (rightHandTransform != null)
                    {
                        rightHandSeq = DOTween.Sequence();
                        rightHandSeq
                            .Append(rightHandTransform.DOAnchorPos(rightHandTargetTransform.position, handMoveDuration).SetEase(Ease.OutCirc))
                            .Join(rightHandTransform.DORotate(rightHandTargetTransform.rotation, handMoveDuration).SetEase(Ease.OutCirc))
                            .Join(rightHandTransform.DOScale(rightHandTargetTransform.scale, handMoveDuration).SetEase(Ease.OutCirc))
                            .OnComplete(() =>
                            {
                                FinishEndAction();
                            });
                    }
                    else
                    {
                        FinishEndAction();
                    }
                });
        }
        else
        {
            if (rightHandTransform != null)
            {
                rightHandSeq = DOTween.Sequence();
                rightHandSeq
                    .Append(rightHandTransform.DOAnchorPos(rightHandTargetTransform.position, handMoveDuration).SetEase(Ease.OutCirc))
                    .Join(rightHandTransform.DORotate(rightHandTargetTransform.rotation, handMoveDuration).SetEase(Ease.OutCirc))
                    .Join(rightHandTransform.DOScale(rightHandTargetTransform.scale, handMoveDuration).SetEase(Ease.OutCirc))
                    .OnComplete(() =>
                    {
                        FinishEndAction();
                    });
            }
            else
            {
                FinishEndAction();
            }
        }
    }

    private void FinishEndAction()
    {
        IsRunningAction = false;
        // 종료 플래그 리셋
        isEnding = false;
        // 점프 플래그 리셋
        isJumping = false;

        // 플레이어 마우스 잠금 해제
        playerCtrl?.UnlockMouseLook();

        // 일시정지 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = false;
        }

        // 모든 시퀀스 정리
        KillAndClearSequences();

        // Prefab 생성 중지
        StopSpawning();
    }

    /// <summary>
    /// 점프 제스처를 수행합니다 (왼손을 위로 올렸다가 내려옵니다).
    /// 점프 중에는 추가 점프가 불가능합니다.
    /// </summary>
    public void PerformJumpGesture()
    {
        if (!IsRunningAction || leftHandTransform == null) return;

        // 이미 점프 중이면 무시
        if (isJumping) return;

        // 점프 중 플래그 설정
        isJumping = true;

        // 현재 위치 저장
        Vector3 currentPos = leftHandTransform.anchoredPosition;

        // 왼손을 위로 올리는 시퀀스
        Sequence jumpSeq = DOTween.Sequence();
        jumpSeq
            .Append(leftHandTransform.DOAnchorPosY(currentPos.y + jumpHeight, jumpDuration).SetEase(Ease.OutQuad))
            .Append(leftHandTransform.DOAnchorPosY(currentPos.y, fallDuration).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                // 점프 완료 - 플래그 리셋
                isJumping = false;
            });
    }

    /// <summary>
    /// Prefab 생성을 시작합니다.
    /// </summary>
    private void StartSpawning()
    {
        if (isSpawning || prefabToSpawn == null) return;

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnPrefabCoroutine());
    }

    /// <summary>
    /// Prefab 생성을 중지합니다.
    /// </summary>
    private void StopSpawning()
    {
        if (!isSpawning) return;

        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    /// <summary>
    /// 일정한 간격으로 prefab을 생성하는 코루틴입니다.
    /// </summary>
    private System.Collections.IEnumerator SpawnPrefabCoroutine()
    {
        while (isSpawning && IsRunningAction)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (prefabToSpawn != null)
            {
                SpawnPrefab();
            }
        }
    }

    /// <summary>
    /// Prefab을 한 개 생성합니다.
    /// </summary>
    private void SpawnPrefab()
    {
        // spawnParent가 있으면 그 위치에서 생성, 없으면 월드 좌표에서 생성
        Vector3 spawnPos = spawnParent != null 
            ? spawnParent.position + spawnOffset 
            : spawnOffset;

        Quaternion spawnRot = spawnParent != null 
            ? spawnParent.rotation 
            : Quaternion.identity;

        Instantiate(prefabToSpawn, spawnPos, spawnRot, spawnParent);

        Debug.Log($"Prefab spawned at {spawnPos}");
    }
}
