using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// Dungeon processing parameters and prisoner-outcome configuration.
    /// TODO: exact three-outcome evaluation weighting beyond rank modifiers is still a balance call.
    /// Prisoner Rank sort direction at Claim is now authored via SortHighRankFirst (Task 05) rather
    /// than hardcoded, since the design leaves the direction itself undecided.
    [CreateAssetMenu(menuName = "Dark Tower/Rules/Dungeon", fileName = "DungeonRules")]
    public sealed class DungeonRulesDefinition : ScriptableObject
    {
        [SerializeField] private GameDuration baseProcessingDuration;
        [SerializeField] private RankModifier[] rankDurationModifiers;
        [SerializeField] private RankModifier[] rankRecruitmentModifiers;
        [SerializeField] private PrisonerOutcomeDistribution outcomeDistribution;
        [SerializeField] private bool recruitedTired = true;
        [SerializeField] private Probability recruitedInjuredProbability = new Probability(0.5f);
        [SerializeField] private EventDefinition[] interrogationEvents;

        // ADDED (Task 05): the design/catalog explicitly leaves the Claim-time prisoner sort
        // direction undecided ("ordering direction is a configurable/design detail to finalize").
        // Rather than hardcode a direction, it is now a designer-authored switch; default (false)
        // sorts ascending (low rank first), matching the field's natural default value.
        [SerializeField] private bool sortHighRankFirst;

        public GameDuration BaseProcessingDuration => baseProcessingDuration;
        public RankModifier[] RankDurationModifiers => rankDurationModifiers;
        public RankModifier[] RankRecruitmentModifiers => rankRecruitmentModifiers;
        public PrisonerOutcomeDistribution OutcomeDistribution => outcomeDistribution;
        public bool RecruitedTired => recruitedTired;
        public Probability RecruitedInjuredProbability => recruitedInjuredProbability;
        public EventDefinition[] InterrogationEvents => interrogationEvents;
        public bool SortHighRankFirst => sortHighRankFirst;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[DungeonRules] baseProcessingDuration={baseProcessingDuration.Value}{baseProcessingDuration.Unit} " +
                $"recruitedTired={recruitedTired} recruitedInjuredProbability={recruitedInjuredProbability.Value} " +
                $"sortHighRankFirst={sortHighRankFirst} interrogationEvents={interrogationEvents?.Length ?? 0}", this);
        }
    }
}
