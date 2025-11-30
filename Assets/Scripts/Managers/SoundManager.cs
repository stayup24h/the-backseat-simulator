using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

public enum SoundType { BGM, Noise, SFX }

public class SoundManager : SingletonBehaviour<SoundManager>
{
    [Header("Audio Mixer")]
    public AudioMixerGroup bgmMixerGroup;

    [Header("Audio Source Targets")]
    public Transform bgmSourceTarget; // BGM 소스를 붙일 타겟 오브젝트

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float noiseVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    public float crossFadeDuration = 2.0f; // BGM 전환 시간

    // 내부 오디오 소스
    private AudioSource bgmSourceA, bgmSourceB, activeBgmSource;
    private GameObject bgmChannelObject; // BGM 소스를 담을 오브젝트
    private AudioSource noiseSourceA, noiseSourceB, activeNoiseSource;
    private GameObject noiseChannelObject; // Noise 소스를 담을 오브젝트
    
    // 현재 재생 정보
    private SoundDataSO currentBgmSO, currentNoiseSO;
    private SoundDataSO.ClipData currentBgmClipData, currentNoiseClipData;

    // 데이터 검색용 딕셔너리
    private Dictionary<string, SoundDataSO> bgmDictionary = new Dictionary<string, SoundDataSO>();
    private Dictionary<string, SoundDataSO> noiseDictionary = new Dictionary<string, SoundDataSO>();
    private Dictionary<string, SoundDataSO> sfxDictionary = new Dictionary<string, SoundDataSO>();

    private bool isCrossFading = false;
    private Coroutine bgmCrossfadeCoroutine;
    private Coroutine noiseCrossfadeCoroutine;
    // 분리된 플래그: BGM/Noise 각각의 교차 페이드 상태를 추적합니다.
    private bool isBgmCrossFading = false;
    private bool isNoiseCrossFading = false;
    
    protected override void Awake()
    {
        base.Awake(); // 부모의 싱글톤 로직 실행
        if (Instance == this) Initialize();
    }

    void Initialize()
    {
        // 1. AudioSource 생성 및 설정
        bgmChannelObject = new GameObject("BGMChannel");
        bgmChannelObject.transform.SetParent(bgmSourceTarget != null ? bgmSourceTarget : this.transform);
        bgmChannelObject.transform.localPosition = Vector3.zero;
        bgmSourceA = bgmChannelObject.AddComponent<AudioSource>();
        bgmSourceB = bgmChannelObject.AddComponent<AudioSource>();

        // Noise 채널용 오브젝트 생성
        noiseChannelObject = new GameObject("NoiseChannel");
        noiseChannelObject.transform.SetParent(this.transform);
        noiseSourceA = noiseChannelObject.AddComponent<AudioSource>();
        noiseSourceB = noiseChannelObject.AddComponent<AudioSource>();

        ConfigureAudioSource(bgmSourceA, true, bgmMixerGroup);
        ConfigureAudioSource(bgmSourceB, true, bgmMixerGroup);
        ConfigureAudioSource(noiseSourceA, true);
        ConfigureAudioSource(noiseSourceB, true);

        // 2. Resources 폴더에서 데이터 자동 로드
        LoadSoundData("Audio/BGM", bgmDictionary);
        LoadSoundData("Audio/Noise", noiseDictionary); // 딕셔너리 수정
        LoadSoundData("Audio/SFX", sfxDictionary);

        activeBgmSource = bgmSourceA;
        activeNoiseSource = noiseSourceA;
    }

    void ConfigureAudioSource(AudioSource source, bool is3D = false, AudioMixerGroup mixerGroup = null)
    {
        source.loop = false;
        source.playOnAwake = false;
        source.outputAudioMixerGroup = mixerGroup;
        if (is3D)
        {
            source.spatialBlend = 1.0f; // 기본 3D로 설정
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 50f;
        }
        else
        {
            source.spatialBlend = 0f;
        }
    }

    // Resources 로드 헬퍼 함수
    void LoadSoundData(string path, Dictionary<string, SoundDataSO> dict)
    {
        SoundDataSO[] loadedData = Resources.LoadAll<SoundDataSO>(path);
        foreach (var data in loadedData)
        {
            if (dict.ContainsKey(data.soundName)) continue;
            dict.Add(data.soundName, data);
        }
        Debug.Log($"[{path}] 로드 완료: {loadedData.Length}개");
    }

