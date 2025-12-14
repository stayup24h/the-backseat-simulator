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
    private float initialHandTweenDuration = 1f;
    private float handMoveDuration = 1f;

    // Tween 핸들 저장 (중복 실행 제어용)
    private Sequence rightHandSeq;
    private Sequence leftHandSeq;
    // 점프 시퀀스 및 점프 오브젝트 시퀀스 저장
    private Sequence jumpSeq;
    private Sequence jumpObjectSeq;
    private Vector3 jumpObjectStartPosition;
    private bool hasSavedJumpObjectStartPos = false;

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
            rightHandSeq.Kill(false);
            rightHandSeq = null;
        }
        if (leftHandSeq != null)
        {
            leftHandSeq.Kill(false);
            leftHandSeq = null;
        }
        // Ensure jump-related sequences are also killed to avoid leaving objects mid-animation
        if (jumpSeq != null)
        {
            jumpSeq.Kill(false);
            jumpSeq = null;
        }
        if (jumpObjectSeq != null)
        {
            jumpObjectSeq.Kill(false);
            jumpObjectSeq = null;
        }
    }

    // Kill only jump-related sequences (used when ending while preserving hand tweens)
    private void KillJumpSequences()
    {
        if (jumpSeq != null)
        {
            jumpSeq.Kill(false);
            jumpSeq = null;
        }
        if (jumpObjectSeq != null)
        {
            jumpObjectSeq.Kill(false);
            jumpObjectSeq = null;
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

        // 저장 (변수 할당을 먼저 진행)
        cameraDirector = cameraDir;
        playerCtrl = player;

        rightHandTransform = rightHand;
        rightHandStartTransform = rightStart;
        rightHandTargetTransform = rightTarget;

        leftHandTransform = leftHand;
        leftHandStartTransform = leftStart;
        leftHandTargetTransform = leftTarget;

        // 카메라 포커스 호출 (변수 할당 후에 호출)
        cameraDirector?.FocusRunningAction();
        
        // 왼손의 Animator 찾기 (자식 포함)
        if (leftHandTransform != null)
        {
            leftHandAnimator = leftHandTransform.GetComponentInChildren<Animator>();
            if (leftHandAnimator != null)
            {
                try
                {
                    leftHandAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    leftHandAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                    leftHandAnimator.Rebind();
                    leftHandAnimator.Update(0f);
                }
                catch (System.Exception) { }

                leftHandAnimator.SetTrigger(runParamName);
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

        // Ensure hand objects are active when running action starts

        // UI 활성화
        if (UIManager.Instance != null)
        {
         UIManager.Instance.SetRunningActionCanvasActive(true);
       UIManager.Instance.FadeOutInGameUI();
        }
        
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

        // Prefab 생성 중지 및 즉시 FadeOut
        StopSpawning();
     FadeOutAndDestroyPrefabs();

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

  // 점프 시퀀스만 정리(왼손/오른손 핸드는 시퀀스로 자연스럽게 복귀시키기 위해 그대로 둡니다)
  KillJumpSequences();

  // 왼손 애니메이션 정리
  if (leftHandAnimator != null)
        {
        leftHandAnimator.ResetTrigger(runParamName);
     leftHandAnimator.ResetTrigger(jumpParamName);
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
       // Ensure any jump tweens are killed and positions reset before finishing
                 if (jumpSeq != null) { jumpSeq.Kill(false); jumpSeq = null; }
            if (jumpObjectSeq != null) { jumpObjectSeq.Kill(false); jumpObjectSeq = null; }
          // position restoration will be handled in FinishEndAction to avoid snapping during tween
        FinishEndAction();
   });
            }
        else
        {
         // Ensure any jump tweens are killed and positions reset before finishing
    if (jumpSeq != null) { jumpSeq.Kill(false); jumpSeq = null; }
       if (jumpObjectSeq != null) { jumpObjectSeq.Kill(false); jumpObjectSeq = null; }
  // position restoration will be handled in FinishEndAction to avoid snapping during tween
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
    // Ensure any jump tweens are killed and positions reset before finishing
        if (jumpSeq != null) { jumpSeq.Kill(false); jumpSeq = null; }
      if (jumpObjectSeq != null) { jumpObjectSeq.Kill(false); jumpObjectSeq = null; }
            // position restoration will be handled in FinishEndAction to avoid snapping during tween
 FinishEndAction();
  });
            }
            else
            {
          // Ensure any jump tweens are killed and positions reset before finishing
         if (jumpSeq != null) { jumpSeq.Kill(false); jumpSeq = null; }
     if (jumpObjectSeq != null) { jumpObjectSeq.Kill(false); jumpObjectSeq = null; }
          // position restoration will be handled in FinishEndAction to avoid snapping during tween
        FinishEndAction();
    }
    }
    }

    /// <summary>
    /// 생성된 모든 프리팹을 FadeOut 애니메이션과 함께 제거합니다.
    /// </summary>
    private void FadeOutAndDestroyPrefabs()
    {
        foreach (GameObject prefab in spawnedPrefabs)
        {
         if (prefab != null)
     {
            CanvasGroup canvasGroup = prefab.GetComponent<CanvasGroup>();
 if (canvasGroup != null)
     {
        // CanvasGroup이 있으면 FadeOut 애니메이션
          canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
       {
     Destroy(prefab);
     });
     }
    else
     {
    // CanvasGroup이 없으면 바로 제거
             Destroy(prefab);
                }
            }
        }
   spawnedPrefabs.Clear();
    }

    /// <summary>
    /// 점프 제스처를 수행합니다 (왼손을 위로 올렸다가 내려옵니다).
    /// 점프 중에는 추가 점프가 불가능합니다.
    /// </summary>
    public void PerformJumpGesture()
    {
        if (!IsRunningAction || leftHandTransform == null) return;

        if (isJumping) return;

        isJumping = true;

        if (!string.IsNullOrEmpty(jumpSfxName) && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(jumpSfxName);
        }

        if (leftHandAnimator != null)
        {
            lastJumpTime = Time.time;

            try { leftHandAnimator.ResetTrigger(runParamName); } catch { }

            leftHandAnimator.SetTrigger(jumpParamName);

            try
            {
                int jumpStateHash = GetPreferredStateHash(leftHandAnimator, jumpStateNames);
                if (jumpStateHash != 0)
                {
                    leftHandAnimator.CrossFade(jumpStateHash, 0f, 0, 0f);
                    leftHandAnimator.Update(0f);
                }
            }
            catch (System.Exception) { }

            TryForcePlayAnimatorState(leftHandAnimator, jumpStateNames);

            try { leftHandAnimator.ResetTrigger(jumpParamName); } catch { }
        }

        Vector3 currentPos = leftHandTransform.anchoredPosition;

        // store jump sequence to allow safe cancellation if End is called mid-jump
        jumpSeq = DOTween.Sequence();
        jumpSeq
             .Append(leftHandTransform.DOAnchorPosY(currentPos.y + jumpHeight, jumpDuration).SetEase(Ease.OutQuad))
             .Append(leftHandTransform.DOAnchorPosY(currentPos.y, fallDuration).SetEase(Ease.InQuad))
             .OnComplete(() =>
             {
                 if (leftHandAnimator != null)
                 {
                     leftHandAnimator.SetTrigger(runParamName);

                     try { leftHandAnimator.ResetTrigger(jumpParamName); } catch { }

                     try
                     {
                         int runStateHash = GetPreferredStateHash(leftHandAnimator, runStateNames);
                         if (runStateHash != 0)
                         {
                             leftHandAnimator.CrossFade(runStateHash, 0f, 0, 0f);
                             leftHandAnimator.Update(0f);
                         }
                     }
                     catch (System.Exception) { }
                     TryForcePlayAnimatorState(leftHandAnimator, runStateNames);

                     try { leftHandAnimator.ResetTrigger(runParamName); } catch { }
                 }
                isJumping = false;
                if (jumpSeq != null) { jumpSeq.Kill(false); jumpSeq = null; }
             });

         if (jumpObject != null)
         {
            // store start position so we can restore if interrupted
            jumpObjectStartPosition = jumpObject.position;
            hasSavedJumpObjectStartPos = true;
            Vector3 jumpObjectCurrentPos = jumpObjectStartPosition;
            Vector3 jumpObjectTargetPos = new Vector3(jumpObjectCurrentPos.x, jumpObjectCurrentPos.y + jumpObjectHeight, jumpObjectCurrentPos.z);

            jumpObjectSeq = DOTween.Sequence();
            jumpObjectSeq
                .Append(jumpObject.DOMove(jumpObjectTargetPos, jumpDuration).SetEase(Ease.OutQuad))
                .Append(jumpObject.DOMove(jumpObjectCurrentPos, fallDuration).SetEase(Ease.InQuad))
                .OnComplete(() => { if (jumpObjectSeq != null) { jumpObjectSeq.Kill(false); jumpObjectSeq = null; } });
         }
        else
        {
            // no jump object
        }
    }

    /// <summary>
    /// 가능한 애니메이션 상태 이름 목록에서 존재하는 상태를 찾아 즉시 재생합니다.
    /// (Transition의 Exit Time으로 인해 트리거로 전환이 지연될 때를 보완)
    /// </summary>
    private void TryForcePlayAnimatorState(Animator animator, string[] candidateStateNames)
    {
        if (animator == null) return;

        if (!animator.enabled) animator.enabled = true;
        animator.Update(0f);

        int layers = Mathf.Max(1, animator.layerCount);
        bool suppressRecentJump = (Time.time - lastJumpTime) < jumpSuppressDuration;

        for (int layer = 0; layer < layers; layer++)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            AnimatorStateInfo next = animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer) : new AnimatorStateInfo();

            foreach (var stateName in candidateStateNames)
            {
                if (string.IsNullOrEmpty(stateName)) continue;

                if (suppressRecentJump && stateName.ToLower().Contains("jump")) continue;

                int hash = Animator.StringToHash(stateName);

                if (current.shortNameHash == hash) return;
                if (animator.IsInTransition(layer) && next.shortNameHash == hash) return;

                bool has = false;
                try
                {
                    has = animator.HasState(layer, hash);
                }
                catch (System.Exception) { }

                if (has)
                {
                    try
                    {
                        animator.Play(hash, layer, 0f);
                        animator.Update(0f);
                        animator.speed = 1f;
                        return;
                    }
                    catch (System.Exception) { }
                }
            }
        }

        try
        {
            animator.Rebind();
            animator.Update(0f);
        }
        catch (System.Exception) { }

        for (int layer = 0; layer < layers; layer++)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            AnimatorStateInfo next = animator.IsInTransition(layer) ? animator.GetNextAnimatorStateInfo(layer) : new AnimatorStateInfo();

            foreach (var stateName in candidateStateNames)
            {
                if (string.IsNullOrEmpty(stateName)) continue;

                if (suppressRecentJump && stateName.ToLower().Contains("jump")) continue;

                int hash = Animator.StringToHash(stateName);

                if (current.shortNameHash == hash || (animator.IsInTransition(layer) && next.shortNameHash == hash))
                {
                    continue;
                }

                try
                {
                    animator.Play(hash, layer, 0f);
                    animator.Update(0f);
                    animator.speed = 1f;
                    return;
                }
                catch (System.Exception) { }

                try
                {
                    animator.CrossFade(hash, 0f, layer, 0f);
                    animator.Update(0f);
                    animator.speed = 1f;
                    return;
                }
                catch (System.Exception) { }
            }
        }

        try
        {
            var controller = animator.runtimeAnimatorController;
            if (controller != null)
            {
                foreach (var clip in controller.animationClips)
                {
                    foreach (var candidate in candidateStateNames)
                    {
                        if (string.IsNullOrEmpty(candidate)) continue;
                        if (clip.name.Equals(candidate, System.StringComparison.OrdinalIgnoreCase))
                        {
                            int clipHash = Animator.StringToHash(clip.name);
                            for (int layer = 0; layer < layers; layer++)
                            {
                                try
                                {
                                    animator.CrossFade(clipHash, 0f, layer, 0f);
                                    animator.Update(0f);
                                    animator.speed = 1f;
                                    return;
                                }
                                catch (System.Exception) { }
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception) { }
    }

    private int GetPreferredStateHash(Animator animator, int candidateStateHash)
    {
        if (animator == null || animator.layerCount == 0) return 0;
        int layers = Mathf.Max(1, animator.layerCount);
        for (int layer = 0; layer < layers; layer++)
        {
            if (animator.HasState(layer, candidateStateHash)) return candidateStateHash;
        }
        // fallback: return 0 so CrossFade/Play will not attempt to switch to a non-existing state
        return 0;
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
    }

    private void FinishEndAction()
    {
   KillAndClearSequences();

        // Prefab 생성 중지는 이미 EndRunningAction()에서 처리됨
        
   IsRunningAction = false;
        // 종료 플래그 리셋
        isEnding = false;
   // 점프 플래그 리셋
     isJumping = false;

  // Ensure any jump sequences are killed and positions reset
        if (jumpSeq != null) { jumpSeq.Kill(false); jumpSeq = null; }
        if (jumpObjectSeq != null) { jumpObjectSeq.Kill(false); jumpObjectSeq = null; }
  // Restore jump object position if we saved it earlier
    if (jumpObject != null && hasSavedJumpObjectStartPos)
        {
         jumpObject.position = jumpObjectStartPosition;
            hasSavedJumpObjectStartPos = false;
        }

 // 플레이어 마우스 잠금 해제
        playerCtrl?.UnlockMouseLook();

      // 일시정지 해제
    if (GameManager.Instance != null)
      {
          GameManager.Instance.isPaused = false;
   }

   // In Game UI FadeIn
   if (UIManager.Instance != null)
        {
UIManager.Instance.FadeInInGameUI();
        }
   }
}