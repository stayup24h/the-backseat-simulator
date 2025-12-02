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

    // --- UI 연결 ---
    [Header("UI")]
    public Slider concentrationSlider; // 인스펙터에서 연결할 슬라이더
    public CanvasGroup titleUICanvasGroup;
    [SerializeField] public GameObject pausePopup;
    [SerializeField] public GameObject itemGetPopup; // 아이템 획득 팝업 UI
    [SerializeField] public TMPro.TMP_Text itemDescriptionText; // 아이템 획득 팝업 설명 텍스트
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

    public bool isItemPopupActive = false; // 아이템 획득 팝업 활성화 상태

    private void OnEnable()
    {
        dialogueRunner.onDialogueStart.AddListener(() => isDialogueMode = true);
    }

    private void OnDisable()
    {
        dialogueRunner.onDialogueStart.RemoveListener(() => isDialogueMode = true);
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

        titleUICanvasGroup.DOFade(0f, 1.0f).OnComplete(() =>
        {
            titleUICanvasGroup.gameObject.SetActive(false);
        });



        pictureRectTransform.DOAnchorPos(pictureTargetTransform.position, moveDuration).SetEase(Ease.OutCirc);
        pictureRectTransform.DORotate(pictureTargetTransform.rotation, moveDuration).SetEase(Ease.OutCirc);
        pictureRectTransform.DOScale(pictureTargetTransform.scale, moveDuration).SetEase(Ease.OutCirc).OnComplete(() =>
        {
            Initialize();
            daynightController.Initialize();
            playerCtrl.Initialize();
            SoundManager.Instance.PlayPositionalNoise("noise", noisePosition, noiseSpatialBlend);
            rightHandTransform.DOAnchorPos(rightHandTargetTransform.position, 1).SetEase(Ease.OutCirc);
            rightHandTransform.DORotate(rightHandTargetTransform.rotation, 1);
            rightHandTransform.DOScale(rightHandTargetTransform.scale, 1);
        });
    }

    // --- UI 업데이트 ---
    private void UpdateConcentrationUI()
    {
        if ( concentrationSlider != null)
        {
            concentrationSlider.value = currentConcentration;
        }
    }

    // --- 다른 스크립트에서 호출할 함수들 ---

    /// <summary>
    /// 상호작용 시 호출되어 집중력을 감소시킵니다.
    /// </summary>
    public void DecreaseOnInteract()
    {
        if (isGameOver) return;

        currentConcentration -= decreasePerInteraction;
        UpdateConcentrationUI();
        Debug.Log("상호작용! 집중력 감소: " + currentConcentration);
    }

    private void Initialize()
    {
        // 게임 시작 시 집중력 초기화
        currentConcentration = maxConcentration;
        isDialogueMode = false;
        isPaused = false;

        // 슬라이더 UI 초기 설정
        if (concentrationSlider != null)
        {
            concentrationSlider.maxValue = maxConcentration;
            concentrationSlider.value = currentConcentration;
        }
        else
        {
            Debug.LogError("Concentration Slider가 GameManager에 연결되지 않았습니다!");
        }

        // pictures 초기화
        for (int i = 0; i < numPictures; i++)
        {
            pictures[i].isGot = false;
            pictures[i].pictureObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);
        }
        isGameOver = false;
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
        pausePopup.SetActive(false);
        isGameOver = false;
        isPaused = false;
        playerCtrl.UnlockMouseLook();
    }

    [YarnCommand("gotoTitle")]
    public void GotoTitle()
    {
        pausePopup.SetActive(false);
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
            titleUICanvasGroup.gameObject.SetActive(true);
            titleUICanvasGroup.DOFade(1f, 1.0f).OnComplete(()=>
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                //GameReset();
            });
        });
    }

    [YarnCommand("getPicture")]
    public void GetPicture(int pictureIndex)
    {
        if (pictureIndex < 0 || pictureIndex >= numPictures) return;
        pictures[pictureIndex].isGot = true;
        SoundManager.Instance.PlaySFX("GetPhoto");
        pictures[pictureIndex].pictureObject.GetComponent<Image>().DOFade(1f, 1.0f).OnComplete(()=>
        {
            CheckGameClear();
        });
        itemGetPopup.SetActive(true); // 아이템 획득 팝업 활성화
        if (itemDescriptionText != null) // itemDescriptionText가 할당되어 있다면
        {
            itemDescriptionText.text = pictures[pictureIndex].pictureDescription; // 팝업 설명 텍스트 설정
        }
        isItemPopupActive = true; // 팝업 활성화 상태로 설정
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
        yield return new WaitUntil(() => !isDialogueMode);

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
        itemGetPopup.SetActive(false); // 아이템 획득 팝업 비활성화
        isItemPopupActive = false; // 팝업 비활성화 상태로 설정
    }
}
