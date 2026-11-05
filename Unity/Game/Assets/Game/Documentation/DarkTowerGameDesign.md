# Dark Tower — Game Mechanics Design

**Version:** 0.6 — Working specification  
**Genre:** Fantasy time-management / strategy  
**Engine:** Unity  
**Companion:** `dark_tower_config_catalog.md`  
**Scope:** Gameplay rules, interfaces between systems, and designer-configurable mechanics. Art direction, exact UI layouts, implementation details, and final numerical balance are outside this specification.

Rules stated as facts describe the intended design. Unspecified behavior is gathered in **Open Design Decisions**. Illustrative formulas are explicitly marked **provisional**.

## 1. Premise, Vocabulary, and Core Loop

The player controls the **Dark Lord**, who manages a tower, sends minions to raid individual missions, receives loot and prisoners, develops facilities, trains the army, and pays weekly salaries.

**Core loop:** Plan a raid → Assign a squad → Wait for completion → Open **Raid Report** and **Claim** → Recover, train, and recruit minions → Manage loot and upgrades → Unlock World Tiers → Pay salaries.

### 1.1 Tower Rooms

A **Room** is a functional facility of the tower. Each Room offers its own actions and individually purchased **Upgrades**.

| Room | Purpose |
| --- | --- |
| **Storehouse** | Item storage, stack sales, storage and selling upgrades |
| **Quarters** | Sleeping beds and automatic recovery from `Tired` |
| **Infirmary** | Treatment beds and automatic recovery from `Injured` |
| **Training Hall** | Manual minion rank training at training stations |
| **Laboratory** | Permanent research and research-related upgrades |
| **Dungeon** | Prisoner cells, interrogation, torture, and recruitment |
| **War Room** | Raid planning, World Map, and intelligence upgrades |

Rooms have distinct upgrades and activities. **Tower Level** is the tower-wide progression value, based on hidden Tower XP earned from specified upgrades.

### 1.2 Screens

| Screen | Purpose |
| --- | --- |
| **Inventory** | Manage Treasures, Items, and Minions |
| **Payroll** | Manually pay the entire weekly salary bill |
| **World Map** | Select missions, assign raid squads, inspect raid progress |
| **Raid Report** | Review a successful or failed raid and confirm Claim |

The World Map is accessed through the War Room. The Dark Lord character is present at the base, where the payday interaction appears.

### 1.3 Victory and Defeat

- The game begins with **three minions**; other initial values come from `StartingBalance`.
- **Victory:** Every mission on the World Map has been successfully completed and **Claimed** at least once.
- **Game Over — Bankruptcy:** The full weekly payroll is unpaid when payday ends.
- Minions can leave the player's service through prolonged lack of beds. Raids themselves do not kill minions.

## 2. Game Clock, Timers, and Payday

- **One real minute equals one in-game day**; one in-game week is seven days.
- The world has a day/night cycle and a visible day counter.
- Time advances while the game is running. The current design has no save system or offline progression.
- Activities use a shared game-time scheduler: raids, resting, treatment, training, sales, research, Dungeon processing, Room upgrades, and payday.
- The player has no ordinary pause or game-speed controls. **Payroll** and **Raid Report** pause game time while open. Ordinary management views do not.
- A developer-only scheduler/debug panel may list ongoing processes, speed up time, and force completion.

### 2.1 Weekly Payroll

Once every seven in-game days, **payday** begins. An interaction icon appears over the **Dark Lord** at the base. The player manually opens the **Payroll** popup by clicking the icon.

Payroll displays the minion population, the total salary due, available Gold, and the resulting Gold balance if paid. A rank breakdown may also be shown.

- Salary is paid in **Gold** and depends on each owned minion's **Rank**.
- Every owned minion is included, regardless of conditions or current activity. Unrecruited prisoners are excluded.
- **Pay All** pays the entire bill in one operation; partial payment is unavailable.
- The payment button is enabled only when enough Gold is available to pay everyone.
- Opening Payroll **pauses** game time. Closing it without paying **resumes** time. The player can perform other actions, such as selling items, and return to the popup later.
- Payment is recorded only when the player clicks **Pay All**.
- If payday ends without full payment, the game ends with **Game Over — Bankruptcy**.
- Unclaimed raid rewards do not contribute to the available Gold balance.

`weekly_salary = sum(rank_data[minion.rank].salary_gold_per_week for each owned minion)`

Normal purchases require sufficient resources; they cannot make a currency balance negative.

## 3. Minions, Ranks, and Conditions

