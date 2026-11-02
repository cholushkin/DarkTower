using UnityEngine;

namespace Game
{
    /// New-run starting state: currencies, the three initial minions, optional item stacks, initial
    /// room capabilities, pre-completed upgrades/research, starting Tower XP, and initial world unlocks.
    /// This is authoring data for a new session only; it is not a save system.
    [CreateAssetMenu(menuName = "Dark Tower/Starting Balance", fileName = "StartingBalance")]
    public sealed class StartingBalanceDefinition : ScriptableObject
    {
        [SerializeField, Min(0)] private int startingGold;
        [SerializeField, Min(0)] private int startingCrystals;
        [SerializeField] private StartingMinion[] startingMinions;
        [SerializeField] private ItemAmount[] startingItemStacks;
        [SerializeField] private RoomCapabilities initialRoomCapabilities;
        [SerializeField] private RoomUpgradeDefinition[] startingCompletedUpgrades;
        [SerializeField] private ResearchDefinition[] startingCompletedResearch;
        [SerializeField, Min(0)] private long startingTowerXp;
        [SerializeField] private WorldUnlockEntry[] startingWorldUnlocks;
        [SerializeField] private string[] startingSecretFlags;

        public int StartingGold => startingGold;
        public int StartingCrystals => startingCrystals;
        public StartingMinion[] StartingMinions => startingMinions;
        public ItemAmount[] StartingItemStacks => startingItemStacks;
        public RoomCapabilities InitialRoomCapabilities => initialRoomCapabilities;
        public RoomUpgradeDefinition[] StartingCompletedUpgrades => startingCompletedUpgrades;
        public ResearchDefinition[] StartingCompletedResearch => startingCompletedResearch;
        public long StartingTowerXp => startingTowerXp;
        public WorldUnlockEntry[] StartingWorldUnlocks => startingWorldUnlocks;
        public string[] StartingSecretFlags => startingSecretFlags;
    }
}
