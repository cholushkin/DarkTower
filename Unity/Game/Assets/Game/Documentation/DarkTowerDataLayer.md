# Dark Tower — Data Layer

Typed configuration (ScriptableObjects + serializable records) and runtime-state Models for Dark
Tower, following the reusable rMVVM / DataFlow conventions. **Data only** — no gameplay services,
UI, DI scopes, save/load, scheduler, or changes to the imported `com.qcat.rmvvm` package.

All types are in namespace `Game`. The runtime assembly is `DarkTower.Data` (asmdef in
`Runtime/DarkTower/`); R3 and ObservableCollections come from the project's NuGet DLLs
(auto-referenced), so it adds no extra asmdef references. Editor validation lives in the editor-only
`DarkTower.Data.Editor` assembly (`Editor/DarkTower/`).

## Layout

```
Runtime/DarkTower/
  Data/Records/        serializable enums, primitives (GameDuration, IntRange, Probability) and records
  Data/Definitions/    ScriptableObject definitions, rules/balance assets, master DarkTowerDatabase
  Models/              runtime session state (R3 / ObservableCollections) + pure data rules
  Factory/             SessionStateFactory (new-session Model construction, data only)
Editor/DarkTower/      DarkTowerDataValidator (menu: Dark Tower/Validate Data)
```

Reference direction is one-way: Models reference Definitions; Definitions never reference Models.

## Catalog coverage map

| Catalog / design section | Definitions (config) | Runtime Models |
| --- | --- | --- |
| StartingBalance (§2.1) | `StartingBalanceDefinition`, `RoomCapabilities`, `StartingMinion` | `SessionStateFactory` builds all Models |
| Ranks (§2.2) | `RankDefinition` | `MinionModel.Rank`, `EffectiveStats`, `StatCalculator` |
| Items & storage (§2.3, §7) | `ItemDefinition`, `ItemAmount` | `ItemStackModel`, `InventoryModel`, `StorageRules` |
| World tiers (§2.4) | `WorldMapDefinition`, `WorldTierEntry`, `WorldMissionPlacement` | `WorldModel`, `MissionProgressModel`, `CurrentTierIndex` |
| Missions (§2.5) | `MissionDefinition`, `ItemRewardEntry`, `RankWeight`, `SquadConstraints`, `UnlockCondition` | `MissionProgressModel`, `RaidModel` |
| Raids & Claim (§5, §6) | `MissionDefinition`, `GlobalBalanceDefinition` (coefficients) | `RaidModel`, `RaidLaunchSnapshot`, `RaidMember`, `DiscoveredItemCopy`, `CapturedPrisoner`, `CarryRules` |
| Seven rooms (§4) | `RoomDefinition`, `RoomCapabilities` | capacities on `InventoryModel`, `DungeonModel`, `RecoveryModel`, `TrainingModel` |
| Room upgrades (§2.6, §8.2) | `RoomUpgradeDefinition`, `UpgradeEffect` | `TowerModel.CompletedUpgradeIds`, `UpgradeProcessModel` |
| Tower progression (§2.13, §8.1) | `TowerLevelProgressionDefinition`, `TowerLevelEntry` | `TowerModel.TotalXp`, `TowerModel.Level`, `TowerProgressionRules` |
| Quarters / Infirmary (§2.7, §4.2–4.3) | `RecoveryRulesDefinition` | `RecoveryModel`, `BedAssignmentModel`, `MinionModel.TiredSinceGameHours` |
| Training (§2.8, §4.4) | `TrainingTransitionDefinition` | `TrainingModel`, `TrainingAssignmentModel` |
| Research (§2.9, §4.5) | `ResearchDefinition`, `GlobalEffect`, `ResearchPrerequisites` | `ResearchModel`, `ResearchProcessModel`, `CollectActiveEffects` |
| Dungeon (§2.10, §4.6) | `DungeonRulesDefinition`, `PrisonerOutcomeDistribution`, `RankModifier`, `EventDefinition` | `DungeonModel`, `PrisonerModel` |
| War Room intelligence (§2.11, §4.7) | `WarRoomIntelligenceDefinition`, `EnemyIntelStage`, `LootIntelStage` | `IntelligenceModel` |
| Sales (§2.12, §7.3) | `StorehouseRulesDefinition`, `SaleDurationRule` | `SaleOperationModel`, `EconomyModel.ActiveSales` |
| Clock / payroll / global rules (§2.14, §2) | `GlobalBalanceDefinition` | `ClockModel`, `PayrollModel`, `EconomyModel` |
| Effects / events (§2.15) | `UpgradeEffect`, `GlobalEffect`, `EventDefinition`, `RewardTableDefinition` | effects resolved from completed research/upgrade defs; `WorldModel.WorldFlags` |
| All content (master) | `DarkTowerDatabase` (+ id lookups) | `GameSessionModel` (root aggregate) |

