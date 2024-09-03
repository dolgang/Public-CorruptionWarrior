using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class UIDailyQuestPanel : UIPanel
{
    [SerializeField] private UIDailyQuestBar questPrefab;
    [SerializeField] private Transform questRoot;
    private CustomPool<UIDailyQuestBar> questPool;

    private int questCount;
    private int pointCount;
    [SerializeField] private Slider pointBar;
    [SerializeField] private List<UIDailyAchievePointReward> pointRewardUI;

    public override UIBase InitUI(UIBase parent)
    {
        questPool = EasyUIPooling.MakePool(questPrefab, questRoot,
            x => x.actOnCallback += () => questPool.Release(x),
            x => x.transform.SetAsLastSibling(), null, 0);

        InitializeBtns();

        return this;
    }

    /// <summary>
    /// DailyQuest가 세팅 된 후 최초 1회 UI 생성
    /// </summary>
    public void SetDailyQuestUI(List<DailyAchievement> achievements, 
        List<DailyAchievePointInfo> pointInfos, int achievePoint)
    {
        InitDailyQuest(achievements);
        InitAchievePoint(pointInfos);
        UpdatePoint(achievePoint);
        SortBars();

        QuestManager.instance.DailyQuestHandler.ModifyPoint += UpdatePoint;
        QuestManager.instance.DailyQuestHandler.OnReceiveReward += SortBars;
    }

    public override void ShowUI()
    {
        ShowQuestUI();

        base.ShowUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();

        questPool.Clear();
    }

    protected override void InitializeBtns()
    {
        base.InitializeBtns();
    }

    /// <summary>
    /// 일일 초기화 시 사용하는 메서드
    /// </summary>
    public void ResetAllUI()
    {
        questPool.Clear();

        for (int i = 0; i < questCount; i++)
        {
            var ui = questPool.Get();
            ui.UpdateUI();
        }

        for (int i = 0; i < pointCount; i++)
        {
            pointRewardUI[i].UpdateUI();
        }
    }

    private void SortBars()
    {
        var sorted = questPool.UsedList.OrderBy(bar => !bar.DailyAchievement.isRewarded)
                                       .ThenBy(bar => bar.DailyAchievement.isComplete)
                                       .ThenBy(bar => bar.DailyAchievement.GetID());

        foreach (var bar in sorted)
        {
            bar.transform.SetAsFirstSibling();
        }
    }

    private void InitDailyQuest(List<DailyAchievement> achievements)
    {
        questCount = achievements.Count;

        for (int i = 0; i < achievements.Count; i++)
        {
            var ui = questPool.Get();
            ui.ShowUI(achievements[i]);
        }
    }

    private void InitAchievePoint(List<DailyAchievePointInfo> pointInfos)
    {
        pointCount = pointInfos.Count;

        for (int i = 0; i < pointInfos.Count; i++)
        {
            pointRewardUI[i].ShowUI(pointInfos[i]);
        }
    }

    private void UpdatePoint(int point)
    {
        pointBar.value = (float)point / questCount;
    }

    private void ShowQuestUI()
    {
        var questPoolCount = questPool.UnusedCount;
        for(int i = 0; i < questPoolCount; i++)
        {
            var bar = questPool.Get();
            bar.ShowUI();
        }

        SortBars();
    }
}
