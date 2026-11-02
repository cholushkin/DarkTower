# Dark Tower — Game Mechanics Design

**Version:** 0.5 — Working specification  
**Genre:** Fantasy time-management / idle-inspired strategy  
**Scope:** Gameplay rules and designer-authored data requirements. Detailed UI layouts, art, implementation code, and numerical balancing are out of scope.  
**Convention:** Rules below are confirmed unless expressly labeled **proposed**. Previously suggested combat and torture formulas are preserved as **provisional balancing formulas**, not yet approved. Their coefficients belong in configuration.

## 1. Core Loop and Game Outcomes

The player is a Dark Lord managing a tower and minions. They select missions, assign squads, wait for raids, **Claim** the results, recover and train minions, recruit prisoners, sell or use rewards, and invest in rooms and research.

**Loop:** Assign squad → Raid → Claim and choose items → Recover / recruit / train → Upgrade / research → Advance through world tiers → Pay weekly salaries.

The main views are **Raids**, **Inventory**, and **World Map**; selecting a room opens its management view. A small portal near the tower represents raid departures and returns.

- The game begins with **three minions**. Other initial conditions are specified by `StartingBalance`.
- **Victory:** Every individual mission on the entire world map must have been **successfully completed and Claimed at least once**.
- **Defeat:** Insufficient Gold at weekly payroll results in **Game Over — Bankruptcy**. Raids themselves never kill minions or cause Game Over.

## 2. Game Time, Scheduler, and Payroll

- **1 real minute = 1 in-game day**; **7 real minutes = 1 in-game week** at normal speed.
- Time advances only while the game is running. There are **no saves and no offline progression**.
- All timed processes use the shared scheduler: raids, recovery, training, research, prisoner processing, weekly payroll.
- The player sees a day counter and sun/moon cycle.
- In normal gameplay, the player **cannot pause or accelerate time**. Opening ordinary management views does **not** stop the clock. The player must manage resources and Claim rewards **before payday**.
- A debug-only scheduler window lists all processes and progress bars and allows force-completing processes and globally accelerating time. These are not player features.

### 2.1 Weekly payroll

At the end of every seventh in-game day:

1. All gameplay timers **freeze** and a **blocking** payroll popup opens. No selling, upgrades, or claiming is possible in the popup.
2. The popup shows the number of owned minions, Gold available, total Gold owed, and the expected remaining balance; a Rank breakdown may also be shown.
3. The player presses **OK**; an animation deducts total salaries from the current Gold balance **before** the popup closes.
4. If the balance becomes negative, show **Game Over — Bankruptcy**. Otherwise resume time.

Every owned minion is paid a Rank-based weekly salary in **Gold only**, regardless of assignment or conditions. Prisoners are not paid until recruited. Salary values are in Rank data.

`weekly_salary = sum(rank_data[minion.rank].salary_gold for minion in owned_minions)`

Only Gold **already credited** to the player counts. Unclaimed rewards cannot pay salaries. Gold purchases are permitted only when the player can afford them; normal purchases cannot make a currency balance negative.

### 2.2 Events at the same timestamp

**Proposed deterministic scheduler order:** (1) finish regular processes due at the boundary and record their results; (2) apply completed automated effects such as treatment/training/recruitment; (3) update the week and mission cooldown eligibility; (4) calculate the payroll snapshot and open the blocking popup; (5) resume after payment if the player survives.

A raid that completes at the payroll boundary becomes **Claimable** but cannot be Claimed before the blocking payroll decision. Its Gold is **not** included in payroll. The exact general priority system for any other event collisions is an implementation detail to formalize during scheduler work.

## 3. Minions, Ranks, and Conditions

All minions have Rank 1–5. Their **baseline Attack, Defense, weekly salary, and individual carrying capacity come from Rank definitions**; completed global research may modify effective values. Individual training does not raise Attack or Defense separately.

| Rank | Name | Baseline Attack / Defense / Salary / Capacity |
| --- | --- | --- |
| 1 | Rookie | Rank config |
| 2 | Grunt | Rank config |
| 3 | Raider | Rank config |
| 4 | Veteran | Rank config |
| 5 | Captain | Rank config |

Names for Ranks 2–4 originated as proposed labels and are used consistently here, but may still be renamed.

**Conditions:**

- `Tired`: Applied to **every** minion returning from a raid and **every** successfully recruited prisoner.
- `Injured`: Independently rolled for each minion after a successful raid. On a failed raid, **every** member is `Injured`. A recruited prisoner has a **50%** chance to be `Injured`.
- Both conditions can coexist. Neither causes death. A minion with either cannot start a raid or training.
- A minion cannot be assigned to more than one activity at a time. A minion whose raid is completed but not Claimed remains unavailable.
- Recovery is manually assigned: one minion per recovery bed; after completion the bed is automatically vacated.

