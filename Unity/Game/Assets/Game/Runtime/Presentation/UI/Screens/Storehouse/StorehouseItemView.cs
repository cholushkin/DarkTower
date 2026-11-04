using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class StorehouseItemView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _quantity;

        public void SetIcon(Sprite icon)
        {
            _icon.sprite = icon;
        }

        public void SetName(string itemName)
        {
            _name.text = itemName;
        }

        public void SetQuantity(int quantity)
        {
            _quantity.text = quantity.ToString();
        }
    }
}