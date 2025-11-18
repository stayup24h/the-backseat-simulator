using UnityEngine;
using Yarn.Unity;
using System.Threading;
using System.Threading.Tasks;

public class YarnCameraConnector : DialoguePresenterBase
{
    [Tooltip("씬에 있는 CameraDirector를 연결하세요.")]
    public CameraDirector cameraDirector;

    [Tooltip("Player에 붙어있는 PlayerCtrl 스크립트를 연결하세요.")]
    public PlayerCtrl mouseLook;

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

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        // [핵심] 일반 대사가 나올 때는 커서를 숨깁니다.
        // (이전에 선택지를 고르느라 커서가 켜져 있었다면 여기서 다시 꺼줍니다)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (cameraDirector != null)
        {
            cameraDirector.FocusOnCharacter(line.CharacterName);
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