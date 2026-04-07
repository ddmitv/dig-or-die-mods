
-- dod-proto.lua - Wireshark Lua Dissector for modified Dig or Die multiplayer TCP packets (used by plugin dedicated-client)
-- Default TCP port: 3776 (can be changed in the settings: Edit -> Preferences -> Protocols -> DOD_PROTO)
--
-- Put this file in:
--   On Windows: %APPDATA%\Wireshark\plugins\dod-proto.lua
--   On Unix-like: ~/.local/lib/wireshark/plugins/dod-proto.lua or ~/.config/wireshark/plugins/dod-proto.lua
-- (alternatively see Help -> About Wireshark -> Folders, find string "Personal Lua Plugins")
-- 
-- Press Ctrl+Shift+L to reload Lua plugins (can be also accessible via Analyze -> Reload Lua Plugins)
--
-- Filter examples:
-- 1. `dod_proto` - include only Dig or Die multiplayer TCP packets
-- 2. `dod_proto.message_id == 9` - include only packets which message has ID equal to 9 (SMessageUnitsPos)
-- 3. `dod_proto.message_id == "SMessageUnitsPos"` - same as above, but message ID written as message name.
--                                                   Works for all messages, items, unit descriptors, background and events
-- 4. `dod_proto.chat_message contains "hello"` - include only packets that contain string "hello" in chat messages (only SMessageChat contains .chat_message field) 
-- 
-- About "modified" part in Dig or Die multiplayer protocol:
-- Each message is additionally prefixed with 4 bytes (uint32) defining its length (excluding the 4-byte length field itself)

local MESSAGE_TABLE = {
    [1] = {"SMessageTest", 0},
    [2] = {"SMessagePing", 8},
    [3] = {"SMessagePong", 8},
    [4] = {"SMessageCamFormat", 4},
    [5] = {"SMessagePlayerInfos", -1},
    [6] = {"SMessageItemsBar", 5},
    [7] = {"SMessagePlayerPos", 12},
    [8] = {"SMessageMonsterChangeTarget", 7},
    [9] = {"SMessageUnitsPos", -1},
    [10] = {"SMessageMonstersAttack", 6},
    [11] = {"SMessageItemUse", 6},
    [12] = {"SMessageItemActivate", 7},
    [13] = {"SMessageItemUpdateFlags", 12},
    [14] = {"SMessageDoDamage", 9},
    [15] = {"SMessageRequestMasterDrone", 2},
    [16] = {"SMessageRequestDig", 6},
    [17] = {"SMessageRequestRemoveWire", 4},
    [18] = {"SMessageRequestRemoveBackwall", 4},
    [19] = {"SMessageRequestBuildWire", 7},
    [20] = {"SMessageRequestBuild", 6},
    [21] = {"SMessageRequestCraft", 34},
    [22] = {"SMessageRequestThrow", 9},
    [23] = {"SMessageRequestStormRain", 4},
    [24] = {"SMessageJoinMe", 12},
    [25] = {"SMessageStartInfos", -1},
    [26] = {"SMessageInventory", 6},
    [27] = {"SMessageServerWorld_OnChange", -1},
    [28] = {"SMessageServerWorld_Water", -1},
    [29] = {"SMessagePickups", -1},
    [30] = {"SMessageSpawnUnit", -1},
    [31] = {"SMessageRemoveUnit", 2},
    [32] = {"SMessageRocketChangeStep", 2},
    [33] = {"SMessageOneBlock", 12},
    [34] = {"SMessageFireMeteor", 8},
    [35] = {"SMessageChat", -1},
    [100] = {"SMessageRequestCreative_SetItem", 6},
    [101] = {"SMessageRequestCreative_SetBg", 5},
    [102] = {"SMessageRequestCreative_SpawnMonster", 7},
    [253] = {"SMessagePing2", 6},
    [254] = {"SMessagePong2", 6},
}

