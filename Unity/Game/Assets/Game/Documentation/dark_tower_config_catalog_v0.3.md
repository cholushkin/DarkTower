# Dark Tower — Configuration Catalog

**Version:** 0.3 — Configuration design (balancing values pending)  
**Companion:** `dark_tower_game_design_v0.5.md`  
**Primary format:** Godot `.tres` resources backed by typed custom `Resource` classes (`.gd`).  
**Scope:** Designer-authored data, relationships, and gameplay data contracts. Runtime implementation, UI layout, and numerical balancing are outside this document.

## 1. Data Principles

- **Definitions and state are separate.** `.tres` files define game content and parameters; timers, inventory quantities, owned minions, progress, and current balances exist in runtime state.
- **Definitions have stable IDs** (`item_id`, `mission_id`, `room_id`, `improvement_id`, etc.) for cross-references and validation.
- **All items are stackable.** Inventory and Treasure Room store item quantities by type.
- **Item classification uses tags** (`tags: Array[StringName]`). The `treasure` tag routes an item to the Treasure Room; other items go to Inventory.
- **Inventory and Treasure Room have unlimited storage.** Capacity restrictions apply only to what a raid squad can bring back during Claim.
- **Data owns tunable values.** Rank defines base unit stats and salary; Item defines carry size and sale price; Mission defines difficulty and discoveries; Room Level defines room capabilities; Lab Improvement defines permanent effects; Global Balance defines shared formulas.
- Time values use a common unit. Randomized parameters are expressed as ranges and/or probabilities in the relevant data definitions.

## 2. Configuration Groups

### 2.1 `StartingBalance`

Initial state for a new game, represented by one resource.

| Field | Purpose |
| --- | --- |
| `starting_gold`, `starting_crystals` | Initial currency balances |
| `starting_minions[]` | Initial minion definitions / ranks and conditions (three minions at game start) |
| `starting_room_levels[]` | Initial level of each room |
| `starting_items[]` | Optional item quantities, routed by tags |
| `starting_completed_improvement_ids[]` | Optional improvements active at game start |

The first entry in the ordered WorldTiers list is the initial available tier.

### 2.2 `Ranks`

Five ranked unit definitions.

| Field | Purpose |
| --- | --- |
| `rank` | Rank number (`1..5`) |
| `name` | Rookie, Grunt, Raider, Veteran, Captain |
| `attack`, `defense` | Base characteristics associated with the rank |
| `salary_gold_per_week` | Weekly salary for one unit of this rank |
| `carry_capacity` | Potentially fractional item-carrying capacity of one unit |

Each completed training assignment advances the minion by one rank. Attack and Defense derive from its rank, with global laboratory effects applied where relevant. Training Room level controls the highest rank the room can train toward.

### 2.3 `Items`

Every physical item — ordinary loot, ingredients, relics, and treasures — has one definition.

| Field | Purpose |
| --- | --- |
| `id`, `name` | Item identity and display label |
| `tags[]` | Gameplay tags, including `treasure` where applicable |
| `carry_size` | Carrying budget consumed by one item copy |
| `sell_price_gold` | Fixed Gold received when selling one copy |

**Storage:** Items tagged `treasure` are automatically added to the Treasure Room and stacked with existing items of the same ID. Other items stack in Inventory. Both stores are unlimited. Every item can be sold.

**Claim:** Discoveries are shown as a flat list, **one item copy per icon**, including repeated copies of the same item. The game preselects items up to the squad's carry budget. Players may deselect entries and select alternatives; the final selection must fit. Identical selected item copies are stacked when credited to storage.

**Carrying budget:**

- Squad capacity = `floor(sum(member.carry_capacity))`.
- Each selected copy consumes its item's `carry_size`.
- A stack of five discovered items is therefore represented by five selectable icons at Claim, even though it becomes one stack in storage.
- Gold and Crystals do not consume capacity; prisoners are also handled separately.

For example, three minions carrying `1.5` each provide a capacity of `floor(4.5) = 4`. If each cup consumes one unit, four cups can be selected from six discovered cups.

