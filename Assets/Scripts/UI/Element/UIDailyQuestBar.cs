using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIDailyQuestBar : UIBase
{
    #region Fields

    [SerializeField] private Image currencyImage;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text count;
    [SerializeField] private Slider countBar;
    [SerializeField] private Button button;
    [SerializeField] private GameObject dimScreen;

    private DailyAchievement dailyAchivement;

    #endregion

    #region Properties

    public DailyAchievement DailyAchievement => dailyAchivement;

    #endregion

    public void ShowUI(DailyAchievement achievement)
    {
        dailyAchivement = achievement;
        InitalizeBtn();

        currencyImage.sprite =
        CurrencyManager.instance.GetIcon((ECurrencyType)(int)dailyAchivement.GetRewardType());
        currencyText.text = dailyAchivement.GetRewardAmount().ToString();

        title.text = dailyAchivement.Title;

        UpdateUI();
    }

    public void UpdateUI()
    {
        // 못깼는데 갱신되는 상황에서 제거 (일단 넣음)
        dailyAchivement.onUpdateCounter -= UpdateCount;

        UpdateCount(dailyAchivement.count);
        UpdateButton();
        SetEventLink();
    }

    private void SetEventLink()
    {
        if (!dailyAchivement.isComplete)
        {
            dailyAchivement.onUpdateCounter += UpdateCount;
            dailyAchivement.onComplete += (achievement) =>
            {
                UpdateButton();
                dailyAchivement.onUpdateCounter -= UpdateCount;
            };
        }
    }

    private void UpdateCount(int count)
    {
        this.count.text = (count > dailyAchivement.Goal)? $"{dailyAchivement.Goal} / {dailyAchivement.Goal}" : $"{count} / {dailyAchivement.Goal}";
        countBar.value = (float)count / dailyAchivement.Goal;
    }

    private void UpdateButton()
    {
        button.gameObject.SetActive(!(dailyAchivement.isRewarded && dailyAchivement.isComplete));
        dimScreen.gameObject.SetActive(dailyAchivement.isRewarded);
        button.interactable = dailyAchivement.isComplete;
    }

    private void InitalizeBtn()
    {
        button.onClick.AddListener(() =>
        {
            if (dailyAchivement.isRewarded || !dailyAchivement.isComplete) return;

            dailyAchivement.GetReward();
            UpdateButton();
        });
    }
}
