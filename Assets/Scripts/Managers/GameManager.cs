using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Yarn.Unity;
using System.Collections;

[System.Serializable]
public struct TransformData
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
}

[System.Serializable]
public class PictureInfo
{
    public bool isGot;
    public GameObject pictureObject;
    public string pictureDescription;
}


public class GameManager : SingletonBehaviour<GameManager>
{
    [Header("Concentration Settings")]
    public float maxConcentration = 100f; // 최대 집중력
    public float decreasePerSecond = 0.5f; // 초당 감소하는 집중력
    public float decreasePerInteraction = 5f; // 상호작용 시 감소하는 집중력

    public PlayerCtrl playerCtrl;

    public float currentConcentration; // 현재 집중력
    public bool isGameOver = true; // 게임 오버 상태 플래그
    public bool isDialogueMode;
    public bool isPaused;
    public bool isInitializationInProgress; // 초기화 진행 중 플래그
    // public bool isRunningAction; // RunningActionManager로 분리되어 제거
    public CameraDirector cameraDirector;

    // --- UI 관련 처리는 UIManager로 위임됩니다 ---
    [Header("UI")]
    [SerializeField] public DialogueRunner dialogueRunner;

    [SerializeField] public int numPictures = 3;
    [SerializeField] public PictureInfo[] pictures = new PictureInfo[3];


    [Header("Picture Movement")]
    public float moveDuration = 2.0f;
    public RectTransform pictureRectTransform; // 인스펙터에서 이동시킬 Picture 오브젝트의 Transform을 할당
    public TransformData pictureStartTransform; // 시작 위치
    public TransformData pictureTargetTransform; // 이동할 목표 위치

    public RectTransform rightHandTransform;
    public TransformData rightHandStartTransform;
    public TransformData rightHandTargetTransform;

    public RectTransform leftHandTransform;
    public TransformData leftHandStartTransform;
    public TransformData leftHandTargetTransform;

    [Header("Sound Settings")]
    public Vector3 noisePosition; // Noise 사운드를 재생할 위치
    [Range(0f, 1f)] public float noiseSpatialBlend = 0.4f; // Noise의 Spatial Blend 값

    [Header("directing")]
    public DaynightController daynightController;

    private void OnEnable()
    {
        // dialogueRunner 또는 onDialogueStart가 null일 수 있으므로 안전하게 구독합니다.
        dialogueRunner?.onDialogueStart?.AddListener(HandleDialogueStart);
    }

    private void OnDisable()
    {
        // 안전하게 구독 해제
        dialogueRunner?.onDialogueStart?.RemoveListener(HandleDialogueStart);
    }

    private void HandleDialogueStart()
    {
        isDialogueMode = true;
    }

    void Start()
    {
       isGameOver = true;

       Cursor.lockState = CursorLockMode.Confined; // 커서 숨기기
       Cursor.visible = true;

       if (pictureRectTransform != null)
       {
           pictureRectTransform.anchoredPosition = pictureStartTransform.position;
           pictureRectTransform.eulerAngles = pictureStartTransform.rotation;
           pictureRectTransform.localScale = pictureStartTransform.scale;
       }
    }

    // Update is called once per frame
    void Update()
    {
        if (isGameOver) return;

        // 1. 시간에 따라 집중력 감소
        if (currentConcentration > 0)
        {
            currentConcentration -= decreasePerSecond * Time.deltaTime;
            UpdateConcentrationUI(); // UI 업데이트
        }
        else
        {
            // 2. 집중력이 0이 되면 게임 오버 처리
            currentConcentration = 0;
            HandleGameOver();
        }
    }

    public void StartBtn()
    {
        dialogueRunner.StartDialogue("RecallWithPhoto");
    }

