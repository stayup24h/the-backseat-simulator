using UnityEngine;

/// <summary>
/// 제네릭 싱글톤 MonoBehaviour 클래스입니다.
/// 이 클래스를 상속받는 클래스(T)를 싱글톤으로 만듭니다.
/// </summary>
/// <typeparam name="T">싱글톤이 될 클래스 타입 (자기 자신)</typeparam>
public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    [Header("Singleton Settings")]
    [Tooltip("true로 설정하면 씬이 바뀌어도 이 오브젝트가 파괴되지 않습니다.")]
    [SerializeField]
    private bool _isPersistent = false; // 씬 전환 시 파괴 여부

    private static T _instance;
    private static readonly object _lock = new object(); // 쓰레드 안전성(Thread-safety)을 위한 잠금 객체

    /// <summary>
    /// 싱글톤 인스턴스에 접근하기 위한 public static 프로퍼티
    /// </summary>
    public static T Instance
    {
        get
        {
            // 애플리케이션이 종료되는 중이면 null 반환
            if (applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' " +
                                 "already destroyed on application quit. Won't create new instance.");
                return null;
            }

            lock (_lock) // 한 번에 하나의 쓰레드만 접근하도록 잠금
            {
                // 1. 인스턴스가 아직 없는지 확인
                if (_instance == null)
                {
                    // 2. 씬에서 T 타입을 가진 활성화된 오브젝트를 찾음
                    _instance = (T)FindObjectOfType(typeof(T));

                    // 3. 씬에 여러 개의 인스턴스가 있는지 확인 (중복)
                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.LogError($"<color=red>[Singleton] Error: 씬에 {typeof(T)}의 인스턴스가 2개 이상 존재합니다.</color>");
                        // (Awake에서 중복을 처리하지만, 혹시 모를 동시 접근 시 로그)
                        return _instance;
                    }

                    // 4. 씬에도 없다면 (예: 씬에 미리 배치하지 않은 경우)
                    if (_instance == null)
                    {
                        // 새 게임 오브젝트를 만들고 컴포넌트를 붙임
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = $"(Singleton) {typeof(T)}";

                        Debug.Log($"<color=cyan>[Singleton] {typeof(T)}의 인스턴스가 씬에 없어 새로 생성합니다.</color>");
                    }
                    else
                    {
                        Debug.Log($"<color=green>[Singleton] {typeof(T)}의 인스턴스를 씬에서 찾았습니다.</color>");
                    }
                }

                // 5. 찾은 (또는 이미 있던) 인스턴스 반환
                return _instance;
            }
        }
    }

    // 애플리케이션 종료 시 '유령 객체'가 생성되는 것을 방지
    private static bool applicationIsQuitting = false;

    /// <summary>
    /// Unity의 OnApplicationQuit 메시지. 애플리케이션 종료 시 플래그 설정
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }

    /// <summary>
    /// Awake가 호출될 때 싱글톤 인스턴스를 설정합니다.
    /// </summary>
    protected virtual void Awake()
    {
        // 1. 이 오브젝트가 영속성을 가지도록 설정
        if (_isPersistent)
        {
            // 인스턴스가 아직 없거나, 이 오브젝트가 바로 그 인스턴스라면
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(this.gameObject); // 씬 전환 시 파괴되지 않도록 설정
            }
            // 인스턴스가 이미 존재하는데, 이 오브젝트가 아니라면 (중복)
            else if (_instance != this)
            {
                Debug.LogWarning($"<color=orange>[Singleton] {typeof(T)}의 영속성 인스턴스가 이미 존재합니다. 이 중복 인스턴스를 파괴합니다.</color>");
                Destroy(this.gameObject); // 중복된 이 오브젝트를 파괴
            }
            // (else: _instance == this 인 경우)
            // 이 경우는 DontDestroyOnLoad로 인해 씬이 다시 로드될 때
            // 이미 존재하는 인스턴스(자기 자신)이므로 아무것도 안 함
        }
        // 2. 영속성이 없는 일반 싱글톤의 경우
        else
        {
            // 인스턴스가 아직 없는 경우
            if (_instance == null)
            {
                _instance = this as T; // 이 오브젝트를 인스턴스로 설정
            }
            // 인스턴스가 이미 존재하는데, 이 오브젝트가 아니라면 (중복)
            else if (_instance != this)
            {
                Debug.LogWarning($"<color=orange>[Singleton] {typeof(T)}의 중복 인스턴스가 발견되어, 이 오브젝트를 파괴합니다.</color>");
                Destroy(this.gameObject); // 중복된 이 오브젝트를 파괴
            }
        }
    }
}