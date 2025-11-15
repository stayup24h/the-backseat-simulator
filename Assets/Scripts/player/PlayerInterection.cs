using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // TextMeshPro를 사용하기 위해 필요

public class PlayerInteraction : MonoBehaviour
{
    [Header("상호작용 설정")]
    [Tooltip("레이캐스트를 쏠 카메라의 Transform")]
    public Transform cameraTransform;

    [Tooltip("상호작용이 가능한 최대 거리")]
    public float interactionDistance = 3f;

    [Header("UI 설정")]
    [Tooltip("상호작용 UI의 부모 GameObject (켜고 끌 때 사용)")]
    public GameObject interactionUIParent;

    [Tooltip("상호작용 프롬프트 텍스트 (TMP_Text)")]
    public TMP_Text interactionText;

    // 현재 바라보고 있는 Interactable
    private Interactable currentInteractable;

    void Start()
    {
        // 카메라가 연결되지 않았다면, PlayerCtrl처럼 자식에서 찾아옵니다.
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }

        // 시작할 때 UI를 숨깁니다.
        HideUI();
    }

    // PlayerInteraction.cs의 Update 함수는 그대로 둡니다.
    void Update()
    {
        CheckForInteractable();
    }

    /// <summary>
    /// 매 프레임 카메라 정면으로 레이캐스트를 쏴서 Interactable을 찾습니다.
    /// </summary>
    private void CheckForInteractable()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Interactable newInteractable = null;

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            newInteractable = hit.collider.GetComponent<Interactable>();
        }

        // --- 상태 관리 (수정됨) ---

        // 1. (새로 발견) 이전에 아무것도 안 봤는데, 새로 Interactable을 발견한 경우
        if (newInteractable != null && currentInteractable == null)
        {
            ShowUI(newInteractable);
            newInteractable.Highlight(); // [추가]
        }
        // 2. (놓침) 이전에 Interactable을 봤는데, 이젠 안 보는 경우
        else if (newInteractable == null && currentInteractable != null)
        {
            HideUI();
            currentInteractable.Unhighlight(); // [추가]
        }
        // 3. (변경) 이전에 A를 봤는데, 이제 B를 보는 경우
        else if (newInteractable != null && currentInteractable != null && newInteractable != currentInteractable)
        {
            ShowUI(newInteractable);
            currentInteractable.Unhighlight(); // [추가] (이전 것은 끄고)
            newInteractable.Highlight(); // [추가] (새로운 것은 켠다)
        }

        currentInteractable = newInteractable;
    }

    /// <summary>
    /// 상호작용 UI를 보여줍니다.
    /// </summary>
    private void ShowUI(Interactable interactable)
    {
        interactionUIParent.SetActive(true);
        interactionText.text = interactable.promptMessage;
    }

    /// <summary>
    /// 상호작용 UI를 숨깁니다.
    /// </summary>
    private void HideUI()
    {
        interactionUIParent.SetActive(false);
    }

    /// <summary>
    /// [Input System] "Interact" 액션이 호출될 때 실행됩니다.
    /// </summary>
    public void OnInteract(InputValue value)
    {
        // 버튼을 눌렀고, 현재 바라보고 있는 상호작용 오브젝트가 있다면
        if (value.isPressed && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }
}