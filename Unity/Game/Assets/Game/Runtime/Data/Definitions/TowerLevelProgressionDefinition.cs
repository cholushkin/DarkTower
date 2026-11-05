using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// The tower's XP-to-Level progression table.
    /// TODO: whether xpRequired entries are per-level increments or cumulative thresholds, the XP
    /// award trigger (purchase vs completion), and any direct level benefits are open decisions.
    [CreateAssetMenu(menuName = "Dark Tower/Tower Level Progression", fileName = "TowerLevelProgression")]
    public sealed class TowerLevelProgressionDefinition : ScriptableObject
    {
        [SerializeField] private TowerLevelEntry[] levels;

        public TowerLevelEntry[] Levels => levels;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[TowerLevelProgression] levels={levels?.Length ?? 0}", this);
        }
    }
}
