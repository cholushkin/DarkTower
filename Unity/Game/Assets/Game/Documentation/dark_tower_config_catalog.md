# Dark Tower — Configuration Catalog

**Version:** 0.6 — Configuration specification (balancing values pending)  
**Engine:** Unity  
**Companion:** `dark_tower_game_design.md`  
**Primary authoring format:** Unity **ScriptableObject** assets (`.asset`) with strongly typed serialized definitions.  
**Scope:** Designer-authored data, cross-references, progression, and runtime data contracts. Final balance values and concrete C# class implementations are separate tasks.

## 1. Data Principles

1. **Definitions and runtime state are separate.** ScriptableObject assets describe game content, costs, rules, and progression. Game-state objects track owned units, timers, quantities, mission progress, and active effects.
2. **Definitions have stable IDs** (`item_id`, `mission_id`, `upgrade_id`, `research_id`, etc.). Unity asset references may be used alongside IDs, but gameplay persistence and validation should not depend on scene-object instance IDs.
3. **Tower Level belongs to the tower.** It is derived from hidden Tower XP and its own progression table. Rooms use individual Upgrade definitions and prerequisite chains.
4. **Every item has a configurable stack limit.** A stack consumes one shared item-storage slot. The `treasure` tag identifies the **Treasures** display filter; remaining items appear in **Items**.
5. **Minions are individuals, not item stacks.** Inventory displays one icon per minion, including status, population count, and aggregate upkeep.
6. **All tunable values are authored.** Rank determines baseline attributes and salary; Item determines stack size, Carry Size, and sale price; Mission determines its challenges and discoveries; Upgrades and Research specify costs, timers, prerequisites, effects, and XP rewards.
7. **Time values use a documented common unit.** Prefer game-time hours for activity durations and game-time days for week/day boundaries. Data definitions must specify ranges and probabilities explicitly.
8. **The UI does not reveal every authored value.** War Room intelligence stages determine which estimates, item types, and probabilities players may see.

The names in the tables below are **logical data fields**. Exact C# property names, ScriptableObject class layout, validation attributes, and asset granularity may be adjusted during implementation without altering gameplay contracts.

## 2. Configuration Catalog

### 2.1 `StartingBalance`

One starting-state definition for a new run.

| Field | Purpose |
| --- | --- |
| `starting_gold`, `starting_crystals` | Initial currency totals |
| `starting_minions[]` | Initial individual minions, with Rank and conditions (three initially) |
| `starting_item_stacks[]` | Optional starting items, stored using per-item stack limits and shared slots |
| `initial_room_capabilities[]` | Initial counts and rates, such as beds, cells, training stations, storage slots, and unlocked research tier |
| `starting_completed_upgrade_ids[]` | Upgrades already active at game start |
| `starting_completed_research_ids[]` | Research effects already active at game start |
| `starting_tower_xp` | Initial Tower XP, if any |
| `starting_world_unlocks[]` | Available missions/tier state and optional initial secret/event flags |

The first World Tier in the ordered WorldTiers definition is initially accessible.

### 2.2 `RankDefinition` / `Ranks`

Five rank definitions, one for each rank.

| Field | Purpose |
| --- | --- |
| `rank` | Rank number, 1 through 5 |
| `display_name` | Rookie, Grunt, Raider, Veteran, Captain |
| `attack`, `defense` | Baseline combat characteristics |
| `salary_gold_per_week` | Weekly upkeep paid for an owned minion of this Rank |
| `carry_capacity` | Individual Carry Capacity; may be fractional |

Training increases Rank by exactly one; the minion's baseline stats and salary then derive from the new Rank. Effective stats may also reflect completed global research.

### 2.3 `ItemDefinition` / `Items`

A single definition describes each physical item, including weapons, materials, magic items, and treasure.

| Field | Purpose |
| --- | --- |
| `item_id`, `display_name`, `description`, `icon` | Identity and presentation |
| `tags[]` | Gameplay categories; `treasure` marks the Treasures Inventory filter |
| `max_stack_size` | Maximum copies in **one Inventory stack/slot**; positive integer |
| `carry_size` | Carry budget cost for **one discovered copy**; positive integer |
| `sell_price_gold` | Fixed full sale value of one copy in Gold |
| `other_gameplay_tags[]` (if needed) | Ingredient, mission, or other data-driven use cases |

Illustrative stack limits:

| Item | `max_stack_size` |
| --- | ---: |
| Golden Cup | 5 |
| Cow | 1 |
| Cobblestone | 30 |

