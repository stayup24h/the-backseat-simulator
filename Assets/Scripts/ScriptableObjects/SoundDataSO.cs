using UnityEngine;

[CreateAssetMenu(fileName = "New Sound Playlist", menuName = "Audio/Sound Playlist", order = 1)]
public class SoundDataSO : ScriptableObject
{
    public string soundName;

    // ✅ [신규 클래스] 클립 하나하나의 설정을 담는 통
    [System.Serializable] // 이게 있어야 인스펙터에서 보입니다!
    public class ClipData
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f; // 이 클립만의 볼륨 보정치
        [Range(0.1f, 3f)] public float pitch = 1f; // 이 클립만의 피치 보정치
    }

    // ✅ AudioClip[] 대신 ClipData[]를 사용
    public ClipData[] clips; 

    [Header("Global Settings (전체 적용)")]
    [Range(0f, 1f)] public float masterVolume = 1f; // 이 SO 전체의 기준 볼륨
    [Range(0.1f, 3f)] public float masterPitch = 1f; // 이 SO 전체의 기준 피치

    public enum PlayMode { Sequential, Random }
    public PlayMode playMode = PlayMode.Sequential;

    private int currentIndex = -1;

    // ✅ 반환 타입 변경: AudioClip -> ClipData
    public ClipData GetNextClipData()
    {
        if (clips.Length == 0) return null;

        if (playMode == PlayMode.Random)
        {
            return clips[Random.Range(0, clips.Length)];
        }
        else
        {
            currentIndex = (currentIndex + 1) % clips.Length;
            return clips[currentIndex];
        }
    }
}