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
        // 시작할 때 잠금 해제 (게임 모드)
        UnlockMouseLook(); 
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
        lookInput = Vector2.zero; // 입력값 초기화
        Cursor.lockState = CursorLockMode.None; // 커서 보이기
        Cursor.visible = true;
    }

    // [추가] 외부에서 호출할 잠금 해제 함수 (게임 모드)
    public void UnlockMouseLook()
    {
        isLocked = false;
        Cursor.lockState = CursorLockMode.Locked; // 커서 숨기기
        Cursor.visible = false;
    }
}