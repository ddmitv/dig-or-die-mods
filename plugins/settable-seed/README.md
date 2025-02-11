
# Settable Seed (WIP)

Adds functionality for creating a new world with specified seed.

> [!IMPORTANT]
> Currently, for some reason doesn't work.

In the menu of world creation settings will add a new field of the world seed. \
If the value for the input field is omitted, will generate a random seed.

## Implementation Info

The command `/param m_seed` will display a seed for the current world, but actually, the displayed seed value is never used in the `Solo`/`Multi` world.
The value that `m_seed` variable contains, it is just a random generated value that doesn't mean anything.

This plugin repurposes `m_seed` variable to be used in `UnityEngine.Random.InitState(int seed)` function at the beginning of the creation of a new world.

Adds new locale string `"SETTABLE_SEED_OPTIONS_SEED"` with the value `"Seed:"`.

## Configuration

### `MaxSeed`

**Setting type:** `int` \
**Default value:** `100000` \
**Acceptable value range:** [`0`,`2147483647`] (`int.MaxValue`).

Species largest seed that you can provide and the game randomly generate.