### 2.4 `WorldTiers`

One resource describing tier order, mission membership, and map placement.

| Field | Purpose |
| --- | --- |
| `tiers[]` | Ordered tiers; array order establishes progression |
| `tiers[i].mission_ids[]` | Missions belonging to that tier |
| `mission_positions[]` | Position of each mission on the world map, associated with its mission ID |

**Progression:**

- The first tier is initially available.
- A mission receives a permanent green checkmark after its **first successful Claim**.
- Completing all missions of the current tier unlocks the next tier.
- Successful Claim of every mission on the map triggers victory.

**Validation:** Mission references resolve to defined missions; each mission belongs to exactly one tier; positions reference known missions.

### 2.5 `Missions`

One definition per mission. Its mission tier and map location are supplied by `WorldTiers`.

| Field | Purpose |
| --- | --- |
| `id`, `name`, `description` | Identity and player-facing text |
| `required_power` | Target strength used by the raid outcome formula |
| `base_duration` | Baseline raid duration |
| `base_injury_chance` | Injury baseline on a successful raid |
| `gold_range`, `crystal_range` | Discovered currency ranges |
| `item_rewards[]` | Configurable item discovery pools, quantities/ranges, and probabilities |
| `prisoner_count_range` | Number of potential prisoner rewards |
| `prisoner_rank_distribution[]` | Rank composition of potential prisoner rewards |

Discovery quantities and probabilities are designer-authored mission data, with randomized results determined by the applicable formulas. The precise internal draw algorithm can be specified when filling in reward configs; the Claim presentation is independent of that choice.

**Raid lifecycle:** A mission launched during a game week becomes unavailable until the next week, regardless of success or failure. Its squad and results remain pending until Claim. A failed raid produces no rewards and returns all participants Injured and Tired. On success, Gold/Crystals, selected items, and admissible prisoners are credited on Claim.

### 2.6 `Rooms` / `RoomLevels`

Each room definition contains its sequence of levels. A level describes the room's capabilities and upgrade requirements.

| Field | Purpose |
| --- | --- |
| `room_id`, `name`, `type` | Room identity and function |
| `levels[].level` | Room level |
| `levels[].upgrade_cost` | Gold, Crystals, and/or item quantities required to start an upgrade |
| `levels[].upgrade_duration` | Time required for the room upgrade |
| `levels[].slots` | Concurrent stations, beds, or cells for relevant room types |
| Room-specific level parameters | Activity speed/duration, effectiveness, rank limit, and similar capabilities |

| Room | Level-dependent parameters |
| --- | --- |
| Accommodations | Recovery beds, Tired recovery duration, maximum minion population |
| Med Bay | Treatment beds, treatment duration |
| Training Room | Stations, time per training, maximum attainable Rank |
| Prison | Cell count, prisoner processing duration and recruitment modifiers |
| Laboratory | Laboratory level unlocks available improvements; one improvement runs at a time |
| Treasure Room | Relevant room progression parameters, if any; item storage remains unlimited |

**Upgrade flow:** Pay the configured cost, start an upgrade timer, apply the new room level when the timer completes. The precise effect of an upgrade on activities already running can be handled during implementation.

### 2.7 `LabImprovements`

Permanent, globally applied improvements unlocked by Laboratory level.

| Field | Purpose |
| --- | --- |
| `id`, `name`, `description` | Improvement identity and player-facing text |
| `required_lab_level` | Minimum Laboratory level at which it is available |
| `cost` | Gold, Crystals, and/or item amounts paid when starting |
| `duration` | Processing time |
| `effects[]` | Global parameter additions or multipliers |

**Improvement flow:**

1. Reaching the required Laboratory level makes an improvement available.
2. The player pays the cost and starts its timer, provided the Laboratory is idle.
3. **Only one Laboratory improvement may be in progress at a time.**
4. At completion, the improvement becomes active globally and is marked completed.
5. Effects apply to units and future raids. Raids already in progress use the stats captured when they launched.

