using UnityEngine;
using Yarn.Unity;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public class YarnCameraConnector : DialoguePresenterBase
{
    [Tooltip("씬에 있는 CameraDirector를 연결하세요.")]
    public CameraDirector cameraDirector;

    [Tooltip("Player에 붙어있는 PlayerCtrl 스크립트를 연결하세요.")]
    public PlayerCtrl mouseLook;

    [System.Serializable]
    public struct CharacterNameMapping
    {
        [Tooltip("Yarn 스크립트의 원본 캐릭터 이름 (고정)")]
        public string yarnCharacterName;
        
        [Tooltip("CameraDirector의 characterId")]
        public string characterId;
    }

    [Tooltip("Yarn 캐릭터 이름을 CameraDirector ID로 변환하는 매핑")]
    public List<CharacterNameMapping> characterMappings;

    void Start()
    {
        if (cameraDirector == null) cameraDirector = FindObjectOfType<CameraDirector>();
        
        if (mouseLook == null)
            mouseLook = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerCtrl>();
    }

    public override async YarnTask OnDialogueStartedAsync()
    {
        // 1. 대화 시작: 시점 잠금 (FPS 회전 멈춤)
        if (mouseLook != null) mouseLook.LockMouseLook();
        
        // 시작할 때는 커서를 일단 숨겨둡니다 (대사만 읽는 상태)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        await Task.CompletedTask;
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        // 2. 대화 종료: 시점 잠금 해제 (게임 모드로 복귀)
        if (mouseLook != null) mouseLook.UnlockMouseLook();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Yarn 캐릭터 이름을 CameraDirector의 ID로 변환합니다.
    /// </summary>
    private string GetCharacterIdFromYarnName(string yarnCharacterName)
    {
        if (string.IsNullOrEmpty(yarnCharacterName)) return yarnCharacterName;

        // characterMappings에서 일치하는 매핑 찾기
        if (characterMappings != null)
        {
            foreach (var mapping in characterMappings)
            {
                if (!string.IsNullOrEmpty(mapping.yarnCharacterName) && 
                    mapping.yarnCharacterName.Equals(yarnCharacterName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return mapping.characterId;
                }
            }
        }

        // 매핑이 없으면 원본 이름을 그대로 반환 (fallback)
        Debug.LogWarning($"[YarnCameraConnector] '{yarnCharacterName}'에 대한 매핑을 찾을 수 없습니다.");
        return yarnCharacterName;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        // [핵심] 일반 대사가 나올 때는 커서를 숨깁니다.
        // (이전에 선택지를 고르느라 커서가 켜져 있었다면 여기서 다시 꺼줍니다)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        GameManager.Instance.isDialogueMode = true;
        
        if (cameraDirector != null && !string.IsNullOrEmpty(line.CharacterName))
        {
            // Yarn 캐릭터 이름을 ID로 변환
            string characterId = GetCharacterIdFromYarnName(line.CharacterName);
            cameraDirector.FocusOnCharacter(characterId);
        }
        await Task.CompletedTask;
    }

    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken token)
    {
        // [핵심] 선택지가 등장하면 커서를 보이게 하고 풀어줍니다.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // null을 반환하면 Yarn Spinner가 이 뷰(View)는 선택지 UI를 직접 그리지 않는다고 판단하고,
        // 다른 뷰(Option List View 등)가 처리할 때까지 기다려줍니다.
        return null;
    }
}