| Room | Conditions removed when its timer completes |
| --- | --- |
| Accommodations | `Tired` |
| Med Bay | `Injured` **and** `Tired` |

There are **no ambush mechanics**; earlier mentions of ambushes were accidental.

## 4. Tower Rooms and Progression

Rooms have predefined purposes. Their starting levels, capacities, upgrades, prices, and process durations are determined by data. Each room's **upgrade level** selects a data-defined configuration; not all numerical room values are prescribed in this document.

### 4.1 Accommodations

- One occupied bed recovers one `Tired` minion per timer.
- Room upgrades may provide more beds, faster recovery, and a larger total **minion population cap**.

### 4.2 Med Bay

- One occupied bed treats one `Injured` minion per timer and also removes `Tired`.
- Room upgrades may provide more beds and faster treatment.

### 4.3 Laboratory

- Research activities have configured costs, prerequisites, and durations. Laboratory upgrades unlock additional research tiers/capabilities.
- All finished research gives **global, permanent effects** to minions rather than individual selected minions. Typical effects are parameter additions and multipliers.
- **Already-running raids are unaffected** by newly completed research. Snapshot relevant effective combat parameters when a raid launches; new research applies to later raids. Effects outside raids are applied to eligible current units, subject to the relevant activity's rules.
- The list of research projects, effects, and costs is entirely configurable.

**Previously discussed examples** (not final balance): Fury spell (2 Axes, 1 Carpet, 1 Cow, 1,000 Gold, 1 Crystal); Improved Swords; Improved Defense. Exact effects remain TBD.

### 4.4 Training Room

- Assigning an eligible minion to a station starts a training timer. **On successful completion, Rank increases by exactly 1**.
- The resulting baseline Attack and Defense are looked up from the **new Rank**, rather than incremented separately.
- Training room upgrades can change **station count**, **training speed**, and **maximum trainable target Rank**.
- A minion already at the room's current maximum allowed Rank cannot be trained there.
- Example: a Level 4 Training Room might have **four stations**, faster training, and **Raider (Rank 3)** as its maximum target. Rookie → Grunt on one completion → Raider on the next; further training is blocked until room upgrades raise the allowed maximum.

### 4.5 Treasure Room

- Stores treasure/relic/trophy items. Storage capacity comes from room upgrades.
- Any item, including a treasure, can be sold at its **fixed Gold sale price** from Item data.
- If Claimed treasures exceed free storage, show a capacity message and **discard the excess**, without automatically selling it.

### 4.6 Prison

- A cell holds one prisoner; prisoner processing (torture) starts automatically and ends with **recruitment or death**.
- Captured prisoners **do not consume squad carrying capacity**. Their potential count/Rank distribution is configured per mission.
- When the raid is Claimed, prisoners exceeding free prison cells **disappear**.
- High-Rank prisoners are harder and slower to recruit. Prison upgrades add cells and improve effectiveness.
- Recruited minions **retain their prisoner's Rank**, using that Rank's baseline Attack and Defense.
- Every recruited minion receives `Tired`; **50%** also receive `Injured` (independent roll). If the population cap is full, show a message and discard the recruit.

**Provisional torture model** (balance coefficients to be configured; evaluate only once when processing ends):

`recruit_chance = clamp(0.75 - 0.14 * (prisoner_rank - 1) + 0.05 * torture_upgrade_level, 0.05, 0.95)`

`process_duration = base_prison_duration * (1 + 0.30 * (prisoner_rank - 1)) * max(0.50, 1 - 0.08 * torture_upgrade_level)`

Roll once: success creates a recruited minion, failure kills the prisoner. These particular numerical coefficients are **not yet confirmed**.

## 5. World Map, Missions, and Raids

### 5.1 World tiers and first completion

- Missions belong to a **world tier**. A tier is open when its prerequisites are satisfied; the initial open tier(s) are specified by `StartingBalance` / world-map definitions.
- To unlock the **next tier**, **every mission in the current tier must have been successfully completed and Claimed at least once**.
- A mission with at least one successfully **Claimed** raid displays a **green check mark** on the map. A successful raid whose results are still unclaimed does **not** count.
- Higher tiers are generally more difficult and more rewarding. The visual concept of a *zone* may group places on the map; its exact relationship to tiers is still to be defined.
- Victory occurs once **all missions on the map** have green checks; finishing only a representative mission or clearing a visual zone is not enough.

### 5.2 Mission selection and raid outcomes