### 3.1 Ranks

Each minion has a Rank between **1 and 5**. The Rank defines baseline Attack, Defense, Salary, and Carry Capacity. Completed Laboratory research may modify effective values.

| Rank | Name |
| --- | --- |
| 1 | Rookie |
| 2 | Grunt |
| 3 | Raider |
| 4 | Veteran |
| 5 | Captain |

A completed Training Hall session increases a minion's Rank by exactly one. Its new base characteristics come from the target Rank definition.

### 3.2 Conditions and Activity

- **`Tired`** prevents starting a raid or training. Minions may become Tired after raids, training, Dungeon recruitment, and other configured events.
- **`Injured`** prevents starting a raid or training. Both conditions may coexist.
- A successful raid makes every participant `Tired`; each participant independently has a configured chance to become `Injured`.
- A failed raid makes every participant both `Tired` and `Injured`.
- Recruited prisoners are always `Tired` and have a **50%** chance of `Injured`.
- At **nightfall**, all owned minions receive `Tired`, reflecting their need for sleep.
- One minion cannot perform multiple occupied activities simultaneously. A completed raid whose Raid Report has not been Claimed still reserves its squad.

Recovery and treatment assignments are automatic. Raid assignments are made on the World Map, and training assignments are made manually in Training Hall.

### 3.3 Population and Upkeep

The game tracks the **current number of owned minions** and their combined salary cost. The Inventory Minions collection is their management view, with one icon per minion and full status information.

The ability to sustain a large population depends on the available beds and the automatic rest system. A minion who cannot obtain a suitable bed for **more than two in-game days** leaves the tower. A popup notifies the player when minions leave.

## 4. Rooms and Their Activities

### 4.1 Storehouse

Storehouse provides the item capacity and selling facilities used through the **Inventory** screen.

**Storage:**

- Treasures and Items share **one pool of inventory slots**.
- Each **stack** consumes one slot. A type's `max_stack_size` controls how many copies fit into a stack.
- Examples of stack sizes: **Golden Cup: 5**, **Cow: 1**, **Cobblestone: 30**.
- The `treasure` tag places an item in the **Treasures filter**; other items appear in **Items**. These are views over the same underlying storage.
- When selected raid loot cannot fit into existing stacks or free slots at Claim, the excess is **silently discarded**.
- Gold and Crystals are currencies shown in the global HUD rather than in Inventory.

**Sales:**

- Any physical item can be sold for Gold. The item definition supplies its **per-copy sale price**.
- The player selects **one entire stack** to sell.
- **Normal Sale** takes time and pays the stack's full sale value. Sale time depends on the **number of copies** in the stack.
- **Instant Sale** completes immediately for **50%** of the stack's normal total value.
- **Sale Efficiency** upgrades progressively reduce normal sale time.

**Upgrades:**

- **Storage Capacity:** each configured upgrade adds a configured number of slots for a configured cost and construction duration.
- **Sale Efficiency:** a sequence of upgrades improves normal selling speed.

### 4.2 Quarters

Quarters supplies beds that allow minions to recover from `Tired`.

- An available, otherwise free minion with `Tired` is **automatically assigned** to a free bed.
- Rest takes **6 in-game hours** and removes `Tired`. The bed is automatically released at completion.
- Daytime rest accommodates minions who become Tired after training, recruitment, raids, or other events.
- At nightfall, all owned minions become `Tired`, and eligible minions automatically take beds.
- **Bed priority:** the minion who has been `Tired` for the **longest time** receives an available bed first.
- A minion who goes **more than two in-game days without obtaining a suitable bed** leaves the tower; the player sees a popup reporting departures.
- **Extra Bed:** each purchase adds **exactly one** Quarters bed.

A minion with `Injured` is routed to Infirmary before Quarters; see the priority rule below.

### 4.3 Infirmary

Infirmary automatically treats injured minions.

- Free minions with `Injured` automatically occupy available **Treatment Beds**.
- **`Injured` takes priority over `Tired`.** A minion with both conditions goes to Infirmary.
- Treatment normally takes **3 in-game days**; the duration is configurable.
- Completing treatment clears **both** `Injured` and `Tired` and frees the bed.
- **Extra Treatment Bed:** each purchase adds exactly one bed.
- **Treatment Efficiency:** a sequential upgrade series reduces treatment duration.

### 4.4 Training Hall

Training Hall increases minion Rank through **manual** training assignments.