Each improvement can be completed once. Specific modifier composition can be settled together with effect data.

### 2.8 `GlobalBalance` / `GameRules`

Shared numerical parameters and formula coefficients.

| Group | Parameters |
| --- | --- |
| Game clock | Duration of an in-game day; seven-day payroll interval |
| Combat power | Attack/Defense weights and rank effects |
| Raid outcome | Failure curve based on squad power vs. required mission power |
| Injuries | Rank/Defense/difficulty modifiers and probability bounds |
| Raid duration | Squad-size/Rank adjustments and duration bounds |
| Prison | Recruitment probability and processing-time formulas |
| Recruited conditions | Always `Tired`; `Injured` chance = `0.5` |
| Carry calculation | Sum member capacities, floor once, apply item carry sizes |

**Payroll:** Every seven in-game days, the scheduler pauses at a blocking salary popup. The payable amount is the sum of the owned minions' rank-specific salaries. Confirming deducts Gold; a negative resulting balance causes bankruptcy and Game Over. Normal gameplay provides no pause or time-speed controls.

### 2.9 Reusable reward tables (optional)

Where missions share discovery distributions, they may reference reusable typed Reward Table resources. Mission-local definitions are also valid.

## 3. Suggested Godot Resource Layout

```text
res://Game/Data/
  starting_balance.tres
  global_balance.tres
  world_tiers.tres
  ranks/
    rookie.tres
    grunt.tres
    raider.tres
    veteran.tres
    captain.tres
  items/
    axe.tres
    golden_cup.tres
    ...
  missions/
    forest_camp.tres
    ...
  rooms/
    accommodations.tres
    med_bay.tres
    training.tres
    prison.tres
    laboratory.tres
    treasure_room.tres
  lab_improvements/
    fury_spell.tres
    improved_swords.tres
    improved_defense.tres
    ...
```

Custom Godot `Resource` scripts provide typed exported fields. Helper Resource types may be used for ranges, costs, item quantities, tiers, mission positions, effects, and reward groups. Separate files for content definitions are a useful authoring layout, not a gameplay requirement.

## 4. Runtime State

Stored in memory during a session:

- Current Gold, Crystals, day/week, and payroll-blocking state.
- Owned minions, current Rank, conditions, assignments and derived stats.
- Unlimited item stacks in Inventory and Treasure Room.
- Room levels, upgrade timers, occupied process slots and recovery/training queues.
- Active raids, squad membership, last mission launch week, resolved discoveries, pending Claims, and completed mission checkmarks.
- Current prisoners, occupied cells, prisoner-processing timers and outcomes.
- Running and completed Laboratory improvements, plus their active effects.
- Scheduler events, completion times, and debug controls.

The current design has no save system or offline progression.

## 5. Data Authoring Order

1. **Ranks:** base Attack/Defense, salary and carrying capacity.
2. **Items:** IDs, tags, carry sizes and fixed sale prices.
3. **StartingBalance:** initial minions, funds, items and room levels.
4. **Rooms:** upgrade prices and timers, slots, training caps, processing rates, population limits and prison effectiveness.
5. **LabImprovements:** unlock level, price, duration and global effects.
6. **Missions:** power, duration, injuries, reward distributions and prisoners.
7. **WorldTiers:** ordered mission membership and positions.
8. **GlobalBalance:** shared formulas, then simulation of raids and payroll across several weeks.

## 6. Design Decisions to Finalize Alongside Data

The overall resource groups are sufficient to begin authoring. A few contracts should be finalized when they directly affect the first concrete configs:

- **Reward distribution contract:** The meaning of item pool entries and their probabilities/count ranges (separate from the already-defined one-icon-per-item Claim interface).
- **Laboratory effect evaluation:** How multiple additive and multiplicative effects combine into derived Attack, Defense, and other parameters.
- **Upgrade transition:** How finishing a room upgrade affects processes that were already running.

Specific mission coordinate conventions and `.tres` file granularity can be decided during implementation without altering the gameplay model.
