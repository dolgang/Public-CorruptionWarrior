using Defines;
using System;

public class CollectionData
{
    #region Fields

    // Deserialize Fields
    private int index;
    private int collectionLevel;
    private string group;
    private string type;
    private int rarityCondition;
    private int levelCondition;
    private string statusType;
    private int statusValue;

    #endregion

    #region Properties

    public int Index => index;
    public int CollectionLevel => collectionLevel;
    public ECollectionCategory CollectionCategory { get; private set; }
    public ECollectionType CollectionType { get; private set; }
    public ERarity Rarity { get; private set; }
    public int LevelCondition => levelCondition;
    public EStatusType StatusType { get; private set; }
    public int StatusValue { get; private set; }
    public float StatusValueFloat { get; private set; }

    #endregion

    /// <summary>
    /// CSVSerializer를 통해 생성되어 생성자가 정상 작동 되지 않아 외부에서 초기화 해주는 메서드
    /// </summary>
    public void Initalize()
    {
        ProcessFields();
    }

    // 그대로 쓸 수 없는 값들을 알맞게 변환
    private void ProcessFields()
    {
        Rarity = (ERarity)rarityCondition;
        CollectionCategory = Enum.Parse<ECollectionCategory>(group);
        CollectionType = Enum.Parse<ECollectionType>(type);
        StatusType = Enum.Parse<EStatusType>(statusType);

        // 스텟 구조로 인해 float 타입 따로 담기
        if (StatusType == EStatusType.DMG_REDU || StatusType == EStatusType.CRIT_CH || StatusType == EStatusType.ATK_SPD || StatusType == EStatusType.MOV_SPD)
        {
            StatusValueFloat = statusValue * 0.01f;
        }
        else
        {
            StatusValue = statusValue;
        }
    }
}
