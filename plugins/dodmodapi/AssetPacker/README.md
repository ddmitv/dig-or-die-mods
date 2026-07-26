# AssetPacker

An MSBuild task for generating C# bindings and optimized sprite atlases for DODModAPI mods.

Instead of manually packing sprites into atlases and writing boilerplate C# code, you describe your assets in a simple config file, and AssetPacker handles everything at build time:

- Packs tiles, sprites, surfaces, and unit animations into optimized atlases
- Generates a strongly-typed C# class with static fields for each asset
- Embeds the resulting PNG atlases as assembly resources
- Calculates average colors for tiles (with game-specific color compensation)
- Merges overlapping tile definitions to minimize atlas size

## Quick Start

### 1. Create a config file

Create a asset configuration file (e.g., `sprites.cfg`) describing your assets:

```cfg
# Items with tile and icon variants
TILE flashLightMK3 tile=flashLightMK3.png icon=flashLightMK3.png
TILE miniaturizorMK6 tile=miniaturizorMK6.png icon=miniaturizorMK6_icon.png

# Syntactic sugar: simple single-tile item
TILE quantumCondenser quantumCondenser.png

# Block of tiles (tiles that are expected to be placed nearby eachother)
TILE waterVaporizer waterVaporizer.png+waterVaporizerAlt.png

# Surface with material texture and top border
SURFACE fertileDirt material=surface_fertileDirt.png top=surfaceTop_fertileDirt.png

# Arbitrary sprites
SPRITE meltdownSnipe meltdownSnipe.png
SPRITE particleImpactGrenade particleImpactGrenade.png

# Unit with animation frames
UNIT myMonster stand=stand1.png+stand2.png run=run1.png+run2.png+run3.png dead=dead1.png
```

### 2. Register it in your `.csproj`

```xml
<ItemGroup>
  <ModAtlas Include="sprites.cfg" AtlasName="Textures" />
</ItemGroup>
```

The `AtlasName` attribute defines the name of the generated C# class. You can use a namespace-qualified name (e.g., `MyMod.Textures`, this will result in generating a class `Textures` in namespace `MyMod`) or just a class name that will be defined in a global namespace.

In the class, there are an automatically generated set of `const`/`readonly static` fields that contain metadata about defined asset info.
Each field name is C# identifier sanitized so you could name any asset with any name you want, but the name will be normalized in C# bindings.

### 3. Use the generated bindings in your code

```csharp

// Register embedded textures with SpriteManager
DODModAPI.SpriteManager.RegisterTexture(Textures.TileSpritesheetResource);
DODModAPI.SpriteManager.RegisterTexture(Textures.SpritesAtlasResource);
DODModAPI.SpriteManager.RegisterTexture(Textures.SurfaceTopsResource);
DODModAPI.SpriteManager.RegisterTexture(Textures.fertileDirt_surfaceMaterial);

// Use generated tile references
var item = new CItem_Device(
    tile: Textures.flashLightMK3_tile,
    tileIcon: Textures.flashLightMK3_icon,
    groupId: DeviceGroupIds.flashLight,
    type: CItem_Device.Type.Passive,
    customValue: 10f
);

// Use generated sprite references
var bullet = new ModBulletDesc(
    sprite: Textures.meltdownSnipe,
    radius: 0.7f,
    dispersionAngleRad: 0.1f,
    speedStart: 50f,
    speedEnd: 30f
);

// Use generated unit animations
var unitDesc = new CUnitMonster.CDesc(...) {
    m_anims = Textures.myMonster
};
```

## Config File Format

The config file is line-based. Each line starts with a directive keyword. 
Lines that starts with `#` symbol are ignored and considered comments.

It also supports quoted strings: `"test1 abc"` is treated just as `test1 abc`.
And, you can also do something like `"abc 123"="hello world"` (key-value + quoted strings).

### `TILE` directive

Defines 128x128 pixel tiles packed into a shared spritesheet.

```
TILE <name> <file.png>[+<file.png>]...
TILE <name> <property>=<file.png>...
TILE <name> (<property>=<file.png>[+<file.png>]...)...
```

| Form                                   | Description                                                                                                           |
| -------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `TILE name file.png`                   | Single tile. Generates field `Textures.name`.                                                                         |
| `TILE name tile=a.png some_prop=b.png` | Multiple variants. Generates `Textures.name_tile` and `Textures.name_some_prop`.                                      |
| `TILE name a.png+b.png`                | Multi-tile blocks (frames concatenated with `+`). Generates a single `ModTile` referencing the first tile's position. |

Use `+` symbol between image filenames (e.g., `a.png+b.png`) is used to merge them into a single tile blocks.
The tile blocks are always placed nearby eachother, in a horizontal line.

Some items in a game are expecting tiles to have continuous tiles nearby. For example, for door items (`CItem_Wall + m_isDoor = true`),
the door closed tile (`m_tileDoorClosed`) must be one right (`x+1`) to the main one (`x`) in the spritesheet.

Additionally, all `ModTile`s contain the `MainColor` property that is an average color of the corresponding image.
You can pass it to `CItemCell` constructor's parameter `uint mainColor` instead of manually picking the color.

| Generated fields          | Type      | Description                                       |
| ------------------------- | --------- | ------------------------------------------------- |
| `<name>_<propertyN>`      | `ModTile` | First tile of `<propertyN>`                       |
| `TileSpritesheetResource` | `string`  | Logical resource name for shared tile spritesheet |

### `SURFACE` directive

Defines a surface with a repeating material texture and a top-border tile.

```
SURFACE <name> material=<material.png> top=<top.png>
```

| Property   | Description                                                                                    |
| ---------- | ---------------------------------------------------------------------------------------------- |
| `material` | Repeating surface texture (any size, recommended **512x512**). Embedded as a separate resource |
| `top`      | Top border tile. Must be **128×128** or **128×64** pixels. Packed into a shared tops atlas     |

