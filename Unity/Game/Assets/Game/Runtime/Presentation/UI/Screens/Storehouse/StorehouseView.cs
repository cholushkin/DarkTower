using UnityEngine;

namespace Game
{
    public sealed class StorehouseView : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private StorehouseSlotView _slotPrefab;
        [SerializeField] private StorehouseItemView _itemPrefab;

        public StorehouseItemView CreateItem()
        {
            return Instantiate(_itemPrefab, _content);
        }

        public void CreateEmptySlot()
        {
            Instantiate(_slotPrefab, _content);
        }

        public void Clear()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                var child = _content.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}