- Each mission supplies designer-authored duration, required power/difficulty, injury baseline, currency ranges, and reward pools with configurable draw counts/ranges and probability parameters.
- A squad contains any number of available minions allowed by mission constraints. Injured, Tired, and already assigned minions cannot be selected.
- Multiple raids may run in parallel on **different missions**. A minion can join only one raid at a time.
- A raid has two outcomes:
  - **Success:** Discover the mission's rewards. Everyone returns `Tired`; roll `Injured` independently for each minion.
  - **Failure:** Discover **no rewards**; everyone returns `Tired` **and** `Injured`.
- Minions never die on raids.

**Provisional combat formulas** (Attack/Defense refer to effective values snapshotted at launch):

`unit_power[i] = (0.65 * Attack[i] + 0.35 * Defense[i]) * (1 + 0.10 * (Rank[i] - 1))`

`total_power = sum(unit_power[i])`

`power_ratio = total_power / mission.required_power` (required power > 0)

`failure_chance = clamp(2.0 * (1 - power_ratio), 0, 1)`

On success, roll injury separately:

`injury_chance[i] = clamp(mission.base_injury_chance + 0.20 * max(0, 1 - power_ratio) - 0.02 * (Rank[i] - 1) - 0.005 * max(0, Defense[i] - 5), 0.01, 0.90)`

Raid duration:

`duration = mission.base_duration * max(0.80, 1 - 0.04 * (squad_size - 1) - 0.01 * (average_rank - 1))`

The mission's duration dominates; more/better minions provide a limited speed increase. Formula coefficients are **proposals**, not approved balance.

### 5.3 Weekly mission cooldown and Claim lock

**A raid launch makes its mission unavailable until the beginning of the following in-game week, regardless of success or failure.** In addition, a mission cannot be restarted while its previous raid is running or completed-but-unclaimed.

The mission is launchable only when **all** conditions hold:

1. Its world tier is unlocked.
2. No raid for that mission is `RUNNING` or `COMPLETED_UNCLAIMED`.
3. The in-game week is later than the week in which the most recent raid was launched.

Thus finishing and Claiming a raid **does not** remove the remainder-of-week cooldown. Equally, a new week does **not** unlock a mission whose raid is still running or unclaimed. Raid **failure** also consumes the weekly opportunity.

A Claimed first success adds the permanent green check; cooldown does not remove that check. Replays remain possible in subsequent weeks.

### 5.4 Reward generation and carrying capacity

- On successful raid completion, the game rolls all discoveries **from the mission's config**. Each reward category has configurable amount/draw-count ranges and relevant randomization parameters; draw procedures and probability formulas are part of a separate configuration/balance pass.
- Reward generation is **independent of squad size**. A bigger squad affects only what the player can carry and the raid performance formulas.
- Each Rank contributes a configured, potentially **fractional** carrying capacity. Sum capacities **first**, then take the integer part to get the squad's available carry units:

`carry_limit = floor(sum(rank_data[minion.rank].carry_capacity for minion in squad))`

- Each **physical item** has a data-defined positive integer `carry_size` (with size `1` for the illustrative Golden Cup example). The total size of selected items must be `<= carry_limit`.
- Example: a minion's configured capacity is **1.5**, so three such minions provide **floor(4.5) = 4** carry units. If six Golden Cups (size **1** each) are discovered, the player may Claim **four**.
- At the Claim step, a valid subset of the physical loot is **selected by default** within carry capacity. The player may deselect/reselect items, but cannot exceed the limit. The default selection order is an implementation/data question, not yet a gameplay balancing rule.
- Unselected physical items are lost after confirmation. Ordinary items go to unlimited **Inventory**; treasures/relics go to the capacity-limited **Treasure Room**.
- **Gold and Crystals** are collected in full and do **not** use carrying capacity; prisoners do **not** use carrying capacity.
- Storage/cell/population overflows are resolved when rewards or recruits are credited, not during physical carry selection.

**Illustrative mission:** `forest_camp`, Gold 500–1,000, Crystals 0–1, ordinary loot (Axe/Chest/Carpet/Cow), possible Golden Cup and prisoners of specified Ranks. Actual draw counts, ranges, weights and probabilities are mission config, not fixed in the game specification.

### 5.5 Claim flow

