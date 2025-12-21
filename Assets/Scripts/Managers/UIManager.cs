using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Yarn.Unity;
using System;
using UnityEngine.Localization.Settings;

public class UIManager : SingletonBehaviour<UIManager>
{
    [Header("UI Elements")]
    public Slider concentrationSlider;
    public CanvasGroup titleUICanvasGroup;
    public CanvasGroup runnigActionCanvasGroup;
    public CanvasGroup pauseUI;
    public CanvasGroup itemGetUI;
    public CanvasGroup endingUICanvasGroup;
    public GameObject pausePopup;
    public GameObject itemGetPopup;
    public TMP_Text itemDescriptionText;
    public CanvasGroup tiredUICanvasGroup; // Tired UI
    
    [Header("Settings Popup UI")]
    public GameObject settingsPopup;
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;
    public TMP_Dropdown languageDropdown;

    public bool IsItemPopupActive { get; private set; }
    
    private Sequence tiredBlinkSequence; // Tired UI ������ ������

    public void SetConcentration(float current, float max)
    {
        if (concentrationSlider == null) return;
        concentrationSlider.maxValue = max;
        concentrationSlider.value = current;
    }

    public void FadeOutTitleUI(float duration = 1f, Action onComplete = null)
    {
        if (titleUICanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        titleUICanvasGroup.DOFade(0f, duration).OnComplete(() =>
        {
titleUICanvasGroup.gameObject.SetActive(false);
            onComplete?.Invoke();
     });
    }

    public void FadeInTitleUI(float duration = 1f, Action onComplete = null)
    {
        if (titleUICanvasGroup == null) 
        {
            onComplete?.Invoke();
            return;
        }

        titleUICanvasGroup.gameObject.SetActive(true);
        titleUICanvasGroup.DOFade(1f, duration).OnComplete(() => onComplete?.Invoke());
    }

    public void SetPauseUIActive(bool active)
    {
        if (pauseUI == null) return;
        
        if (active)
        {
            pauseUI.gameObject.SetActive(true);
            pauseUI.alpha = 0f;
            pauseUI.DOFade(1f, 0.3f);
        }
        else
        {
            pauseUI.DOFade(0f, 0.3f).OnComplete(() =>
            {
                pauseUI.gameObject.SetActive(false);
            });
        }
    }

    public void SetItemGetUIActive(bool active)
    {
        if (itemGetUI == null) return;
        
        if (active)
        {
            itemGetUI.gameObject.SetActive(true);
            itemGetUI.alpha = 0f;
            itemGetUI.DOFade(1f, 0.3f);
        }
        else
        {
            itemGetUI.DOFade(0f, 0.3f).OnComplete(() =>
            {
                itemGetUI.gameObject.SetActive(false);
            });
        }
    }

    /// <summary>
    /// In Game UI (Pause UI + Item Get UI)�� FadeOut�մϴ�.
    /// </summary>
    public void FadeOutInGameUI(float duration = 0.5f, Action onComplete = null)
    {
        int completedCount = 0;
        int totalAnimations = 0;

        // Pause UI FadeOut
        if (pauseUI != null)
        {
            totalAnimations++;
            pauseUI.DOFade(0f, duration).OnComplete(() =>
            {
                completedCount++;
                if (completedCount == totalAnimations)
                {
                    onComplete?.Invoke();
                }
            });
        }

        // Item Get UI FadeOut
        if (itemGetUI != null)
        {
            totalAnimations++;
            itemGetUI.DOFade(0f, duration).OnComplete(() =>
            {
                completedCount++;
                if (completedCount == totalAnimations)
                {
                        onComplete?.Invoke();
                } 
            });
        }

        // �� �� null�̸� ��� �ݹ� ȣ��
        if (totalAnimations == 0)
        {
         onComplete?.Invoke();
        }
    }

    /// <summary>
    /// In Game UI (Pause UI + Item Get UI)�� FadeIn�մϴ�.
    /// </summary>
    public void FadeInInGameUI(float duration = 0.5f, Action onComplete = null)
    {
        int completedCount = 0;
        int totalAnimations = 0;

        // Pause UI FadeIn
        if (pauseUI != null)
        { 
            totalAnimations++;
            pauseUI.gameObject.SetActive(true);
            pauseUI.DOFade(1f, duration).OnComplete(() =>
            {
                completedCount++;
                if (completedCount == totalAnimations)
                {
                    onComplete?.Invoke();
                }
            });
        }

        // Item Get UI FadeIn
      if (itemGetUI != null)
        {
            totalAnimations++;
            itemGetUI.gameObject.SetActive(true);
            itemGetUI.DOFade(1f, duration).OnComplete(() =>
            { 
                completedCount++;
                if (completedCount == totalAnimations)
                {
                    onComplete?.Invoke();
                }
            });
        }
      
        if (totalAnimations == 0)
        {
            onComplete?.Invoke();
        }
    }

    public void SetPausePopupActive(bool active)
  {
        if (pausePopup == null) return;
     pausePopup.SetActive(active);
    }

    public void ShowItemPopup(string description)
    {
  if (itemGetPopup == null) return;
        if (itemDescriptionText != null) itemDescriptionText.text = description;
 itemGetPopup.SetActive(true);
        IsItemPopupActive = true;
    }

  public void CloseItemPopup()
  {
     if (itemGetPopup == null) return;
        itemGetPopup.SetActive(false);
   IsItemPopupActive = false;
    }

    public void SetRunningActionCanvasActive(bool active)
{
        if (runnigActionCanvasGroup == null) return;
        runnigActionCanvasGroup.gameObject.SetActive(active);
    }

    public void SetEndingUIActive(bool active)
    {
        if (endingUICanvasGroup == null) return;
        endingUICanvasGroup.gameObject.SetActive(active);
    }
    
    [YarnCommand("startTiredUiBlink")]
    public void StartTiredUIBlink()
    {
   if (tiredUICanvasGroup == null) return;

   // ���� �������� ������ ����
        if (tiredBlinkSequence != null) tiredBlinkSequence.Kill();

   // Tired UI
        tiredUICanvasGroup.gameObject.SetActive(true);

        
        tiredBlinkSequence = DOTween.Sequence();
        tiredBlinkSequence
            .Append(tiredUICanvasGroup.DOFade(0f, 1f))  
          .Append(tiredUICanvasGroup.DOFade(1f, 1f))  
            .SetLoops(-1, LoopType.Restart);             
    }
    
    [YarnCommand("stopTiredUiBlink")]
    public void StopTiredUIBlink()
    {
        if (tiredBlinkSequence != null)
        {
            tiredBlinkSequence.Kill();
            tiredBlinkSequence = null;
        }
        
        if (tiredUICanvasGroup != null)
        {
            tiredUICanvasGroup.alpha = 1f;
        }
    }

    // ===== 설정 팝업 함수들 =====

    /// <summary>
    /// 설정 팝업을 엽니다.
    /// </summary>
    public void OpenSettingsPopup()
    {
        if (settingsPopup == null) return;
        settingsPopup.SetActive(true);
        InitializeSettingsUI();
    }

    /// <summary>
    /// 설정 팝업을 닫습니다.
    /// </summary>
    public void CloseSettingsPopup()
    {
        if (settingsPopup == null) return;
        settingsPopup.SetActive(false);
    }

    /// <summary>
    /// 설정 UI의 초기값을 설정합니다.
    /// </summary>
    private void InitializeSettingsUI()
    {
        // 마스터 볼륨 슬라이더 초기화
        if (masterVolumeSlider != null && SoundManager.Instance != null)
        {
            masterVolumeSlider.value = SoundManager.Instance.GetMasterVolume();
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        // BGM 볼륨 슬라이더 초기화
        if (bgmVolumeSlider != null && SoundManager.Instance != null)
        {
            bgmVolumeSlider.value = SoundManager.Instance.GetBGMVolume();
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        // SFX 볼륨 슬라이더 초기화
        if (sfxVolumeSlider != null && SoundManager.Instance != null)
        {
            sfxVolumeSlider.value = SoundManager.Instance.GetSFXVolume();
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        // 언어 드롭다운 초기화
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.AddListener(SetLanguage);
            UpdateLanguageDropdown();
        }
    }

    /// <summary>
    /// 전체 음향 볼륨을 설정합니다.
    /// </summary>
    /// <param name="volume">0~1 범위의 볼륨 값</param>
    public void SetMasterVolume(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMasterVolume(volume);
        }
    }

    /// <summary>
    /// BGM(배경음악) 볼륨을 설정합니다.
    /// </summary>
    /// <param name="volume">0~1 범위의 볼륨 값</param>
    public void SetBGMVolume(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(volume);
        }
    }

    /// <summary>
    /// SFX(효과음) 볼륨을 설정합니다.
    /// </summary>
    /// <param name="volume">0~1 범위의 볼륨 값</param>
    public void SetSFXVolume(float volume)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetSFXVolume(volume);
        }
    }

    /// <summary>
    /// 언어를 설정합니다.
    /// </summary>
    /// <param name="languageIndex">0: 한국어(ko), 1: 영어(en)</param>
    public void SetLanguage(int languageIndex)
    {
        if (LocalizationSettings.AvailableLocales == null) return;

        string localeCode = languageIndex switch
        {
            0 => "ko",  // 한국어
            1 => "en",  // 영어
            _ => "en"   // 기본값
        };

        // 해당 로케일 찾기
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code == localeCode)
            {
                LocalizationSettings.SelectedLocale = locale;
                Debug.Log($"[UIManager] 언어 변경: {localeCode}");
                return;
            }
        }

        Debug.LogWarning($"[UIManager] '{localeCode}' 로케일을 찾을 수 없습니다.");
    }

    /// <summary>
    /// 현재 선택된 언어로 드롭다운을 업데이트합니다.
    /// </summary>
    private void UpdateLanguageDropdown()
    {
        if (languageDropdown == null) return;

        string currentLocaleCode = LocalizationSettings.SelectedLocale.Identifier.Code;
        int selectedIndex = currentLocaleCode switch
        {
            "ko" => 0,  // 한국어
            "en" => 1,  // 영어
            _ => 1      // 기본값
        };

        languageDropdown.value = selectedIndex;
    }

    /// <summary>
    /// 모든 슬라이더와 드롭다운 리스너를 제거합니다. (OnDestroy에서 호출)
    /// </summary>
    private void UnsubscribeSettingsUI()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.RemoveListener(SetBGMVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSFXVolume);

        if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(SetLanguage);
    }

    private void OnDestroy()
    {
        UnsubscribeSettingsUI();
    }
}
