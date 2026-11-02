using System.Collections.Generic;
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
    }
}
