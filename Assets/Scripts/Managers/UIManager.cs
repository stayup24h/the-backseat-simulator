using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System;

public class UIManager : SingletonBehaviour<UIManager>
{
    [Header("UI Elements")]
    public Slider concentrationSlider;
    public CanvasGroup titleUICanvasGroup;
    public CanvasGroup runnigActionCanvasGroup;
    public CanvasGroup inGameUI;
    public GameObject pausePopup;
    public GameObject itemGetPopup;
    public TMP_Text itemDescriptionText;

    public bool IsItemPopupActive { get; private set; }

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

    public void SetInGameUIActive(bool active)
    {
        if (inGameUI == null) return;
        inGameUI.gameObject.SetActive(active);
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
}