local ITEM_ID_TO_CODENAME = {
    [0] = "[null]",
    [1] = "miniaturizorMK1",
    [2] = "miniaturizorMK2",
    [3] = "miniaturizorMK3",
    [4] = "miniaturizorMK4",
    [5] = "miniaturizorMK5",
    [6] = "miniaturizorUltimate",
    [7] = "potionHp",
    [8] = "potionHpRegen",
    [9] = "potionHpBig",
    [10] = "potionHpMega",
    [11] = "potionArmor",
    [12] = "potionPheromones",
    [13] = "potionCritics",
    [14] = "potionInvisibility",
    [15] = "potionSpeed",
    [16] = "armorMk1",
    [17] = "armorMk2",
    [18] = "armorMk3",
    [19] = "armorUltimate",
    [20] = "defenseShield",
    [21] = "drone",
    [22] = "droneCombat",
    [23] = "droneWar",
    [24] = "flashLight",
    [25] = "minimapper",
    [26] = "effeilGlasses",
    [27] = "metalDetector",
    [28] = "waterDetector",
    [29] = "flashLightMK2",
    [30] = "waterBreather",
    [31] = "jetpack",
    [32] = "invisibilityDevice",
    [33] = "ultimateJetpack",
    [34] = "ultimateBrush",
    [35] = "ultimateRebreather",
    [36] = "gunRifle",
    [37] = "gunShotgun",
    [38] = "gunMachineGun",
    [39] = "gunSnipe",
    [40] = "gunLaser",
    [41] = "gunRocket",
    [42] = "gunZF0",
    [43] = "gunMegaSnipe",
    [44] = "gunLaserGatling",
    [45] = "gunStorm",
    [46] = "gunGrenadeLaunch",
    [47] = "gunParticlesShotgun",
    [48] = "gunParticlesSniper",
    [49] = "gunFlamethrower",
    [50] = "gunLightSword",
    [51] = "gunUltimateParticlesGatling",
    [52] = "gunUltimateGrenadeLauncher",
    [53] = "ultimateWaterPistol",
    [54] = "ultimateLavaPistol",
    [55] = "ultimateSpongePistol",
    [56] = "ultimateTotoroGun",
    [57] = "ultimateMonstersGun",
    [58] = "turret360",
    [59] = "turretGatling",
    [60] = "turretReparator",
    [61] = "turretHeavy",
    [62] = "turretMine",
    [63] = "turretSpikes",
    [64] = "turretReparatorMK2",
    [65] = "turretCeiling",
    [66] = "turretLaser",
    [67] = "turretTesla",
    [68] = "turretFlame",
    [69] = "turretParticles",
    [70] = "explosive",
    [71] = "wallWood",
    [72] = "platform",
    [73] = "wallConcrete",
    [74] = "wallIronSupport",
    [75] = "backwall",
    [76] = "wallReinforced",
    [77] = "wallDoor",
    [78] = "platformSteel",
    [79] = "generatorWater",
    [80] = "waterPump",
    [81] = "wallComposite",
    [82] = "wallCompositeSupport",
    [83] = "wallCompositeLight",
    [84] = "wallCompositeDoor",
    [85] = "wallUltimate",
    [86] = "autoBuilderMK1",
    [87] = "autoBuilderMK2",
    [88] = "light",
    [89] = "autoBuilderMK3",
    [90] = "lightSticky",
    [91] = "electricWire",
    [92] = "generatorSun",
    [93] = "lightSun",
    [94] = "teleport",
    [95] = "elecSwitch",
    [96] = "autoBuilderMK4",
    [97] = "autoBuilderMK5",
    [98] = "elecSwitchPush",
    [99] = "elecSwitchRelay",
    [100] = "elecCross",
    [101] = "elecSignal",
    [102] = "elecClock",
    [103] = "elecToggle",
    [104] = "elecDelay",
    [105] = "elecWaterSensor",
    [106] = "elecProximitySensor",
    [107] = "elecDistanceSensor",
    [108] = "elecAND",
    [109] = "elecOR",
    [110] = "elecXOR",
    [111] = "elecNOT",
    [112] = "elecLight",
    [113] = "elecAlarm",
    [114] = "reactor",
    [115] = "rocketTop",
    [116] = "rocketTank",
    [117] = "rocketEngine",
    [118] = "autoBuilderUltimate",
    [119] = "dirt",
    [120] = "dirtRed",
    [121] = "silt",
    [122] = "dirtBlack",
    [123] = "dirtSky",
    [124] = "rock",
    [125] = "iron",
    [126] = "coal",
    [127] = "copper",
    [128] = "gold",
    [129] = "aluminium",
    [130] = "rockFlying",
    [131] = "rockGaz",
    [132] = "crystal",
    [133] = "crystalLight",
    [134] = "crystalBlack",
    [135] = "granit",
    [136] = "uranium",
    [137] = "titanium",
    [138] = "lightonium",
    [139] = "thorium",
    [140] = "sulfur",
    [141] = "sapphire",
    [142] = "organicRock",
    [143] = "organicRockHeart",
    [144] = "lava",
    [145] = "lavaOld",
    [146] = "diamonds",
    [147] = "organicRockDefenseLead",
    [148] = "organicRockDefense",
    [149] = "wood",
    [150] = "woodwater",
    [151] = "woodSky",
    [152] = "woodGranit",
    [153] = "deadPlant",
    [154] = "tree",
    [155] = "treePine",
    [156] = "treeWater",
    [157] = "treeSky",
    [158] = "treeGranit",
    [159] = "bush",
    [160] = "flowerBlue",
    [161] = "flowerWhite",
    [162] = "fernRed",
    [163] = "waterBush",
    [164] = "waterLight",
    [165] = "waterCoral",
    [166] = "blackGrass",
    [167] = "blackMushroom",
    [168] = "skyBush",
    [169] = "lavaFlower",
    [170] = "lavaPlant",
    [171] = "bushGranit",
    [172] = "organicHair",
    [173] = "metalScrap",
    [174] = "lightGem",
    [175] = "energyGem",
    [176] = "darkGem",
    [177] = "dogHorn",
    [178] = "dogHorn3",
    [179] = "moleShell",
    [180] = "moleShellBlack",
    [181] = "fish2Regen",
    [182] = "fish3Regen",
    [183] = "bat2Sonar",
    [184] = "bat3Sonar",
    [185] = "antShell",
    [186] = "sharkSkin",
    [187] = "unstableGemResidue",
    [188] = "lootDwellerLord",
    [189] = "lootParticleGround",
    [190] = "lootParticleBirds",
    [191] = "lootLargeParticleBirds",
    [192] = "lootLavaSpider",
    [193] = "lootLavaBat",
    [194] = "lootMiniBalrog",
    [195] = "bloodyFlesh1",
    [196] = "bloodyFlesh2",
    [197] = "bloodyFlesh3",
    [198] = "bossMadCrabSonar",
    [199] = "bossMadCrabMaterial",
    [200] = "masterGem",
    [201] = "lootBalrog"
}

local UNIT_ID_TO_CODENAME = {
    [1] = "player",
    [2] = "playerLocal",
    [3] = "defense",
    [4] = "drone",
    [5] = "droneCombat",
    [6] = "droneWar",
    [7] = "hound",
    [8] = "firefly",
    [9] = "fireflyRed",
    [10] = "dweller",
    [11] = "fish",
    [12] = "bat",
    [13] = "houndBlack",
    [14] = "fireflyBlack",
    [15] = "dwellerBlack",
    [16] = "fishBlack",
    [17] = "batBlack",
    [18] = "bossMadCrab",
    [19] = "shark",
    [20] = "fireflyExplosive",
    [21] = "antClose",
    [22] = "antDist",
    [23] = "bossFirefly",
    [24] = "bossDweller",
    [25] = "lavaAnt",
    [26] = "lavaBat",
    [27] = "particleGround",
    [28] = "particleBird",
    [29] = "particleBird2",
    [30] = "balrogMini",
    [31] = "bossBalrog",
    [32] = "bossBalrog2"
}
local EVENT_ID_TO_NAME = {
    [-1] = "[none]",
    [0] = "heatWave",
    [1] = "rainFlood",
    [2] = "meteorShower",
    [3] = "volcanoEruption",
    [4] = "earthquake",
    [5] = "quietNight",
    [6] = "restlessNight",
    [7] = "gravityWaves",
    [8] = "acidWater",
    [9] = "drowsiness",
    [10] = "matingSeason",
    [11] = "luckyDay",
    [12] = "sunEclipse",
    [13] = "mist",
    [14] = "emp",
    [15] = "sharkstorm",
    [16] = "meteorShower",
}