1. On launch, the mission enters `RUNNING`, reserves the squad, and starts its weekly cooldown.
2. On timer completion, the outcome and generated discoveries are finalized. The raid enters `COMPLETED_UNCLAIMED`; it continues to reserve its minions and mission.
3. The player presses **Claim**. Successful results display discovered Gold, Crystals, physical items and prisoners; failed results display no rewards.
4. On success, the physical item list comes with a capacity-valid default selection. The player may modify the selection and then confirm **OK**.
5. On confirmed Claim: credit Gold/Crystals; transfer selected ordinary items into Inventory and treasures into available Treasure Room space; transfer prisoners into available cells; discard overflow; apply/confirm returning conditions; free minions; and record first-success completion if applicable.
6. If this Claim finishes the last unfinished mission of a world tier, unlock the next tier. If it finishes the final unfinished mission on the map, **Victory**.
7. The mission remains on **weekly cooldown**, even after Claim, until a subsequent week. A failed raid must also be Claimed to release minions.

**Time-sensitive accounting:** A raid becoming Claimable does not credit anything. Opening Claim without confirming it also does not credit anything.

## 6. Inventory, Economy, and Capacity

- **Currencies:** Gold and Crystals only; other rewards are sellable or consumable items.
- **Inventory:** Unlimited capacity for ordinary items. Every item has a fixed sale price in **Gold** (including ordinary items, treasures and relics).
- **Treasure Room:** Separate, limited-capacity storage. Storage upgrades expand its cap; exceeding it discards surplus treasure and displays a message.
- **Prison:** One prisoner per available cell. Excess prisoners at Claim simply disappear.
- **Population:** A maximum owned-minion population derived from Accommodations/starting settings. Excess recruits disappear, with a message.
- **Purchases:** Pay configured costs in Gold, Crystals and/or items only when the player has enough resources. Purchases never create negative balances.
- **Payroll:** Only automatic weekly salaries can make Gold negative, triggering immediate bankruptcy after the deduction animation.
- All pricing, capacity, time and distribution numbers are **data-driven**.

## 7. Designer-authored Data vs. Runtime State

A separate catalog, **`dark_tower_config_catalog_v0.1.md`**, inventories the data files and fields. At a minimum, configuration must describe:

1. **StartingBalance:** starting Gold/Crystals, three starting minions and their Ranks/conditions, initial room levels/capacities, open tier(s), optional starter inventory.
2. **Ranks:** Rank ID/name, baseline Attack/Defense, salary, fractional carry capacity.
3. **Items:** Item ID/type, integer carry size, fixed Gold sale price, possible research/upgrade ingredient role.
4. **World map / tiers:** tier IDs/order, mission IDs, tier unlock prerequisites and positions/grouping as needed.
5. **Missions:** tier, required power, base duration/injury chance, Gold/Crystal ranges, item/treasure/prisoner draw ranges and distributions, squad restrictions if any.
6. **Rooms and upgrade levels:** initial/target levels, cost, slots, population/storage capacities, process durations/speed, max training Rank, prison effectiveness, lab tier unlocking.
7. **Research:** project IDs, prerequisites, costs, durations, global additive/multiplicative effects and targets.
8. **Global balance/rules:** coefficients for raid success, injury/duration, torture probabilities/duration, and chance-based conditions; optional common reward tables.

**Runtime state is not authored configuration:** the current week and payday, currency balances, living minions and assignments, current room levels, active scheduler timers, ongoing/Claimable raids and their frozen discovered rewards, mission last-launch weeks, permanent first-clear flags, research completion flags, prisoners, and inventories. None of these belong in static mission/rank definitions.

Godot JSON vs custom `.tres` Resources is an implementation choice; no rules should depend on the file format.

## 8. Open Design / Implementation Decisions

The questions in the previous version about victory, tier unlocking, training, weekly payroll, item sales, inventory, ambushes, prisoner conditions and spending have been resolved above. Remaining issues:

- **World tiers vs zones:** Are visual zones identical to tiers, or can a tier contain multiple geographic zones? If later tiers branch, must *all* previous tiers be cleared to unlock each successor?
- **Mission rewards:** Exact draw algorithm per category (independent per-item chance vs weighted selection), how configured ranges feed it, duplicates, and whether there are reusable shared reward tables. Numeric values come later.
- **Claim defaults:** How to choose the initially selected set when items have different `carry_size` values; selection/order for storage overflow when many selected treasures are Claimed.
- **Room progression:** Can upgrades occur concurrently with running activities? Does an upgrade affect already-running timers, or only new assignments?
- **Research modifiers:** Precise add/multiply stacking order, targets, and interaction with processes already in progress outside raids.
- **Scheduler:** Formal tie-breaking for simultaneous events beyond the payroll-boundary rule; what happens when a debug force-completion would collide with payroll.
- **Gameplay balance pass:** Define all actual starting values, stat curves, salary prices, item values, room level tables, durations, mission difficulty, reward distributions and probabilities in configuration.
