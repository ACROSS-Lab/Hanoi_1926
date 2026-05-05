using System.Collections;
using TMPro;
using UnityEngine;

public class GameTimeManager : MonoBehaviour 
{
    [SerializeField] TextMeshProUGUI dayText;
    [SerializeField] TextMeshProUGUI monthText;
    [SerializeField] TextMeshProUGUI hourText;

    [SerializeField] GameTime startSimulationTime = new GameTime()
    {
        month = 7,
        day = 23,
        hour = 0
    };

    int lastRenderedDay = -1;
    int lastRenderedMonth = -1;
    int lastRenderedHour = -1;

    void Start()
    {
        UpdateUITexts(startSimulationTime);
    }

    void UpdateUITexts(GameTime currentTime)
    {
        if (currentTime.day != lastRenderedDay)
        {
            dayText.text = currentTime.day.ToString("00");
            lastRenderedDay = currentTime.day;
        }

        if (currentTime.month != lastRenderedMonth)
        {
            string key = GameTime.monthKeys[currentTime.month - 1];
            monthText.text = LocalizationManager.Instance.GetLocalizedValue(key);
            lastRenderedMonth = currentTime.month;
        }

        if (currentTime.hour != lastRenderedHour)
        {
            hourText.text = $"{currentTime.hour:D2}h";
            lastRenderedHour = currentTime.hour;
        }
    }

    public void AdvanceTime(GameTime startTime, GameTime endTime, float duration)
    {
        StartCoroutine(LerpTimeRoutine(startTime, endTime, duration));
    }

    private IEnumerator LerpTimeRoutine(GameTime startTime, GameTime endTime, float duration)
    {
        GameTime currentTime;

        float startHours = startTime.ToTotalHours();
        float endHours = endTime.ToTotalHours();
        
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentTotalHours = Mathf.Lerp(startHours, endHours, t);
            currentTime = GameTime.FromTotalHours(currentTotalHours);
            UpdateUITexts(currentTime);
            yield return null;
        }

        currentTime = GameTime.FromTotalHours(endHours);
        UpdateUITexts(currentTime);
    }

}