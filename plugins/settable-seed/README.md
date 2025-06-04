
# Settable Seed

Adds functionality for creating a new world with specified seed.

> [!IMPORTANT]
> Some parts of the world generation are not deterministic. Only world generation is preserved (and maybe some other things).
> See [World Generation Determinism](#world-generation-determinism) for more info.

In the menu of world creation settings will add a new field of the world seed. \
If the value for the input field is omitted, the random seed is selected.

## Implementation Info

The command `/param m_seed` will display a seed for the current world, but actually, the displayed seed value is never used in the `Solo`/`Multi` world.
The value that `m_seed` variable contains, it is just a random generated value that doesn't mean anything.

This plugin repurposes `m_seed` variable to be used in `UnityEngine.Random.InitState(int seed)` function at the beginning of the creation of a new world.

Adds new locale string `"SETTABLE_SEED_OPTIONS_SEED"` with the value `"Seed:"`.

## World Generation Determinism

Seems like after simulation preprocessing (method `SWorldDll.ProcessSimu`, called in `SGameStartEnd.GenerateWorld`), the RNG becomes nondeterministic, and even with the same starting seed the results become inconsistent across runs.
My assumption for this is that `SWorldDll.ProcessSimu` is using a nondeterministic total simulation time, which combined with the RNG, becomes itself nondeterministic afterwards.

Although after attempting to remove that nondeterministic behavior in `SWorldDll.ProcessSimu` (by replacing global simulation time (`GVars.m_simuTimeD`) with a custom one for this specific method arguments), it resulted in same nondeterministic behavior. After this I stopped trying to fix this.

The list of nondeterministic initial conditions that will change even with the same seed:
- Simulation preprocessing (`SWorldDll.ProcessSimu`).
  - Plants initial location.
  - Organic rock defense location.
  - Grass location.
- Player spawn location (`CModeSolo.OnNewGame`).
- Initial metal scrap pickups locations (`CModeSolo.CreateInitialMetalScrapPickups`). 

## Configuration

### `[General]` `MaxSeed`

**Setting type:** `int` \
**Default value:** `100000` \
**Acceptable value range:** [`0`,`2147483647`] (`int.MaxValue`).

Specifies the largest seed that you can provide and the game randomly generate.