- The player assigns an eligible minion to an available **Training Station**.
- Each completed session increases Rank by **exactly one**.
- The **cost and duration are configured separately for each Rank transition**: 1→2, 2→3, 3→4, 4→5.
- Training is available only when that target transition has been unlocked.
- Minions with `Tired` or `Injured` cannot begin training.
- A trained minion gains `Tired` and can automatically rest in Quarters.

**Upgrades:**

- **Extra Training Station:** adds exactly one station per purchase.
- **Advanced Training:** unlocks Rank transitions sequentially: first 1→2, then 2→3, then 3→4, then 4→5.

### 4.5 Laboratory

Laboratory conducts research with **permanent, global** effects.

- Each research project has configured costs, prerequisites, duration, and effects.
- Only **one** Laboratory research/improvement can be active at a time; there is **no queue**.
- Completing a research project permanently activates its effects. Each project is researched only once.
- Bonuses can affect Attack, Defense, and other globally eligible parameters.
- A raid uses the effective stats captured when it launches; research completed afterward does not change that raid.

**Upgrades:**

- **Research Tier Unlock:** sequential purchases open the next tier of available research.
- **Research Efficiency:** a sequential upgrade series reduces research duration.
- Individual research and upgrades can have additional configured prerequisites.

Examples of research topics are **Improved Weapons**, **Improved Armor**, and **Fury Spell**. Their numerical balance is supplied through configuration.

### 4.6 Dungeon

Dungeon holds prisoners captured during raids and processes them automatically.

- One prisoner occupies one **Cell**.
- Prisoners are credited when Raid Report is Claimed and automatically allocated to free cells; excess prisoners are lost.
- Capturing prisoners consumes **no squad Carry Capacity**.
- Torture/processing starts automatically when a prisoner enters a cell.
- Prisoner Rank influences recruitment difficulty and process duration: higher-Rank prisoners are harder and slower to recruit.

Possible processing outcomes:

| Outcome | Result |
| --- | --- |
| **Recruitment** | Prisoner becomes an owned minion, retaining their Rank |
| **Interrogation** | Reveals secrets that can unlock certain World Map locations or influence configured events |
| **Death** | Prisoner dies without joining the army |

A recruited minion becomes `Tired`, has a **50%** chance to become `Injured`, and enters the automatic recovery/treatment system.

**Upgrades:**

- **Extra Cell:** each purchase adds exactly one cell.
- **Torture Efficiency:** a sequential upgrade series reduces processing duration.
- **Recruitment Efficiency:** a sequential upgrade series increases recruitment chance.

At Claim, prisoners are **sorted by Rank** before filling the available cells; the ordering direction is a configurable/design detail to finalize.

### 4.7 War Room

War Room provides access to the **World Map**, mission selection, squad assignment, and raid intelligence.

The player selects a specific mission (for example, a village), chooses an available squad for that mission, and launches a raid.

Two independent upgrade series improve intelligence displayed for each mission:

- **Enemy Intelligence:** progressively reveals tighter **estimated ranges** of enemy army strength. The estimate becomes more precise with further upgrades.
- **Loot Intelligence:** progressively reveals possible loot types, resources, and their estimated drop chances. Early intelligence may not reveal even the item types.

Intelligence upgrades change the player's **knowledge**, not the mission's actual enemy strength or reward-generation probabilities.

## 5. World Map, Mission Progression, and Raids

### 5.1 World Tiers and Map Layout

- The **tower is at the center** of the World Map.
- **Concentric rings** spread outward from the tower. Each ring represents **one World Tier**.
- Missions are individual marked points within a ring, representing villages or other locations.
- The first World Tier is available at the start.
- When **every mission in the current tier** has been successfully Claimed once, the next tier unlocks.
- A mission's first successful Claim gives it a permanent **green checkmark**.
- Completing every mission on the map at least once is the victory condition.
- Dungeon secrets can reveal or unlock specific locations and influence events according to mission/event configuration.

### 5.2 Mission Selection and Visibility

The map indicates each mission's progression and raid state, including:

| State | Meaning |
| --- | --- |
| `LOCKED` | Not currently accessible |
| `AVAILABLE` | Raid can be launched |
| `RUNNING` | Raid is active; progress is visible on the mission marker |
| `COMPLETED_UNCLAIMED` | Raid is finished; Raid Report awaits Claim |
| `COOLDOWN` | This mission has consumed its weekly attempt |
| `COMPLETED_ONCE` | Permanent success checkmark, alongside current availability/raid state |

Clicking an active mission exposes its raid status and information **from the World Map**. The World Map handles individual mission squad selection and launches.

### 5.3 Raid Rules