**Inventory contract:** Treasures and Items share one pool of item slots. The storage algorithm can combine copies into non-full stacks up to `max_stack_size`, then allocate additional free slots. Any overflow at Claim is silently discarded. Inventory category filters do not change capacity accounting.

**Raid Report contract:** The screen shows one selectable icon **per item copy**, regardless of how those copies will later be stacked. Selection costs `carry_size` per copy.

**Selling contract:** A sale targets **one entire stack**. Full stack value equals `quantity × sell_price_gold`; instant sale pays half. Normal sale duration depends on stack quantity and current Sale Efficiency effects.

### 2.4 `WorldTiers` / `WorldMapDefinition`

One ordered world definition governs mission placement and tier progression.

| Field | Purpose |
| --- | --- |
| `tiers[]` | Ordered World Tiers; order determines their concentric ring from the central tower |
| `tiers[i].tier_id` | Stable tier identity |
| `tiers[i].mission_ids[]` | Missions on that ring |
| `mission_positions[]` | Map coordinates for individual mission markers |
| `world_visual_parameters` (optional) | Designer-controlled ring spacing and map presentation |

**Progression:**

- The first tier is accessible at the start.
- A mission receives a permanent completion checkmark after its **first successful Claim**.
- Claiming **all missions** in the current tier opens the next tier.
- Successfully Claiming every mission in the world at least once triggers victory.
- Mission/event definitions can use configured secret or event conditions for location visibility and access.

**Validation:** Every mission exists, belongs to exactly one World Tier, and has a defined map position. Ring ordering and mission IDs resolve consistently.

### 2.5 `MissionDefinition` / `Missions`

One definition for each individually selectable village/location/mission.

| Field | Purpose |
| --- | --- |
| `mission_id`, `display_name`, `description`, `icon` | Identity and World Map presentation |
| `required_power` | Enemy strength/difficulty input for outcome calculations |
| `base_duration` | Baseline raid duration |
| `base_injury_chance` | Injury baseline on a successful raid |
| `gold_range`, `crystal_range` | Discovered currency distributions |
| `item_rewards[]` | Item references, draw-count/amount ranges, and probability parameters |
| `prisoner_count_range` | Number of possible prisoners |
| `prisoner_rank_distribution[]` | Rank distribution of prisoners |
| `unlock_conditions[]` (when applicable) | Requirements such as tier progression, revealed secrets, or event flags |
| `squad_constraints` (when applicable) | Additional mission-specific squad limits |

Reward creation is independent of squad size. The squad impacts combat and Carry Capacity, not the authored reward pool.

**Raid lifecycle:** `RUNNING` → `COMPLETED_UNCLAIMED` → Claimed. Launching a mission consumes its weekly opportunity. It cannot be launched again until a later week **and** any running/unclaimed raid has been resolved. Success/failure outcomes and discoveries become fixed when the raid finishes, but do not enter player balances before Claim.

**Outcome:**

- Success: configured discoveries are generated; all squad members get `Tired`, and each has an independently evaluated `Injured` chance.
- Failure: no discoveries; all squad members get `Tired` and `Injured`.
- Squad members are reserved until Claim, in both cases.

### 2.6 `RoomDefinition` / `RoomUpgradeDefinition`

The seven Room IDs are:

`storehouse`, `quarters`, `infirmary`, `training_hall`, `laboratory`, `dungeon`, `war_room`.

A Room definition supplies base capabilities and references to its available upgrades. Each upgrade is individually authored.

| Field | Purpose |
| --- | --- |
| `room_id`, `display_name` | Room identity |
| `starting_capabilities` | Initial slots/stations/beds, rates, and available features |
| `upgrade_ids[]` | Upgrades offered in this Room |

| Upgrade field | Purpose |
| --- | --- |
| `upgrade_id`, `room_id`, `display_name`, `description` | Stable identity, owner Room, and presentation |
| `series_id`, `step_index` (when sequential) | Sequence membership and its ordered position |
| `cost` | Gold, Crystals, and/or required items |
| `duration` | Construction time |
| `prerequisite_upgrade_ids[]` | Required earlier upgrades |
| `additional_requirements[]` | Other configurable conditions |
| `effects[]` | Capacity/rate changes, unlocks, or other gameplay effects |
| `tower_xp_reward` | Hidden Tower XP awarded by this upgrade; zero when it grants none |

A paid upgrade starts a timer. On completion its configured capability/effect is activated. Ordered upgrade chains are tracked through their completed IDs and defined prerequisites.

**Room-specific upgrade catalog:**

