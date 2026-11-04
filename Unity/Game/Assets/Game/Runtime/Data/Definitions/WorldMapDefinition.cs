using UnityEngine;

namespace Game
{
    /// The ordered world layout: concentric tier rings, the missions placed on each, and their
    /// map positions. Order of tiers determines ring distance from the central tower.
    [CreateAssetMenu(menuName = "Dark Tower/World Map", fileName = "WorldMap")]
    public sealed class WorldMapDefinition : ScriptableObject
    {
        [SerializeField] private WorldTierEntry[] tiers;

        [Header("Presentation (optional)")]
        [SerializeField, Min(0f)] private float ringSpacing;

        public WorldTierEntry[] Tiers => tiers;
        public float RingSpacing => ringSpacing;

        /// Index of the tier that holds the given mission, or -1 if not placed.
        public int FindTierIndexOf(MissionDefinition mission)
        {
            if (tiers == null || mission == null) return -1;
            for (int i = 0; i < tiers.Length; i++)
            {
                var missions = tiers[i].Missions;
                if (missions == null) continue;
                for (int j = 0; j < missions.Length; j++)
                {
                    if (missions[j].Mission == mission) return i;
                }
            }
            return -1;
        }
    }
}
