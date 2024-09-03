using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollectionInfo
{
    #region Fields

    private int index;
    private int level;
    private bool isUpgrade;

    private List<CollectionData> collectionDatas;
    private CollectionData currentData;

    public event Action<bool> OnUpgradePossible;
    public event Action<int> OnUpgrade;

    #endregion

    #region Properties

    public int Index => index;
    public int Level => level;
    public bool IsUpgrade
    {
        get { return isUpgrade; }
        private set
        {
            isUpgrade = value;
            OnUpgradePossible?.Invoke(value);
        }
    }
    public int LimitLevel => collectionDatas.Count - 1;

    #endregion

    public CollectionInfo(int index)
    {
        this.index = index;
        collectionDatas = new List<CollectionData>();
    }

    /// <summary>
    /// 실행 시 인덱스별로 도감 정보 하나하나 쌓아주는 용도
    /// </summary>
    public void AddCollectionData(CollectionData collectionData)
    {
        collectionDatas.Add(collectionData);
    }

    /// <summary>
    /// 도감 정보 다 쌓고 오름차순 정렬 후 초기화
    /// </summary>
    public void Initalize()
    {
        collectionDatas.Sort((x, y) => x.CollectionLevel.CompareTo(y.CollectionLevel));
        // 모든 info 초기화
        foreach (var data in collectionDatas)
        {
            data.Initalize();
        }

        currentData = GetCollectionData();
        SubscribeEvent();
    }

    public CollectionData GetCollectionData()
    {
        return collectionDatas[Level];
    }

    /// <summary>
    /// info의 다음 레벨 정보를 반환합니다. 없으면 null을 반환합니다.
    /// </summary>
    public CollectionData GetNextCollectionData()
    {
        if (LimitLevel <= level) return collectionDatas[Level];
        else return collectionDatas[Level + 1];
    }

    public void OnCollectionUpgrade(int index)
    {
        level++;
        OnUpgrade?.Invoke(index);
        if (level < LimitLevel) SubscribeEvent();
        Save();
        CheckInitalUpgrade();
    }

    private void SubscribeEvent()
    {
        if (currentData.CollectionCategory == ECollectionCategory.Equipment)
        {
            CollectionManager.instance.OnSkillCollectionCheck -= CheckType;
            CollectionManager.instance.OnEquipmentCollectionCheck += CheckType;
        }
        else if (currentData.CollectionCategory == ECollectionCategory.Skill)
        {
            CollectionManager.instance.OnEquipmentCollectionCheck -= CheckType;
            CollectionManager.instance.OnSkillCollectionCheck += CheckType;
        }

        
    }

    private void UnsubscribeEvent()
    {
        CollectionManager.instance.OnSkillCollectionCheck -= CheckType;
        CollectionManager.instance.OnEquipmentCollectionCheck -= CheckType;
    }

    private List<int> GetCurrentLevel(CollectionData data)
    {
        List<int> levels = new List<int>();

        switch (data.CollectionType)
        {
            case ECollectionType.Weapon:
                var weaponInfos = EquipmentManager.instance.GetRarityWeapons(data.Rarity);
                levels = weaponInfos.Select(x => x.enhancementLevel).ToList();
                break;
            case ECollectionType.Armor:
                var armorInfos = EquipmentManager.instance.GetRarityArmors(data.Rarity);
                levels = armorInfos.Select(x => x.enhancementLevel).ToList();
                break;
            case ECollectionType.Active:
                var activeSkills = SkillManager.instance.GetSkillsOnRarity(data.Rarity).Where(s => s is ActiveSkillData || s is BuffSkillData).ToList();
                levels = activeSkills.Select(s => s.levelFrom0).ToList();
                break;
            case ECollectionType.Passive:
                var passiveSkills = SkillManager.instance.GetSkillsOnRarity(data.Rarity)
                                    .OfType<PassiveSkillData>()
                                    .ToList();
                levels = passiveSkills.Select(x => x.levelFrom0).ToList();
                break;
            default:
                return levels;
        }

        return levels;
    }

    #region Upgrade Check Event

    /// <summary>
    /// 초기화 및 업그레이드 직후 재검사 하여 업그레이드 가능 여부 판단하는 메서드
    /// </summary>
    public void CheckInitalUpgrade()
    {
        var data = GetNextCollectionData();

        var currentLevels = GetCurrentLevel(data);
        CheckInfoLevelUp(currentLevels, data.LevelCondition);
    }

    private void CheckType(ERarity rarity, List<Equipment> equipmentList)
    {
        if (currentData.Rarity != rarity) return;

        if (currentData.CollectionType == ECollectionType.Weapon)
        {
            var weaponList = equipmentList.OfType<WeaponInfo>().ToList();
            if (weaponList.Count > 0)
            {
                var levels = weaponList.Select(x => x.enhancementLevel).ToList();
                var nextData = GetNextCollectionData();
                CheckInfoLevelUp(levels, nextData.LevelCondition);
            }
        }
        else if (currentData.CollectionType == ECollectionType.Armor)
        {
            var ArmorList = equipmentList.OfType<ArmorInfo>().ToList();
            if (ArmorList.Count > 0)
            {
                var levels = equipmentList.Select(x => x.enhancementLevel).ToList();
                var nextData = GetNextCollectionData();
                CheckInfoLevelUp(levels, nextData.LevelCondition);
            }
        }
    }

    private void CheckType(ERarity rarity, List<BaseSkillData> skillList)
    {
        if (currentData.Rarity != rarity) return;

        if (currentData.CollectionType == ECollectionType.Active)
        {
            List<int> activeSkillsLevel = new List<int>();

            for (int i = 0; i < skillList.Count; i++)
            {
                // 일단 액티브의 경우 액티브랑 버프타입은 모두 포함시키는 것으로
                if (skillList[i] is ActiveSkillData || skillList[i] is BuffSkillData)
                {
                    activeSkillsLevel.Add(skillList[i].levelFrom0);
                }
            }
            if (activeSkillsLevel.Count > 0)
            {
                var nextData = GetNextCollectionData();
                CheckInfoLevelUp(activeSkillsLevel, nextData.LevelCondition);
            }
        }
        else if (currentData.CollectionType == ECollectionType.Passive)
        {
            var passiveSkills = skillList.OfType<PassiveSkillData>().ToList();
            if (passiveSkills.Count > 0)
            {
                var levels = passiveSkills.Select(x => x.levelFrom0).ToList();
                var nextData = GetNextCollectionData();
                CheckInfoLevelUp(levels, nextData.LevelCondition);
            }
        }
    }

    private void CheckInfoLevelUp(List<int> levels, int levelCondition)
    {
        // 최고 레벨일 때 즉시 불가 처리
        if (level >= LimitLevel)
        {
            IsUpgrade = false;
            return;
        }

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] < levelCondition)
            {
                IsUpgrade = false;
                return;
            }
        }

        IsUpgrade = true;
        UnsubscribeEvent();
        Debug.Log($"Collection_{index} is Can Upgrade!");
    }

    #endregion

    #region Save & Load

    public void Save()
    {
        DataManager.Instance.Save($"Collection_{index}_Level", level);
    }

    public void Load()
    {
        level = DataManager.Instance.Load<int>($"Collection_{index}_Level", 0);
    }

    #endregion
}
