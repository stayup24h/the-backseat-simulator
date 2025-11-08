using UnityEngine;
using UnityEngine.InputSystem; // Input System을 사용하기 위해 필수!

public class playerCtrl : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    private float xRotation = 0f;
    private Vector2 lookInput; // 마우스 입력을 저장할 변수

    void Start()
    {
        // 마우스 커서 고정 및 숨기기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Input System이 'Look' 액션 입력을 감지하면 이 함수를 호출합니다.
    // Player 객체에 붙은 PlayerInput 컴포넌트가 이 함수를 찾아 실행시킵니다.
    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        // OnLook에서 받아온 입력을 기반으로 실제 회전을 처리합니다.
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // 1. 좌우 회전 (Player Body)
        playerBody.Rotate(Vector3.up * mouseX);

        // 2. 상하 회전 (Camera)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}