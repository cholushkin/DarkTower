# Dark Tower — Mechanics Prototype

A minimal, mechanically-complete TypeScript prototype of **Dark Tower**, built directly from
`Unity/Game/Assets/Game/Documentation/DarkTowerGameDesign.md`. It demonstrates the full core loop —
raid → claim → recover/train/research → upgrade → payroll → progress to Victory or Bankruptcy —
with plain HTML controls and no visual polish. This replaces the previous `ocean-crawler-upgrades-proto`
prototype in this folder, which was an unrelated game and shared no gameplay logic with Dark Tower.

## Running it

```bash
npm install
npm run dev        # starts a Vite dev server; open the printed localhost URL
npm run build      # typechecks (tsc) and produces a production build in dist/
npm run selftest   # headless engine smoke test, no browser needed (see below)
```

A **Debug / Scheduler** panel at the bottom of the screen lets you speed up game time (1×/5×/20×)
and force-complete any in-progress process instantly — useful since 1 real minute = 1 in-game day
by design (§2 of the design doc), which would otherwise make a full playthrough slow to exercise.

## Architecture

- `src/data/config.ts` — single source of truth for all tunable values (ranks, items, missions,
  world tiers, room upgrades, research, training transitions, starting balance, formula constants).
- `src/model/` — runtime state (`GameState` and friends), DOM-free.
- `src/systems/` — one module per game-design area (Storehouse, Quarters/Infirmary recovery,
  Training Hall, Laboratory, Dungeon, World/raids, Raid Report/Claim, Payroll, Upgrades, Tower XP,
  day/week lifecycle). Pure functions operating on `GameState`, no UI dependency.
- `src/engine/GameEngine.ts` — orchestrates the clock/scheduler and exposes the action API the UI calls.
- `src/ui/` — thin, string-template render functions, one per screen/modal.
- `selftest.ts` — headless script that drives the engine directly (raid → claim → train → research →
  payday → bankruptcy → victory) and asserts the whole loop behaves correctly, independent of the UI.

## Defaults chosen for Open Design Decisions (design doc §11)

The design doc intentionally leaves some mechanics unspecified. Per the task brief, this prototype
fills each gap with a sensible, documented default rather than inventing a permanent rule for the
real game. If/when the design doc is updated to resolve these, update `src/data/config.ts` and the
relevant `src/systems/*.ts` file accordingly:

- **Tower XP** is awarded on upgrade/research **completion**; thresholds in `towerXpThresholds` are
  **cumulative**; reaching a new level just logs a message (no gameplay unlock is tied to it).
- **Upgrade scheduling**: concurrent room constructions are allowed; a completed upgrade does not
  retroactively change an already-running process.
- **Training Hall**: no separate training-speed upgrade exists, only stations + sequential rank unlocks.
- **Recovery**: Infirmary strictly takes priority over Quarters for any minion that is both Tired and
  Injured. Both rooms assign their free beds to the longest-waiting idle candidate first. The 2-day
  bed-eviction clock only runs while a minion is idle and conditioned (occupied minions aren't penalized).
- **Sales**: multiple normal sales can run concurrently; the stack is removed/reserved the instant a
  sale starts; duration = `(1h + 0.5h × copies) × saleEfficiencyMultiplier`; instant sale pays
  `floor(0.5 × full value)`.
- **Claim resolution**: selected loot is accepted in the player's selection order if storage fills
  (surplus silently discarded, per §6.1); captured prisoners are sorted **high-to-low** Rank before
  filling free Dungeon cells.
- **Dungeon outcome split**: base `{recruit 60%, interrogate 15%, death 25%}`, shifted by prisoner Rank
  (higher Rank → harder to recruit, more likely to die) and by the Recruitment Efficiency upgrade bonus.
  Processing duration scales up with prisoner Rank and down with Torture Efficiency.
- **Research stacking order**: additive effects apply first, then multiplicative effects.
- **Payday window**: once payday becomes due, the player has **24 in-game hours** to open Payroll and
  Pay All before Bankruptcy ends the game.
- **Raid Report item selection**: modeled as "how many copies of each discovered item type to keep,"
  with slot cost `ceil(count / max_stack_size)` per item type — equivalent to the doc's per-copy
  description but simpler to present as plain +/- controls. The game preselects a valid subset within
  the carry budget; the player can freely add/remove per item type afterward.
- **World Map layout**: tiers are listed as grouped sections rather than drawn as literal concentric
  rings — a presentation simplification only; the unlock/progression rules are unchanged.

## Sample content

2 World Tiers (7 missions total), 5 Ranks, 9 items (including `treasure`-tagged ones and the doc's
own stack-size examples), a full upgrade series for all 7 rooms, 3 research projects, and 4 training
rank transitions — enough to play from 3 starting Rookies through to Victory or a forced Bankruptcy.
