using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class UICollectionPanel : UIPanel
{
    #region Fields

    [SerializeField] private UICollectionBar collectionPrefab;
    [SerializeField] private RectTransform collectionRoot;
    [SerializeField] private Toggle[] categoryToggles;
    [SerializeField] private ToggleGroup categoryGroup;
    private CustomPool<UICollectionBar> collectionPool;
    private LinkedList<UICollectionBar> collectionList;
    [SerializeField] private ScrollRect scrollRect;

    private ECollectionCategory currentCategory;

    #endregion

    public override UIBase InitUI(UIBase parent)
    {
        collectionPool = EasyUIPooling.MakePool(collectionPrefab, collectionRoot,
            x => x.actOnCallback += () => collectionPool.Release(x),
            x => x.transform.SetAsLastSibling(), null, 0);

        collectionList = new LinkedList<UICollectionBar>();

        // 왠지 버튼 연결이 안돼서 자체 초기화
        InitializeBtns();

        return this;
    }

    /// <summary>
    /// CollectionManger가 초기화 될 때 UI도 처음에 초기화 시켜줌
    /// </summary>
    public void SetCollectionList()
    {
        var idxList = CollectionManager.instance.GetIndexList();

        for (int i = 0; i < idxList.Count; i++)
        {
            var collection = collectionPool.Get();
            collection.InitalizeUI(idxList[i]);
            collection.OnUpgradeStateChanged += HandleUpgradeStateChanged;
            collectionList.AddLast(collection);
        }
        collectionPool.Clear();
        SortBars();
    }

    public override void ShowUI()
    {
        base.ShowUI();

        OpenTab(currentCategory);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        collectionPool.Clear();
    }

    protected override void InitializeBtns()
    {
        base.InitializeBtns();

        for (int i = 0; i < categoryToggles.Length; i++)
        {
            ECollectionCategory category = (ECollectionCategory)i;
            categoryToggles[i].onValueChanged.AddListener(x =>
            {
                if (x) OpenTab(category);
                else CloseTab(category);
                scrollRect.normalizedPosition = new Vector2(0, 1);
            });
        }
    }


    private void OpenTab(ECollectionCategory category)
    {
        currentCategory = category;
        foreach (var uiBar in collectionList)
        {
            if (uiBar.Category == category) uiBar.gameObject.SetActive(true);
        }

    }

    private void CloseTab(ECollectionCategory category)
    {
        foreach (var uiBar in collectionList)
        {
            if (uiBar.Category == category) uiBar.gameObject.SetActive(false);
        }
    }

    private void HandleUpgradeStateChanged(UICollectionBar bar, bool isUpgrade)
    {
        SortBars();
        ReddotTree.instance.CheckCollectionUpgrade(collectionList.Any(bar => bar.IsUpgrade));
    }

    private void SortBars()
    {
        var sorted = collectionList.OrderBy(bar => !bar.IsUpgrade)
                                   .ThenBy(bar => bar.Index)
                                   .ToList();

        foreach (var bar in sorted)
        {
            bar.transform.SetAsLastSibling();
        }
        scrollRect.normalizedPosition = new Vector2(0, 1);
    }
}