local msg_names_map = {}
for id, info in pairs(MESSAGE_TABLE) do
    msg_names_map[id] = info[1]
end

dod_proto = Proto("dod_proto", "Dig or Die protocol")

local f = {
    total_length = ProtoField.uint32("dod_proto.total_length", "Message Payload Length", base.DEC_HEX),
    message_id = ProtoField.uint8("dod_proto.message_id", "Message ID", base.DEC_HEX, msg_names_map),
    message_size = ProtoField.int32("dod_proto.message_size", "Message Size"),
    steam_id = ProtoField.uint64("dod_proto.steam_id", "Player Steam ID", base.DEC_HEX),
    player_unit_id = ProtoField.uint16("dod_proto.player_unit_id", "Player Unit ID"),
    item_id = ProtoField.uint16("dod_proto.item_id", "Item ID", base.DEC, ITEM_ID_TO_CODENAME),
    unit_instance_id = ProtoField.uint16("dod_proto.unit_instance_id", "Unit Instance ID"),
    unit_desc_id = ProtoField.uint8("dod_proto.unit_desc_id", "Unit Descriptor ID", base.DEC, UNIT_ID_TO_CODENAME),
    chat_message = ProtoField.string("dod_proto.chat_message", "Chat Message", base.UNICODE),
    background_id = ProtoField.uint8("dod_proto.background_id", "Background ID", base.DEC, { [1] = "Dirt", [2] = "Rock", [3] = "Granit", [4] = "Crystal", [5] = "Lava", [6] = "Organic", [255] = "None" }),
    monster_night_spawn = ProtoField.bool("dod_proto.monster_night_spawn", "Monster Night Spawn"),
    unit_hp = ProtoField.float("dod_proto.unit_hp", "Unit HP"),
    craft_out_id = ProtoField.uint16("dod_proto.craft.out_id", "Output Item ID", base.DEC, ITEM_ID_TO_CODENAME),
    craft_in1_id = ProtoField.uint16("dod_proto.craft.in1_id", "Ingredient 1 ID", base.DEC, ITEM_ID_TO_CODENAME),
    craft_in2_id = ProtoField.uint16("dod_proto.craft.in2_id", "Ingredient 2 ID", base.DEC, ITEM_ID_TO_CODENAME),
    craft_in3_id = ProtoField.uint16("dod_proto.craft.in3_id", "Ingredient 3 ID", base.DEC, ITEM_ID_TO_CODENAME),
    craft_all_free = ProtoField.bool("dod_proto.craft.all_free", "All Free"),
    craft_is_upgrade = ProtoField.bool("dod_proto.craft.is_upgrade", "Is Upgrade"),
    clock = ProtoField.float("dod_proto.clock", "Clock"),
    cloud = ProtoField.float("dod_proto.cloud", "Cloud Position Ratio"),
    nights = ProtoField.float("dod_proto.nights", "Nights Survived"),
    ach_locked = ProtoField.bool("dod_proto.ach_locked", "Achievements Locked"),
    event_id = ProtoField.int32("dod_proto.event_id", "Event ID", base.DEC, EVENT_ID_TO_NAME),
    event_delta = ProtoField.float("dod_proto.event_delta", "Event Delta"),
    eruption_delta = ProtoField.float("dod_proto.eruption_delta", "Eruption Time Delta"),
}
dod_proto.fields = f

local function read_float_from_ushort(tvb, offset, max)
    return tvb(offset, 2):le_uint() * max / 65535.0
end

local function read_float_from_short(tvb, offset, max)
    return tvb(offset, 2):le_int() * max / 32767.0
end

local function read_vector2_from_ushort2(tvb, offset, max)
    local x = read_float_from_ushort(tvb, offset, max)
    local y = read_float_from_ushort(tvb, offset + 2, max)
    return x, y
end

local function read_vector2_from_short2(tvb, offset, max)
    local x = read_float_from_short(tvb, offset, max)
    local y = read_float_from_short(tvb, offset + 2, max)
    return x, y
end

local function read_7_bit_encoded_int(tvb, offset)
    local result = 0
    local shift = 0
    
    while true do
        local chunk = tvb(offset, 1):uint()
        offset = offset + 1
        
        result = result | ((chunk & 0x7F) << shift)
        if chunk & 0x80 == 0 then
            break
        end
        shift = shift + 7
        if shift >= 35 then break end
    end
    return result, offset
end

local function get_bit(bitmap_bytes, bit_index)
    local byte_index = math.floor(bit_index / 8) + 1
    local bit_in_byte = bit_index % 8
    local byte_val = string.byte(bitmap_bytes, byte_index) or 0
    return (byte_val >> bit_in_byte) & 1
end

local function item_codename(id)
    return ITEM_ID_TO_CODENAME[id] or "UNKNOWN"
end

