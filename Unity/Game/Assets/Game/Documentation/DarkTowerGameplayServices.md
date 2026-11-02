# Dark Tower — Gameplay Services (Task 05)

Headless gameplay/application layer on top of the Task 04 data layer (`Data/`, `Models/`,
`Factory/`, asmdef `DarkTower.Data`). This layer adds the asmdef `DarkTower.Gameplay`
(`Runtime/DarkTower/Gameplay/`) and `DarkTower.Gameplay.Tests`
(`Tests/DarkTower/EditMode/`). **No UI, no save/load, no new frameworks** — services, a VContainer
session composition root, and a deterministic headless simulation only.

## Layout

```
Runtime/DarkTower/Gameplay/
  Services/
    Core/        OperationResult, IRandomSource, ClockService, GameScheduler,
                 UpgradeEffectRules, UnlockConditionRules
    Economy/     EconomyService, InventoryService (Storehouse), SalesService
    Minions/     MinionService, RecoveryService, TrainingService
    Tower/       TowerUpgradeService
    Laboratory/  ResearchService
    Dungeon/     DungeonService
    World/       WorldProgressionService, RaidService, RaidCombatRules, RaidClaimPolicy,
                 IntelligenceService
    Payroll/     PayrollService
  Events/        DomainEvents.cs (VitalRouter ICommand notifications)
  DependencyInjection/
    GameInstallers, ConfigInstaller, CoreInstaller, EconomyInstaller, MinionsInstaller,
    TowerAndLaboratoryInstaller, WorldAndDungeonInstaller, PayrollInstaller,
    GameSessionStartup, GameSessionLifetimeScope (scene composition root),
    GameplaySessionComposer / GameplaySession (headless composition — no scene, used by tests
    and any future developer-only runner)
Tests/DarkTower/EditMode/
  DarkTower.Gameplay.Tests.asmdef + test fixtures/suite
```

## Composition and lifetime

One VContainer session scope only (no per-feature child scopes, no UI child scope — per
`DataFlow.md` §5). `GameInstallers.RegisterAll(builder, database, randomSeed)` wires every service;
`GameSessionLifetimeScope` (a real scene `LifetimeScope`) and `GameplaySessionComposer.Create(db,
seed)` (a plain `ContainerBuilder`, no scene — the headless entry point) both call it. Startup order
is explicit via `GameSessionStartup : IInitializable` (real scope auto-dispatches it;
`GameplaySessionComposer` resolves and calls `Initialize()` directly), which runs one world-
accessibility refresh and one bed auto-assignment pass before returning control. Disposing the
container disposes `GameSessionModel`, which cascades through every sub-model (unchanged Task 04
behavior).

`GameplaySession` (returned by `GameplaySessionComposer.Create`) is the acceptance-demonstration
surface from the task brief: it exposes `Session` (the `GameSessionModel`), `Clock`, `Scheduler`,
`Random`, and every service, so a test or small runner can create a session, query/mutate through
services, advance time, and inspect reactive Models without any Canvas/ViewModel/Composer/window.

## The clock and scheduler

`ClockService` wraps the existing `ClockModel` and adds nothing but a pause ref-count
(`RequestPause()` → `IDisposable`; nested/overlapping requests only resume at zero outstanding).
`GameScheduler.AdvanceGameTime(hours)` is the **only** method that moves the clock forward. It
repeatedly jumps to the smallest of (remaining target, next nightfall, next payday boundary, the
minimum "next due" reported by every timed service), processes everything due at that exact instant
in a fixed order, and repeats — so a large jump or several simultaneous events complete every timer
exactly once, never skipped.

Fixed per-boundary order: nightfall (if applicable) → recovery completions → training → sales →
research → room upgrades → dungeon processing → raid finish-resolution → payday deadline check (if
applicable) → one recovery auto-assignment + bed-shortage-departure pass. This is a **documented
technical** ordering, not a gameplay-priority decision: raids never auto-credit anything at this
point (only `TryClaimRaid` does), so nothing in this fixed order can change a gameplay outcome by
being processed before or after payday/nightfall on the same instant, with one caveat — see "Open
design decisions" below.

