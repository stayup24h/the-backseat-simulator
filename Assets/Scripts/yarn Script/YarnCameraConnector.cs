using UnityEngine;
using Yarn.Unity;
using System.Threading;
using System.Threading.Tasks;

public class YarnCameraConnector : DialoguePresenterBase
{
    [Tooltip("씬에 있는 CameraDirector를 연결하세요.")]
    public CameraDirector cameraDirector;

    // [추가] 플레이어에 붙어있는 PlayerCtrl 스크립트를 연결합니다.
    [Tooltip("Player에 붙어있는 PlayerCtrl 스크립트를 연결하세요.")]
    public PlayerCtrl mouseLook;

    void Start()
    {
        if (cameraDirector == null)
        {
            cameraDirector = FindObjectOfType<CameraDirector>();
        }
        // [추가]
        if (mouseLook == null)
        {
            // Player 태그가 있는 오브젝트에서 스크립트를 찾습니다. (또는 FindObjectOfType)
            mouseLook = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerCtrl>();
        }
    }

    /// <summary>
    /// 대화가 시작될 때 호출됩니다.
    /// </summary>
    public override async YarnTask OnDialogueStartedAsync()
    {
        // [추가] FPS 마우스 입력을 잠급니다 (대화 모드 시작)
        if (mouseLook != null)
        {
            mouseLook.LockMouseLook();
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 대화가 끝났을 때 호출됩니다.
    /// </summary>
    public override async YarnTask OnDialogueCompleteAsync()
    {
        // [추가] FPS 마우스 입력을 다시 활성화합니다 (게임 모드 시작)
        if (mouseLook != null)
        {
            mouseLook.UnlockMouseLook();
        }

        // (선택 사항) 카메라를 기본 뷰로 되돌립니다.
        // if (cameraDirector != null)
        // {
        //    cameraDirector.ResetCameraFocus(); 
        // }
        await Task.CompletedTask;
    }

    // --- (RunLineAsync, RunOptionsAsync 등 나머지 코드는 그대로 둡니다) ---
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        if (cameraDirector != null)
        {
            string characterName = line.CharacterName;
            cameraDirector.FocusOnCharacter(characterName);
        }
        await Task.CompletedTask;
    }

    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken token)
    {
        await Task.CompletedTask;
        return null;
    }
}