| Room | Upgrade family | Gameplay effect |
| --- | --- | --- |
| **Storehouse** | `storage_capacity` | Adds the configured number of shared item-storage slots |
| | `sale_efficiency` | Sequential steps reduce normal-sale duration |
| **Quarters** | `extra_bed` | Each upgrade adds **one** bed |
| **Infirmary** | `extra_treatment_bed` | Each upgrade adds **one** treatment bed |
| | `treatment_efficiency` | Sequential steps reduce treatment duration |
| **Training Hall** | `extra_training_station` | Each upgrade adds **one** training station |
| | `advanced_training` | Sequentially unlocks rank transitions 1→2 through 4→5 |
| **Laboratory** | `research_tier_unlock` | Sequentially unlocks the next research tier |
| | `research_efficiency` | Sequential steps reduce research duration |
| **Dungeon** | `extra_cell` | Each upgrade adds **one** prisoner cell |
| | `torture_efficiency` | Sequential steps reduce processing duration |
| | `recruitment_efficiency` | Sequential steps increase recruitment chance |
| **War Room** | `enemy_intelligence` | Successively tightens estimated enemy strength ranges |
| | `loot_intelligence` | Gradually reveals possible loot types and estimated drop chances |

Storage Capacity upgrade steps each configure their own **slot increment** and **cost**. Extra Bed, Extra Treatment Bed, Extra Training Station, and Extra Cell upgrades each increase capacity by exactly **one** per purchase.

### 2.7 `QuartersRules` / `InfirmaryRules`

Shared recovery and condition parameters are authored in one or more typed definitions.

| Field | Purpose |
| --- | --- |
| `rest_duration_hours` | Quarters time to clear `Tired`; **6 game hours** |
| `nightfall_time` | Daily point when every owned minion receives `Tired` |
| `bed_shortage_departure_hours` | Time without a suitable bed before departure; **more than 2 game days** |
| `rest_assignment_priority` | Longest current time in `Tired` is assigned first |
| `treatment_duration_hours` | Infirmary base duration; normally **3 game days** |
| `condition_priority` | `Injured` before `Tired`; minions with both are routed to Infirmary |
| `treatment_completion_conditions` | Clears both `Injured` and `Tired` |

**Runtime allocation:** Free minions with relevant conditions take free beds automatically. One bed holds one minion; completed processes vacate beds. The system notifies the player via popup when minions depart because of extended bed shortage.

### 2.8 `TrainingTransitionDefinition`

One entry for each allowed rank transition.

| Field | Purpose |
| --- | --- |
| `from_rank`, `to_rank` | Adjacent rank transition (1→2, 2→3, 3→4, 4→5) |
| `cost` | Transition-specific Gold/Crystals/item requirements |
| `duration` | Transition-specific training duration |
| `required_advanced_training_upgrade_id` | Upgrade that unlocks this transition |

The player manually assigns an eligible, non-Tired, non-Injured minion to a free Training Hall station. Completing training increases their Rank by one and applies `Tired`.

### 2.9 `ResearchDefinition` / `ResearchCatalog`

Each Laboratory research project is a uniquely authored permanent global improvement.

| Field | Purpose |
| --- | --- |
| `research_id`, `display_name`, `description` | Identity and presentation |
| `research_tier` | Tier required for availability |
| `prerequisites[]` | Additional research, upgrade, or gameplay requirements |
| `cost` | Gold, Crystals, and/or item quantities |
| `duration` | Research timer |
| `effects[]` | Global additive/multiplicative/unlock effects |

- Research tiers become available through sequential Laboratory `research_tier_unlock` upgrades.
- Only **one Laboratory research/improvement process** is allowed at a time. No queue is defined.
- Research can complete only once; effects remain globally active.
- Active raid parameters are captured at raid launch and remain unchanged by research completed during that raid.

Illustrative research IDs: `improved_weapons`, `improved_armor`, `fury_spell`.

### 2.10 `DungeonRules` / `PrisonerOutcomes`

| Field | Purpose |
| --- | --- |
| `base_processing_duration` | Baseline torture/interrogation processing duration |
| `rank_duration_modifiers` | Higher-Rank prisoner processing time adjustments |
| `rank_recruitment_modifiers` | Higher-Rank prisoner recruitment difficulty |
| `outcome_distribution` | Configured probabilities/requirements for Recruitment, Interrogation, Death |
| `recruited_tired` | Always true |
| `recruited_injured_probability` | **0.5** |
| `interrogation_secrets_or_events[]` | Possible location unlocks and event effects |

