using UnityEngine;
using Yarn.Unity;
using System.Threading;
using System.Threading.Tasks; // Task.CompletedTask를 위해 필요

public class YarnCameraConnector : DialoguePresenterBase
{
    [Tooltip("씬에 있는 CameraDirector를 연결하세요.")]
    public CameraDirector cameraDirector;

    void Start()
    {
        if (cameraDirector == null)
        {
            cameraDirector = FindObjectOfType<CameraDirector>();
        }
    }

    // --- DialoguePresenterBase의 필수 구현 메서드 ---

    /// <summary>
    /// DialogueRunner가 라인을 전달할 때 호출됩니다.
    /// </summary>
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        // 이 로그가 찍히는지 확인하는 것이 가장 중요합니다.
        Debug.Log($"[Connector] RunLineAsync 호출됨. 캐릭터 이름: '{line.CharacterName}'");

        if (cameraDirector != null)
        {
            string characterName = line.CharacterName;
            cameraDirector.FocusOnCharacter(characterName);
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 옵션이 전달될 때 호출됩니다.
    /// </summary>
    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken token)
    {
        // [수정됨] YarnTask.Empty -> Task.CompletedTask
        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// 대화가 시작될 때 호출됩니다.
    /// </summary>
    public override async YarnTask OnDialogueStartedAsync()
    {
        // [수정됨] YarnTask.Empty -> Task.CompletedTask
        await Task.CompletedTask;
    }

    /// <summary>
    /// 대화가 끝났을 때 호출됩니다.
    /// </summary>
    public override async YarnTask OnDialogueCompleteAsync()
    {
        // [수정됨] YarnTask.Empty -> Task.CompletedTask
        // if (cameraDirector != null)
        // {
        //    cameraDirector.ResetCameraFocus();
        // }
        await Task.CompletedTask;
    }
}