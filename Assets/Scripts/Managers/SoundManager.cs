using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum SoundType { BGM, Noise, SFX }

public class SoundManager : SingletonBehaviour<SoundManager>
{
    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float noiseVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    public float crossFadeDuration = 2.0f; // BGM 전환 시간

    // 내부 오디오 소스
    private AudioSource bgmSourceA, bgmSourceB, activeBgmSource;
    private AudioSource noiseSourceA, noiseSourceB, activeNoiseSource;
    
    // 현재 재생 정보
    private SoundDataSO currentBgmSO, currentNoiseSO;
    private SoundDataSO.ClipData currentBgmClipData, currentNoiseClipData;

    // 데이터 검색용 딕셔너리
    private Dictionary<string, SoundDataSO> bgmDictionary = new Dictionary<string, SoundDataSO>();
    private Dictionary<string, SoundDataSO> noiseDictionary = new Dictionary<string, SoundDataSO>();
    private Dictionary<string, SoundDataSO> sfxDictionary = new Dictionary<string, SoundDataSO>();

    private bool isCrossFading = false;    
    
    protected override void Awake()
    {
        base.Awake(); // 부모의 싱글톤 로직 실행
        if (Instance == this) Initialize();
    }

    void Initialize()
    {
        // 1. AudioSource 생성 및 설정
        bgmSourceA = gameObject.AddComponent<AudioSource>();
        bgmSourceB = gameObject.AddComponent<AudioSource>();
        noiseSourceA = gameObject.AddComponent<AudioSource>();
        noiseSourceB = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource(bgmSourceA);
        ConfigureAudioSource(bgmSourceB);
        ConfigureAudioSource(noiseSourceA);
        ConfigureAudioSource(noiseSourceB);

        // 2. Resources 폴더에서 데이터 자동 로드
        LoadSoundData("Audio/BGM", bgmDictionary);
        LoadSoundData("Audio/Noise", noiseDictionary); // 딕셔너리 수정
        LoadSoundData("Audio/SFX", sfxDictionary);

        activeBgmSource = bgmSourceA;
        activeNoiseSource = noiseSourceA;
    }

    void ConfigureAudioSource(AudioSource source)
    {
        source.loop = false;
        source.playOnAwake = false;
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
        if (currentSO == null || activeSource == null || activeSource.clip == null || isCrossFading) return;

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
    public void PlayNoise(string name) => PlayPlaylist(name, SoundType.Noise);

    private void PlayPlaylist(string name, SoundType type)
    {
        var dict = GetDictionary(type);
        if (dict == null || !dict.ContainsKey(name))
        {
            Debug.LogWarning($"Sound '{name}' of type {type} not found.");
            return;
        }
        
        var nextSO = dict[name];

        if (type == SoundType.BGM)
        {
            if (currentBgmSO != nextSO)
            {
                currentBgmSO = nextSO;
                PlayNextTrackInPlaylist(currentBgmSO, type);
            }
        }
        else if (type == SoundType.Noise)
        {
            if (currentNoiseSO != nextSO)
            {
                currentNoiseSO = nextSO;
                PlayNextTrackInPlaylist(currentNoiseSO, type);
            }
        }
    }

    // 플레이리스트 다음 곡 재생
    private void PlayNextTrackInPlaylist(SoundDataSO currentSoundSO, SoundType type)
    {
        if (currentSoundSO == null) return;

        SoundDataSO.ClipData nextClipData = currentSoundSO.GetNextClipData();
        if (nextClipData == null) return;

        if (type == SoundType.BGM)
            currentBgmClipData = nextClipData;
        else if (type == SoundType.Noise)
            currentNoiseClipData = nextClipData;
        
        StopAllCoroutines();
        StartCoroutine(CrossFade(nextClipData, type));
    }

    IEnumerator CrossFade(SoundDataSO.ClipData clipData, SoundType type)
    {
        isCrossFading = true; // 전환 중 표시

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
        
        // 4. 목표 볼륨 계산
        float targetVolume = GetTargetVolume(type);
        float startVolume = activeSource.volume; // 현재 곡의 볼륨
        
        while (timer < crossFadeDuration)
        {
            timer += Time.deltaTime;
            float ratio = timer / crossFadeDuration;

            // 이전 곡 볼륨 줄이기
            if (activeSource.isPlaying)
                activeSource.volume = Mathf.Lerp(startVolume, 0f, ratio);

            // 다음 곡 볼륨 키우기
            nextSource.volume = Mathf.Lerp(0f, targetVolume, ratio);

            yield return null;
        }

        // 5. 마무리
        activeSource.Stop();
        activeSource.volume = 0f;
        nextSource.volume = targetVolume;

        // 6. 활성 소스 교체
        if (type == SoundType.BGM)
            activeBgmSource = nextSource;
        else
            activeNoiseSource = nextSource;

        isCrossFading = false; // 전환 끝
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
}