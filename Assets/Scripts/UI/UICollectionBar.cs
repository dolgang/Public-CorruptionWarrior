using Defines;
using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class UICollectionBar : UIBase
{
    #region Fields

    [SerializeField] private UIEquipment equipmentUI;
    [SerializeField] private UISkillIcon skillIconUI;
    [SerializeField] private RectTransform ConditionIconRoot;
    [SerializeField] private TMP_Text collectionStatus;
    [SerializeField] private Button upgradeBtn;
    [SerializeField] private TMP_Text upgradeBtnText;

    private CustomPool<UIEquipment> equipmentPool;
    private CustomPool<UISkillIcon> skillIconPool;

    public event Action<UICollectionBar, bool> OnUpgradeStateChanged;

    private StringBuilder sb = new StringBuilder();

    private CollectionInfo collectionInfo;

    private Vector3 iconSize = new Vector3(0.88f, 0.88f, 1f);

    #endregion

    #region Properties

    public int Index => collectionInfo.Index;
    public bool IsUpgrade => collectionInfo.IsUpgrade;
    public ECollectionCategory Category => collectionInfo.GetCollectionData().CollectionCategory;

    #endregion

    #region Initalize Method

    /// <summary>
    /// CollectionManager에서 초기 세팅 시 Panel을 초기화 시킬 때 Bar에도 인덱스를 넘겨 정보 확인
    /// </summary>
    public void InitalizeUI(int index)
    {
        collectionInfo = CollectionManager.instance.GetCollectionInfo(index);
        PoolInitalize();
        InitalizeBtn();

        var info = CollectionManager.instance.GetCollectionInfo(index);
        info.OnUpgradePossible += UpdateButton;
        info.OnUpgrade += UpdateBar;
        UpdateButton(info.IsUpgrade);

        SetCurrentCollectionData();
        SetNextCollectionData();
    }

    private void PoolInitalize()
    {
        equipmentPool = EasyUIPooling.MakePool(equipmentUI, ConditionIconRoot,
            x => x.actOnCallback += () => equipmentPool.Release(x),
            x => x.transform.SetAsLastSibling(), null, 0);

        skillIconPool = EasyUIPooling.MakePool(skillIconUI, ConditionIconRoot,
            x => x.actOnCallback += () => skillIconPool.Release(x),
            x => x.transform.SetAsLastSibling(), null, 0);
    }

    private void InitalizeBtn()
    {
        upgradeBtn.onClick.AddListener(() => CollectionManager.instance.CollectionLevelUp(Index));
    }

    #endregion

    private void SetCurrentCollectionData()
    {
        var collectionData = collectionInfo.GetCollectionData();

        // 현재 적용 능력치를 1차로 스트링 빌더에 담아둠
        if (collectionInfo.Level == 0) sb.Clear().Append("<color=#808080>");
        else sb.Clear().Append("<color=#ffff00>");
        sb.Append("Lv.").Append(collectionInfo.Level)
            .Append($"</color> {Strings.statusTypeToKor[(int)collectionData.StatusType]} +<color=#30FF2F>");
        if (collectionData.StatusType == EStatusType.DMG_REDU || collectionData.StatusType == EStatusType.CRIT_CH || collectionData.StatusType == EStatusType.ATK_SPD || collectionData.StatusType == EStatusType.MOV_SPD)
        {
            sb.Append(collectionData.StatusValueFloat * 100).Append("</color>%");
        }
        else
        {
            if (collectionData.StatusType >= EStatusType.SKILL_DMG && collectionData.StatusType <= EStatusType.GOLD_INCREASE) sb.Append(collectionData.StatusValue).Append("</color>%");
            else sb.Append(collectionData.StatusValue).Append("</color>");
        }
    }

    private void SetNextCollectionData()
    {
        var collectionData = collectionInfo.GetNextCollectionData();

        if (collectionInfo.Level >= collectionInfo.LimitLevel)
        {
            // 최고레벨이면 텍스트 적용
            collectionStatus.text = sb.ToString();
        }
        else
        {
            // 최고 레벨이 아니면 다음 레벨 능력치를 스트링 빌더에 담고
            sb.Append(" -> ").Append($" {Strings.statusTypeToKor[(int)collectionData.StatusType]} +<color=#30FF2F>");
            if (collectionData.StatusType == EStatusType.DMG_REDU || collectionData.StatusType == EStatusType.CRIT_CH || collectionData.StatusType == EStatusType.ATK_SPD || collectionData.StatusType == EStatusType.MOV_SPD)
            {
                sb.Append(collectionData.StatusValueFloat * 100).Append("</color>%");
            }
            else
            {
                if (collectionData.StatusType >= EStatusType.SKILL_DMG && collectionData.StatusType <= EStatusType.GOLD_INCREASE) sb.Append(collectionData.StatusValue).Append("</color>%");
                else sb.Append(collectionData.StatusValue).Append("</color>");
            }
            // 텍스트 적용
            collectionStatus.text = sb.ToString();
        }

        ShowConditionIcon();
    }

    private void ShowConditionIcon()
    {
        var collectionData = collectionInfo.GetNextCollectionData();

        if (collectionData.CollectionCategory == ECollectionCategory.Equipment)
        {
            for (int i = 0; i < EquipmentManager.instance.MaxLevel; i++)
            {
                // 장비 데이터 받아오기
                var equip = EquipmentManager.TryGetEquipment(sb.Clear().Append(collectionData.CollectionType.ToString()).Append("_").Append(collectionData.Rarity.ToString()).Append($"_{i+1}").ToString());

                // 받아온 데이터로 열어주기. 패널은 받아도 사용 안되는듯
                var equipIcon = equipmentPool.Get();
                equipIcon.ShowUI(equip, collectionInfo);
                // caution: 아이콘의 사이즈를 통째로 줄이고 있으나, 사이즈를 줄여도 LayoutGroup에선 스케일 변화를 감지하지 않습니다.
                // LayoutGroup은 앵커와 sizeDelta만 참고하고 있어 그냥 컴포넌트 설정에서 Spacing과 Padding을 -12로 조절하여 해결했습니다.
                equipIcon.transform.localScale = iconSize;
            }
        }
        else if (collectionData.CollectionCategory == ECollectionCategory.Skill)
        {
            // 스킬 데이터 받아오기
            var skillList = SkillManager.instance.GetSkillsOnRarity(collectionData.Rarity);

            if (collectionData.CollectionType == ECollectionType.Active)
            {
                for (int i = 0; i < skillList.Count; i++)
                {
                    // 일단 액티브의 경우 액티브랑 버프타입은 모두 포함시키는 것으로
                    if (skillList[i] is ActiveSkillData || skillList[i] is BuffSkillData)
                    {
                        var skillIcon = skillIconPool.Get();
                        skillIcon.ShowUI(skillList[i], collectionInfo);
                        skillIcon.transform.localScale = iconSize;
                    }
                }
            }
            else if (collectionData.CollectionType == ECollectionType.Passive)
            {
                for (int i = 0; i < skillList.Count; i++)
                {
                    if (skillList[i] is PassiveSkillData)
                    {
                        var skillIcon = skillIconPool.Get();
                        skillIcon.ShowUI(skillList[i], collectionInfo);
                        skillIcon.transform.localScale = iconSize;
                    }
                }
            }
        }
    }

    private void UpdateButton(bool isUpgrade)
    {
        if (collectionInfo.Level >= collectionInfo.LimitLevel) upgradeBtnText.text = "최고레벨";
        upgradeBtn.interactable = isUpgrade;
        OnUpgradeStateChanged?.Invoke(this, isUpgrade);
    }

    private void UpdateBar(int index)
    {
        var info = CollectionManager.instance.GetCollectionInfo(index);

        if (equipmentPool.UsedCount > 0)equipmentPool.Clear();
        else skillIconPool.Clear();
        SetCurrentCollectionData();
        SetNextCollectionData();
    }
}
