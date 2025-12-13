using UnityEngine;
using UnityEngine.InputSystem;
using Yarn.Unity;
using System.Collections;

public class PlayerCtrl : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;
    
    
    private float xRotation = 0f;
    private Vector2 lookInput;
    
    // [추가] 스크립트 잠금 플래그
    private bool isLocked = false;
    
    public void Initialize()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
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
        if (GameManager.Instance.isGameOver) return;
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
        // 잠금 해제를 바로 적용하지 않고, 동기화 작업을 먼저 수행합니다.
        // 이렇게 하면 플레이어와 카메라의 회전 동기화 과정에서 발생하는 순간적인 시각적 점프를 방지할 수 있습니다.

        // 먼저 입력 처리를 차단합니다.
        isLocked = true;

        // 1) 현재 카메라의 월드 회전을 저장
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
            if (cameraTransform == null)
            {
                // 카메라가 없으면 그냥 잠금 해제
                StartCoroutine(ReleaseLockNextFrame());
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                lookInput = Vector2.zero;
                return;
            }
        }

        Quaternion cameraWorldRotation = cameraTransform.rotation;

        // 2) 플레이어의 Y축(수평) 방향을 카메라의 수평 방향으로 맞춥니다.
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 playerForward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;

        if (playerForward.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(playerForward, Vector3.up);
        }

        // 3) 플레이어 회전 적용 후 카메라의 월드 회전을 복원하여 시각적 점프를 제거합니다.
        cameraTransform.rotation = cameraWorldRotation;

        // 4) 카메라의 local X 회전(상하)를 xRotation에 동기화합니다.
        float currentXAngle = cameraTransform.localEulerAngles.x;
        if (currentXAngle > 180f) currentXAngle -= 360f;
        xRotation = currentXAngle;

        // 5) 입력을 초기화하여 Unlock 직후 남아있는 마우스 입력이 즉시 적용되지 않도록 합니다.
        lookInput = Vector2.zero;

        // 6) 커서 상태를 설정하고 잠금 해제는 다음 프레임으로 지연합니다.
        Cursor.lockState = CursorLockMode.Locked; // 커서 잠금
        Cursor.visible = false;
        StartCoroutine(ReleaseLockNextFrame());
    }

    private IEnumerator ReleaseLockNextFrame()
    {
        // 프레임을 한 번 기다려 Update에서 즉시 적용되는 입력을 차단
        yield return null;
        // 짧은 추가 지연으로 안전성 증가
        yield return new WaitForEndOfFrame();
        lookInput = Vector2.zero;
        isLocked = false;
    }
}