using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : SingletonBehaviour<SoundManager>
{
    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
    public float crossFadeDuration = 2.0f; // BGM 전환 시간

    // 내부 오디오 소스 (크로스페이드용 2개)
    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private AudioSource activeBgmSource;

    // 현재 재생 정보
    private SoundDataSO currentBgmSO;
    private SoundDataSO.ClipData currentBgmClipData;

    // 데이터 검색용 딕셔너리
    private Dictionary<string, SoundDataSO> bgmDictionary = new Dictionary<string, SoundDataSO>();
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

        // 코드로 루프를 제어하므로 Unity 기본 루프는 끔
        bgmSourceA.loop = false;
        bgmSourceB.loop = false;
        bgmSourceA.playOnAwake = false;
        bgmSourceB.playOnAwake = false;

        // 2. Resources 폴더에서 데이터 자동 로드
        LoadSoundData("Audio/BGM", bgmDictionary);
        LoadSoundData("Audio/SFX", sfxDictionary);

        activeBgmSource = bgmSourceA;
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
        // 1. 안전장치: 데이터나 소스가 없으면 아무것도 안 함
        if (currentBgmSO == null || activeBgmSource == null || activeBgmSource.clip == null) return;

        // 2. 이미 크로스페이드 중이라면 중복 실행 방지
        if (isCrossFading) return;

        // 3. [핵심 변경] "곡이 완전히 끝났을 때"가 아니라, "끝나기 직전"을 감지해야 함
        // 남은 시간 = 전체 길이 - 현재 재생 시간
        float remainingTime = activeBgmSource.clip.length - activeBgmSource.time;

        // 4. 재생 중이고, 남은 시간이 '크로스페이드 시간'보다 적게 남았다면? -> 교체 시작!
        if (activeBgmSource.isPlaying && remainingTime <= crossFadeDuration)
        {
            Debug.Log($"⚡ 크로스페이드 타이밍 진입! (남은 시간: {remainingTime:F2}초 <= 설정값: {crossFadeDuration}초)");
            PlayNextTrackInPlaylist();
        }
        
        // (혹시 모를 예외 처리: 재생 중이 아닌데 시간이 끝까지 갔다면 바로 넘김)
        else if (!activeBgmSource.isPlaying && activeBgmSource.time >= activeBgmSource.clip.length)
        {
            PlayNextTrackInPlaylist();
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
    // 🎵 3. BGM 재생 (플레이리스트 시작)
    // ===========================
    public void PlayBGM(string name)
    {
        Debug.Log($"PlayBGM 호출: {name}");
        if (!bgmDictionary.ContainsKey(name)) return;
        SoundDataSO nextSO = bgmDictionary[name];

        // 다른 플레이리스트로 바꿀 때만 실행
        if (currentBgmSO != nextSO)
        {
            currentBgmSO = nextSO;
            PlayNextTrackInPlaylist();
        }
    }

    // 플레이리스트 다음 곡 재생
    private void PlayNextTrackInPlaylist()
    {
        if (currentBgmSO == null) return;

        SoundDataSO.ClipData nextClipData = currentBgmSO.GetNextClipData();
        if (nextClipData == null) return;

        currentBgmClipData = nextClipData;
        StopAllCoroutines();
        StartCoroutine(CrossFadeBGM(nextClipData));
    }

    IEnumerator CrossFadeBGM(SoundDataSO.ClipData clipData)
    {
        isCrossFading = true; // 전환 중 표시

        // 1. 다음 소스 결정 (A <-> B 스위칭)
        AudioSource nextSource = (activeBgmSource == bgmSourceA) ? bgmSourceB : bgmSourceA;

        // 2. 다음 소스 세팅
        nextSource.clip = clipData.clip;
        nextSource.pitch = currentBgmSO.masterPitch * clipData.pitch;
        nextSource.volume = 0f; // 🔴 시작할 때 0이어야 서서히 커짐
        nextSource.Play();

        float timer = 0f;
        
        // 목표 볼륨 계산
        float targetVolume = GetCurrentBGMTargetVolume();
        float startVolume = activeBgmSource.volume; // 현재 곡의 볼륨
        

        while (timer < crossFadeDuration)
        {
            timer += Time.deltaTime;
            float ratio = timer / crossFadeDuration;

            // 이전 곡 볼륨 줄이기
            if (activeBgmSource.isPlaying)
                activeBgmSource.volume = Mathf.Lerp(startVolume, 0f, ratio);

            // 다음 곡 볼륨 키우기
            nextSource.volume = Mathf.Lerp(0f, targetVolume, ratio);

            // 🔍 [디버깅] 진행 상황을 로그로 확인 (너무 많이 뜨면 주석 처리하세요)
            // Debug.Log($"   Running... Ratio: {ratio:F2} / Vol A: {activeBgmSource.volume:F2} / Vol B: {nextSource.volume:F2}");

            yield return null; // ⚠️ 이게 없으면 즉시 끝납니다!
        }

        // 마무리
        activeBgmSource.Stop();
        activeBgmSource.volume = 0f;
        
        nextSource.volume = targetVolume;

        // 🔴 [핵심] 활성 소스 교체 (다음 번엔 반대로 작동하도록)
        activeBgmSource = nextSource;

        isCrossFading = false; // 전환 끝
    }

    // ===========================
    // 🎚️ 4. 볼륨 조절 및 유틸
    // ===========================
    
    // 현재 BGM의 목표 볼륨 계산
    private float GetCurrentBGMTargetVolume()
    {
        if (currentBgmSO == null || currentBgmClipData == null) return 0f;
        return masterVolume * bgmVolume * currentBgmSO.masterVolume * currentBgmClipData.volume;
    }

    // 옵션 조절 시 실시간 반영
    public void SetMasterVolume(float vol) { masterVolume = vol; UpdateActiveBGMVolume(); }
    public void SetBGMVolume(float vol) { bgmVolume = vol; UpdateActiveBGMVolume(); }
    public void SetSFXVolume(float vol) { sfxVolume = vol; }

    private void UpdateActiveBGMVolume()
    {
        if (activeBgmSource != null && activeBgmSource.isPlaying)
        {
            activeBgmSource.volume = GetCurrentBGMTargetVolume();
        }
    }
}