- A squad is assigned to **one mission**.
- Squad members must be available and must have neither `Tired` nor `Injured`.
- Multiple raids can run in parallel on **different missions**.
- One minion can participate in only one activity/raid at a time.
- Raid outcome and duration are driven by mission configurations, squad effective stats, and balancing formulas.
- Each mission provides difficulty/required power, baseline duration, injury chance, rewards, and prisoner distributions.

**Success:** Generate the mission's rewards; every participant becomes `Tired`, and `Injured` is rolled independently for each participant.

**Failure:** No rewards are generated; every participant becomes both `Tired` and `Injured`.

Minions are not released from a finished raid until its Raid Report is **Claimed**.

### 5.4 Weekly Mission Cooldown

Launching a raid consumes that mission's opportunity for the **current in-game week**, even if the raid fails.

A mission is launchable when all of the following apply:

1. It is accessible according to tier/mission unlocks.
2. It has no raid currently `RUNNING` or `COMPLETED_UNCLAIMED`.
3. Its last launch was in an **earlier** in-game week.

A mission may be replayed in a later week, including after a previous successful completion.

## 6. Raid Report and Claim

**Raid Report** is the shared results screen for both successful and failed raids.

- When a raid finishes, it becomes `COMPLETED_UNCLAIMED` on World Map.
- The player manually opens Raid Report from the mission marker.
- Opening Raid Report **pauses game time**.
- Viewing or closing the report without confirming does not credit rewards, free minions, or complete the mission.
- One **Claim** button confirms the full report and releases the reserved squad, including on failure.

### 6.1 Carrying Capacity and Selection

Reward generation is based on mission configuration and does not scale with the number of squad members. A larger/better squad may improve raid performance and the quantity of physical loot it can carry.

`carry_limit = floor(sum(rank_data[minion.rank].carry_capacity for each squad member))`

`carry_limit` is a count of **carry slots** — the same unit as Storehouse storage slots.

- A successful raid shows **one selectable icon per discovered physical item copy**, including duplicates.
- Selected copies of the same item type stack by that item's `max_stack_size`, exactly like Storehouse; each resulting stack costs **one carry slot**. Selecting another copy of an item already selected is free until its current stack is full.
- The game preselects a valid subset of copies within the budget.
- The player may deselect and reselect individual copies; the resulting stacks' total slot count must not exceed the budget.
- **Gold and Crystals** are taken in full without consuming Carry Capacity.
- **Prisoners** also use no Carry Capacity.

For example, three minions each carrying `1.5` provide `floor(4.5) = 4` carry slots. If six Golden Cups with `max_stack_size = 5` were found, selecting all six forms two stacks (`5` + `1`), costing two of the four available slots.

### 6.2 Claim Resolution

On pressing **Claim**:

1. Credit discovered Gold and Crystals on success.
2. Stack selected physical items by their configured `max_stack_size` (the same stacking used to compute the carry budget in §6.1), filling available Inventory stacks and shared storage slots. Silently discard any surplus that does not fit.
3. Sort captured prisoners by Rank and automatically assign them to available Dungeon cells; discard any excess.
4. Apply returning `Tired`/`Injured` conditions and release all participating minions.
5. Mark a successful first Claim as mission completion and unlock the next World Tier or trigger victory when its condition is satisfied.
6. Leave the mission's weekly launch cooldown in place until the next eligible week.

On failure, Claim still performs the appropriate minion return and mission-state updates, but credits no loot.

## 7. Inventory, Stacks, and Economy

### 7.1 Inventory Collections

**Inventory** presents three collections:

| Collection | Contents | Capacity behavior |
| --- | --- | --- |
| **Treasures** | Items tagged `treasure` | Shared Storehouse slots |
| **Items** | All other physical items | Shared Storehouse slots |
| **Minions** | One icon per owned minion, all conditions and current activity/status | Population count; no hard population cap |

Treasures and Items are filters over **one shared item-storage pool**. The Minions section displays total population and the **combined weekly upkeep/salary**. Each minion can be inspected and managed there; raid squads are assigned through World Map and training through Training Hall.

Gold and Crystals appear in the **HUD**.

### 7.2 Items and Stacks

- Each item definition has an ID, tags, `max_stack_size`, and `base_value_gold` per copy.
- A stack holds **up to** its item's maximum count and consumes **one Inventory slot**.
- Identical item copies may occupy multiple stacks when the maximum is exceeded.
- Items can be sold or consumed as costs for research and upgrades.

### 7.3 Selling