- Processing begins automatically when a prisoner is assigned to a Dungeon cell.
- Dungeon `torture_efficiency` and `recruitment_efficiency` effects modify applicable formulas.
- A recruited minion keeps the prisoner's Rank.
- Rank-sorted captured prisoners automatically fill available cells at Claim; overflow is discarded.
- Interrogation can reveal secrets that unlock map locations or affect configured events.

The precise three-outcome evaluation formula and prisoner sorting direction are determined during the balance/implementation pass.

### 2.11 `WarRoomIntelligenceDefinition`

The War Room has **two independent upgrade series**. Each stage defines what intelligence is visible for a mission.

| Field | Purpose |
| --- | --- |
| `enemy_intelligence_stages[]` | Estimated enemy-strength display rules by achieved upgrade stage |
| `enemy_estimate_precision` | Amount/granularity of estimated range narrowing |
| `loot_intelligence_stages[]` | Reveal rules for resource/item identities and drop estimates |
| `reveal_item_types` | Whether possible item identities can be shown at the current stage |
| `reveal_drop_estimates` | Whether estimated drop chances/ranges can be shown |
| `loot_estimate_precision` | Precision of information when estimates become visible |

Early intelligence stages may hide all possible item identities. Higher stages reveal increasing detail and precision. These rules affect **information disclosure only**; mission enemy strength and reward distributions are defined by Mission data.

### 2.12 `StorehouseRules` / `SaleRules`

| Field | Purpose |
| --- | --- |
| `initial_storage_slots` | Starting shared item capacity, measured in stack slots |
| `sale_duration_rule` | Configurable rule for normal-sale time as a function of stack quantity |
| `sale_efficiency_modifiers` | Duration modifiers from completed Storehouse upgrades |
| `instant_sale_multiplier` | **0.5**, applied to full stack value |
| `sale_price_rounding_rule` | Currency rounding behavior if required by integer balances |

Normal Sale is a timed operation on one selected **full stack**. Instant Sale credits half its normal value without a timer. The number of parallel sales and the precise moment items are reserved/consumed remain defined by future sales scheduling rules.

### 2.13 `TowerLevelProgression`

A dedicated ScriptableObject asset defines the tower's XP-to-Level progression.

| Field | Purpose |
| --- | --- |
| `levels[]` | Ordered Tower Level entries |
| `levels[i].level` | Tower Level identifier |
| `levels[i].xp_required` | XP requirement for that Tower Level |

**Tower XP** is a hidden accumulated score. `RoomUpgradeDefinition.tower_xp_reward` assigns XP to the designated upgrades. Current Tower Level is calculated against the configured requirements. The exact award trigger (purchase versus completion), threshold convention, and direct level benefits are open design decisions.

### 2.14 `GlobalBalance` / `GameRules`

Cross-system rules and coefficients that are not specific to a content definition.

| Group | Configurable parameters |
| --- | --- |
| Game clock | `real_seconds_per_game_day = 60`, game-time hours per day, day/night boundary |
| Payroll | `payday_interval_days = 7`, deadline at end of payday, unpaid consequence `BANKRUPTCY` |
| Quarters | Rest duration, bed priority, departure grace duration |
| Infirmary | Treatment duration, condition priority |
| Combat | Attack/Defense weights and Rank modifiers |
| Raid outcome | Squad power versus required mission power, failure curve |
| Raid injuries | Mission baseline, rank/defense/difficulty modifiers and bounds |
| Raid duration | Squad-size and Rank adjustments |
| Carry | Sum fractional individual capacity, floor **once**, then apply per-copy Carry Size |
| Dungeon | Processing time, Rank difficulty, recruitment/outcome probabilities |
| Sales | Quantity-based duration, speed reductions, instant-sale multiplier |
| Research | Global effect application and stacking policies |
| Tower | XP requirements via dedicated progression asset |

Provisional balancing models can be authored through reusable coefficients or more explicit curves/tables. The companion Game Mechanics Design contains an illustrative combat model.

### 2.15 Optional `RewardTableDefinition` / `EventDefinition`

- **Reward Tables** may hold shared item/resource distributions referenced by multiple missions. Mission-local reward data is also valid.
- **Event Definitions** may express Dungeon Interrogation secrets and their effects on World Map locations or other game events.
- All event and reward references must resolve to known IDs during data validation.

## 3. Unity Asset Organization

An illustrative project layout:

