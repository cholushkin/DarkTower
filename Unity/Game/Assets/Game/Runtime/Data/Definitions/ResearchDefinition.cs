using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// A Laboratory research project: a uniquely authored permanent global improvement.
    /// Only one research can be active at a time; each completes once.
    [CreateAssetMenu(menuName = "Dark Tower/Research", fileName = "Research")]
    public sealed class ResearchDefinition : ScriptableObject, IIdentifiedDefinition
    {
        [SerializeField] private string researchId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField, Min(0)] private int researchTier;
        [SerializeField] private ResearchPrerequisites prerequisites;
        [SerializeField] private ResourceCost cost;
        [SerializeField] private GameDuration duration;
        [SerializeField] private GlobalEffect[] effects;

        public string Id => researchId;
        public string DisplayName => displayName;
        public string Description => description;
        public int ResearchTier => researchTier;
        public ResearchPrerequisites Prerequisites => prerequisites;
        public ResourceCost Cost => cost;
        public GameDuration Duration => duration;
        public GlobalEffect[] Effects => effects;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[Research:{researchId}] '{displayName}' tier={researchTier} " +
                $"cost(gold={cost.Gold},crystals={cost.Crystals}) duration={duration.Value}{duration.Unit} " +
                $"effects={effects?.Length ?? 0}", this);
        }
    }
}
