using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; // List를 사용하기 위해 추가

public class Interactable : MonoBehaviour
{
    [Header("UI 설정")]
    [Tooltip("UI에 표시될 상호작용 프롬프트 메시지 (예: [E] 문 열기)")]
    public string promptMessage = "[E] 상호작용";

    [Header("하이라이트 설정")]
    [Tooltip("하이라이트 시킬 발광 색상")]
    public Color highlightColor = Color.white;
    
    [Tooltip("하이라이트 강도 (Emission Intensity)")]
    [Range(0f, 5f)]
    public float highlightIntensity = 1.5f;

    [Header("이벤트")]
    [Tooltip("상호작용 시 실행될 이벤트를 여기에 연결합니다.")]
    public UnityEvent onInteract;

    // --- [새로 추가된 변수] ---
    private Renderer _renderer; // 오브젝트의 렌더러
    private List<Material> _materials = new List<Material>(); // 렌더러의 머티리얼 리스트
    private List<Color> _originalEmissionColors = new List<Color>(); // 원래 발광 색상 저장용
    private bool _isHighlighted = false;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            _renderer = GetComponentInChildren<Renderer>();
        }

        if (_renderer != null)
        {
            // 런타임에 머티리얼 인스턴스를 가져옵니다. (공유 머티리얼 원본이 아닌)
            _renderer.GetMaterials(_materials);

            // 원본 머티리얼의 Emission 색상을 저장해둡니다.
            foreach (var mat in _materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    _originalEmissionColors.Add(mat.GetColor("_EmissionColor"));
                }
                else
                {
                    // Emission 속성이 없는 머티리얼(예: Unlit)인 경우
                    _originalEmissionColors.Add(Color.black);
                }
            }
        }
    }

    /// <summary>
    /// 플레이어가 이 오브젝트와 상호작용할 때 호출될 함수입니다.
    /// </summary>
    public void Interact()
    {
        onInteract.Invoke();
    }

    // --- [새로 추가된 함수] ---

    /// <summary>
    /// 하이라이트(발광)를 켭니다.
    /// </summary>
    public void Highlight()
    {
        if (_renderer == null || _materials.Count == 0 || _isHighlighted) return;

        _isHighlighted = true;
        // HDR 컬러 계산 (강도 적용)
        Color finalColor = highlightColor * Mathf.LinearToGammaSpace(highlightIntensity);

        foreach (var mat in _materials)
        {
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION"); // Emission 활성화
                mat.SetColor("_EmissionColor", finalColor);
            }
        }
    }

    /// <summary>
    /// 하이라이트(발광)를 끕니다. (원래대로 복구)
    /// </summary>
    public void Unhighlight()
    {
        if (_renderer == null || _materials.Count == 0 || !_isHighlighted) return;

        _isHighlighted = false;
        for (int i = 0; i < _materials.Count; i++)
        {
            if (_materials[i].HasProperty("_EmissionColor"))
            {
                // 저장해둔 원래 색상으로 복구
                _materials[i].SetColor("_EmissionColor", _originalEmissionColors[i]);

                // 원래 색상이 검은색(0,0,0)이었다면 Emission 비활성화
                if (_originalEmissionColors[i] == Color.black)
                {
                    _materials[i].DisableKeyword("_EMISSION");
                }
            }
        }
    }
}