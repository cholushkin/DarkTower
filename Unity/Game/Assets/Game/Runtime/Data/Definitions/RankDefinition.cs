using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// Baseline characteristics for one minion rank (1..5). Effective stats may additionally
    /// reflect completed global research; this asset holds only the authored baseline.
    [CreateAssetMenu(menuName = "Dark Tower/Rank", fileName = "Rank")]
    public sealed class RankDefinition : ScriptableObject
    {
        [SerializeField, Range(1, 5)] private int rank = 1;
        [SerializeField] private string displayName;
        [SerializeField, Min(0)] private int attack;
        [SerializeField, Min(0)] private int defense;
        [SerializeField, Min(0)] private int salaryGoldPerWeek;
        [SerializeField, Min(0f)] private float carryCapacity;

        public int Rank => rank;
        public string DisplayName => displayName;
        public int Attack => attack;
        public int Defense => defense;
        public int SalaryGoldPerWeek => salaryGoldPerWeek;
        public float CarryCapacity => carryCapacity;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[Rank {rank}] '{displayName}' attack={attack} defense={defense} " +
                $"salaryGoldPerWeek={salaryGoldPerWeek} carryCapacity={carryCapacity}", this);
        }
    }
}
