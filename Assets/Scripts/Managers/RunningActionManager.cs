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
    private Animator leftHandAnimator; // 왼손 애니메이터 추가

    // 트윈 지속시간들 (기본값은 기존 GameManager와 동일하게 설정)
    private float initialHandTweenDuration = 0.5f;
    private float handMoveDuration = 1f;

    // Tween 핸들 저장 (중복 실행 제어용)
    private Sequence rightHandSeq;
    private Sequence leftHandSeq;

    // Animator 파라미터명 상수
    private const string runParamName = "run";
    private const string jumpParamName = "jump";

    // Animator 파라미터 해시 (효율성 개선)
    private int animRunHash;
    private int animJumpHash;

    [Header("JumpSetting")]
    [SerializeField] private float jumpHeight = 100f; // 조정 가능한 값
    [SerializeField] private float jumpDuration = 0.3f; // 올라가는 시간
    [SerializeField] private float fallDuration = 0.3f;
    
    [Tooltip("점프할 때 움직일 3D 오브젝트")]
    [SerializeField] private Transform jumpObject;
    
    [Tooltip("점프 오브젝트가 올라갈 높이 (월드 Y축 기준)")]
    [SerializeField] private float jumpObjectHeight = 2f;
    
    [Tooltip("점프할 때 재생할 효과음 이름 (SoundManager의 SFX 폴더에서 로드). 비워두면 재생하지 않습니다.")]
    [SerializeField] private string jumpSfxName = "jump";
    
    // 점프 중 플래그: 점프가 완료될 때까지 추가 점프를 받지 않음
    private bool isJumping = false;
    
    // Prefab 생성 관련 필드
    [Header("Prefab Spawning")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Transform spawnParent;
    
    [Tooltip("첫 번째 스폰 위치의 오프셋")]
    [SerializeField] private Vector3 spawnOffset1 = Vector3.zero;
    
    [Tooltip("두 번째 스폰 위치의 오프셋")]
    [SerializeField] private Vector3 spawnOffset2 = Vector3.zero;
    
    [SerializeField] private float spawnInterval = 0.5f;
    
    [Tooltip("0~100 사이의 값. 각 스폰 시도마다 이 확률로 프리팹을 생성합니다.")]
    [SerializeField] private float spawnProbability = 50f;

    private float spawnTimer;
    private bool isSpawning;
    private Coroutine spawnCoroutine;

    // 스포된 프리팹들을 추적하기 위한 리스트
    private System.Collections.Generic.List<GameObject> spawnedPrefabs = new System.Collections.Generic.List<GameObject>();

    [Tooltip("점프 상태 후보 이름들 (Animator에서 사용하는 상태 이름들). 우선순위 순서대로 넣으세요.")]
    [SerializeField] private string[] jumpStateNames = new string[] { "Jump", "jump", "JumpState", "LeftJump" };

    [Tooltip("런 상태 후보 이름들 (Animator에서 사용하는 상태 이름들). 우선순위 순서대로 넣으세요.")]
    [SerializeField] private string[] runStateNames = new string[] { "Run", "run", "LeftRun" };

    // 점프 재트리거 방지: 최근 점프 시간 기록
    private float lastJumpTime = -10f;
    private const float jumpSuppressDuration = 0.25f; // 이 시간 이내에 애니메이터가 jump 상태로 강제 전환되는 것을 무시

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

    private void InitializeAnimatorHashes()
    {
        animRunHash = Animator.StringToHash("run");
        animJumpHash = Animator.StringToHash("jump");
    }

    public void StartRunningAction(CameraDirector cameraDir, PlayerCtrl player,
        RectTransform rightHand, TransformData rightStart, TransformData rightTarget,
        RectTransform leftHand, TransformData leftStart, TransformData leftTarget)
    {
        // 이미 시작 중이거나 실행 중이면 무시
        if (IsRunningAction || isStarting) return;

        // Animator 해시 초기화 (첫 호출 시)
        if (animRunHash == 0)
        {
            InitializeAnimatorHashes();
        }

        // 이전 프리팹들 정리
        foreach (GameObject prefab in spawnedPrefabs)
        {
            if (prefab != null)
            {
                Destroy(prefab);
            }
        }
        spawnedPrefabs.Clear();

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
        
        // 왼손의 Animator 찾기 (자식 포함)
        if (leftHandTransform != null)
        {
            leftHandAnimator = leftHandTransform.GetComponentInChildren<Animator>();
            if (leftHandAnimator != null)
            {
                // Animator 동작 보장 설정
                try
                {
                    leftHandAnimator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
                    leftHandAnimator.updateMode = UnityEngine.AnimatorUpdateMode.UnscaledTime;
                    leftHandAnimator.Rebind();
                    leftHandAnimator.Update(0f);
                    Debug.Log("[RunningActionManager] leftHandAnimator culling/update 설정 및 Rebind 수행", this);

                    // 할당된 컨트롤러와 클립 목록 출력(디버그)
                    var controller = leftHandAnimator.runtimeAnimatorController;
                    if (controller != null)
                    {
                        Debug.Log($"[RunningActionManager] Animator Controller: {controller.name}", this);
                        foreach (var clip in controller.animationClips)
                        {
                            Debug.Log($"[RunningActionManager] Animator clip: {clip.name}", this);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[RunningActionManager] leftHandAnimator 초기화 중 예외: {ex.Message}", this);
                }

                // run 애니메이션 재생 (문자열 파라미터 사용)
                leftHandAnimator.SetTrigger(runParamName);
                Debug.Log("[RunningActionManager] 왼손 run 애니메이션 시작", this);
            }
            else
            {
                Debug.LogWarning("[RunningActionManager] leftHandAnimator를 찾지 못했습니다.", this);
            }
        }

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

        // 왼손 애니메이션 정리
        if (leftHandAnimator != null)
        {
            // 모든 애니메이션 상태 초기화
            leftHandAnimator.ResetTrigger(runParamName);
            leftHandAnimator.ResetTrigger(jumpParamName);
            Debug.Log("[RunningActionManager] 왼손 애니메이션 상태 초기화", this);
        }

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
        KillAndClearSequences();

        // Prefab 생성 중지
        StopSpawning();

        // 모든 스포된 프리팹 제거
        foreach (GameObject prefab in spawnedPrefabs)
        {
            if (prefab != null)
            {
                Destroy(prefab);
            }
        }
        spawnedPrefabs.Clear();
        
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

        // 점프 효과음 재생 (SoundManager 사용)
        if (!string.IsNullOrEmpty(jumpSfxName) && SoundManager.Instance != null)
        {
            Debug.Log($"[RunningActionManager] 점프 효과음 재생: {jumpSfxName}", this);
            SoundManager.Instance.PlaySFX(jumpSfxName);
        }

        // 왼손 점프 애니메이션 재생
        if (leftHandAnimator != null)
        {
            // 기록: 최근 점프 시간
            lastJumpTime = Time.time;

            // 현재 상태 정보 로깅
            var stateInfo = leftHandAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[RunningActionManager] Animator current state hash={stateInfo.shortNameHash}, normalizedTime={stateInfo.normalizedTime}", this);

            // run 트리거 리셋(안정화)
            try { leftHandAnimator.ResetTrigger(runParamName); } catch {}

            leftHandAnimator.SetTrigger(jumpParamName);
            Debug.Log("[RunningActionManager] 왼손 jump 트리거 설정", this);

            // 즉시 CrossFade로 강제 전환 시도
            try
            {
                int jumpStateHash = GetPreferredStateHash(leftHandAnimator, jumpStateNames);
                if (jumpStateHash != 0)
                {
                    leftHandAnimator.CrossFade(jumpStateHash, 0f, 0, 0f);
                    leftHandAnimator.Update(0f);
                    Debug.Log("[RunningActionManager] 왼손 CrossFade로 jump 즉시 전환 시도", this);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RunningActionManager] CrossFade 시도 실패: {ex.Message}", this);
            }

            // 트리거 기반 전환이 지연될 수 있으므로 가능한 상태명을 즉시 재생 시도
            TryForcePlayAnimatorState(leftHandAnimator, jumpStateNames);

            // 재발동 방지: 트리거 즉시 리셋
            try { leftHandAnimator.ResetTrigger(jumpParamName); } catch {}
        }
        else
        {
            Debug.LogWarning("[RunningActionManager] leftHandAnimator가 할당되지 않았습니다.", this);
        }

        // 현재 위치 저장
        Vector3 currentPos = leftHandTransform.anchoredPosition;

        // 왼손을 위로 올리는 시퀀스
        Sequence jumpSeq = DOTween.Sequence();
        jumpSeq
            .Append(leftHandTransform.DOAnchorPosY(currentPos.y + jumpHeight, jumpDuration).SetEase(Ease.OutQuad))
            .Append(leftHandTransform.DOAnchorPosY(currentPos.y, fallDuration).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                // 점프 완료 - run 애니메이션으로 복귀
                if (leftHandAnimator != null)
                {
                    // run 트리거 설정
                    leftHandAnimator.SetTrigger(runParamName);
                    Debug.Log("[RunningActionManager] 왼손 run 트리거 설정", this);

                    // jump 트리거 리셋하여 자동 재발동 방지
                    try { leftHandAnimator.ResetTrigger(jumpParamName); } catch {}

                    // 즉시 run 상태로 복귀 시도
                    try
                    {
                        int runStateHash = GetPreferredStateHash(leftHandAnimator, runStateNames);
                        if (runStateHash != 0)
                        {
                            leftHandAnimator.CrossFade(runStateHash, 0f, 0, 0f);
                            leftHandAnimator.Update(0f);
                            Debug.Log("[RunningActionManager] 왼손 CrossFade로 run 즉시 전환 시도", this);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[RunningActionManager] CrossFade(run) 시도 실패: {ex.Message}", this);
                    }
                    TryForcePlayAnimatorState(leftHandAnimator, runStateNames);

                    // 트리거 즉시 리셋
                    try { leftHandAnimator.ResetTrigger(runParamName); } catch {}
                }
                // 점프 완료 - 플래그 리셋
                isJumping = false;
            });

        // 점프 오브젝트도 함께 움직이기
        if (jumpObject != null)
        {
            Vector3 jumpObjectCurrentPos = jumpObject.position;
            Vector3 jumpObjectTargetPos = new Vector3(
                jumpObjectCurrentPos.x,
                jumpObjectCurrentPos.y + jumpObjectHeight,
                jumpObjectCurrentPos.z
            );

            Sequence jumpObjectSeq = DOTween.Sequence();
            jumpObjectSeq
                .Append(jumpObject.DOMove(jumpObjectTargetPos, jumpDuration).SetEase(Ease.OutQuad))
                .Append(jumpObject.DOMove(jumpObjectCurrentPos, fallDuration).SetEase(Ease.InQuad));
        }
        else
        {
            Debug.LogWarning("[RunningActionManager] Jump Object가 할당되지 않았습니다.", this);
        }
    }

    /// <summary>
    /// 가능한 애니메이션 상태 이름 목록에서 존재하는 상태를 찾아 즉시 재생합니다.
    /// (Transition의 Exit Time으로 인해 트리거로 전환이 지연될 때를 보완)
    /// </summary>
    private void TryForcePlayAnimatorState(Animator animator, string[] candidateStateNames)
    {
        if (animator == null) return;

        // Ensure animator is enabled and bindings are up-to-date
        if (!animator.enabled) animator.enabled = true;
        animator.Update(0f);

        int layers = Mathf.Max(1, animator.layerCount);

        // 짧은 시간 내(최근 jump가 발생한 경우) jump 후보 무시
        bool suppressRecentJump = (Time.time - lastJumpTime) < jumpSuppressDuration;

        // 1) 모든 레이어에서 현재/다음 상태를 확인하고 Play/CrossFade 시도
        for (int layer = 0; layer < layers; layer++)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            AnimatorStateInfo next = animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer) : new AnimatorStateInfo();

            foreach (var stateName in candidateStateNames)
            {
                if (string.IsNullOrEmpty(stateName)) continue;

                // 최근 점프 억제: stateName이 jump 관련이면 스킵
                if (suppressRecentJump && stateName.ToLower().Contains("jump"))
                {
                    Debug.Log($"[RunningActionManager] TryForcePlayAnimatorState: suppressed recent jump candidate '{stateName}'", this);
                    continue;
                }

                int hash = Animator.StringToHash(stateName);

                // 이미 현재 상태거나 다음(전환 중) 상태라면 건너뜀
                if (current.shortNameHash == hash)
                {
                    Debug.Log($"[RunningActionManager] TryForcePlayAnimatorState: already in state '{stateName}' on layer {layer}", this);
                    return; // 이미 목표 상태이므로 더 이상 시도하지 않음
                }
                if (animator.IsInTransition(layer) && next.shortNameHash == hash)
                {
                    Debug.Log($"[RunningActionManager] TryForcePlayAnimatorState: next transition targets '{stateName}' on layer {layer}", this);
                    return; // 전환 중이면 기다림
                }

                // HasState 체크로 상태 존재 여부 확인
                bool has = animator.HasState(layer, hash);
                Debug.Log($"[RunningActionManager] TryForcePlayAnimatorState: layer={layer} checking '{stateName}' (hash={hash}) hasState={has}", this);
                if (has)
                {
                    try
                    {
                        animator.Play(hash, layer, 0f);
                        animator.Update(0f);
                        animator.speed = 1f;
                        Debug.Log($"[RunningActionManager] Animator 즉시 상태 재생: {stateName} on layer {layer}", this);
                        return;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[RunningActionManager] Animator Play failed for {stateName} on layer {layer}: {ex.Message}", this);
                    }
                }
            }
        }

        // 2) HasState로 찾지 못했으면 Rebind + Play/CrossFade fallback 시도
        try
        {
            animator.Rebind();
            animator.Update(0f);
            Debug.Log("[RunningActionManager] Animator Rebind performed", this);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[RunningActionManager] Animator Rebind failed: {ex.Message}", this);
        }

        for (int layer = 0; layer < layers; layer++)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            AnimatorStateInfo next = animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer) : new AnimatorStateInfo();

            foreach (var stateName in candidateStateNames)
            {
                if (string.IsNullOrEmpty(stateName)) continue;

                if (suppressRecentJump && stateName.ToLower().Contains("jump")) continue;

                int hash = Animator.StringToHash(stateName);

                // 이미 현재 상태거나 전환 중이면 건너뜀
                if (current.shortNameHash == hash || (animator.IsInTransition(layer) && next.shortNameHash == hash))
                {
                    continue;
                }

                try
                {
                    animator.Play(hash, layer, 0f);
                    animator.Update(0f);
                    animator.speed = 1f;
                    Debug.Log($"[RunningActionManager] Animator Play fallback succeeded: {stateName} on layer {layer}", this);
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[RunningActionManager] Animator Play fallback failed for {stateName} on layer {layer}: {ex.Message}", this);
                }

                try
                {
                    animator.CrossFade(hash, 0f, layer, 0f);
                    animator.Update(0f);
                    animator.speed = 1f;
                    Debug.Log($"[RunningActionManager] Animator CrossFade fallback succeeded: {stateName} on layer {layer}", this);
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[RunningActionManager] Animator CrossFade fallback failed for {stateName} on layer {layer}: {ex.Message}", this);
                }
            }
        }

        Debug.Log("[RunningActionManager] TryForcePlayAnimatorState: 후보 상태를 찾지 못했습니다.", this);
    }

    private int GetPreferredStateHash(Animator animator, string[] candidateStateNames)
    {
        if (animator == null || candidateStateNames == null || candidateStateNames.Length == 0) return 0;
        int layers = Mathf.Max(1, animator.layerCount);
        for (int layer = 0; layer < layers; layer++)
        {
            foreach (var name in candidateStateNames)
            {
                if (string.IsNullOrEmpty(name)) continue;
                int hash = Animator.StringToHash(name);
                if (animator.HasState(layer, hash)) return hash;
            }
        }
        // fallback: return hash of first candidate so CrossFade/Play will at least try
        return Animator.StringToHash(candidateStateNames[0]);
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
                // spawnProbability 확률로 프리팹 생성
                float randomValue = Random.Range(0f, 100f);
                if (randomValue < spawnProbability)
                {
                    SpawnPrefab();
                }
            }
        }
    }

    /// <summary>
    /// Prefab을 2개 위치 중 하나에 생성합니다.
    /// </summary>
    private void SpawnPrefab()
    {
        // 2개 위치 중 랜덤하게 선택
        bool useSecondLocation = Random.value > 0.5f;
        Vector3 spawnOffset = useSecondLocation ? spawnOffset2 : spawnOffset1;
        
        // 스폰 위치 결정
        Vector3 spawnPos = spawnParent != null 
            ? spawnParent.position + spawnOffset 
            : spawnOffset;

        Quaternion spawnRot = spawnParent != null 
            ? spawnParent.rotation 
            : Quaternion.identity;

        GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPos, spawnRot, spawnParent);
        
        // 생성된 프리팹을 리스트에 추가
        spawnedPrefabs.Add(spawnedObj);

        Debug.Log($"Prefab spawned at {spawnPos} (Offset {(useSecondLocation ? 2 : 1)})");
    }
}