If the image is **128x128**, the higher **128x64** part is used for the main surface top and the lower **128x64** part is for an alternative surface top (you would need to provide `hasAltTop: true` for `DODModAPI.ModSurface`).
If the image is **128x64**, it's entirety used as a main surface top.

| Generated fields         | Type      | Description                                               |
| ------------------------ | --------- | --------------------------------------------------------- |
| `<name>_surfaceMaterial` | `string`  | Logical resource name for the material texture            |
| `<name>_surfaceTops`     | `ModTile` | Tile of surface top                                       |
| `SurfaceTopsResource`    | `string`  | Logical resource name for shared surface tops spritesheet |

### `SPRITE` directive

Defines sprites of any dimensions, packed into a single, shared atlas.

```
SPRITE <name> <file>
```

You can use it to embed any sprites into mod's assembly. Usually, it's for adding bullet sprites (`ModBulletDesc`).

| Generated fields       | Type        | Description                                    |
| ---------------------- | ----------- | ---------------------------------------------- |
| `<name>`               | `ModSprite` | Sprite info for `<name>`                       |
| `SpritesAtlasResource` | `string`    | Logical resource name for shared sprites atlas |

### `UNIT` directive

Defines unit animation frames grouped into a spritesheet. All frames for a unit must be square and the same size.

```
UNIT <name> <anim>=<frame1.png>[+<frame2.png>]... [<anim2>=<frame3.png>[+<frame4.png>]...]...
```

Units with the same sprite size are packed into a shared atlas. Units with different sizes get separate atlases.

Note that all frames for a single unit must have identical square dimensions.
All unit tiles with the same animation dimensions are placed in a shared spritesheet made for tile with identical dimensions.
For example, if unit #1 and unit #2 have identical tile dimensions, the their sprites are placed in the same spritesheet.

**Allowed animation names** (in canonical order):

| Animation   | Description           |
| ----------- | --------------------- |
| `stand`     | Idle animation        |
| `run`       | Walking/running       |
| `jump`      | Jumping               |
| `fight`     | Attacking             |
| `hurt`      | Taking damage         |
| `dead`      | Death                 |
| `standWall` | Idle on wall          |
| `runWall`   | Moving on wall        |
| `fightWall` | Attacking on wall     |
| `hurtWall`  | Taking damage on wall |

| Generated fields                 | Type         | Description                                                                          |
| -------------------------------- | ------------ | ------------------------------------------------------------------------------------ |
| `<name>`                         | `CTilesList` | Sprite info for `<name>`                                                             |
| `UnitsAtlasResource_<tile-size>` | `string`     | Logical resource name for a shared spritesheet containing sprites with the same size |

## Generated Code

For a config with `AtlasName="Textures"`, the task generates a file like:

```csharp
// <auto-generated />
#pragma warning disable

internal static class Textures {
    public const string TileSpritesheetResource = "my_mod_Textures_tile_spritesheet";
    public static readonly global::DODModAPI.ModTile flashLightMK3_tile = new global::DODModAPI.ModTile(0, 0, "my_mod_Textures_tile_spritesheet", 12345678U);
    public static readonly global::DODModAPI.ModTile flashLightMK3_icon = new global::DODModAPI.ModTile(1, 0, "my_mod_Textures_tile_spritesheet", 12345678U);

    public const string SurfaceTopsResource = "my_mod_Textures_surface_tops";
    public const string fertileDirt_surfaceMaterial = "my_mod_Textures_fertileDirt_material";
    public static readonly global::DODModAPI.ModTile fertileDirt_surfaceTops = new global::DODModAPI.ModTile(0, 0, "my_mod_Textures_surface_tops");

    public const string SpritesAtlasResource = "my_mod_Textures_sprites_atlas";
    public static readonly global::DODModAPI.ModSprite meltdownSnipe = new global::DODModAPI.ModSprite("my_mod_Textures_sprites_atlas", 0, 0, 255, 119);

    public const string UnitsAtlasResource_64 = "my_mod_Textures_units_64";
    public static readonly global::CTilesList myMonster = new global::CTilesList(0, 0, 256, 64, "my_mod_Textures_units_64", 2, 3, 0, 0, 0, 1, 0, 0, 0, 0);
}
```

## Atlas Packing Strategy

| Asset Type          | Strategy                                       | Atlas Size                               |
| ------------------- | ---------------------------------------------- | ---------------------------------------- |
| Tiles (128×128)     | Shelf packing with tile-block merging          | Power-of-two, minimum fits widest block  |
| Surface tops        | Simple grid (row-major)                        | Power-of-two                             |
| Sprites (arbitrary) | Shelf packing sorted by height (descending)    | Power-of-two, area heuristic lower bound |
| Units               | Grouped by sprite size, sequential row packing | Power-of-two per size group              |

### Tile Optimization

For `TILE` directive, the tile blocks that share common image files are automatically merged.
For example, if two items use the same base tile with different icons, the shared tile is stored only once in the atlas.

## MSBuild Integration

The package includes a `.props` file that automatically:

1. Defines the `ModAtlas` item type
2. Runs the `PackSpriteSheet` task before compilation
3. Adds generated `.g.cs` files to `<Compile>`
4. Adds generated atlas PNGs to `<EmbeddedResource>`

## Requirements

- **.NET Framework 4.7.2** or **.NET Standard 2.0** (MSBuild task runtime)
- **DODModAPI** (for `ModTile`, `ModSprite`, `CTilesList` types at compile time)
- Image formats: **PNG**, **JPEG**, **BMP** (via [SixLabors.ImageSharp](https://github.com/sixLabors/ImageSharp))