`AdvanceGameTime` is explicitly the **test/debug entry point** the design calls for ("direct time
advancement is an internal test/debug capability only") and deliberately bypasses
`ClockService.IsPaused`. Nothing in this task drives the clock from real time (no `Update` loop, no
`UniTask.Delay` tick — out of scope per "stop at the headless simulation"), so that distinction has
no other observable effect yet; a future runtime driver would check `IsPaused` itself before calling
in, never inside the scheduler.

## Service → design/catalog coverage map

| Service | Design §§ | Catalog §§ | Notes |
| --- | --- | --- | --- |
| ClockService / GameScheduler | §2, §2.1 | §2.14 (clock/payroll) | boundary-jump simulation above |
| EconomyService | §2.1, §6 | §2.14 | all-or-nothing combined currency+item cost |
| InventoryService | §4.1, §7.1–7.2 | §2.3, §4 | shared stack-slot pool, Treasures/Items are a tag filter only |
| SalesService | §4.1, §7.3 | §2.12 | stack removed at sale start (see open decisions) |
| MinionService | §3 | §2.2 | eligibility, effective stats, nightfall Tired |
| RecoveryService | §3.2–3.3, §4.2–4.3 | §2.7 | Infirmary-before-Quarters, longest-waiting-first, departure |
| TrainingService | §4.4 | §2.8 | manual, adjacent-rank-only, station reservation |
| TowerUpgradeService | §8 | §2.6, §2.13 | effects + XP on completion only |
| ResearchService | §4.5 | §2.9 | single active process, tier gating |
| DungeonService | §4.6 | §2.10 | admission at Claim only, weighted outcome roll |
| WorldProgressionService | §5.1–5.4 | §2.4–2.5 | accessibility, weekly cooldown, tier unlock, victory |
| RaidService / RaidCombatRules | §5.3–5.4, §6, §10 | §2.5 | provisional combat/injury/duration formulas, Claim |
| IntelligenceService | §4.7 | §2.11 | query-only, never touches real mission data |
| PayrollService | §2, §2.1 | §2.14 | full-payment-only, bankruptcy terminal state |

## Resolved technical/implementation choices (documented, not silent)

These are all either (a) a technical scheduling/implementation detail the design explicitly allows
to be resolved locally as long as it doesn't settle a gameplay-balance question, or (b) a minimal,
fully-configurable policy added to permit correct behavior without inventing a numeric balance value.

- **Payday deadline window**: `GlobalBalanceDefinition` only authors `paydayIntervalDays`, no
  separate grace-window field. Resolved as "the start of the next payday interval" — the player has
  the full week to pay before the next boundary closes the previous one out. Needs no new config.
- **AdvanceGameTime vs. pause**: see "The clock and scheduler" above.
- **Sale stack reservation point**: a stack is removed from Storehouse the instant a sale (Normal or
  Instant) starts, not when it completes. Avoids inventing a second "reserved" stack state Task 04
  doesn't model.
- **Sale/treatment/research/torture efficiency convention**: `UpgradeEffect.Amount` for every
  `Reduce*Duration` kind is a **multiplicative factor**, compounded per completed step in completion
  order (two 0.9 steps → ×0.81). `IncreaseRecruitmentChance` is an **additive** probability delta,
  summed across completed steps. See `UpgradeEffectRules.cs`.
- **Tower XP award point**: on upgrade **completion**, not purchase — the only point the design
  itself calls observable ("its gameplay effect becomes active when completed").
- **Prisoner Claim sort direction**: now a designer-authored `DungeonRulesDefinition.SortHighRankFirst`
  bool (default false = ascending) instead of a hardcoded direction — a minimal, documented Task 04
  data-contract addition (see below), not an invented direction.
- **Dungeon three-outcome evaluation**: weighted draw over
  `(Recruitment + rankRecruitmentModifier + recruitmentEfficiencyBonus, Interrogation, Death)`, all
  values from authored config/modifiers, not invented numbers. See `DungeonService.RollOutcome`.
- **Research tier gating**: computed on demand as `StartingBalance.InitialRoomCapabilities
  .UnlockedResearchTier + count(completed research_tier_unlock upgrades)` — no new stored field.
- **Training-transition unlock convention**: an `UnlockTrainingTransition` upgrade effect's
  `TargetId` is authored as `"{fromRank}-{toRank}"` (e.g. `"1-2"`), matching
  `TrainingModel.TransitionKey` — a data-authoring convention, not a contract change (`TargetId` was
  already a free-form string).
- **Bed-wait / departure timing**: the >2-day departure clock is tracked independently of
  `MinionModel.TiredSinceGameHours` (which can start while a minion is still busy elsewhere, e.g.
  nightfall hitting a minion mid-raid) — it starts only once a minion is observed free+idle+needing a
  bed with none available (`RecoveryService`'s internal wait-tracking dictionary). This directly
  addresses the design's open "bed-wait accrual for occupied minions" question: an occupied minion
  never accrues shortage time.
- **Infirmary queue tie-break**: resolved by reusing the same longest-`TiredSinceGameHours`-first rule
  used for Quarters (the design leaves this tie-break open; in practice every path that sets Injured
  also sets Tired at the same instant, so this is a faithful proxy).
- **War Room estimate shape**: at achieved precision *p* ∈ [0,1], the shown enemy-strength range is
  centered on the true `required_power` with width `required_power*(1-p)` — collapses to the exact
  value at *p*=1, fully hidden at *p*=0. A direct reading of "progressively tighter ranges" using only
  the authored precision curve, not a fabricated number.
- **Raid return-condition roll timing**: the per-member independent Injured roll on a successful raid
  happens at **Claim**, not at raid-finish — per §4.6 "Apply return conditions at the specified Claim
  transition." The underlying power-ratio/injury-chance formula is a pure function of the immutable
  launch snapshot + mission data, so computing it at Claim instead of at finish produces identical
  results and is not a "reroll" of anything the design calls fixed at finish (outcome and discovered
  loot *are* fixed at finish and never recomputed).
- **Default carry preselection**: `RaidService.TryClaimRaid` never auto-picks a selection — the caller
  supplies one. `RaidClaimPolicy.DefaultPreselect` is an optional, separate, swappable pure function a
  caller may use instead, per the design's explicit allowance for either approach.
- **Upgrade/research concurrency**: no cap on concurrent room-upgrade construction (nothing in the
  design caps it); Laboratory remains hard-capped at one active process (explicitly required).

## Task 04 data-contract additions (all additive, backward-compatible, documented here)

1. `DungeonRulesDefinition.SortHighRankFirst` (bool, default false) — see above.
2. `RaidModel.Claimed` (bool, default false) + `MarkClaimed()` — Claim idempotency, kept separate from
   `Outcome` so `Outcome` stays a pure Success/Failure display value.
3. `GameSessionModel.IsVictory` / `IsBankrupt` (bool, default false) + `IsTerminal` + `DeclareVictory()`
   / `DeclareBankruptcy()` — terminal-state flags, set exactly once each, only by
   `WorldProgressionService` / `PayrollService` respectively.

No other Task 04 type changed. Every other process/state shape (snapshots, discovered copies,
bed/training/sale/upgrade/research process models, world/mission progress) already had exactly the
shape the services needed.

## Open design decisions (explicitly NOT resolved — surfaced, not invented)

These remain genuinely open per the design/catalog's own "Open Design Decisions" sections; nothing
here silently picks a gameplay-balance answer the design calls undecided:

- Exact numeric balance everywhere (combat/injury/duration coefficients, costs, durations, XP
  thresholds, reward distributions) — read entirely from the (tiny, test-only) config assets; this
  task authors no production balance.
- Whether Tower Level itself grants any direct gameplay benefit (XP award point is resolved above;
  level *benefits* are not — none are implemented, matching "not yet defined" in the design).
- Tower XP threshold convention: `TowerProgressionRules.DeriveLevel` (Task 04) reads thresholds as
  cumulative; unchanged here.
- Exact probability/duration curve shape for Dungeon outcomes beyond the weighted-draw structure
  above — the weights/modifiers are fully authored, but the design calls the precise formula a
  balance-pass decision.
- Scheduler tie-breaking is fixed and documented (see above) only to the extent it cannot change a
  gameplay outcome. One residual caveat: if a bed frees up and a minion crosses the Tired/Injured
  departure grace boundary in the *same* processed instant as a nightfall Tired application, the
  fixed order (nightfall, then completions, then auto-assignment/departure-check) means a
  newly-nightfall-Tired minion is never immediately eligible for departure at that same instant — this
  is intentional (a minion can't be judged "overdue" the instant it first needed a bed), but is called
  out here as the one place the fixed technical order is load-bearing rather than purely cosmetic.

## Testing

See `Assets/Game/Tests/DarkTower/EditMode/`. EditMode tests build a tiny in-memory
`DarkTowerDatabase` fixture (not production content) and drive sessions through
`GameplaySessionComposer`, using a seeded `IRandomSource` for determinism. Run via Unity's Test
Runner window (`Window > General > Test Runner > EditMode`) or `Unity.exe -runTests -batchmode
-projectPath <path> -testPlatform EditMode -testResults <file>`.
