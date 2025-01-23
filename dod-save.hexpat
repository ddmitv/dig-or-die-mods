#pragma description Dig Or Die Uncompressed Save File
#pragma endian little
#pragma magic [ 09 53 41 56 45 20 46 49 4C 45 ] @ 0x00

// === Pattern for ImHex Editor ===
//
// The Dig Or Die's save file pattern is reverse engineered from game's C# disassembly of save's load and write functionality.
// More specifically from methods:
// - SDataSave.Save
// - SDataSave.LoadData
//
// The original save files are stored in compressed form using the LZF compression algorithm, so you need to decompress them to use this pattern.
//
// Note that this pattern does not support viewing some sections in save file:
// - Game Params Data (the data is serialized with C#'s BinaryFormatter, so it is hard/impossible to recreate)
// - World Data (Includes large amount of data, so it is very hard to view and edit them + there are some problems with reverse engineering World Data format)
// - Vars Data (the data is serialized with C#'s BinaryFormatter, so it is hard/impossible to recreate)
//
// This pattern also supports different versions of the game and includes some corruption checking functionality.
//
// General information about this binary format:
// The file is split info 8 sections: Header, Game Params Data, Players, Environments, World Data, Units Data, Vars Data.
// Each section has it's own magic string on the section end (and also every save file starts with magic string).
// The game has backwards compatibility with older versions of the save files, so the pattern will dynamically parse some data depending on defined build id.

import type.magic;
import type.byte;
import std.sys;
import std.array;
import std.mem;
import std.string;

u8 read_count = 0;
s32 save_build = null;

fn lookup_code_name(s16 id) {
    std::assert(id >= 1, std::format("Invalid item id: {}", id));
    // id - 1 for accounting for a 0th `null` item
    return data.items_data.code_names[id - 1];
};

fn read_7_bit_encoded_int(u128 address) {
    s32 num = 0;
    s32 offset = 0;
    u8 i;
    for (i = 0, i < 5, i += 1) {
        u8 chuck @ address + i;
        num |= s32(chuck & 127) << offset;
        offset += 7;
        if ((chuck & 128) == 0) {
            break;
        }
    }
    read_count = i;
    return num;    
};

struct EncodedInt7Bit {
    s32 number = read_7_bit_encoded_int($);
    std::assert(read_count < 5, "Too many bytes in what should have been a 7 bit encoded Int32");
    u8 chunks[read_count] [[inline]];
} [[format("format_encoded_int_7_bit"), transform("transform_encoded_int_7_bit"), sealed]];
fn format_encoded_int_7_bit(ref auto value) {
    return value.number;
};
fn transform_encoded_int_7_bit(ref auto value) {
    return value.number;  
};

struct String {
    EncodedInt7Bit length [[comment("C#'s strings length is encoded with 7-bit signed integer")]];
    std::assert(length >= 0, "Invalid string (length < 0)");
    
    char data[length];
} [[format("format_string"), transform("transform_string")]];
fn format_string(ref auto value) {
    return value.data;
};
fn transform_string(ref auto value) {
    return value.data;  
};

struct MagicString<auto Expected> {
    String string;
    
    std::assert(string == Expected, std::format(
        "Invalid magic value! Expected \"{}\", got \"{}\" at position 0x{:X}", type::escape_bytes(Expected), type::escape_bytes(string), $ - std::string::length(Expected))
    );
} [[sealed, format("format_magic_sized_string")]];

fn format_magic_sized_string(ref auto magic) {
    return std::format("\"{}\"", type::escape_bytes(magic.string));
};

struct Color24 {
    u8 r;
    u8 g;
    u8 b;
} [[format("format_Color24")]];
fn format_Color24(ref auto value) {
    return std::format("rgb({}, {}, {})", value.r, value.g, value.b);
};

struct Vector2<T> {
    T x;
    T y;
} [[format("format_vector2")]];
fn format_vector2(ref auto x) { return std::format("({:.3f}, {:.3f})", x.x, x.y); };

fn format_item_id(auto id) {
    if (id == 0) {
        return "[empty]";
    } else {
        return std::format("{} ({})", lookup_code_name(id), id);
    }
};

struct SaveDataHeader {
    float version;
    
    s32 build;
    std::assert_warn(build >= 481, std::format("build '{}' is not compatible with the game version", build));
    save_build = build;
    
    String game_name;
    MagicString<"Header">;
};

struct GameParamsData {
    $ = std::mem::find_string(0, "\x10Game Params Data");
    std::assert($ >= 0, "Failed to find ending of the \"Game Params Data\" section");

    MagicString<"Game Params Data">;
};

struct Pickup {
    s16 id;
    std::assert(id >= 0, "Invalid pickup id (id < 0)");
    
    Vector2<float> position;
    float creation_time;
} [[format("format_pickup")]];
fn format_pickup(ref auto pickup) {
    return std::format("{} ({})", lookup_code_name(pickup.id), pickup.id);
};

struct ItemsData {
    s32 count [[comment("Number of all items in the game. Note that this also includes 0th 'null' item")]];
    std::assert(count >= 0, "Invalid code names count (size < 0)");
    String code_names[count - 1] [[single_color]];
    
    s32 pickups_actives_count;
    std::assert(pickups_actives_count >= 0, "Invalid pickups actives count (size < 0)");
    Pickup pickups[pickups_actives_count] [[single_color]];
    MagicString<"Items Data">;
};

struct Item {
    u16 id;
    s32 amount;
} [[format("format_item")]];
fn format_item(ref auto value) { return std::format("{} ({}) x{}", lookup_code_name(value.id), value.id, value.amount); };

struct BarItem {
    u16 id [[comment("`0` for empty bar item slot")]];
} [[format("format_bar_item")]];
fn format_bar_item(ref auto x) { return format_item_id(x.id); };

