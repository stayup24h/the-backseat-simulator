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

        // 플레이어 마우스 잠금 해제
        playerCtrl?.UnlockMouseLook();

        // 일시정지 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = false;
        }

        // 모든 시퀀스 정리
        KillAndClearSequences();
    }
}