    [YarnCommand("gameStart")]
    public void GameStart()
    {
        isPaused = true;
        // UIManager에 타이틀 페이드와 인게임 UI 활성화를 위임
        if (UIManager.Instance != null)
        {
            UIManager.Instance.FadeOutTitleUI();
            UIManager.Instance.SetInGameUIActive(true);
        }

        MovePictureToTarget(() =>
        {
            InitializeGameComponents();
            PlayInitialSounds();
            MoveHandsToTarget(() =>
            {
                dialogueRunner.StartDialogue("tireddrive");
                isPaused = false;
                isInitializationInProgress = false;
            });
        });
    }

    private void MovePictureToTarget(System.Action onComplete)
    {
        pictureRectTransform.DOAnchorPos(pictureTargetTransform.position, moveDuration).SetEase(Ease.OutCirc);
        pictureRectTransform.DORotate(pictureTargetTransform.rotation, moveDuration).SetEase(Ease.OutCirc);
        pictureRectTransform.DOScale(pictureTargetTransform.scale, moveDuration).SetEase(Ease.OutCirc).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    private void InitializeGameComponents()
    {
        Initialize();
        daynightController.Initialize();
        playerCtrl.Initialize();
    }

    private void PlayInitialSounds()
    {
        SoundManager.Instance.PlayPositionalNoise("noise", noisePosition, noiseSpatialBlend);
    }

    private void MoveHandsToTarget(System.Action onComplete)
    {
        rightHandTransform.DOAnchorPos(rightHandTargetTransform.position, 1).SetEase(Ease.OutCirc);
        rightHandTransform.DORotate(rightHandTargetTransform.rotation, 1);
        rightHandTransform.DOScale(rightHandTargetTransform.scale, 1).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    // --- UI 업데이트 ---
    private void UpdateConcentrationUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetConcentration(currentConcentration, maxConcentration);
        }
    }

    // --- 다른 스크립트에서 호출할 함수들 ---

    /// <summary>
    /// 상호작용 시 호출되어 집중력을 감소시킵니다.
    /// </summary>
    public void DecreaseOnInteract()
    {
        if (isGameOver) return;
        if (isInitializationInProgress) return;

        currentConcentration -= decreasePerInteraction;
        UpdateConcentrationUI();
        Debug.Log("상호작용! 집중력 감소: " + currentConcentration);
    }

    private void Initialize()
    {
        isInitializationInProgress = true;
        Debug.Log("게임 초기화 시작...");

        // 게임 시작 시 집중력 초기화
        currentConcentration = maxConcentration;
        isDialogueMode = false;
        isPaused = false;

        // 슬라이더 UI 초기 설정을 UIManager로 위임
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetConcentration(currentConcentration, maxConcentration);
        }
        else
        {
            Debug.LogError("UIManager 인스턴스가 없습니다. Concentration UI를 설정할 수 없습니다.");
        }

        // pictures 초기화
        for (int i = 0; i < numPictures; i++)
        {
            pictures[i].isGot = false;
            pictures[i].pictureObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        }
        isGameOver = false;

