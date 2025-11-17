using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameManager : SingletonBehaviour<GameManager>
{
    [Header("Concentration Settings")]
    public float maxConcentration = 100f; // 최대 집중력
    public float decreasePerSecond = 0.5f; // 초당 감소하는 집중력
    public float decreasePerInteraction = 5f; // 상호작용 시 감소하는 집중력

    private float currentConcentration; // 현재 집중력
    private bool isGameOver = false; // 게임 오버 상태 플래그

    // --- UI 연결 ---
    [Header("UI")]
    public Slider concentrationSlider; // 인스펙터에서 연결할 슬라이더
    
    [SerializeField] public bool[] pictures;
    [SerializeField] public int numPictures = 3;
    
    void Start()
    {
        Initialize();
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
    
    // --- UI 업데이트 ---
    private void UpdateConcentrationUI()
    {
        if (concentrationSlider != null)
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
    
    // --- 게임 오버 처리 ---
    private void HandleGameOver()
    {
        isGameOver = true;
        Debug.Log("게임 오버: 집중력이 0이 되었습니다.");
        
        // 여기에 게임 패배 연출 (화면 암전, UI 표시 등) 로직을 추가합니다.
        // 예: UIManager.Instance.ShowGameOverScreen();
    }
}
