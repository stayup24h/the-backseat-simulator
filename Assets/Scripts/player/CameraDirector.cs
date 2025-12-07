using UnityEngine;
using System.Collections.Generic;
using DG.Tweening; // DOTween을 사용하기 위해 필수!

public class CameraDirector : MonoBehaviour
{
    [System.Serializable]
    public struct CharacterTarget
    {
        [Tooltip("Yarn Spinner 스크립트에서 사용하는 캐릭터 이름 (예: Player, Guard)")]
        public string yarnCharacterName;
        
        [Tooltip("카메라가 바라볼 실제 캐릭터의 Transform")]
        public Transform targetTransform;
        
        [Tooltip("캐릭터의 발이 아닌 머리를 보게 하기 위한 오프셋 (예: 0, 1.6, 0)")]
        public Vector3 offset;
    }

    [Tooltip("제어할 메인 카메라. 비어있으면 Camera.main을 사용합니다.")]
    public Transform cameraTransform;

    [Tooltip("카메라가 회전하는 데 걸리는 시간(초)")]
    public float tweenDuration = 0.5f; // rotationSpeed 대신 사용합니다.

    [Tooltip("카메라 회전 시 사용할 Ease 타입")]
    public Ease easeType = Ease.OutQuad;

    [Tooltip("씬에 있는 모든 캐릭터 타겟을 여기에 등록합니다.")]
    public List<CharacterTarget> characterTargets;
    // DOTween은 Update가 필요 없으므로 currentTarget, currentOffset 변수가 필요 없습니다.
    // private Transform currentTarget;
    // private Vector3 currentOffset;

    [Tooltip("씬에 있는 playerCtrl를 연결하세요.")]
    public PlayerCtrl playerCtrl;
    
    void Awake()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    /// <summary>
    /// 이 캐릭터를 바라보도록 명령하는 공개 함수 (다른 스크립트에서 호출됨)
    /// </summary>
    /// <param name="characterName">Yarn Spinner에서 받은 캐릭터 이름</param>
    public void FocusOnCharacter(string characterName)
    {
        // 1. 캐릭터 이름이 비어있다면 (나레이션 등)
        if (string.IsNullOrEmpty(characterName))
        {
            // TODO: 나레이션일 때 기본 카메라 위치로 되돌리기
            // cameraTransform.DOKill(); // 진행 중인 트윈 중지
            playerCtrl.UnlockMouseLook();
            return;
        }

        // 2. 등록된 캐릭터 리스트에서 일치하는 이름 찾기
        foreach (var entry in characterTargets)
        {
            if (entry.yarnCharacterName == characterName)
            {
                // --- DOTween 로직 시작 ---
                playerCtrl.LockMouseLook();
                // 1. 타겟의 실제 위치 (오프셋 포함) 계산
                Vector3 targetPosition = entry.targetTransform.position + entry.offset;

                // 2. 카메라 위치에서 타겟 위치를 바라보는 방향 벡터 계산
                Vector3 direction = targetPosition - cameraTransform.position;

                // 3. 해당 방향을 바라보는 목표 회전값(Quaternion) 계산
                Quaternion targetRotation = Quaternion.LookRotation(direction);


                cameraTransform.DOKill(true); // true: 즉시 현재 위치에서 중지

                // 5. DOTween으로 카메라를 부드럽게 회전시킵니다.
                cameraTransform.DORotateQuaternion(targetRotation, tweenDuration)
                               .SetEase(easeType); // 설정한 Ease 타입 적용
                
                return; // 타겟을 찾았으므로 함수 종료
            }
        }

        // 3. 리스트에 등록되지 않은 캐릭터 이름인 경우
        Debug.LogWarning($"[CameraDirector] '{characterName}'에 해당하는 타겟을 찾을 수 없습니다.");
    }
}