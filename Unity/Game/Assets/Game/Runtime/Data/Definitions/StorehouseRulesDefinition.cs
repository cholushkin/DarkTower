using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// Storehouse storage and sales rules.
    /// InitialStorageSlots is a fallback starting-capacity value only — StartingBalanceDefinition
    /// .InitialRoomCapabilities.StorageSlots is the authoritative source and is used whenever a
    /// StartingBalanceDefinition is assigned and its StorageSlots is non-zero (see SessionStateFactory).
    /// TODO: active-sale concurrency, the moment a stack is reserved/consumed, and half-price
    /// rounding are open design decisions.
    [CreateAssetMenu(menuName = "Dark Tower/Rules/Storehouse", fileName = "StorehouseRules")]
    public sealed class StorehouseRulesDefinition : ScriptableObject
    {
        [SerializeField, Min(0)] private int initialStorageSlots;
        [SerializeField] private SaleDurationRule saleDurationRule;
        [SerializeField, Range(0f, 1f)] private float instantSaleMultiplier = 0.5f;
        [SerializeField] private SaleRoundingRule priceRoundingRule = SaleRoundingRule.Floor;

        public int InitialStorageSlots => initialStorageSlots;
        public SaleDurationRule SaleDurationRule => saleDurationRule;
        public float InstantSaleMultiplier => instantSaleMultiplier;
        public SaleRoundingRule PriceRoundingRule => priceRoundingRule;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[StorehouseRules] initialStorageSlots={initialStorageSlots} " +
                $"instantSaleMultiplier={instantSaleMultiplier} priceRoundingRule={priceRoundingRule}", this);
        }
    }
}