    void Update()
    {
        UpdateChannel(activeBgmSource, currentBgmSO, SoundType.BGM);
        UpdateChannel(activeNoiseSource, currentNoiseSO, SoundType.Noise);
    }

    private void UpdateChannel(AudioSource activeSource, SoundDataSO currentSO, SoundType type)
    {
        if (currentSO == null || activeSource == null || activeSource.clip == null) return;

        // 타입별 교차 페이드 여부 확인
        if (type == SoundType.BGM && isBgmCrossFading) return;
        if (type == SoundType.Noise && isNoiseCrossFading) return;

        float remainingTime = activeSource.clip.length - activeSource.time;

        if ((activeSource.isPlaying && remainingTime <= crossFadeDuration) ||
            (!activeSource.isPlaying && activeSource.time >= activeSource.clip.length - 0.1f))
        {
            PlayNextTrackInPlaylist(currentSO, type);
        }
    }

    // ===========================
    // 🔊 1. SFX 재생 (2D - UI 등)
    // ===========================
    public void PlaySFX(string name)
    {
        PlaySFXCommon(name, null); // 타겟이 없으면 2D로 재생
    }

    // ===========================
    // 🔗 2. SFX 재생 (3D - 따라다니기)
    // ===========================
    public void PlaySFXAttached(string name, Transform target)
    {
        PlaySFXCommon(name, target);
    }

    // SFX 공통 로직
    private void PlaySFXCommon(string name, Transform target)
    {
        if (!sfxDictionary.ContainsKey(name))
        {
            Debug.LogWarning($"SFX '{name}' 없음");
            return;
        }

        SoundDataSO dataSO = sfxDictionary[name];
        SoundDataSO.ClipData clipData = dataSO.GetNextClipData(); // 랜덤/순차 가져오기

        if (clipData == null || clipData.clip == null) return;

        // 임시 오브젝트 생성
        GameObject audioObj = new GameObject($"TempSFX_{name}");
        
        if (target != null)
        {
            audioObj.transform.position = target.position;
            audioObj.transform.SetParent(target); // 타겟 따라다니기
        }
        else
        {
            audioObj.transform.SetParent(this.transform); // 매니저에 붙이기 (2D)
        }

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clipData.clip;

        // ✅ 볼륨 & 피치 최종 계산
        source.volume = masterVolume * sfxVolume * dataSO.masterVolume * clipData.volume;
        source.pitch = dataSO.masterPitch * clipData.pitch;

        // 3D/2D 설정
        source.spatialBlend = (target != null) ? 1f : 0f;
        source.minDistance = 1f;
        source.maxDistance = 20f;
        source.rolloffMode = AudioRolloffMode.Linear;

        source.Play();
        
        // 재생 완료 후 파괴 (피치 고려)
        float destroyTime = clipData.clip.length / Mathf.Abs(source.pitch);
        Destroy(audioObj, destroyTime + 0.1f);
    }

    // ===========================
    // 🎵 3. BGM/Noise 재생 (플레이리스트 시작)
    // ===========================
    public void PlayBGM(string name) => PlayPlaylist(name, SoundType.BGM);
    public void PlayNoise(string name)
    {
        // 2D 노이즈를 위해 위치와 spatial blend 초기화
        if (noiseChannelObject != null)
        {
            noiseChannelObject.transform.localPosition = Vector3.zero;
            noiseSourceA.spatialBlend = 0f;
            noiseSourceB.spatialBlend = 0f;
        }
        PlayPlaylist(name, SoundType.Noise);
    }

    public void PlayPositionalNoise(string name, Vector3 position, float spatialBlend)
    {
        if (noiseChannelObject != null)
        {
            noiseChannelObject.transform.position = position;
            noiseSourceA.spatialBlend = spatialBlend;
            noiseSourceB.spatialBlend = spatialBlend;
        }
        PlayPlaylist(name, SoundType.Noise);
    }

