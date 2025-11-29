using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public struct TransformData
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
}

public class GameManager : SingletonBehaviour<GameManager>
{
    [Header("Concentration Settings")]
    public float maxConcentration = 100f; // 최대 집중력
    public float decreasePerSecond = 0.5f; // 초당 감소하는 집중력
    public float decreasePerInteraction = 5f; // 상호작용 시 감소하는 집중력
    
    public PlayerCtrl playerCtrl;
    
    private float currentConcentration; // 현재 집중력
    public bool isGameOver = true; // 게임 오버 상태 플래그
    
    // --- UI 연결 ---
    [Header("UI")]
    public Slider concentrationSlider; // 인스펙터에서 연결할 슬라이더
    public CanvasGroup titleUICanvasGroup;
    
    [SerializeField] public bool[] pictures;
    [SerializeField] public int numPictures = 3;
    
    [Header("Picture Movement")]
    public float MoveDuration = 2.0f;
    public RectTransform pictureRectTransform; // 인스펙터에서 이동시킬 Picture 오브젝트의 Transform을 할당
    public TransformData pictureStartTransform; // 시작 위치
    public TransformData pictureTargetTransform; // 이동할 목표 위치
    
    public RectTransform rightHandTransform;
    public TransformData rightHandStartTransform;
    public TransformData rightHandTargetTransform;
    
    public RectTransform leftHandTransform;
    public TransformData leftHandStartTransform;
    public TransformData leftHandTargetTransform;
    
    [Header("directing")]
    public DaynightController daynightController;
    void Start()
    {
       isGameOver = true;
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
    
    public void GameStart()
    {
      
        titleUICanvasGroup.DOFade(0f, 1.0f).OnComplete(() =>
        {
            titleUICanvasGroup.gameObject.SetActive(false);
        });
        
        
        
        pictureRectTransform.DOAnchorPos(pictureTargetTransform.position, MoveDuration).SetEase(Ease.OutCirc);
        pictureRectTransform.DORotate(pictureTargetTransform.rotation, MoveDuration).SetEase(Ease.OutCirc);
        pictureRectTransform.DOScale(pictureTargetTransform.scale, MoveDuration).SetEase(Ease.OutCirc).OnComplete(() =>
        {
            isGameOver = false;
            Initialize();
            daynightController.Initialize();
            playerCtrl.Initialize();
            SoundManager.Instance.PlayNoise("noise");
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
        isGameOver = true;
        Debug.Log("게임 오버: 집중력이 0이 되었습니다.");
        
        // 여기에 게임 패배 연출 (화면 암전, UI 표시 등) 로직을 추가합니다.
        // 예: UIManager.Instance.ShowGameOverScreen();
    }
}
