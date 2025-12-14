using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Yarn.Unity;
using System;

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
    public CanvasGroup tiredUICanvasGroup; // Tired UI 추가

    public bool IsItemPopupActive { get; private set; }
    
    private Sequence tiredBlinkSequence; // Tired UI 깜박임 시퀀스

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
    /// In Game UI (Pause UI + Item Get UI)를 FadeOut합니다.
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

        // 둘 다 null이면 즉시 콜백 호출
        if (totalAnimations == 0)
        {
         onComplete?.Invoke();
        }
    }

    /// <summary>
    /// In Game UI (Pause UI + Item Get UI)를 FadeIn합니다.
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

        // 둘 다 null이면 즉시 콜백 호출
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

    /// <summary>
    /// Tired UI를 깜박이게 합니다. 함수를 다시 호출할 때까지 계속 깜박입니다.
    /// </summary>
    [YarnCommand("startTiredUiBlink")]
    public void StartTiredUIBlink()
    {
   if (tiredUICanvasGroup == null) return;

   // 기존 시퀀스가 있으면 중지
        if (tiredBlinkSequence != null) tiredBlinkSequence.Kill();

   // Tired UI 활성화
        tiredUICanvasGroup.gameObject.SetActive(true);

        // 깜박이는 시퀀스 생성 (무한 반복)
        tiredBlinkSequence = DOTween.Sequence();
        tiredBlinkSequence
            .Append(tiredUICanvasGroup.DOFade(0f, 0.3f))  // 0.3초에 투명하게
          .Append(tiredUICanvasGroup.DOFade(1f, 0.3f))  // 0.3초에 불투명하게
            .SetLoops(-1, LoopType.Restart);              // 무한 반복
    }

/// <summary>
    /// Tired UI 깜박임을 중지합니다.
    /// </summary>
    [YarnCommand("stopTiredUiBlink")]
    public void StopTiredUIBlink()
{
        if (tiredBlinkSequence != null)
        {
            tiredBlinkSequence.Kill();
            tiredBlinkSequence = null;
        }

        // Tired UI를 비활성화하거나 원래 상태로 복원
        if (tiredUICanvasGroup != null)
 {
        tiredUICanvasGroup.alpha = 1f;
      }
    }
}