## Key invariants represented (not executed)

- One stack = one slot; `max_stack_size` limits copies per stack; `treasure` is a filter flag only.
  Gold/Crystals are currencies (`EconomyModel`), not inventory stacks.
- Discovered item **copies** (`DiscoveredItemCopy`, one per copy) are distinct from stacked inventory;
  selection costs `carry_size`; carry limit floors the summed capacities once (`CarryRules`).
- Prisoners and currency use no carry capacity. Overflow loss happens at Claim — not implemented here.
- A raid is `Pending` while RUNNING, then carries a resolved `RaidOutcome` + fixed discoveries while
  COMPLETED_UNCLAIMED; the squad stays reserved until Claim. Discoveries are never credited pre-Claim.
- A raid captures effective stats at launch (`RaidLaunchSnapshot`); later research does not change it.
- Rooms have no level; progression is completed-upgrade ids plus hidden Tower XP → derived level.
- Mutually-exclusive lifecycle uses enums/pending-result records, not loose parallel booleans.

## Lifecycle / ownership

Models that own R3 resources derive from `ModelBase` and register properties in `Disposables`.
`GameSessionModel` owns every sub-model and disposes them on shutdown; collection-owning models
dispose child models on remove/reset and on their own disposal. The owning VContainer session scope
(future task) disposes the root. Configuration ScriptableObjects are read-only during play.

## Open design decisions (left as config/TODO, not invented)

- **Tower XP:** award on purchase vs completion; XP thresholds cumulative vs incremental
  (`TowerProgressionRules.DeriveLevel` currently reads them as cumulative); whether level grants benefits.
- **Prisoner Rank sort direction** at Claim (high→low vs low→high).
- **Reward draws:** weighted/independent draw procedure, count ranges, default copy preselection.
- **Sales:** concurrency, stack reservation point, quantity→duration function, half-price rounding.
- **Scheduler:** tie-breaking for simultaneous nightfall / payday / raid-finish / upgrade-completion.
- **Research:** additive vs multiplicative global-effect composition order (`StatCalculator` default is add-then-multiply).
- **Intelligence:** exact visible estimate and precision at each War Room stage.
- **Beds:** bed-wait accrual for occupied minions; Infirmary shortage vs the two-day departure rule; treatment-queue tie-break.
- **Upgrades:** concurrent construction limits; effect of completed upgrades on already-running activities.
- **Starting state:** `initial_room_capabilities` is treated as authoritative starting capacity;
  starting completed upgrades/research are recorded but their effects are not re-applied by the factory.

## Not implemented (by scope)

Gameplay services (combat, Claim, payroll processing, sales, recruitment, timers/scheduler, research
effect application, construction, training, economy transactions), UI/ViewModels/Composers/windows,
`LifetimeScope`/DI registration, VitalRouter handlers, persistence/save-load, offline progression,
and any change to the `com.qcat.rmvvm` package.

## Architecture-package note

The installed `com.qcat.rmvvm` package currently ships only the window system (rWS); the rMVVM/DataFlow
Model/ViewModel conventions are documentation, with no base types to inherit. This data layer therefore
follows those conventions with plain classes and does not depend on the package. No package change is
needed for this task; if the package later adds shared Model base types, these Models can adopt them.
