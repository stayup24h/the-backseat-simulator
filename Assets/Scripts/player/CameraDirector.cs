using UnityEngine;
using System.Collections.Generic;

public class CameraDirector : MonoBehaviour
{
    // [System.Serializable]은 이 구조체를 인스펙터 창에 노출시킵니다.
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

    [Tooltip("카메라 회전 속도")]
    public float rotationSpeed = 5f;

    [Tooltip("씬에 있는 모든 캐릭터 타겟을 여기에 등록합니다.")]
    public List<CharacterTarget> characterTargets;

    private Transform currentTarget; // 현재 바라보고 있는 타겟
    private Vector3 currentOffset;   // 현재 타겟의 오프셋

    void Awake()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // 현재 타겟이 설정되어 있다면
        if (currentTarget != null)
        {
            // 타겟의 실제 위치 (오프셋 포함)
            Vector3 targetPosition = currentTarget.position + currentOffset;

            // 카메라 위치에서 타겟 위치를 바라보는 방향 벡터
            Vector3 direction = targetPosition - cameraTransform.position;

            // 해당 방향을 바라보는 목표 회전값(Quaternion) 계산
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 현재 카메라 회전값에서 목표 회전값으로 부드럽게 보간 (Slerp)
            cameraTransform.rotation = Quaternion.Slerp(
                cameraTransform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }

    /// <summary>
    /// 이 캐릭터를 바라보도록 명령하는 공개 함수 (다른 스크립트에서 호출됨)
    /// </summary>
    /// <param name="characterName">Yarn Spinner에서 받은 캐릭터 이름</param>
    public void FocusOnCharacter(string characterName)
    {
        Debug.Log($"[Director] FocusOnCharacter 호출됨. 전달받은 이름: '{characterName}'");

        if (string.IsNullOrEmpty(characterName))
        {
            currentTarget = null; 
            return;
        }

        foreach (var entry in characterTargets)
        {
            if (entry.yarnCharacterName == characterName)
            {
                // 이 로그가 찍히면 이름이 일치하는 타겟을 찾은 것입니다.
                Debug.Log($"[Director] 일치하는 타겟 찾음: {entry.targetTransform.name}");
                currentTarget = entry.targetTransform;
                currentOffset = entry.offset;
                return;
            }
        }

        // 이 경고가 뜬다면, 이름은 전달됐지만 리스트에 등록된 이름과 일치하는 것이 없는 것입니다.
        Debug.LogWarning($"[CameraDirector] '{characterName}'에 해당하는 타겟을 찾을 수 없습니다.");
        currentTarget = null;
    }

    /// <summary>
    /// (선택 사항) 대화가 끝났을 때 카메라를 원래 위치로 되돌리는 함수
    /// </summary>
    public void ResetCameraFocus()
    {
        currentTarget = null;
        // TODO: 카메라를 기본 위치/회전으로 되돌리는 로직을 추가할 수 있습니다.
    }
}