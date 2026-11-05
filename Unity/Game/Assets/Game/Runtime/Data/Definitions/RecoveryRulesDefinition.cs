using Alchemy.Inspector;
using UnityEngine;

namespace Game
{
    /// Quarters (rest) and Infirmary (treatment) recovery parameters.
    /// TODO: bed-wait accrual for occupied minions and treatment-queue tie-breaking are open decisions.
    [CreateAssetMenu(menuName = "Dark Tower/Rules/Recovery", fileName = "RecoveryRules")]
    public sealed class RecoveryRulesDefinition : ScriptableObject
    {
        [Header("Quarters")]
        [SerializeField] private GameDuration restDuration = new GameDuration(6f, DurationUnit.Hours);
        [SerializeField, Range(0f, 24f)] private float nightfallHourOfDay = 20f;
        [SerializeField] private GameDuration bedShortageDeparture = new GameDuration(2f, DurationUnit.Days);
        [SerializeField] private RestAssignmentPriority restAssignmentPriority = RestAssignmentPriority.LongestTiredFirst;

        [Header("Infirmary")]
        [SerializeField] private GameDuration treatmentDuration = new GameDuration(3f, DurationUnit.Days);

        public GameDuration RestDuration => restDuration;
        public float NightfallHourOfDay => nightfallHourOfDay;
        public GameDuration BedShortageDeparture => bedShortageDeparture;
        public RestAssignmentPriority RestAssignmentPriority => restAssignmentPriority;
        public GameDuration TreatmentDuration => treatmentDuration;

        /// Design rule: Injured is handled before Tired; a minion with both is routed to Infirmary.
        public bool InjuredBeforeTired => true;
        /// Design rule: completing treatment clears both Injured and Tired.
        public bool TreatmentClearsBoth => true;

        [Button]
        private void PrintInfo()
        {
            Debug.Log($"[RecoveryRules] restDuration={restDuration.Value}{restDuration.Unit} " +
                $"nightfallHourOfDay={nightfallHourOfDay} bedShortageDeparture={bedShortageDeparture.Value}{bedShortageDeparture.Unit} " +
                $"restAssignmentPriority={restAssignmentPriority} treatmentDuration={treatmentDuration.Value}{treatmentDuration.Unit}", this);
        }
    }
}
