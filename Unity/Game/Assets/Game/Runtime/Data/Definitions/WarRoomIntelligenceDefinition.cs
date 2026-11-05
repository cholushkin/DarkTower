using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// War Room intelligence reveal stages. Two independent series: enemy strength and loot.
    /// These affect information disclosure only, never the mission's actual values.
    [CreateAssetMenu(menuName = "Dark Tower/Rules/War Room Intelligence", fileName = "WarRoomIntelligence")]
    public sealed class WarRoomIntelligenceDefinition : ScriptableObject
    {
        [SerializeField] private EnemyIntelStage[] enemyIntelligenceStages;
        [SerializeField] private LootIntelStage[] lootIntelligenceStages;

        public EnemyIntelStage[] EnemyIntelligenceStages => enemyIntelligenceStages;
        public LootIntelStage[] LootIntelligenceStages => lootIntelligenceStages;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[WarRoomIntelligence] enemyIntelligenceStages={enemyIntelligenceStages?.Length ?? 0} " +
                $"lootIntelligenceStages={lootIntelligenceStages?.Length ?? 0}", this);
        }
    }
}