local payload_dissectors = {
    [1] = function(tvb, tree) -- SMessageTest
        tree:add_le(dod_proto, tvb(), "Raw payload")
    end,
    [2] = function(tvb, tree) -- SMessagePing
        tree:add_le(dod_proto, tvb(), "No payload")
    end,
    [3] = function(tvb, tree) -- SMessagePong
        tree:add_le(dod_proto, tvb(), "No payload")
    end,
    [4] = function(tvb, tree) -- SMessageCamFormat
        local aspect = tvb(0, 4):le_float()
        tree:add_le(dod_proto, tvb(0, 4), "Camera Aspect: " .. aspect)
    end,
    [5] = function(tvb, tree) -- SMessagePlayerInfos
        local offset = 0
        tree:add_le(f.steam_id, tvb(offset, 8))
        offset = offset + 8

        local is_female = tvb(offset, 1):le_uint() ~= 0
        tree:add_le(dod_proto, tvb(offset,1), "Skin Female: " .. tostring(is_female))
        offset = offset + 1

        local skin_color = tvb(offset, 4):le_float()
        tree:add_le(dod_proto, tvb(offset,4), "Skin Color: " .. skin_color)
        offset = offset + 4

        local hair_style = tvb(offset, 4):le_int()
        tree:add_le(dod_proto, tvb(offset,4), "Hair Style: " .. hair_style)
        offset = offset + 4

        local hair_r = tvb(offset, 1):le_uint()
        local hair_g = tvb(offset+1, 1):le_uint()
        local hair_b = tvb(offset+2, 1):le_uint()
        tree:add_le(dod_proto, tvb(offset,3), "Hair Color: RGB("..hair_r..","..hair_g..","..hair_b..")")
        offset = offset + 3

        local eyes_r = tvb(offset, 1):le_uint()
        local eyes_g = tvb(offset+1, 1):le_uint()
        local eyes_b = tvb(offset+2, 1):le_uint()
        tree:add_le(dod_proto, tvb(offset,3), "Eyes Color: RGB("..eyes_r..","..eyes_g..","..eyes_b..")")
        offset = offset + 3

        local inventory_tree = tree:add(dod_proto, tvb(offset), "Inventory")

        local item_count = 0
        while offset < tvb:len() do
            local item_tree = inventory_tree:add(dod_proto, tvb(offset, 18))

            local item_id = tvb(offset, 2):le_uint()
            item_tree:add_le(f.item_id, tvb(offset,2))
            offset = offset + 2

            local nb = tvb(offset, 2):le_uint()
            item_tree:add_le(dod_proto, tvb(offset,2), "Count: " .. nb)
            offset = offset + 2

            local slot = tvb(offset, 4):le_int()
            item_tree:add_le(dod_proto, tvb(offset,4), "Bar Slot: " .. slot)
            offset = offset + 4

            local time_act = tvb(offset, 4):le_float()
            item_tree:add_le(dod_proto, tvb(offset,4), "Time Since Activation: " .. time_act)
            offset = offset + 4

            local time_last_use = tvb(offset, 4):le_float()
            item_tree:add_le(dod_proto, tvb(offset,4), "Time Since Last Use: " .. time_last_use)
            offset = offset + 4

            local passive = tvb(offset, 1):le_uint() ~= 0
            item_tree:add_le(dod_proto, tvb(offset,1), "Passive On: " .. tostring(passive))
            offset = offset + 1

            local jetpack = tvb(offset, 1):le_uint() ~= 0
            item_tree:add_le(dod_proto, tvb(offset,1), "Jetpack Active: " .. tostring(jetpack))
            offset = offset + 1

            item_tree:set_text(item_codename(item_id).." ("..item_id..") x"..item_count)

            item_count = item_count + 1
        end
        inventory_tree:append_text(" ("..item_count.." items)")
    end,
    [6] = function(tvb, tree) -- SMessageItemsBar
        local offset = 0
        tree:add_le(f.player_unit_id, tvb(offset,2))
        offset = offset + 2

        local slot = tvb(offset, 1):le_uint()
        tree:add_le(dod_proto, tvb(offset,1), "Slot: " .. slot)
        offset = offset + 1

        local item_id = tvb(offset,2):le_uint()
        tree:add_le(f.item_id, tvb(offset,2))
    end,
    [7] = function(tvb, tree) -- SMessagePlayerPos
        local offset = 0
        local x, y = read_vector2_from_ushort2(tvb, offset, 1024)
        tree:add_le(dod_proto, tvb(offset,4), "Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        local vx, vy = read_vector2_from_short2(tvb, offset, 100)
        tree:add_le(dod_proto, tvb(offset,4), "Speed: (" .. vx .. ", " .. vy .. ")")
        offset = offset + 4

        local lookAngle = read_float_from_short(tvb, offset, 3.5)
        tree:add_le(dod_proto, tvb(offset,2), "Look Angle: " .. lookAngle)
        offset = offset + 2

        local hp = read_float_from_ushort(tvb, offset, 1000)
        tree:add_le(f.unit_hp, tvb(offset,2), hp)
    end,
    [8] = function(tvb, tree) -- SMessageMonsterChangeTarget
        local offset = 0
        local monster_id = tvb(offset, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,2), "Monster ID: " .. monster_id)
        offset = offset + 2

        local target_id = tvb(offset, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,2), "Target ID: " .. target_id)
        offset = offset + 2

        tree:add_le(f.monster_night_spawn, tvb(offset,1))
        offset = offset + 1

        local hp = read_float_from_ushort(tvb, offset, 20000)
        tree:add_le(f.unit_hp, tvb(offset,2), hp)
    end,
    [9] = function(tvb, tree) -- SMessageUnitsPos
        local offset = 0
        local unit_count = 0
        local unit_count_label = tree:add_le(dod_proto, tvb(0, 0), "Units count: "):set_generated()
        while offset < tvb:len() do
            local unit_tree = tree:add(dod_proto, tvb(offset, 14))

            local unit_instance_id = tvb(offset, 2):le_uint()
            unit_tree:add_le(f.unit_instance_id, tvb(offset,2))
            offset = offset + 2

            local x, y = read_vector2_from_ushort2(tvb, offset, 1024)
            unit_tree:add_le(dod_proto, tvb(offset,4), "Position: (" .. x .. ", " .. y .. ")")
            offset = offset + 4

            local vx, vy = read_vector2_from_short2(tvb, offset, 100)
            unit_tree:add_le(dod_proto, tvb(offset,4), "Speed: (" .. vx .. ", " .. vy .. ")")
            offset = offset + 4

            local look_angle = read_float_from_short(tvb, offset, 3.5)
            unit_tree:add_le(dod_proto, tvb(offset,2), "Look Angle: " .. look_angle)
            offset = offset + 2

            local hp = read_float_from_ushort(tvb, offset, 20000)
            unit_tree:add_le(f.unit_hp, tvb(offset,2), hp)
            offset = offset + 2

            unit_tree:set_text("Unit #"..unit_instance_id.." at ("..x..", "..y..")")

            unit_count = unit_count + 1
        end
        unit_count_label:append_text(unit_count)
    end,
    [10] = function(tvb, tree) -- SMessageMonstersAttack
        local offset = 0
        tree:add_le(f.unit_instance_id, tvb(offset,2))
        offset = offset + 2

        local x, y = read_vector2_from_ushort2(tvb, offset, 1024)
        tree:add_le(dod_proto, tvb(offset,4), "Target Position: (" .. x .. ", " .. y .. ")")
    end,
    [11] = function(tvb, tree) -- SMessageItemUse
        local offset = 0
        local item_id_with_shift = tvb(offset, 2):le_uint()
        local shift = item_id_with_shift >= 32767
        local item_id = item_id_with_shift % 32767
        tree:add_le(f.item_id, tvb(offset,2), item_id):set_generated()
        tree:add_le(dod_proto, tvb(offset,2), "Shift: "..tostring(shift)):set_generated()
        offset = offset + 2

        local x, y = read_vector2_from_ushort2(tvb, offset, 1024)
        tree:add_le(dod_proto, tvb(offset,4), "Position: (" .. x .. ", " .. y .. ")")
    end,
    [12] = function(tvb, tree) -- SMessageItemActivate
        local offset = 0
        local item_id = tvb(offset, 2):le_uint()
        tree:add_le(f.item_id, tvb(offset,2))
        offset = offset + 2

        local x = tvb(offset, 2):le_uint()
        local y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Cell Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        local is_mode = tvb(offset, 1):le_uint() ~= 0
        tree:add_le(dod_proto, tvb(offset,1), "Is Mode: " .. tostring(is_mode))
    end,
    [13] = function(tvb, tree) -- SMessageItemUpdateFlags
        local offset = 0
        local item_id = tvb(offset, 2):le_uint()
        tree:add_le(f.item_id, tvb(offset,2))
        offset = offset + 2

        local time_since_act = tvb(offset, 4):le_float()
        tree:add_le(dod_proto, tvb(offset,4), "Time Since Activation: " .. time_since_act)
        offset = offset + 4

        local time_since_last_use = tvb(offset, 4):le_float()
        tree:add_le(dod_proto, tvb(offset,4), "Time Since Last Use: " .. time_since_last_use)
        offset = offset + 4

        local passive = tvb(offset, 1):le_uint() ~= 0
        tree:add_le(dod_proto, tvb(offset,1), "Passive On: " .. tostring(passive))
        offset = offset + 1

        local jetpack = tvb(offset, 1):le_uint() ~= 0
        tree:add_le(dod_proto, tvb(offset,1), "Jetpack Active: " .. tostring(jetpack))
    end,
    [14] = function(tvb, tree) -- SMessageDoDamage
        local offset = 0
        tree:add_le(f.unit_instance_id, tvb(offset,2))
        offset = offset + 2

        local damage = tvb(offset, 4):le_float()
        tree:add_le(dod_proto, tvb(offset,4), "Damage: " .. damage)
        offset = offset + 4

        local attacker_id = tvb(offset, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,2), "Attacker ID: " .. attacker_id)
        offset = offset + 2

        local show_damage = tvb(offset, 1):le_uint() ~= 0
        tree:add_le(dod_proto, tvb(offset,1), "Show Damage: " .. tostring(show_damage))
    end,
    [15] = function(tvb, tree) -- SMessageRequestMasterDrone
        tree:add_le(f.player_unit_id, tvb(0,2))
    end,
    [16] = function(tvb, tree) -- SMessageRequestDig
        local offset = 0
        local x = tvb(offset, 2):le_uint()
        local y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Cell Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        local damage = tvb(offset, 2):le_int()
        tree:add_le(dod_proto, tvb(offset,2), "Damage: " .. damage)
    end,
    [17] = function(tvb, tree) -- SMessageRequestRemoveWire
        local x = tvb(0, 2):le_uint()
        local y = tvb(2, 2):le_uint()
        tree:add_le(dod_proto, tvb(0,4), "Cell Position: (" .. x .. ", " .. y .. ")")
    end,
    [18] = function(tvb, tree) -- SMessageRequestRemoveBackwall
        local x = tvb(0, 2):le_uint()
        local y = tvb(2, 2):le_uint()
        tree:add_le(dod_proto, tvb(0,4), "Cell Position: (" .. x .. ", " .. y .. ")")
    end,
    [19] = function(tvb, tree) -- SMessageRequestBuildWire
        local offset = 0
        local x = tvb(offset, 2):le_uint()
        local y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Cell Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        local item_id = tvb(offset, 2):le_uint()
        tree:add_le(f.item_id, tvb(offset,2))
        offset = offset + 2

        local wire_dir = tvb(offset, 1):le_uint()
        tree:add_le(dod_proto, tvb(offset,1), "Wire Direction: " .. wire_dir)
    end,
    [20] = function(tvb, tree) -- SMessageRequestBuild
        local offset = 0
        local x = tvb(offset, 2):le_uint()
        local y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Cell Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        tree:add_le(f.item_id, tvb(offset,2))
    end,
    [21] = function(tvb, tree) -- SMessageRequestCraft
        local offset = 0
        tree:add_le(f.craft_out_id, tvb(offset,2))
        offset = offset + 2

        local out_nb = tvb(offset, 4):le_int()
        tree:add_le(dod_proto, tvb(offset,4), "Output Count: " .. out_nb)
        offset = offset + 4

        tree:add_le(f.craft_in1_id, tvb(offset,2))
        offset = offset + 2

        local in1_nb = tvb(offset, 4):le_int()
        tree:add_le(dod_proto, tvb(offset,4), "Input1 Count: " .. in1_nb)
        offset = offset + 4

        tree:add_le(f.craft_in2_id, tvb(offset,2))
        offset = offset + 2

        local in2_nb = tvb(offset, 4):le_int()
        tree:add_le(dod_proto, tvb(offset,4), "Input2 Count: " .. in2_nb)
        offset = offset + 4

        tree:add_le(f.craft_in3_id, tvb(offset,2))
        offset = offset + 2

        local in3_nb = tvb(offset, 4):le_int()
        tree:add_le(dod_proto, tvb(offset,4), "Input3 Count: " .. in3_nb)
        offset = offset + 4

        tree:add_le(f.is_upgrade, tvb(offset,1))
        offset = offset + 1

        local nb_to_craft = tvb(offset, 4):le_int()
        tree:add_le(dod_proto, tvb(offset,4), "Number to Craft: " .. nb_to_craft)
        offset = offset + 4

        local ab_x = tvb(offset, 2):le_uint()
        local ab_y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "AutoBuilder Position: (" .. ab_x .. ", " .. ab_y .. ")")
        offset = offset + 4

        tree:add_le(f.craft_all_free, tvb(offset,1))
    end,
    [22] = function(tvb, tree) -- SMessageRequestThrow
        local offset = 0
        tree:add_le(f.item_id, tvb(offset,2))
        offset = offset + 2

        local x = tvb(offset, 2):le_uint()
        local y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        local random = tvb(offset, 1):le_uint() ~= 0
        tree:add_le(dod_proto, tvb(offset,1), "Random: " .. tostring(random))
        offset = offset + 1

        local unit_id = tvb(offset, 2):le_uint()
        tree:add_le(f.unit_instance_id, tvb(offset,2), "Target Unit ID: " .. unit_id)
    end,
    [23] = function(tvb, tree) -- SMessageRequestStormRain
        local x = tvb(0, 2):le_uint()
        local y = tvb(2, 2):le_uint()
        tree:add_le(dod_proto, tvb(0,4), "Cell Position: (" .. x .. ", " .. y .. ")")
    end,
    [24] = function(tvb, tree) -- SMessageJoinMe
        local offset = 0
        local phase = tvb(offset, 4):le_int()
        tree:add_le(dod_proto, tvb(offset,4), "Phase: " .. phase)
        offset = offset + 4

        local lobby_id = tvb(offset, 8):le_uint64()
        tree:add_le(dod_proto, tvb(offset,8), "Lobby ID: " .. lobby_id)
    end,
    [25] = function(tvb, tree) -- SMessageStartInfos
        local offset = 0
        local params_len = tvb(offset, 4):le_int()
        tree:add_le(dod_proto, tvb(offset, 4), "Params length: "..params_len)
        local params_bytes = tvb(offset + 4, params_len):bytes()
        tree:add_le(dod_proto, tvb(offset + 4, params_len), "Compressed Params Data: "..tostring(params_bytes))
        offset = offset + 4 + params_len

        local world_len = tvb(offset, 4):le_int()
        tree:add_le(dod_proto, tvb(offset, 4), "World Data length: "..world_len)
        local world_bytes = tvb(offset + 4, world_len):bytes()
        tree:add_le(dod_proto, tvb(offset + 4, world_len), "Compressed World Data: "..tostring(world_bytes))
        offset = offset + 4 + world_len

        tree:add_le(f.player_unit_id, tvb(offset,2))
        offset = offset + 2

        local pos_saved_x = tvb(offset, 4):le_float()
        local pos_saved_y = tvb(offset + 4, 4):le_float()
        tree:add_le(dod_proto, tvb(offset, 8), "Saved Position: (" .. pos_saved_x .. ", " .. pos_saved_y .. ")")
    end,
    [26] = function(tvb, tree) -- SMessageInventory
        local offset = 0
        tree:add_le(f.player_unit_id, tvb(offset,2))
        offset = offset + 2

        tree:add_le(f.item_id, tvb(offset,2))
        offset = offset + 2

        local nb = tvb(offset, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,2), "Count: " .. nb)
    end,
    [27] = function(tvb, tree) -- SMessageServerWorld_OnChange
        local offset = 0
        local chunk_count = 0
        local chunk_count_label = tree:add(dod_proto, tvb(0, 0), "Chunk count: "):set_generated()
        while offset < tvb:len() do
            local chunk_tree = tree:add(dod_proto, tvb(offset, 2 + 8*3*3), "Chunk")
            local i = tvb(offset, 1):le_uint()
            local j = tvb(offset+1, 1):le_uint()
            chunk_tree:add_le(dod_proto, tvb(offset,2), "Index (" .. i .. ", " .. j .. ")")
            chunk_tree:append_text(" at ("..i..", "..j..")")
            offset = offset + 2

            for k = 0, 3 do
                for l = 0, 3 do
                    local flags = tvb(offset, 4):le_uint()
                    local content_id = tvb(offset+4, 2):le_uint()
                    local content_hp = tvb(offset+6, 2):le_uint()
                    chunk_tree:add_le(dod_proto, tvb(offset,8), string.format("Cell (%d,%d) flags=0x%08x, content=%d, hp=%d",
                        i*4+k, j*4+l, flags, content_id, content_hp))
                    offset = offset + 8
                end
            end
            chunk_count = chunk_count + 1
        end
        chunk_count_label:append_text(chunk_count)
    end,
    [28] = function(tvb, tree) -- SMessageServerWorld_Water
        local offset = 0
        local clock = read_float_from_ushort(tvb, offset, 1)
        tree:add_le(f.clock, tvb(offset,2), clock)
        offset = offset + 2

        local cloud_pos = read_float_from_ushort(tvb, offset, 1)
        tree:add_le(f.cloud, tvb(offset,2), cloud_pos)
        offset = offset + 2

        tree:add_le(f.nights, tvb(offset,4))
        offset = offset + 4

        tree:add_le(f.ach_locked, tvb(offset,1))
        offset = offset + 1

        tree:add_le(f.event_id, tvb(offset,4))
        offset = offset + 4

        tree:add_le(f.event_delta, tvb(offset,4))
        offset = offset + 4

        local rect_x = tvb(offset, 2):le_uint()
        local rect_y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Rect: (" .. rect_x .. ", " .. rect_y .. ")")
        offset = offset + 4

        local rect_w = tvb(offset, 1):le_uint()
        local rect_h = tvb(offset+1, 1):le_uint()
        tree:add_le(dod_proto, tvb(offset,2), "Size: " .. rect_w .. "x" .. rect_h)
        offset = offset + 2

        local water_bitmap_tvb = tvb(offset, math.ceil((rect_w * rect_h) / 8))
        local water_bitmap_subtree = tree:add(dod_proto, water_bitmap_tvb, "Water Presence Bitmap")
        offset = offset + water_bitmap_tvb:len()

        local water_subtree = tree:add(dod_proto, tvb(offset), "Water Data")

        local water_entry_count = 0
        for i = 0, rect_w - 1 do
            local start_byte = math.floor((i * rect_h) / 8)
            local end_byte = math.floor(((i + 1) * rect_h - 1) / 8)
            local bitmap_row_tvb = water_bitmap_tvb(start_byte, end_byte - start_byte + 1)
            local bitmap_row = water_bitmap_subtree:add(dod_proto, bitmap_row_tvb, string.format("Row %03d: ", i))
            for j = 0, rect_h - 1 do
                local bit = get_bit(water_bitmap_tvb:raw(), i * rect_h + j)
                bitmap_row:append_text(bit)
                if bit == 1 then
                    local water_raw = tvb(offset, 2):le_uint()
                    
                    local is_lava = (water_raw & 0x8000) ~= 0
                    local water_value = (water_raw & 0x3FFF) * ((water_raw & 0x4000) == 0 and 1 or 256) / 16384.0
                    
                    local world_x = rect_x + i
                    local world_y = rect_y + j
                    water_entry_count = water_entry_count + 1
                    water_subtree:add(dod_proto, tvb(offset, 2), "("..world_x..", "..world_y.."): "..water_value..(is_lava and " (lava)" or ""))
                    offset = offset + 2
                end
            end
        end
        water_subtree:set_len(water_entry_count * 2)
        water_subtree:append_text(" ("..water_entry_count.." entries)")

        local elec_len = tvb(offset, 4):le_int()
        offset = offset + 4
        local elec_subtree = tree:add(dod_proto, tvb(offset, elec_len * 4), "Electrical Data")

        for i = 1, elec_len do
            local index = tvb(offset, 2):le_uint()
            local cons = tvb(offset + 2, 1):le_uint()
            local prod = tvb(offset + 3, 1):le_uint()
            
            local world_x = rect_x + math.floor(index / rect_h)
            local world_y = rect_y + index % rect_h
            elec_subtree:add(dod_proto, tvb(offset, 4), "("..world_x..", "..world_y.."): cons="..cons..", prod="..prod)
            offset = offset + 4
        end
        elec_subtree:append_text(" ("..elec_len.." entries)")

        local burning_len = tvb(offset, 4):le_int()
        offset = offset + 4
        local burning_subtree = tree:add(dod_proto, tvb(offset, burning_len * 2), "Burning Cells")
        for i = 1, burning_len do
            local index = tvb(offset, 2):le_uint()
            
            local world_x = rect_x + math.floor(index / rect_h)
            local world_y = rect_y + index % rect_h
            burning_subtree:add(dod_proto, tvb(offset, 2), "("..world_x..", "..world_y.."): burning")
            offset = offset + 2
        end
        burning_subtree:append_text(" (" .. burning_len .. " entries)")

        if offset < tvb:len() then
            tree:add_le(f.eruption_delta, tvb(offset, 4))
        end
    end,
    [29] = function(tvb, tree) -- SMessagePickups
        local offset = 0
        local pickup_count = 0
        while offset < tvb:len() do
            local id = tvb(offset, 2):le_uint()
            local removed = id >= 32767
            local actual_id = removed and (id - 32767) or id
            tree:add_le(dod_proto, tvb(offset,2), string.format("Pickup ID: %d %s", actual_id, removed and "(removed)" or ""))
            offset = offset + 2

            if not removed then
                tree:add_le(f.item_id, tvb(offset,2))
                offset = offset + 2

                local x, y = read_vector2_from_ushort2(tvb, offset, 1024)
                tree:add_le(dod_proto, tvb(offset,4), "Position: (" .. x .. ", " .. y .. ")")
                offset = offset + 4

                local vx, vy = read_vector2_from_short2(tvb, offset, 128)
                tree:add_le(dod_proto, tvb(offset,4), "Speed: (" .. vx .. ", " .. vy .. ")")
                offset = offset + 4

                local special = tvb(offset, 2):le_int()
                tree:add_le(dod_proto, tvb(offset,2), "Special (thrower/moveTo): " .. special)
                offset = offset + 2
            end
            pickup_count = pickup_count + 1
        end
        tree:add_le(dod_proto, tvb(), pickup_count.." pickups"):set_generated()
    end,
    [30] = function(tvb, tree) -- SMessageSpawnUnit
        local offset = 0
        local unit_desc_id = tvb(offset, 1):le_uint()
        tree:add_le(f.unit_desc_id, tvb(offset,1))
        offset = offset + 1

        local x, y = read_vector2_from_ushort2(tvb, offset, 1024)
        tree:add_le(dod_proto, tvb(offset,4), "Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        tree:add_le(f.unit_instance_id, tvb(offset,2))
        offset = offset + 2

        if unit_desc_id == 1 then -- GUnits.player.m_id
            tree:add_le(f.steam_id, tvb(offset,8))
            offset = offset + 8
        elseif unit_desc_id == 3 then -- GUnits.defense.m_id
            tree:add_le(f.item_id, tvb(offset,2))
        end
    end,
    [31] = function(tvb, tree) -- SMessageRemoveUnit
        tree:add_le(f.unit_instance_id, tvb(0,2))
    end,
    [32] = function(tvb, tree) -- SMessageRocketChangeStep
        local offset = 0
        local step = tvb(offset, 1):le_uint()
        tree:add_le(dod_proto, tvb(offset,1), "Rocket Step: " .. step)
        offset = offset + 1

        local postgame = tvb(offset, 1):le_uint() ~= 0
        tree:add_le(dod_proto, tvb(offset,1), "Postgame: " .. tostring(postgame))
    end,
    [33] = function(tvb, tree) -- SMessageOneBlock
        local offset = 0
        local x = tvb(offset, 2):le_uint()
        local y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Cell Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        local flags = tvb(offset, 4):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Flags: 0x" .. string.format("%08x", flags))
        offset = offset + 4

        tree:add_le(f.item_id, tvb(offset,2))
        offset = offset + 2

        local content_hp = tvb(offset, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,2), "Content HP: " .. content_hp)
    end,
    [34] = function(tvb, tree) -- SMessageFireMeteor
        local offset = 0
        local x, y = read_vector2_from_ushort2(tvb, offset, 1024)
        tree:add_le(dod_proto, tvb(offset,4), "Start Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        local dx, dy = read_vector2_from_short2(tvb, offset, 1300)
        tree:add_le(dod_proto, tvb(offset,4), "Destination: (" .. dx .. ", " .. dy .. ")")
    end,
    [35] = function(tvb, tree) -- SMessageChat
        local inner_len = tvb(0, 4):le_int()
        tree:add_le(dod_proto, tvb(0, 4), "Inner Buffer Len: "..inner_len)
        tree:add_le(dod_proto, tvb(4, 22), "NRBF Metadata Bytes")
        
        local text_len, offset = read_7_bit_encoded_int(tvb, 4 + 22)
        tree:add_le(dod_proto, tvb(4 + 22, offset - 4 - 22), "Text Length: "..text_len)
        local text = tvb(offset, text_len):string()
        tree:add_le(f.chat_message, tvb(offset, text_len))

        tree:add_le(dod_proto, tvb(offset + text_len, 1), "NRBR Metadata Bytes")
    end,
    [100] = function(tvb, tree) -- SMessageRequestCreative_SetItem
        local offset = 0
        local x = tvb(offset, 2):le_uint()
        local y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Cell Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        tree:add_le(f.item_id, tvb(offset,2))
    end,
    [101] = function(tvb, tree) -- SMessageRequestCreative_SetBg
        local offset = 0
        local x = tvb(offset, 2):le_uint()
        local y = tvb(offset+2, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,4), "Cell Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        tree:add_le(f.background_id, tvb(offset,1))
    end,
    [102] = function(tvb, tree) -- SMessageRequestCreative_SpawnMonster
        local offset = 0
        local x, y = read_vector2_from_ushort2(tvb, offset, 1024)
        tree:add_le(dod_proto, tvb(offset,4), "Position: (" .. x .. ", " .. y .. ")")
        offset = offset + 4

        tree:add_le(f.unit_desc_id, tvb(offset,1))
        offset = offset + 1

        tree:add_le(f.monster_night_spawn, tvb(offset,1))
    end,
    [253] = function(tvb, tree) -- SMessagePing2
        local offset = 0
        local build = tvb(offset, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,2), "Build Version: " .. build)
        offset = offset + 2

        local ping_sent = tvb(offset, 4):le_float()
        tree:add_le(dod_proto, tvb(offset,4), "Ping Sent Time: " .. ping_sent)
    end,
    [254] = function(tvb, tree) -- SMessagePong2
        local offset = 0
        local build = tvb(offset, 2):le_uint()
        tree:add_le(dod_proto, tvb(offset,2), "Build Version: " .. build)
        offset = offset + 2

        local ping_sent = tvb(offset, 4):le_float()
        tree:add_le(dod_proto, tvb(offset,4), "Ping Sent Time: " .. ping_sent)
    end
}