    private void PlayPlaylist(string name, SoundType type)
    {
        Debug.Log($"[SoundManager] PlayPlaylist called: name={name}, type={type}");
        var dict = GetDictionary(type);
        if (dict == null || !dict.ContainsKey(name))
        {
            Debug.LogWarning($"Sound '{name}' of type {type} not found.");
            return;
        }
        
        var nextSO = dict[name];

        if (type == SoundType.BGM)
        {
            // 항상 새로 재생하도록 변경: 동일한 플레이리스트 이름이더라도 재생을 요청하면 첫 곡부터 재생 시작
            currentBgmSO = nextSO;
            PlayNextTrackInPlaylist(currentBgmSO, type);
        }
        else if (type == SoundType.Noise)
        {
            // Noise도 동일하게 항상 재생 요청 시 재시작
            currentNoiseSO = nextSO;
            PlayNextTrackInPlaylist(currentNoiseSO, type);
        }
    }

    // 플레이리스트 다음 곡 재생
    private void PlayNextTrackInPlaylist(SoundDataSO currentSoundSO, SoundType type)
    {
        Debug.Log($"[SoundManager] PlayNextTrackInPlaylist called for type={type}");
        if (currentSoundSO == null) { Debug.LogWarning("currentSoundSO is null"); return; }

        SoundDataSO.ClipData nextClipData = currentSoundSO.GetNextClipData();
        if (nextClipData == null) { Debug.LogWarning("nextClipData is null"); return; }

        Debug.Log($"[SoundManager] Next clip data: " + (nextClipData.clip != null ? nextClipData.clip.name : "<null>"));

        if (type == SoundType.BGM)
            currentBgmClipData = nextClipData;
        else if (type == SoundType.Noise)
            currentNoiseClipData = nextClipData;
        
        if (type == SoundType.BGM)
        {
            if (bgmCrossfadeCoroutine != null) StopCoroutine(bgmCrossfadeCoroutine);
            bgmCrossfadeCoroutine = StartCoroutine(CrossFade(nextClipData, type));
        }
        else // Noise
        {
            if (noiseCrossfadeCoroutine != null) StopCoroutine(noiseCrossfadeCoroutine);
            noiseCrossfadeCoroutine = StartCoroutine(CrossFade(nextClipData, type));
        }
    }

    IEnumerator CrossFade(SoundDataSO.ClipData clipData, SoundType type)
    {
        // 타입별 플래그 세팅
        if (type == SoundType.BGM) isBgmCrossFading = true;
        else isNoiseCrossFading = true;

        // 1. 타입에 맞는 오디오 소스와 정보 가져오기
        AudioSource activeSource, sourceA, sourceB;
        SoundDataSO currentSO;

        if (type == SoundType.BGM)
        {
            activeSource = activeBgmSource;
            sourceA = bgmSourceA;
            sourceB = bgmSourceB;
            currentSO = currentBgmSO;
        }
        else // Noise
        {
            activeSource = activeNoiseSource;
            sourceA = noiseSourceA;
            sourceB = noiseSourceB;
            currentSO = currentNoiseSO;
        }

        // 2. 다음 소스 결정 (A <-> B 스위칭)
        AudioSource nextSource = (activeSource == sourceA) ? sourceB : sourceA;

        // 3. 다음 소스 세팅
        nextSource.clip = clipData.clip;
        nextSource.pitch = currentSO.masterPitch * clipData.pitch;
        nextSource.volume = 0f; // 🔴 시작할 때 0이어야 서서히 커짐
        nextSource.Play();

        float timer = 0f;

        // 4. 목표 볼륨 계산 (안전하게 startVolume 처리)
        float targetVolume = GetTargetVolume(type);
        float startVolume = (activeSource != null) ? activeSource.volume : 0f; // 현재 곡의 볼륨 (없으면 0)

        while (timer < crossFadeDuration)
        {
            timer += Time.deltaTime;
            float ratio = timer / crossFadeDuration;

            // 이전 곡 볼륨 줄이기
            if (activeSource != null && activeSource.isPlaying)
                activeSource.volume = Mathf.Lerp(startVolume, 0f, ratio);

            // 다음 곡 볼륨 키우기
            nextSource.volume = Mathf.Lerp(0f, targetVolume, ratio);

            yield return null;
        }

        // 5. 마무리
        if (activeSource != null)
        {
            activeSource.Stop();
            activeSource.volume = 0f;
            activeSource.clip = null; // 안전하게 클립 제거
        }
        nextSource.volume = targetVolume;

        // 6. 활성 소스 교체
        if (type == SoundType.BGM)
            activeBgmSource = nextSource;
        else
            activeNoiseSource = nextSource;

        // 타입별 플래그 리셋
        if (type == SoundType.BGM)
            isBgmCrossFading = false;
        else
            isNoiseCrossFading = false;

        // Coroutine 참조 정리
        if (type == SoundType.BGM)
            bgmCrossfadeCoroutine = null;
        else
            noiseCrossfadeCoroutine = null;
    }
    