```text
Assets/Game/
  Scripts/DarkTower/
    Data/                  # ScriptableObject classes and serializable data types
    Runtime/               # Runtime state and gameplay systems
  Data/DarkTower/
    StartingBalance.asset
    GlobalBalance.asset
    TowerLevelProgression.asset
    WorldTiers.asset
    Ranks/
      Rookie.asset
      Grunt.asset
      Raider.asset
      Veteran.asset
      Captain.asset
    Items/
      GoldenCup.asset
      Cow.asset
      Cobblestone.asset
    Missions/
      ForestCamp.asset
    Rooms/
      Storehouse.asset
      Quarters.asset
      Infirmary.asset
      TrainingHall.asset
      Laboratory.asset
      Dungeon.asset
      WarRoom.asset
    RoomUpgrades/
      ...
    TrainingTransitions/
      ...
    Research/
      ...
    Intelligence/
      ...
    RewardTables/
      ...
    Events/
      ...
```

Serializable child records or separate ScriptableObject assets can represent costs, ranges, reward entries, effects, unlock requirements, upgrade steps, and stage definitions. Maintain stable IDs and validation regardless of file granularity.

## 4. Runtime State

Keep current gameplay values separate from authoring assets. Representative runtime records include:

| State domain | Contents |
| --- | --- |
| **Clock** | Current game time, day/week, nightfall, active payroll window and deadline |
| **Economy** | Gold, Crystals, active/pending sale operations and completed income |
| **Tower** | Total Tower XP, derived Tower Level, completed/in-progress Room upgrades |
| **Minions** | Individual IDs, Ranks, Conditions, when Tired began, activity, rest/treatment/training assignments |
| **Beds and stations** | Actual Quarters/Infirmary/Training Hall occupancy and completion timers |
| **Inventory** | Shared capacity in slots, item stacks and quantities, Treasure/Item tags through Item definitions |
| **Dungeon** | Prisoners in cells, Rank, active processing timers and resolved outcomes |
| **Research** | Completed projects, current Laboratory process, permanent active global effects |
| **World** | Mission unlock/event flags, tier state, completed-first-success markers |
| **Raids** | Reserved squads, launch weeks, remaining timers, resolved outcomes/discoveries, pending Claims |
| **Intelligence** | Completed War Room upgrades and derived visible enemy/loot estimates |
| **Scheduler** | Pending events, timers, time-bound activity completion and deadline events |

Runtime code must preserve Claim atomicity: opening Raid Report cannot credit rewards or release units. The confirmed Claim operation applies its resource, prisoner, unit-return, and world-progression effects together.

The current game design has no save system or offline progression.

## 5. Suggested Data Authoring Order

1. **Ranks:** base combat stats, salary, and Carry Capacity.
2. **Items:** tags, stack sizes, Carry Size, and sale price per copy.
3. **Global Balance:** game clock, recovery, treatment, payroll, and shared calculation rules.
4. **Starting Balance:** starting minions, currency, storage slots, and Room capabilities.
5. **Room Upgrades:** individual step costs/timers/effects, sequential dependencies, and Tower XP rewards.
6. **Tower Level Progression:** XP requirements for each Tower Level.
7. **Training Transitions and Research:** prices, durations, rank/tier availability, and research effects.
8. **Dungeon and War Room:** prisoner outcomes/secrets and intelligence reveal stages.
9. **Missions/Reward Tables:** difficulty, duration, injuries, discoveries, and prisoners.
10. **World Tiers/Events:** concentric rings, mission positions, and unlock conditions.
11. **Balance verification:** test weekly upkeep, bedtime bed pressure, three-day treatment, sales timing, stack overflow, raid Carry Capacity, mission cooldowns, and Tower XP pacing.

## 6. Contracts to Finalize During Implementation

- **Tower XP:** purchase-versus-completion award event, cumulative-versus-incremental level thresholds, and any Tower-Level-based unlock rules.
- **Upgrade concurrency:** how many construction processes can run simultaneously, handling of extra prerequisites, and effects on processes already running.
- **Training Efficiency:** whether training duration also has its own Room upgrade series.
- **Beds and statuses:** how bed wait duration accumulates for occupied minions; interaction between Infirmary shortages and the two-day departure rule; queue tie-breaking for injured units.
- **Stacked storage:** order of inserting selected reward copies and resolving partial stacks when near capacity.
- **Sales:** active-sale concurrency, item reservation point, quantity-to-duration rule, and half-price rounding.
- **Raid reward draws:** precise weighted/independent draw procedures, count ranges, and default item preselection.
- **Dungeon:** evaluation of all three outcomes, secrets/events, and prisoner Rank sort direction.
- **Intelligence:** exact visibility states and estimation precision at every War Room upgrade step.
- **Global effects:** additive/multiplicative research modifier composition.
- **Scheduler:** event order for simultaneous nightfall, process completion, payday deadline, and Claim-related state changes.
