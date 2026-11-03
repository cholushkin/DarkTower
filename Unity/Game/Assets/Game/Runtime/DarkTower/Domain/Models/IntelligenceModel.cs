// TODO: exact visible estimates and precision at each War Room stage are open design decisions;
//   this model stores the achieved stages, and a future service derives what is shown from mission
//   data + the stage definitions. No mission values are duplicated here.

using R3;

namespace Game
{
    /// Achieved War Room intelligence stages. Two independent series: enemy strength and loot.
    /// Changing a stage changes player knowledge only, never mission data.
    public sealed class IntelligenceModel : ModelBase
    {
        private readonly ReactiveProperty<int> _enemyStage;
        private readonly ReactiveProperty<int> _lootStage;

        public IntelligenceModel(int enemyStage = 0, int lootStage = 0)
        {
            _enemyStage = new ReactiveProperty<int>(enemyStage);
            _lootStage = new ReactiveProperty<int>(lootStage);
            Disposables.Add(_enemyStage);
            Disposables.Add(_lootStage);
        }

        public ReadOnlyReactiveProperty<int> EnemyStage => _enemyStage;
        public ReadOnlyReactiveProperty<int> LootStage => _lootStage;

        internal void SetEnemyStage(int stage) => _enemyStage.Value = stage;
        internal void SetLootStage(int stage) => _lootStage.Value = stage;
        internal void AdvanceEnemyStage() => _enemyStage.Value++;
        internal void AdvanceLootStage() => _lootStage.Value++;
    }
}
