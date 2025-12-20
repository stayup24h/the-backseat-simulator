using UnityEngine;
using System.Collections.Generic;
using DG.Tweening; // DOTween을 사용하기 위해 필수!

public class CameraDirector : MonoBehaviour
{
    [System.Serializable]
    public struct CharacterTarget
    {
        [Tooltip("고유한 캐릭터 ID (이름과 무관하게 언어와 상관없이 사용)")]
        public string characterId;
        
        [Tooltip("Yarn Spinner 스크립트에서 사용하는 캐릭터 이름 (참고용, 실제 매칭에는 사용 안 함)")]
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
    /// 캐릭터 ID로 해당 캐릭터를 바라보도록 명령하는 공개 함수
    /// Localization과 무관하게 작동합니다 (ID 기반)
    /// </summary>
    /// <param name="characterId">고유한 캐릭터 ID (예: "mother", "guard", "player")</param>
    public void FocusOnCharacter(string characterId)
    {
        // 1. playerCtrl이 없으면 조기 종료
        if (playerCtrl == null)
        {
            Debug.LogError("[CameraDirector] playerCtrl이 할당되지 않았습니다.");
            return;
        }

        // 2. characterTargets 리스트가 비어있으면 조기 종료
        if (characterTargets == null || characterTargets.Count == 0)
        {
            Debug.LogError("[CameraDirector] characterTargets 리스트가 비어있습니다.");
            return;
        }

        // 3. 캐릭터 ID가 비어있다면 (나레이션 등)
        if (string.IsNullOrEmpty(characterId) || characterId.Equals("Narrator", System.StringComparison.OrdinalIgnoreCase))
        {
            // 나레이션일 때는 마우스 락 해제
            playerCtrl.UnlockMouseLook();
            return;
        }

        // 4. 등록된 캐릭터 리스트에서 ID와 일치하는 항목 찾기
        CharacterTarget? foundTarget = null;
        foreach (var entry in characterTargets)
        {
            if (!string.IsNullOrEmpty(entry.characterId) && 
                entry.characterId.Equals(characterId, System.StringComparison.OrdinalIgnoreCase))
            {
                foundTarget = entry;
                break;
            }
        }

        // 5. 타겟을 찾았으면 카메라 방향 전환
        if (foundTarget.HasValue && foundTarget.Value.targetTransform != null)
        {
            var target = foundTarget.Value;
            
            // --- DOTween 로직 시작 ---
            playerCtrl.LockMouseLook();
            
            // a. 타겟의 실제 위치 (오프셋 포함) 계산
            Vector3 targetPosition = target.targetTransform.position + target.offset;

            // b. 카메라 위치에서 타겟 위치를 바라보는 방향 벡터 계산
            Vector3 direction = targetPosition - cameraTransform.position;

            // c. 방향이 유효한지 확인 (거리가 0이 아님)
            if (direction.sqrMagnitude < 0.001f)
            {
                Debug.LogWarning($"[CameraDirector] '{characterId}'의 타겟 위치가 카메라와 너무 가깝습니다.");
                return;
            }

            // d. 해당 방향을 바라보는 목표 회전값(Quaternion) 계산
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // e. 진행 중인 트윈 중지
            cameraTransform.DOKill(true);

            // f. DOTween으로 카메라를 부드럽게 회전시킵니다.
            cameraTransform.DORotateQuaternion(targetRotation, tweenDuration)
                           .SetEase(easeType);
            
            return;
        }

        // 6. 리스트에 등록되지 않은 캐릭터 ID인 경우
        Debug.LogWarning($"[CameraDirector] '{characterId}'에 해당하는 타겟을 찾을 수 없습니다. 등록된 ID: {string.Join(", ", System.Linq.Enumerable.Select(characterTargets, c => c.characterId))}");
    }

    public void FocusRunningAction()
    {
        playerCtrl.LockMouseLook();
        Vector3 targetPosition = characterTargets[3].targetTransform.position + characterTargets[3].offset;
        Vector3 direction = targetPosition - cameraTransform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        cameraTransform.DOKill(true);
        cameraTransform.DORotateQuaternion(targetRotation, tweenDuration).SetEase(easeType); // 설정한 Ease 타입 적용
    }
}