The player chooses a **full stack** to sell.

- **Normal Sale:** a timer runs for a duration based on the number of copies. On completion the player receives the full stack sale value.
- **Instant Sale:** no timer; the player receives half of the stack's full sale value.
- Storehouse's **Sale Efficiency** upgrades shorten normal selling durations.

The cost and duration of every upgrade are configured. Regular transactions require available funds and items.

## 8. Tower Level and Upgrade System

### 8.1 Tower Experience

The tower has a single overall **Tower Level**, determined by accumulated **Tower XP**.

- Tower XP is a **hidden progression score** rather than a currency displayed to the player.
- Certain configured Room Upgrades grant a configured amount of Tower XP.
- A dedicated **Tower Level progression configuration** specifies how much XP is required for each Tower Level.
- The player's current Tower Level is derived from their total awarded XP and the configured thresholds.

### 8.2 Room Upgrades

- Each Room has separately defined Upgrades with configured costs, durations, prerequisites, and effects.
- Upgrade series can be sequential and may include additional requirements.
- Purchasing an upgrade starts its configured construction/timer process; its gameplay effect becomes active when completed.
- Examples include adding beds, cells, training stations, or storage slots; speeding sales, treatment, research, or torture; unlocking training transitions and research tiers; and enhancing War Room intelligence.

The specific gameplay privileges granted directly by Tower Level, if any, have not yet been defined.

## 9. Configuration and Runtime Ownership

The Unity game uses designer-authored configurations for ranks, items, starting state, missions, World Tiers, upgrades, research, intelligence, Tower XP thresholds, and global formulas. The accompanying **Configuration Catalog** defines these contracts.

**Static config** specifies values and rules. **Runtime state** tracks current Gold/Crystals, owned minions and their statuses, bed occupancy, inventory stacks, sales, prison inmates, active timers, raid discoveries, Claims, upgrades and research, total Tower XP/Level, and mission progression.

## 10. Provisional Balancing Formulas

The following formulas illustrate one configurable combat model; the coefficients are **not final balance values**.

```text
unit_power[i] = (0.65 * Attack[i] + 0.35 * Defense[i])
                * (1 + 0.10 * (Rank[i] - 1))
total_power = sum(unit_power[i])
power_ratio = total_power / mission.required_power
failure_chance = clamp(2.0 * (1 - power_ratio), 0, 1)

injury_chance[i] = clamp(
    mission.base_injury_chance
    + 0.20 * max(0, 1 - power_ratio)
    - 0.02 * (Rank[i] - 1)
    - 0.005 * max(0, Defense[i] - 5),
    0.01, 0.90)

raid_duration = mission.base_duration
                * max(0.80,
                    1 - 0.04 * (squad_size - 1)
                      - 0.01 * (average_rank - 1))
```

The Dungeon's recruitment chance and processing duration must account for prisoner Rank and configured efficiency upgrades. Their exact formulas and the probability of the Interrogation outcome are balancing decisions.

## 11. Open Design Decisions

- **Tower XP:** whether XP is awarded at upgrade purchase or completion; whether Tower Level directly unlocks gameplay features; whether level-up has a presentation/notification.
- **Tower progression thresholds:** whether configured XP amounts are per-level increments or cumulative thresholds.
- **Upgrade scheduling:** concurrent Room construction limits, prerequisite handling during construction, and the effect of completed upgrades on already-running activities.
- **Training Hall:** whether training-speed upgrades exist in addition to station and rank-unlock upgrades.
- **Sleeping and recovery:** eligibility and shortage accounting for minions occupied by raids/training at night; whether/how prolonged Infirmary bed shortages interact with the two-day bed rule; treatment-queue ordering among multiple Injured minions.
- **Inventory sales:** how many normal sales can run concurrently, whether a stack is reserved/removed when a sale starts, time-scaling formula, and rounding of half-price sales.
- **Raid Report:** order of accepting selected items if storage fills; whether Rank sorting for prisoners is high-to-low or low-to-high.
- **Dungeon:** relative probabilities for Recruitment, Interrogation, and Death; event/secret definitions; their interaction with tier unlocking.
- **World Map:** detailed mission visibility rules for events, secrets, and intelligence upgrade stages.
- **Research:** precise order for stacking additive/multiplicative global effects.
- **Scheduler:** event tie-breaking at midnight, payday deadlines, raid finishes, condition changes, and upgrade completions.
- **Balance/data:** numeric costs, base capacities, durations, XP thresholds, combat coefficients, reward distributions, training costs, and initial upgrade availability.