    // ===========================
    // 🎚️ 4. 볼륨 조절 및 유틸
    // ===========================
    
    // 현재 BGM/Noise의 목표 볼륨 계산
    private float GetTargetVolume(SoundType type)
    {
        if (type == SoundType.BGM)
        {
            if (currentBgmSO == null || currentBgmClipData == null) return 0f;
            return masterVolume * bgmVolume * currentBgmSO.masterVolume * currentBgmClipData.volume;
        }
        if (type == SoundType.Noise)
        {
            if (currentNoiseSO == null || currentNoiseClipData == null) return 0f;
            return masterVolume * noiseVolume * currentNoiseSO.masterVolume * currentNoiseClipData.volume;
        }
        return 0f;
    }

    // 옵션 조절 시 실시간 반영
    public void SetMasterVolume(float vol) { masterVolume = vol; UpdateAllActiveVolumes(); }
    public void SetBGMVolume(float vol) { bgmVolume = vol; UpdateAllActiveVolumes(); }
    public void SetNoiseVolume(float vol) { noiseVolume = vol; UpdateAllActiveVolumes(); }
    public void SetSFXVolume(float vol) { sfxVolume = vol; }

    private void UpdateAllActiveVolumes()
    {
        if (activeBgmSource != null && activeBgmSource.isPlaying)
        {
            activeBgmSource.volume = GetTargetVolume(SoundType.BGM);
        }
        if (activeNoiseSource != null && activeNoiseSource.isPlaying)
        {
            activeNoiseSource.volume = GetTargetVolume(SoundType.Noise);
        }
    }

    private Dictionary<string, SoundDataSO> GetDictionary(SoundType type)
    {
        switch (type)
        {
            case SoundType.BGM: return bgmDictionary;
            case SoundType.Noise: return noiseDictionary;
            case SoundType.SFX: return sfxDictionary;
            default: return null;
        }
    }

    // BGM 채널을 완전히 정지하고 관련 상태/코루틴을 정리합니다.
    public void StopBGM()
    {
        Debug.Log("[SoundManager] StopBGM called");
        // 코루틴 정리
        if (bgmCrossfadeCoroutine != null)
        {
            StopCoroutine(bgmCrossfadeCoroutine);
            bgmCrossfadeCoroutine = null;
        }

        // 모든 BGM 소스 정지
        if (bgmSourceA != null) { bgmSourceA.Stop(); bgmSourceA.clip = null; bgmSourceA.volume = 0f; bgmSourceA.pitch = 1f; }
        if (bgmSourceB != null) { bgmSourceB.Stop(); bgmSourceB.clip = null; bgmSourceB.volume = 0f; bgmSourceB.pitch = 1f; }

        // 상태 리셋
        activeBgmSource = bgmSourceA != null ? bgmSourceA : activeBgmSource;
        currentBgmSO = null;
        currentBgmClipData = null;
        isBgmCrossFading = false;
    }

    // 토글: 재생 중이면 끄고, 정지 상태면 항상 새 음악을 처음부터 재생합니다.
    public void bgmOnOff(string bgmName)
    {
        bool anyPlaying = (bgmSourceA != null && bgmSourceA.isPlaying) || (bgmSourceB != null && bgmSourceB.isPlaying);
        Debug.Log($"[SoundManager] bgmOnOff called name={bgmName} anyPlaying={anyPlaying}");

        if (anyPlaying)
        {
            // 켜져 있으면 완전 정지 (다음에 켤 때는 새 음악부터 재생되도록 함)
            StopBGM();
            return;
        }

        // 정지 상태이면 항상 새 음악을 처음부터 재생
        StopBGM(); // 안전하게 상태 리셋
        PlayBGM(bgmName);
    }
}
