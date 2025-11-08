using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCtrl : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    private float xRotation = 0f;
    private Vector2 lookInput;
    
    // [추가] 스크립트 잠금 플래그
    private bool isLocked = false;

    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        // [추가] 잠겨있으면 마우스 입력을 처리하지 않음
        if (isLocked)
        {
            return;
        }

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    // [추가] 외부에서 호출할 잠금 함수 (대화 모드)
    public void LockMouseLook()
    {
        isLocked = true;
    }

    // [추가] 외부에서 호출할 잠금 해제 함수 (게임 모드)
    public void UnlockMouseLook()
    {
        isLocked = false;
        Cursor.lockState = CursorLockMode.Locked; // 커서 숨기기
        Cursor.visible = false;
        
        Vector3 cameraForward = cameraTransform.forward;
        
        // Y축(높이)을 0으로 만들어 수평 방향 벡터만 추출합니다.
        Vector3 playerForward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;

        // 수평 방향이 0이 아닌 경우에만 (카메라가 정수직 상/하를 보고 있지 않을 때)
        if (playerForward.sqrMagnitude > 0.001f)
        {
            // Player(transform)의 정면을 카메라의 수평 정면과 일치시킵니다.
            transform.rotation = Quaternion.LookRotation(playerForward, Vector3.up);
        }

        // 2. 상하(X) 회전 동기화 (Camera)
        // Player의 Y축 회전이 동기화되었으므로, 이제 카메라의 로컬 X축 회전값을 가져옵니다.
        float currentXAngle = cameraTransform.localEulerAngles.x;

        // localEulerAngles는 0~360 값을 반환하므로, -180~180 범위로 변환합니다.
        if (currentXAngle > 180)
        {
            currentXAngle -= 360f;
        }

        // 스크립트의 xRotation 변수를 현재 카메라의 X축 각도로 덮어씁니다.
        xRotation = currentXAngle;
    }
}