        // 초기화 완료
        Debug.Log("게임 초기화 완료!");
    }

    public float ConcentrationRatio
    {
        get
        {
            // 0으로 나누기 방지
            if (maxConcentration == 0) return 0f;

            // 현재값 / 최대값 (예: 50 / 100 = 0.5)
            return currentConcentration / maxConcentration;
        }
    }

    // --- 게임 오버 처리 ---
    private void HandleGameOver()
    {
        Debug.Log("게임 오버: 집중력이 0이 되었습니다.");
        DialogueManager.Instance.StartDialogue("GameOver");
        isGameOver = true;
    }

    public void GameReset()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void GameExit()
    {
        Application.Quit();
    }

    public void Nope()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPausePopupActive(false);
        }
        isGameOver = false;
        isPaused = false;
        playerCtrl.UnlockMouseLook();
    }

    [YarnCommand("gotoTitle")]
    public void GotoTitle()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPausePopupActive(false);
            UIManager.Instance.SetInGameUIActive(true);
        }
        
        DialogueManager.Instance.StartDialogue("endDialogue");
            
        isGameOver = true;
        isPaused = false;
        SoundManager.Instance.BGMFadeOut();
        SoundManager.Instance.NoiseFadeOut();
        pictureRectTransform.DOAnchorPos(pictureStartTransform.position, moveDuration).SetEase(Ease.OutCirc);
        pictureRectTransform.DORotate(pictureStartTransform.rotation, moveDuration).SetEase(Ease.OutCirc);
        pictureRectTransform.DOScale(pictureStartTransform.scale, moveDuration).SetEase(Ease.OutCirc).OnComplete(() =>
        {
            for(int i = 0; i < numPictures; i++)
            {
                pictures[i].isGot = false;
                pictures[i].pictureObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            }

            rightHandTransform.DOAnchorPos(rightHandStartTransform.position, 1).SetEase(Ease.OutCirc);
            rightHandTransform.DORotate(rightHandStartTransform.rotation, 1);
            rightHandTransform.DOScale(rightHandStartTransform.scale, 1);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.FadeInTitleUI(onComplete: () =>
                {
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                    //GameReset();
                });
            }
            else
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
        });
    }

    [YarnCommand("getPicture")]
    public void GetPicture(int pictureIndex)
    {
        if (pictureIndex < 0 || pictureIndex >= numPictures || pictures[pictureIndex].isGot) return;
        if (isInitializationInProgress) return;
        pictures[pictureIndex].isGot = true;
        SoundManager.Instance.PlaySFX("GetPhoto");
        pictures[pictureIndex].pictureObject.GetComponent<Image>().DOFade(1f, 1.0f).OnComplete(() =>
        {
            CheckGameClear();
        });

        // UIManager를 통해 아이템 팝업을 표시
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowItemPopup(pictures[pictureIndex].pictureDescription);
        }
    }

    private void CheckGameClear()
    {
        for (int i = 0; i < numPictures; i++)
        {
            if (!pictures[i].isGot)
            {
                return;
            }
        }

        StartCoroutine(ClearGameCoroutine());
    }

    private IEnumerator ClearGameCoroutine()
    {
        // UIManager에서 팝업 활성 상태를 확인하도록 변경
        yield return new WaitUntil(() => !isDialogueMode && (UIManager.Instance == null || !UIManager.Instance.IsItemPopupActive));

        Debug.Log("모든 사진 획득! 게임 클리어!");
        dialogueRunner.StartDialogue("Ending");
    }



    [YarnCommand("dialogueEnd")]
    public void DialogueEnd()
    {
        isDialogueMode = false;
    }

    public void CloseItemPopup()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseItemPopup();
        }
    }

    [YarnCommand("RunningActionStart")]
    public void StartRunningAction()
    {
        if (isPaused) return;

        if (RunningActionManager.Instance == null)
        {
            Debug.LogWarning("RunningActionManager 인스턴스가 없습니다. RunningAction을 시작할 수 없습니다.");
            return;
        }

        // RunningActionManager로 위임
        RunningActionManager.Instance.StartRunningAction(cameraDirector, playerCtrl,
            rightHandTransform, rightHandStartTransform, rightHandTargetTransform,
            leftHandTransform, leftHandStartTransform, leftHandTargetTransform);
    }
    
    /// <summary>
    /// 특정 액션이 종료될 때 호출하여 isRunningAction을 false로 설정합니다.
    /// </summary>
    [YarnCommand("RunningActionEnd")]
    public void EndRunningAction()
    {
        if (isPaused) return;

        if (RunningActionManager.Instance == null)
        {
            Debug.LogWarning("RunningActionManager 인스턴스가 없습니다. RunningAction을 종료할 수 없습니다.");
            return;
        }

        RunningActionManager.Instance.EndRunningAction();
    }

    [YarnCommand("getTired")]
    public void GetTired(int value)
    {
        currentConcentration += value;
        currentConcentration = Mathf.Min(maxConcentration, currentConcentration);
    }
}