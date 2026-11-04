using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class StorehouseItemView : MonoBehaviour, IDisposable
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _quantity;

        private IDisposable _quantitySubscription;

        public void Bind(ItemStackModel stack)
        {
            _icon.sprite = stack.Item.Icon;
            _name.text = stack.Item.DisplayName;
            SetQuantity(stack.Quantity.CurrentValue);

            _quantitySubscription = stack.Quantity.Subscribe(SetQuantity);
        }

        public void Dispose()
        {
            _quantitySubscription?.Dispose();
            _quantitySubscription = null;
        }

        private void SetQuantity(int quantity)
        {
            _quantity.text = quantity.ToString();
        }
    }
}