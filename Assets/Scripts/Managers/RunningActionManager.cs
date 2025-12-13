using UnityEngine;
using DG.Tweening;

public class RunningActionManager : SingletonBehaviour<RunningActionManager>
{
    public bool IsRunningAction { get; private set; }

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

    public void StartRunningAction(CameraDirector cameraDir, PlayerCtrl player,
        RectTransform rightHand, TransformData rightStart, TransformData rightTarget,
        RectTransform leftHand, TransformData leftStart, TransformData leftTarget)
    {
        if (IsRunningAction) return; // 이미 실행 중이면 무시

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

        // 오른손을 초기 위치로 이동시키는 트윈
        if (rightHandTransform != null)
        {
            rightHandTransform.DOAnchorPos(rightHandStartTransform.position, initialHandTweenDuration).SetEase(Ease.OutCirc);
            rightHandTransform.DORotate(rightHandStartTransform.rotation, initialHandTweenDuration).SetEase(Ease.OutCirc);
            rightHandTransform.DOScale(rightHandStartTransform.scale, initialHandTweenDuration).SetEase(Ease.OutCirc).OnComplete(() =>
            {
                // 왼손을 타겟으로 이동
                if (leftHandTransform != null)
                {
                    leftHandTransform.DOAnchorPos(leftHandTargetTransform.position, handMoveDuration).SetEase(Ease.OutCirc);
                    leftHandTransform.DORotate(leftHandTargetTransform.rotation, handMoveDuration).SetEase(Ease.OutCirc);
                    leftHandTransform.DOScale(leftHandTargetTransform.scale, handMoveDuration).SetEase(Ease.OutCirc);
                }

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
            });
        }
        else
        {
            // 오른손 트랜스폼이 없으면 즉시 활성화 처리
            if (leftHandTransform != null)
            {
                leftHandTransform.DOAnchorPos(leftHandTargetTransform.position, handMoveDuration).SetEase(Ease.OutCirc);
                leftHandTransform.DORotate(leftHandTargetTransform.rotation, handMoveDuration).SetEase(Ease.OutCirc);
                leftHandTransform.DOScale(leftHandTargetTransform.scale, handMoveDuration).SetEase(Ease.OutCirc);
            }

            IsRunningAction = true;
            if (UIManager.Instance != null) UIManager.Instance.SetRunningActionCanvasActive(true);
            cameraDirector?.FocusRunningAction();
            if (GameManager.Instance != null) GameManager.Instance.isPaused = false;
        }
    }

    public void EndRunningAction()
    {
        if (!IsRunningAction) return;

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

        // 왼손을 시작 위치로 이동
        if (leftHandTransform != null)
        {
            leftHandTransform.DOAnchorPos(leftHandStartTransform.position, initialHandTweenDuration).SetEase(Ease.OutCirc).SetAutoKill(false);
            leftHandTransform.DORotate(leftHandStartTransform.rotation, initialHandTweenDuration).SetEase(Ease.OutCirc).SetAutoKill(false);
            leftHandTransform.DOScale(leftHandStartTransform.scale, initialHandTweenDuration).SetEase(Ease.OutCirc).SetAutoKill(false).OnComplete(() =>
            {
                // 오른손을 타겟으로 복귀
                if (rightHandTransform != null)
                {
                    rightHandTransform.DOAnchorPos(rightHandTargetTransform.position, handMoveDuration).SetEase(Ease.OutCirc);
                    rightHandTransform.DORotate(rightHandTargetTransform.rotation, handMoveDuration).SetEase(Ease.OutCirc);
                    rightHandTransform.DOScale(rightHandTargetTransform.scale, handMoveDuration).SetEase(Ease.OutCirc).OnComplete(() =>
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
                rightHandTransform.DOAnchorPos(rightHandTargetTransform.position, handMoveDuration).SetEase(Ease.OutCirc);
                rightHandTransform.DORotate(rightHandTargetTransform.rotation, handMoveDuration).SetEase(Ease.OutCirc);
                rightHandTransform.DOScale(rightHandTargetTransform.scale, handMoveDuration).SetEase(Ease.OutCirc).OnComplete(() =>
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

        // 플레이어 마우스 잠금 해제
        playerCtrl?.UnlockMouseLook();

        // 일시정지 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = false;
        }
    }
}
