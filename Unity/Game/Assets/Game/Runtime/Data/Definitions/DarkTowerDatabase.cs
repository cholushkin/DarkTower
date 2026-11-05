using System.Collections.Generic;
using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// Master designer-authored catalog: the single entry point that references every definition
    /// set and the shared rules/balance assets. Also the root for editor-time validation.
    /// Lookups are lazily built on first access and treated as read-only during a session.
    [CreateAssetMenu(menuName = "Dark Tower/Database", fileName = "DarkTowerDatabase")]
    public sealed class DarkTowerDatabase : ScriptableObject
    {
        [Header("Content")]
        [SerializeField] private RankDefinition[] ranks;
        [SerializeField] private ItemDefinition[] items;
        [SerializeField] private MissionDefinition[] missions;
        [SerializeField] private RoomDefinition[] rooms;
        [SerializeField] private RoomUpgradeDefinition[] roomUpgrades;
        [SerializeField] private TrainingTransitionDefinition[] trainingTransitions;
        [SerializeField] private ResearchDefinition[] research;
        [SerializeField] private RewardTableDefinition[] rewardTables;
        [SerializeField] private EventDefinition[] events;

        [Header("World")]
        [SerializeField] private WorldMapDefinition worldMap;

        [Header("Rules & balance")]
        [SerializeField] private GlobalBalanceDefinition globalBalance;
        [SerializeField] private StartingBalanceDefinition startingBalance;
        [SerializeField] private TowerLevelProgressionDefinition towerLevelProgression;
        [SerializeField] private StorehouseRulesDefinition storehouseRules;
        [SerializeField] private RecoveryRulesDefinition recoveryRules;
        [SerializeField] private DungeonRulesDefinition dungeonRules;
        [SerializeField] private WarRoomIntelligenceDefinition warRoomIntelligence;

        public IReadOnlyList<RankDefinition> Ranks => ranks;
        public IReadOnlyList<ItemDefinition> Items => items;
        public IReadOnlyList<MissionDefinition> Missions => missions;
        public IReadOnlyList<RoomDefinition> Rooms => rooms;
        public IReadOnlyList<RoomUpgradeDefinition> RoomUpgrades => roomUpgrades;
        public IReadOnlyList<TrainingTransitionDefinition> TrainingTransitions => trainingTransitions;
        public IReadOnlyList<ResearchDefinition> Research => research;
        public IReadOnlyList<RewardTableDefinition> RewardTables => rewardTables;
        public IReadOnlyList<EventDefinition> Events => events;

        public WorldMapDefinition WorldMap => worldMap;
        public GlobalBalanceDefinition GlobalBalance => globalBalance;
        public StartingBalanceDefinition StartingBalance => startingBalance;
        public TowerLevelProgressionDefinition TowerLevelProgression => towerLevelProgression;
        public StorehouseRulesDefinition StorehouseRules => storehouseRules;
        public RecoveryRulesDefinition RecoveryRules => recoveryRules;
        public DungeonRulesDefinition DungeonRules => dungeonRules;
        public WarRoomIntelligenceDefinition WarRoomIntelligence => warRoomIntelligence;

        private Dictionary<string, ItemDefinition> _itemsById;
        private Dictionary<string, MissionDefinition> _missionsById;
        private Dictionary<string, RoomUpgradeDefinition> _upgradesById;
        private Dictionary<string, ResearchDefinition> _researchById;
        private Dictionary<int, RankDefinition> _ranksByRank;

        public ItemDefinition GetItem(string id) => Lookup(ref _itemsById, items, d => d.Id, id);
        public MissionDefinition GetMission(string id) => Lookup(ref _missionsById, missions, d => d.Id, id);
        public RoomUpgradeDefinition GetUpgrade(string id) => Lookup(ref _upgradesById, roomUpgrades, d => d.Id, id);
        public ResearchDefinition GetResearch(string id) => Lookup(ref _researchById, research, d => d.Id, id);

        public RankDefinition GetRank(int rank)
        {
            if (_ranksByRank == null)
            {
                _ranksByRank = new Dictionary<int, RankDefinition>();
                if (ranks != null)
                {
                    foreach (var r in ranks)
                    {
                        if (r != null && !_ranksByRank.ContainsKey(r.Rank))
                            _ranksByRank[r.Rank] = r;
                    }
                }
            }
            return _ranksByRank.TryGetValue(rank, out var def) ? def : null;
        }

        /// Clears cached lookups so they rebuild. Call from editor tooling after editing the catalog.
        public void InvalidateCaches()
        {
            _itemsById = null;
            _missionsById = null;
            _upgradesById = null;
            _researchById = null;
            _ranksByRank = null;
        }

        private static TValue Lookup<TValue>(ref Dictionary<string, TValue> cache, TValue[] source,
            System.Func<TValue, string> idSelector, string id) where TValue : class
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (cache == null)
            {
                cache = new Dictionary<string, TValue>();
                if (source != null)
                {
                    foreach (var entry in source)
                    {
                        if (entry == null) continue;
                        var key = idSelector(entry);
                        if (!string.IsNullOrEmpty(key) && !cache.ContainsKey(key))
                            cache[key] = entry;
                    }
                }
            }
            return cache.TryGetValue(id, out var value) ? value : null;
        }

        [Button]
        private void RebuildCaches()
        {
            InvalidateCaches();
            Debug.Log("[DarkTowerDatabase] Caches invalidated; will rebuild lazily on next lookup.", this);
        }

        [Button]
        private void PrintCatalogSummary()
        {
            Debug.Log($"[DarkTowerDatabase] ranks={ranks?.Length ?? 0} items={items?.Length ?? 0} " +
                $"missions={missions?.Length ?? 0} rooms={rooms?.Length ?? 0} roomUpgrades={roomUpgrades?.Length ?? 0} " +
                $"trainingTransitions={trainingTransitions?.Length ?? 0} research={research?.Length ?? 0} " +
                $"rewardTables={rewardTables?.Length ?? 0} events={events?.Length ?? 0}", this);
        }

        [Button]
        private void PrintItemsInfo()
        {
            if (items == null || items.Length == 0)
            {
                Debug.Log("[DarkTowerDatabase] No items in catalog.", this);
                return;
            }

            foreach (ItemTag tag in System.Enum.GetValues(typeof(ItemTag)))
            {
                if (tag == ItemTag.None) continue;
                int count = 0;
                foreach (var item in items)
                    if (item != null && (item.Tags & tag) != 0) count++;
                Debug.Log($"[DarkTowerDatabase] Tag {tag}: {count} item(s).", this);
            }

            ItemDefinition cheapest = null;
            ItemDefinition mostExpensive = null;
            foreach (var item in items)
            {
                if (item == null) continue;
                if (cheapest == null || item.BaseValueGold < cheapest.BaseValueGold) cheapest = item;
                if (mostExpensive == null || item.BaseValueGold > mostExpensive.BaseValueGold) mostExpensive = item;
            }

            if (cheapest != null)
                Debug.Log($"[DarkTowerDatabase] Cheapest item: '{cheapest.Id}' ({cheapest.BaseValueGold}g).", this);
            if (mostExpensive != null)
                Debug.Log($"[DarkTowerDatabase] Most expensive item: '{mostExpensive.Id}' ({mostExpensive.BaseValueGold}g).", this);
        }

        [Button]
        private void PrintMissionsInfo()
        {
            if (missions == null || missions.Length == 0)
            {
                Debug.Log("[DarkTowerDatabase] No missions in catalog.", this);
                return;
            }

            foreach (var mission in missions)
            {
                if (mission == null) continue;
                Debug.Log($"[DarkTowerDatabase] Mission '{mission.Id}': gold={mission.GoldRange.Min}-{mission.GoldRange.Max} " +
                    $"crystals={mission.CrystalRange.Min}-{mission.CrystalRange.Max} " +
                    $"itemRewards={mission.ItemRewards?.Length ?? 0} prisoners={mission.PrisonerCountRange.Min}-{mission.PrisonerCountRange.Max}", this);
            }
        }
    }
}
