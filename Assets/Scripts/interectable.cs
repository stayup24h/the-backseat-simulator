using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; // List 사용을 위해 유지

public class Interactable : MonoBehaviour
{
    [Header("UI 설정")]
    [Tooltip("UI에 표시될 상호작용 프롬프트 메시지")]
    public string promptMessage = "[E] 상호작용";

    [Header("하이라이트 설정")]
    [Tooltip("하이라이트 시 사용할 '추가' 머티리얼 (예: 아웃라인 쉐이더)")]
    public Material highlightMaterial; // [수정] Emission 대신 하이라이트 머티리얼을 받음

    // [삭제] Emission 관련 변수들은 이제 필요 없습니다.
    // public Color highlightColor = Color.white;
    // public float highlightIntensity = 1.5f;

    [Header("이벤트")]
    [Tooltip("상호작용 시 실행될 이벤트를 여기에 연결합니다.")]
    public UnityEvent onInteract;

    // --- [수정된 변수] ---
    private Renderer _renderer;
    
    // [수정] 원본 머티리얼 '배열'을 저장할 리스트
    private List<Material> _originalMaterials = new List<Material>();
    private bool _isHighlighted = false;
    
    // [삭제] 원본 Emission 색상 저장은 필요 없음
    // private List<Color> _originalEmissionColors = new List<Color>();

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            _renderer = GetComponentInChildren<Renderer>();
        }

        if (_renderer != null)
        {
            // [수정] 현재 렌더러의 '공유 머티리얼(sharedMaterials)'을 원본으로 저장합니다.
            // .materials를 사용하면 인스턴스가 생성되므로 .sharedMaterials를 사용합니다.
            _renderer.GetSharedMaterials(_originalMaterials);
        }
        else
        {
            Debug.LogWarning("Interactable이 렌더러를 찾지 못했습니다.", this);
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
    /// 하이라이트(머티리얼 추가)를 켭니다.
    /// </summary>
    public void Highlight()
    {
        if (_renderer == null || _isHighlighted || highlightMaterial == null) return;

        _isHighlighted = true;

        // 1. 원본 머티리얼 리스트를 기반으로 새 리스트를 만듭니다.
        List<Material> newMaterialsList = new List<Material>(_originalMaterials);

        // 2. 새 리스트의 끝에 하이라이트 머티리얼을 추가합니다.
        newMaterialsList.Add(highlightMaterial);

        // 3. 렌더러의 'materials' 프로퍼티를 새 리스트의 배열(ToArray)로 교체합니다.
        // (이때 머티리얼 인스턴스들이 생성됩니다)
        _renderer.materials = newMaterialsList.ToArray();
    }

    /// <summary>
    /// 하이라이트(머티리얼 제거)를 끕니다. (원래대로 복구)
    /// </summary>
    public void Unhighlight()
    {
        if (_renderer == null || !_isHighlighted) return;

        _isHighlighted = false;

        // 1. 렌더러의 머티리얼을 저장해둔 '원본' 머티리얼 리스트의 배열로 되돌립니다.
        _renderer.materials = _originalMaterials.ToArray();
    }
}