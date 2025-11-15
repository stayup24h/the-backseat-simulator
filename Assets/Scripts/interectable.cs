using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("UI 설정")]
    [Tooltip("UI에 표시될 상호작용 프롬프트 메시지")]
    public string promptMessage = "[E] 상호작용";

    [Header("하이라이트 레이어 설정")]
    [Tooltip("오브젝트가 하이라이트될 때 변경될 레이어 이름")]
    public string highlightLayerName = "Outlined Object";

    [Header("이벤트")]
    [Tooltip("상호작용 시 실행될 이벤트를 여기에 연결합니다.")]
    public UnityEvent onInteract;

    // --- 내부 변수 ---
    private int originalLayerIndex; // 오브젝트의 원래 레이어
    private int highlightLayerIndex; // "Highlight" 레이어의 숫자 인덱스
    private bool _isHighlighted = false;

    void Awake()
    {
        // 1. 오브젝트의 원본 레이어를 저장합니다.
        originalLayerIndex = gameObject.layer;

        // 2. 문자열 이름("Highlight")을 기반으로 실제 레이어 인덱스(숫자)를 찾습니다.
        highlightLayerIndex = LayerMask.NameToLayer(highlightLayerName);

        if (highlightLayerIndex == -1)
        {
            // "Highlight" 레이어가 프로젝트에 등록되지 않은 경우 오류를 출력합니다.
            Debug.LogError($"[Interactable] '{highlightLayerName}' 레이어를 찾을 수 없습니다. Edit > Project Settings > Tags and Layers에서 이 레이어를 생성했는지 확인하세요.", this);
        }
    }

    /// <summary>
    /// 플레이어가 이 오브젝트와 상호작용할 때 호출될 함수입니다.
    /// </summary>
    public void Interact()
    {
        onInteract.Invoke();
    }

    /// <summary>
    /// 하이라이트(레이어 변경)를 켭니다.
    /// </summary>
    public void Highlight()
    {
        // 하이라이트 레이어를 못 찾았거나 이미 하이라이트된 상태면 무시
        if (_isHighlighted || highlightLayerIndex == -1) return;

        _isHighlighted = true;
        // 자신과 모든 자식 오브젝트의 레이어를 "Highlight"로 변경합니다.
        SetLayerRecursively(transform, highlightLayerIndex);
    }

    /// <summary>
    /// 하이라이트(레이어 복구)를 끕니다.
    /// </summary>
    public void Unhighlight()
    {
        if (!_isHighlighted) return;

        _isHighlighted = false;
        // 자신과 모든 자식 오브젝트의 레이어를 원래 레이어로 되돌립니다.
        SetLayerRecursively(transform, originalLayerIndex);
    }

    /// <summary>
    /// 자신(Transform)과 모든 자식 오브젝트의 레이어를 재귀적으로 변경합니다.
    /// (복잡한 모델이라도 모두 하이라이트되도록 하기 위함)
    /// </summary>
    private void SetLayerRecursively(Transform target, int layer)
    {
        target.gameObject.layer = layer;
        foreach (Transform child in target)
        {
            SetLayerRecursively(child, layer);
        }
    }
}