struct PlayerInventory {
    s32 items_count;
    std::assert(items_count >= 0, "Invalid items count in player's inventory (size < 0)");
    Item items[items_count];
    
    s32 bar_items_count;
    std::assert_warn(bar_items_count == 20, std::format("Bar items count should equal to 20 (instead of {})", bar_items_count));
    std::assert(bar_items_count >= 0, "Invalid bar items count in player's inventory (size < 0)");
    BarItem bar_items[bar_items_count];
    
    u16 item_selected_id [[format("format_item_id"), comment("`0` for empty selected item slot")]];
};

struct ItemVarDico {
    String name;
    float value;
} [[format("format_item_var_dico")]];
fn format_item_var_dico(ref auto x) { return std::format("{} = {}", x.name, x.value); };

struct ItemVar {
    bool exists;
    if (exists) {
        float time_last_use [[comment("`float.MinValue` for unused item")]];
        float time_activation [[comment("`float.MinValue` for unused item")]];
        s32 dictionary_size;
        ItemVarDico dictionary[dictionary_size];
    }
} [[format("format_item_var")]];
fn format_item_var(ref auto x) {
    if (!x.exists) { return "-"; }
    return format_item_id(std::core::array_index());
};

struct PlayerSkin {
    bool is_female;
    float color;
    s32 hair_style;
    Color24 color_hair [[single_color]];
    Color24 color_eyes [[single_color]];
};
struct Player {
    u64 steam_id;
    String name;
    Vector2<float> position;
    u16 unit_player_id;
    if (save_build >= 495) {
        PlayerSkin skin;
    }
    PlayerInventory inventory;
    s32 item_vars_count;
    std::assert(item_vars_count >= 0, "Invalid item vars count (size < 0)");
    ItemVar item_vars[item_vars_count];
} [[format("format_player")]];
fn format_player(ref auto x) { return std::format("{} ({})", x.name, x.steam_id); };

struct PlayersData {
    s32 players_count;
    std::assert(players_count >= 0, "Invalid players count (size < 0)");
    Player players[players_count];
    MagicString<"Players">;
};

struct EnvironmentsData {
    s32 last_events_count;
    std::assert(last_events_count >= 0, "Invalid last events count (size < 0)");
    String last_events[last_events_count];
    MagicString<"Environments">;
};

struct Cell {
    // CellFlags flags;
    u32 flags;
    u16 content_id;
    u16 content_hp;
    float water [[comment("Note that this also includes lava")]];
    s16 force_x;
    s16 force_y;
    Color24 light;
};
struct WorldData {
    // std::Array<std::Array<Cell, 1024>, 2> data;
    $ = std::mem::find_string(0, "\x0AWorld Data");
    std::assert($ >= 0, "Failed to find ending of the \"World Data\" section");
    MagicString<"World Data">;
};

fn is_monster_id(str id) {
    match(id) {
        ("hound" | "firefly" | "fireflyRed" | "dweller" | "fish" | "bat" | "houndBlack" | "fireflyBlack" | "fireflyBlack"
      | "dwellerBlack" | "fishBlack" | "batBlack" | "bossMadCrab" | "shark" | "fireflyExplosive" | "antClose" | "antDist"
      | "bossFirefly" | "bossDweller" | "lavaAnt" | "lavaBat" | "particleGround" | "particleBird" | "particleBird2" | "balrogMini"
      | "bossBalrog" | "bossBalrog2"):
            return true;
    }
    return false;
};
struct Unit {
    String code_name;
    Vector2<float> position;
    u16 id;
    float hp;
    float air;
    
    bool is_monster = is_monster_id(code_name) [[export, comment("Only works on non-modded monsters")]];
    if (is_monster) {
        bool is_night_spawn;
        if (save_build >= 626) {
            u16 target_id [[comment("`0xFFFF` for no target")]];
        }
        if (save_build >= 830) {
            bool is_creative_spawn;
        }
    }
} [[single_color, format("format_unit")]];
fn format_unit(ref auto unit) { return std::format("{} ({})", unit.code_name, unit.id); };

struct SpecieKilled {
    String code_name;
    s32 amount;
    float last_kill_time;
} [[single_color, format("format_specie_killed")]];
fn format_specie_killed(ref auto value) { return std::format("{} x{}", value.code_name, value.amount); };

struct UnitsData {
    s32 units_count;
    std::assert(units_count >= 0, "Invalid units count (size < 0)");
    Unit units[units_count];
    s32 species_killed_count;
    std::assert(species_killed_count >= 0, "Invalid species killed count (size < 0)");
    SpecieKilled species_killed[species_killed_count];
    
    MagicString<"Units Data">;
};

struct VarsData {
    $ = std::mem::find_string(0, "\x09Vars Data");
    std::assert($ >= 0, "Failed to find ending of the \"Vars Data\" section");
    MagicString<"Vars Data">;
};

struct SaveData {
    MagicString<"SAVE FILE">;
    SaveDataHeader header [[name("Header")]];
    GameParamsData game_params_data [[name("Game Params Data"), comment("Contains C#'s BinaryFormatter serialized data")]];
    ItemsData items_data [[name("Items Data")]];
    PlayersData players_data [[name("Players")]];
    
    if (save_build >= 815) {
        EnvironmentsData environments_data [[name("Environments")]];
    }
    
    WorldData world_data [[name("World Data"), comment("Includes large amount of data, so it is literally impossible to view them")]];
    UnitsData units_data [[name("Units Data")]];
    VarsData vars_data [[name("Vars Data"), comment("Contains C#'s BinaryFormatter serialized data")]];
};

SaveData data @ 0x00 [[inline]];