function dod_proto.dissector(buffer, pinfo, tree)
    local length = buffer:len()
    if length == 0 then
        return
    end
    if buffer:reported_len() ~= length then
        return 0
    end

    pinfo.cols.protocol = dod_proto.name
    local subtree = tree:add(dod_proto, buffer(), "Dig or Die Protocol Data")
    local offset = 0
    local found_messages_in_packet = {}

    while offset < length do
        if offset + 4 > length then
            pinfo.desegment_offset = offset
            pinfo.desegment_len = DESEGMENT_ONE_MORE_SEGMENT
            return length
        end
        local total_len = buffer(offset, 4):le_uint()
        local total_msg_len = 4 + total_len
        if offset + total_msg_len > length then
            pinfo.desegment_offset = offset
            pinfo.desegment_len = total_msg_len - (length - offset)
            return length
        end

        local msg_tvb = buffer(offset, total_msg_len)

        local msg_id = msg_tvb(4, 1):le_uint()
        local msg_info = MESSAGE_TABLE[msg_id] or {"UNKNOWN", 0}
        local msg_name = msg_info[1]
        local fixed_msg_size = msg_info[2]

        local header_size = 4 + 1
        local body_size = fixed_msg_size

        if fixed_msg_size == -1 then
            body_size = msg_tvb(5, 4):le_uint()
            header_size = 4 + 1 + 4
        end

        if header_size + body_size > total_msg_len then
            return
        end
        local msg_tree = subtree:add(dod_proto, msg_tvb, msg_name .. " (" .. msg_id .. ")")

        local header_subtree = msg_tree:add(dod_proto, msg_tvb(0, header_size), "Header")
        header_subtree:add_le(f.total_length, msg_tvb(0, 4))
        header_subtree:add_le(f.message_id, msg_tvb(4, 1))
        if fixed_msg_size < 0 then
            header_subtree:add_le(f.message_size, msg_tvb(5, 4))
        else
            header_subtree:add_le(f.message_size, fixed_msg_size):set_generated()
        end
        local payload_tvb = msg_tvb(header_size, body_size)
        local payload_subtree = msg_tree:add_le(dod_proto, payload_tvb, "Payload")

        local dissector = payload_dissectors[msg_id]
        if dissector ~= nil then
            dissector(payload_tvb, payload_subtree)
        else
            payload_subtree:add_le(dod_proto, payload_tvb, "Unknown message type")
        end
        found_messages_in_packet[#found_messages_in_packet+1] = msg_name.."("..msg_id..")"
        
        offset = offset + total_msg_len
    end
    if #found_messages_in_packet > 0 then
        local info_str = #found_messages_in_packet == 1 and ("DODMsg="..found_messages_in_packet[1]) or ("DODMsg=["..table.concat(found_messages_in_packet, ", ").."]")
        pinfo.cols.info:append(" " .. info_str)
    end
    return length
end

dod_proto.prefs.ports = Pref.range("TCP port(s)", "3776", "TCP ports", 65535)

local tcp_port = DissectorTable.get("tcp.port")
tcp_port:add(dod_proto.prefs.ports, dod_proto)

function dod_proto.prefs_changed()
    tcp_port:remove_all(dod_proto)
    tcp_port:add(dod_proto.prefs.ports, dod_proto)
end

