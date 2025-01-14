
import type.magic;
import type.byte;
import std.sys;
import std.array;
import std.mem;

u8 read_count = 0;
s32 save_build_id = null;

fn assert_valid_enum(auto pattern) {
    std::assert(std::core::is_valid_enum(pattern),
        std::format("Invalid enum value: '{}'", s128(pattern))
    );  
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
} [[format("format_encoded_int_7_bit")]];

fn format_encoded_int_7_bit(ref auto value) {
    return value.number;
};

struct String {
    EncodedInt7Bit length;
    char data[length.number];
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
fn format_vector2(ref auto x) { return std::format("({}, {})", x.x, x.y); };

struct SaveDataHeader {
    float version;
    
    s32 build;
    std::assert_warn(build >= 481, std::format("build '{}' is not compatible with the game version", build));
    save_build_id = build;
    
    String game_name;
    MagicString<"Header">;
};

struct BinaryHeader {
    u8;
    s32;
    s32 hasHeaders [[comment("`true` when `hasHeaders == 2`, otherwise `false`")]];
    s32;
    s32;
};

struct GameParamsData {
    String field_name;
    BinaryHeader binary_header;
   
    u8 unknown[std::mem::find_string_in_range(0, $, builtin::std::mem::size(), "\x10Game Params Data") - $];
    
    MagicString<"Game Params Data">;
};

struct Pickup {
    s16 id;
    Vector2<float> position;
    float creation_time;
};

struct ItemsData {
    s32 count [[comment("Number of all items in the game. Note that this also includes 0th 'null' item")]];
    String code_names[count - 1] [[single_color]];
    
    s32 pickups_actives_count;
    Pickup pickups[pickups_actives_count] [[single_color]];
    MagicString<"Items Data">;
};

struct Item {
    u16 id;
    s32 amount;
} [[format("format_item")]];
fn format_item(ref auto value) { return std::format("id: {}, amount: {}", value.id, value.amount); };

struct BarItem {
    u16 id [[comment("`0` for empty bar item slot")]];
} [[format("format_bar_item")]];
fn format_bar_item(ref auto x) { return std::format("id: {}", x.id); };

struct PlayerInventory {
    s32 items_count;
    Item items[items_count];
    
    s32 bar_items_count;
    BarItem bar_items[bar_items_count];
    
    u16 item_selected_id;
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
        s32 dictionaries_count;
        ItemVarDico dictionaries[dictionaries_count];
    }
} [[format("format_item_var")]];
fn format_item_var(ref auto x) {
    return !x.exists ? "-" : "{ ... }";
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
    if (save_build_id >= 495) {
        PlayerSkin skin;
    }
    PlayerInventory inventory;
    s32 item_vars_count;
    ItemVar item_vars[item_vars_count];
} [[format("format_player")]];
fn format_player(ref auto x) { return std::format("{} ({})", x.name, x.steam_id); };

struct PlayersData {
    s32 players_count;
    Player players[players_count];
    MagicString<"Players">;
};

struct EnvironmentsData {
    s32 last_events_count;
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
    $ = std::mem::find_string_in_range(0, $, builtin::std::mem::size(), "\x0AWorld Data");
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
    
    bool is_monster = is_monster_id(code_name) [[export]];
    if (is_monster) {
        bool is_night_spawn;
        u16 target_id [[comment("`0xFFFF` for no target")]];
        bool is_creative_spawn;
    }
} [[single_color, format("format_unit")]];
fn format_unit(ref auto unit) { return std::format("{} ({})", unit.code_name, unit.id); };

struct SpecieKilled {
    String code_name;
    s32 amount;
    float last_kill_time;
} [[single_color, format("format_specie_killed")]];
fn format_specie_killed(ref auto value) { return std::format("{}, amount: {}", value.code_name, value.amount); };

struct UnitsData {
    s32 units_count;
    Unit units[units_count];
    s32 species_killed_count;
    SpecieKilled species_killed[species_killed_count];
    
    MagicString<"Units Data">;
};

struct VarsData {
    $ = std::mem::find_string_in_range(0, $, builtin::std::mem::size(), "\x09Vars Data");
    MagicString<"Vars Data">;
};

struct SaveData {
    MagicString<"SAVE FILE">;
    SaveDataHeader header [[name("Header")]];
    GameParamsData game_params_data [[name("Game Params Data")]];
    ItemsData items_data [[name("Items Data")]];
    PlayersData players_data [[name("Players")]];
    EnvironmentsData environments_data [[name("Environments")]];
    WorldData world_data [[name("World Data")]];
    UnitsData units_data [[name("Units Data")]];
    VarsData vars_data [[name("Vars Data")]];
};

SaveData data @ 0x00;
