using Defines;
using Keiwando.BigInteger;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager instance;

    #region Fields

    [SerializeField] private TextAsset collectionInfosCSV;

    private Dictionary<int, CollectionInfo> collectionInfos; // index, level

    public event Action<ERarity, List<Equipment>> OnEquipmentCollectionCheck;
    public event Action<ERarity, List<BaseSkillData>> OnSkillCollectionCheck;

    #endregion

    #region Properties

    #endregion

    #region Unity Event

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    #endregion

    /// <summary>
    /// 게임 매니저 초기화 메서드
    /// </summary>
    public void InitCollectionManager()
    {
        collectionInfos = new Dictionary<int, CollectionInfo>();
        InitCollectionInfos();

        Load();

        foreach (var pair in collectionInfos)
        {
            pair.Value.CheckInitalUpgrade();
            if (pair.Value.Level > 0) InitStatus(pair.Value.GetCollectionData());
        }

        UIManager.instance.TryGetUI<UICollectionPanel>().SetCollectionList();
    }

    #region Get Info Method

    /// <summary>
    /// 패널에서 현재 있는 도감 인덱스 전부 들고가는 용도
    /// </summary>
    public List<int> GetIndexList()
    {
        return collectionInfos.Keys.ToList();
    }

    /// <summary>
    /// 바에서 현재 도감 정보 받아오기
    /// </summary>
    public CollectionInfo GetCollectionInfo(int index)
    {
        collectionInfos.TryGetValue(index, out var info);
        return info;
    }

    /// <summary>
    /// 외부, 내부에서 인덱스에 따른 레벨을 반환받는 메서드
    /// </summary>
    public int GetLevelFromIndex(int index)
    {
        collectionInfos.TryGetValue(index, out var levelData);
        return levelData.Level;
    }

    /// <summary>
    /// info중 하나라도 업그레이드 가능한 지 확인. 레드닷 초기화에 사용
    /// </summary>
    public bool GetAllUpgradeInfo()
    {
        return collectionInfos.Values.Any(info => info.IsUpgrade);
    }

    #endregion

    #region Data Initalize

    private void InitCollectionInfos()
    {
        var csvList = CSVSerializer.Deserialize<CollectionData>(collectionInfosCSV.text);

        // 인덱스 끼리 그룹화
        foreach (var info in csvList)
        {
            if (!collectionInfos.ContainsKey(info.Index))
            {
                collectionInfos[info.Index] = new CollectionInfo(info.Index);
            }
            collectionInfos[info.Index].AddCollectionData(info);
        }

        // info 리스트를 레벨 기준 오름차순으로 정렬
        foreach (var pair in collectionInfos)
        {
            pair.Value.Initalize();
        }
    }

    #endregion

    #region Related To Collection Levelup

    /// <summary>
    /// 장비 레벨업 시 도감 레벨업 여부 판단 이벤트 실행
    /// </summary>
    public void CheckLevelUpEvent(Equipment equipment) 
    {
        if (equipment is WeaponInfo)
        {
            var weaponInfos = EquipmentManager.instance.GetRarityWeapons(equipment.rarity);
            OnEquipmentCollectionCheck?.Invoke(equipment.rarity, weaponInfos.Cast<Equipment>().ToList());
        }
        else if (equipment is ArmorInfo)
        {
            var armorInfos = EquipmentManager.instance.GetRarityArmors(equipment.rarity);
            OnEquipmentCollectionCheck?.Invoke(equipment.rarity, armorInfos.Cast<Equipment>().ToList());
        }
    }

    /// <summary>
    /// 스킬 레벨업 시 도감 레벨업 여부 판단 이벤트 실행
    /// </summary>
    public void CheckLevelUpEvent(BaseSkillData skillData)
    {
        // TODO: 여기서도 타입 체크하고 보내주고도 타입 체크해서 이중 검사하고 있을듯 수정 필요
        if (skillData is ActiveSkillData)
        {
            var skillDatas = SkillManager.instance.GetSkillsOnRarity(skillData.rarity);
            List<BaseSkillData> activeSkillDatas = new List<BaseSkillData>();
            foreach (var skill in skillDatas)
            {
                if (skill is ActiveSkillData) activeSkillDatas.Add(skill);
            }
            OnSkillCollectionCheck?.Invoke(skillData.rarity, activeSkillDatas);
        }
        else if (skillData is PassiveSkillData)
        {
            var skillDatas = SkillManager.instance.GetSkillsOnRarity(skillData.rarity);
            List<BaseSkillData> activeSkillDatas = new List<BaseSkillData>();
            foreach (var skill in skillDatas)
            {
                if (skill is PassiveSkillData) activeSkillDatas.Add(skill);
            }
            OnSkillCollectionCheck?.Invoke(skillData.rarity, activeSkillDatas);
        }
    }
    
    public void CollectionLevelUp(int index)
    {
        collectionInfos.TryGetValue(index, out var collectionInfo);
        var currentData = (collectionInfo.Level > 0)? collectionInfo.GetCollectionData() : null; // 원래 레벨 뽑아두고(0레벨이면 없음)
        collectionInfo.OnCollectionUpgrade(index);
        var nextData = collectionInfo.GetCollectionData(); // 다음 레벨 뽑아서
        ApplyStatus(currentData, nextData); // 스텟 적용
        PlayerManager.instance.UpdateBattleScore();
    }

    #endregion

    #region Status Management

    private void ApplyStatus(CollectionData currentData, CollectionData nextData)
    {
        // 기존꺼 내려주고
        if (currentData != null)
        {
            if (currentData.StatusValue != 0)
                if (currentData.StatusType >= EStatusType.SKILL_DMG && currentData.StatusType <= EStatusType.GOLD_INCREASE)
                    PlayerManager.instance.status.ChangePercentStat(currentData.StatusType, -new BigInteger(currentData.StatusValue));
                else
                    PlayerManager.instance.status.ChangeBaseStat(currentData.StatusType, -new BigInteger(currentData.StatusValue));
            else
                PlayerManager.instance.status.ChangeBaseStat(currentData.StatusType, -currentData.StatusValueFloat);
        }

        // 다음꺼 올려줌
        if (nextData.StatusValue != 0)
            if (nextData.StatusType >= EStatusType.SKILL_DMG && nextData.StatusType <= EStatusType.GOLD_INCREASE)
                PlayerManager.instance.status.ChangePercentStat(nextData.StatusType, new BigInteger(nextData.StatusValue));
            else
                PlayerManager.instance.status.ChangeBaseStat(nextData.StatusType, new BigInteger(nextData.StatusValue));
        else
            PlayerManager.instance.status.ChangeBaseStat(nextData.StatusType, nextData.StatusValueFloat);
    }

    private void InitStatus(CollectionData data)
    {
        if (data.StatusValue != 0)
            if (data.StatusType >= EStatusType.SKILL_DMG && data.StatusType <= EStatusType.GOLD_INCREASE)
                PlayerManager.instance.status.ChangePercentStat(data.StatusType, new BigInteger(data.StatusValue));
            else
                PlayerManager.instance.status.ChangeBaseStat(data.StatusType, new BigInteger(data.StatusValue));
        else
            PlayerManager.instance.status.ChangeBaseStat(data.StatusType, data.StatusValueFloat);
    }

    #endregion

    #region Save & Load

    private void Load()
    {
        foreach (var pair in collectionInfos)
        {
            pair.Value.Load();
        }
    }

    #endregion
}