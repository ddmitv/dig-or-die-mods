
# Enable Asserts

Basic plugin that makes built-in game assertion mechanism functional again (`SMisc.Assert` method overloads).

<details>

The `SMisc.Assert` in release version of the game is replaced with nothing, but it seems like they provide a pretty useful functionality so I re-added them back.

Places in the game where they are used:
- `CMeshQuad.Init`
- `CMeshQuadLit.Init`
- `SSingletonMono.get_Inst`
- `SSingletonScreen.get_Inst`
- `SDataLua.RegisterDesc`
- `SDrawWorld.DrawUnits`
- `SLoc.LoadLanguage`
- `SMessageSpawnUnit.Send`
- `SMisc.Serialize_Dynamic`
- `SNetworkLobbies.UpdateLobbyData`

</details>

# Configuration

### `[General]` `IsFatal`

**Setting Type:** `bool` \
**Default value:** `true`

When an assertion fails, the game process terminates immediately with an error code of `1`.
