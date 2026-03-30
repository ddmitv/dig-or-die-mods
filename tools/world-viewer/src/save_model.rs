
use std::num::NonZeroU16;

use crate::misc::{cs_binary::{self, CSBinReader}, cs_binfmt::BinFmtReadable};
use anyhow::{Context, Result, bail, ensure};

#[derive(Default)]
pub struct SaveModel {
    pub header: Header,
    pub params: Params,
    pub foreign_item_id_to_native: Vec<u16>,
    pub pickups: Vec<Pickup>,
    pub players: Vec<Player>,
    pub last_events: Vec<String>,
    pub cell_grid: CellGrid,
    pub units: Vec<Unit>,
    pub species_killed: Vec<SpeciesKillsInfo>,
    pub vars: Vars,
}

#[derive(Default, Clone, Copy, PartialEq)]
pub struct Int2 {
    pub x: i32,
    pub y: i32,
}

#[derive(Default, Clone, Copy, PartialEq)]
pub struct Vector2 {
    pub x: f32,
    pub y: f32,
}

pub struct ItemVar {
    pub time_last_use: f32,
    pub time_activation: f32,
    pub dico: Vec<KeyValue>,
}

pub struct KeyValue {
    pub key: String,
    pub value: f32,
}

pub struct Pickup {
    pub id: u16,
    pub pos: Vector2,
    pub creation_time: f32,
}

pub struct Unit {
    pub code_name: String,
    pub pos: Vector2,
    pub instance_id: u16,
    pub desc_id: Option<NonZeroU16>,
    pub hp: f32,
    pub air: f32,
    pub monster_data: Option<UnitMonsterData>,
}

pub struct UnitMonsterData {
    pub is_night_spawn: bool,
    pub target_id: u16,
    pub is_creative_spawn: bool,
}

#[derive(Default)]
pub struct Header {
    pub version: f32,
    pub build: i32,
    pub mode_name: String,
}

pub struct SpeciesKillsInfo {
    pub code_name: String,
    pub nb: i32,
    pub last_kill_time: f32,
}

pub struct Inventory {
    pub items: Vec<Item>,
    pub bar_items: Vec<u16>,
    pub item_selected: u16,
}

pub struct Item {
    pub id: u16,
    pub nb: i32,
}

#[derive(Default, Clone, Copy)]
pub struct Color24 {
    pub r: u8,
    pub g: u8,
    pub b: u8,
}

pub struct Player {
    pub steam_id: u64,
    pub name: String,
    pub pos: Vector2,
    pub unit_player_id: u16,
    pub skin_is_female: bool,
    pub skin_color_skin: f32,
    pub skin_hair_style: i32,
    pub skin_color_hair: Color24,
    pub skin_color_eyes: Color24,
    pub inventory: Inventory,
    pub item_vars: Vec<ItemVar>,
}

macro_rules! define_binfmt_struct {
    (struct $struct:ident { $($field:ident : $type:ty = $default:expr),* $(,)? }) => {
        #[allow(non_snake_case)]
        pub struct $struct {
            $(pub $field: $type,)*
        }
        impl Default for $struct {
            fn default() -> Self {
                return Self { $($field: $default,)* };
            }
        }
        impl $struct {
            fn deserialize(reader: &mut cs_binary::CSBinReader) -> Result<Self> {
                let mut result = Self::default();
                for i in 0..10_000 {
                    let field_name = reader.read_string().with_context(|| format!("Reading BinFmt field name #{i}"))?;
                    if field_name.len() == 0 { break; }

                    match field_name {
                        $(stringify!($field) => result.$field = <$type as BinFmtReadable>::binfmt_read_from(reader)?,)*
                        _ => bail!("Unknown field name: \"{field_name}\" for struct \"{}\" (index {})", stringify!($struct), i),
                    };
                }
                return Ok(result);
            }
        }
    };
}

define_binfmt_struct! {
struct Params {
    m_mod: String = "Solo".to_string(),
    m_id: String = "params".to_string(),
    m_idNum: i32 = 0,

    m_difficulty: i32 = 0,
    m_startCheated: bool = false,
    m_eventsActive: bool = false,
    m_spawnPos: Int2 = Int2 { x: 0, y: 0 },
    m_shipPos: Int2 = Int2 { x: 0, y: 0 },
    m_gridSize: Int2 = Int2 { x: 1024, y: 1024 },
    m_seed: i32 = 0,
    m_gameName: String = String::new(),
    m_visibility: i32 = 0,
    m_passwordMD5: String = String::new(),
    m_hostId: u64 = 0,
    m_hostName: String = String::new(),
    m_gameOverIfAllDead: bool = true,
    m_nbPlayersMax: i32 = 4,
    m_clientGetHostItems: bool = true,
    m_banGiveLootToHost: bool = true,
    m_devMode: bool = false,
    m_checkMinerals: bool = true,
    m_dynamicSpawn: bool = false,
    m_cloudCycleDistance: f32 = 1624.0,
    m_cloudCycleDuration: f32 = 200.0,
    m_cloudRadius: f32 = 100.0,
    m_rainQuantity: f32 = 0.012,
    m_generationOreDiv: f32 = 1.0,
    m_weightMult: f32 = 1.0,
    m_dropChanceMult: f32 = 1.0,
    m_lavaPressureBottomCycle: f32 = 2.0,
    m_lavaPressureTopCycle: f32 = 40.0,
    m_eruptionDurationTotal: f32 = 800.0,
    m_eruptionDurationAcc: f32 = 180.0,
    m_eruptionDurationUp: f32 = 500.0,
    m_eruptionPressure: f32 = 125.0,
    m_eruptionCheckMinY: f32 = 780.0,
    m_dayDurationTotal: f32 = 720.0,
    m_nightDuration: f32 = 108.0,
    m_gravityPlayers: f32 = 1.0,
    m_eventsDelayMin: f32 = 700.0,
    m_eventsDelayMax: f32 = 1000.0,
    m_rocketPreparationDuration: i32 = 300,
    m_speedSimu: f32 = 1.0,
    m_speedSimuWorld: f32 = 1.0,
    m_speedSimuWorldLocked: bool = false,
    m_rainY: i32 = 880,
    m_fastEvaporationYMax: i32 = 280,
    m_sunLightYMin: i32 = 600,
    m_sunLightYMax: i32 = 665,
    m_respawnDelay: i32 = -1,
    m_dropAtDeathPercent_Peaceful: f32 = 0.0,
    m_dropAtDeathPercent_Easy: f32 = 0.0,
    m_dropAtDeathPercent_Normal: f32 = 0.05,
    m_dropAtDeathPercent_Hard: f32 = 0.1,
    m_dropAtDeathPercent_Brutal: f32 = 0.15,
    m_dropAtDeathMax: i32 = 20,
    m_monstersDayNb: i32 = 7,
    m_monstersDayNbAddPerPlayer: i32 = 2,
    m_bossRespawnDelay: f32 = -1.0,
    m_monstersNightSpawnRateMult: f32 = 1.0,
    m_monstersNightSpawnRateAddPerPlayer: f32 = 0.35,
    m_monstersHpMult: f32 = 1.0,
    m_monstersHpAddPerPlayer: f32 = 0.2,
    m_monstersDamagesMult: f32 = 1.0,
    m_monstersDamagesAddPerPlayer: f32 = 0.1,
}
}

#[repr(i32)]
pub enum RocketStep {
    Inactive = 0,
    Count0_50 = 1,
    Count50Wait = 2,
    Count50to100 = 3,
    Liftoff = 4,
}

define_binfmt_struct! {
struct Vars {
    m_lastSaveDate: String = String::new(),
    m_simuTimeD: f64 = 0.0,
    m_worldTimeD: f64 = 0.0,
    m_clock: f32 = 0.0,
    m_cloudPosRatio: f32 = 0.0,
    m_droneTargetId: u16 = 0,
    m_achievementsLocked: bool = false,
    m_eventIdNum: i32 = -1,
    m_eventStartTime: f32 = 0.0,
    m_lavaCycleSkipped: bool = false,
    m_bossAreas: String = String::new(),
    m_monsterT2AlreadyHit: bool = false,
    m_eruptionTime: f32 = 0.0,
    m_eruptionStartPressure: f32 = 0.0,
    m_brokenHeart: bool = false,
    m_heartPos: Int2 = Int2 { x: -1, y: -1 },
    m_cinematicIntroTime: f32 = f32::MIN,
    m_cinematicRocketPos: Vector2 = Vector2 { x: 0.0, y: 0.0 },
    m_cinematicRocketStep: RocketStep = RocketStep::Inactive,
    m_cinematicRocketStepStartTime: f32 = f32::MIN,
    m_postGame: bool = false,
    m_autoBuilderLastTimeFound: f32 = 0.0,
    m_achievNoElectricity: bool = false,
    m_achievNoShoot: bool = false,
    m_achievNoCraft: bool = false,
    m_achievNoMK2: bool = false,
    m_achievWentToSea: bool = false,
    m_achievEarlyDive: bool = false,
    m_aiSentencesTold: Vec<String> = Vec::new(),
    m_autoBuilderLevelBuilt: i32 = 0,
    m_autoBuilderLevelUsed: i32 = -1,
    m_nbNightsSurvived: i32 = 0,
    m_bossKilled_Madcrab: bool = false,
    m_bossKilled_FireflyQueen: bool = false,
    m_bossKilled_DwellerLord: bool = false,
    m_bossKilled_Balrog: bool = false,
    m_droneLastTimeEnters: f32 = f32::MIN,
    m_droneLastTimeDontEnter: f32 = f32::MIN,
    m_droneComboNb: i32 = 1,
}
}

pub struct Cell {
    pub flags: u32,
    pub content_id: u16,
    pub content_hp: u16,
    pub water: f32,
    pub force_x: i16,
    pub force_y: i16,
    pub light: Color24,
}

#[derive(Default)]
pub struct CellGrid {
    grid: Box<[Cell]>,
    width: usize,
    height: usize,
}

pub struct BgSurface {
    pub id: u8,
    pub color: egui::Color32,
    pub name: &'static str,
}

impl CellGrid {
    pub fn new(grid: Box<[Cell]>, width: usize, height: usize) -> Result<Self> {
        ensure!(grid.len() == width.checked_mul(height).context("Width and height multiplication overflowed")?, "Invalid grid array length");
        return Ok(Self { grid, width, height });
    }
    pub fn dimensions(&self) -> (usize, usize) {
        return (self.width, self.height);
    }
    pub fn width(&self) -> usize {
        return self.width;
    }
    pub fn height(&self) -> usize {
        return self.height;
    }
    pub fn flatten_at(&self, index: usize) -> &Cell {
        return &self.grid[index];
    }
    pub fn get(&self, x: usize, y: usize) -> Option<&Cell> {
        if x >= self.width || y >= self.height {
            return None;
        }
        return Some(&self.grid[y + x * self.height]);
    }
    pub fn get_mut(&mut self, x: usize, y: usize) -> Option<&mut Cell> {
        if x >= self.width || y >= self.height {
            return None;
        }
        return Some(&mut self.grid[y + x * self.height]);
    }
    pub fn iter(&self) -> CellGridIter<'_> {
        return CellGridIter { grid_iter: self.grid.iter(), width: self.width, cur_x: 0, cur_y: 0 };
    }
    pub fn is_valid_pos(&self, (x, y): (usize, usize)) -> bool {
        return x < self.width && y < self.height;
    }
}

pub struct CellGridIter<'a> {
    grid_iter: std::slice::Iter<'a, Cell>,
    width: usize,
    cur_x: usize,
    cur_y: usize,
}

impl<'a> Iterator for CellGridIter<'a> {
    type Item = (usize, usize, &'a Cell);

    fn next(&mut self) -> Option<Self::Item> {
        let cell = self.grid_iter.next()?;
        let x = self.cur_x;
        let y = self.cur_y;

        self.cur_y += 1;
        if self.cur_y >= self.width {
            self.cur_y = 0;
            self.cur_x += 1;
        }

        Some((x, y, cell))
    }
}


impl std::fmt::Display for Int2 {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        return write!(f, "({}, {})", self.x, self.y);
    }
}
impl std::fmt::Display for Vector2 {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        if let Some(precision) = f.precision() {
            return write!(f, "({1:.0$}, {2:.0$})", precision, self.x, self.y);
        } else {
            return write!(f, "({}, {})", self.x, self.y);
        }
    }
}

impl Cell {
    pub const FLAG_CUSTOM_DATA_0: u32 = 1;
    pub const FLAG_CUSTOM_DATA_1: u32 = 2;
    pub const FLAG_CUSTOM_DATA_2: u32 = 4;
    pub const FLAG_IS_X_REVERSED: u32 = 16;
    pub const FLAG_IS_BURNING: u32 = 32;
    pub const FLAG_IS_MAPPED: u32 = 64;
    pub const FLAG_BACK_WALL_0: u32 = 256;
    pub const FLAG_BG_SURFACE_0: u32 = 512;
    pub const FLAG_BG_SURFACE_1: u32 = 1024;
    pub const FLAG_BG_SURFACE_2: u32 = 2048;
    pub const FLAG_WATER_FALL: u32 = 4096;
    pub const FLAG_STREAM_L_FAST: u32 = 8192;
    pub const FLAG_STREAM_R_FAST: u32 = 16384;
    pub const FLAG_IS_LAVA: u32 = 32768;
    pub const FLAG_HAS_WIRE_RIGHT: u32 = 65536;
    pub const FLAG_HAS_WIRE_TOP: u32 = 131072;
    pub const FLAG_ELECTRIC_ALGO_STATE: u32 = 262144;
    pub const FLAG_IS_POWERED: u32 = 524288;

    pub fn has_backwall(&self) -> bool {
        return self.flags & Self::FLAG_BACK_WALL_0 != 0;
    }
    pub fn is_mineral_dirt(&self) -> bool {
        return matches!(self.content_id, 119..=123);
    }
    pub fn is_lava(&self) -> bool {
        return self.flags & Self::FLAG_IS_LAVA != 0;
    }
    pub fn bg_surface(&self) -> Option<&BgSurface> {
        use egui::Color32;
        const BG_SURFACES: [BgSurface; 6] = [
            BgSurface { id: 1, color: Color32::from_rgb(91, 75, 59), name: "Dirt" },
            BgSurface { id: 2, color: Color32::from_rgb(56, 59, 70), name: "Rock" },
            BgSurface { id: 3, color: Color32::from_rgb(42, 42, 48), name: "Granit" },
            BgSurface { id: 4, color: Color32::from_rgb(83, 128, 143), name: "Crystal" },
            BgSurface { id: 5, color: Color32::from_rgb(57, 57, 57), name: "Lava" },
            BgSurface { id: 6, color: Color32::from_rgb(51, 15, 14), name: "Organic" },
        ];

        let mut id = 0;
        if self.flags & Self::FLAG_BG_SURFACE_0 != 0 { id += 1 }
        if self.flags & Self::FLAG_BG_SURFACE_1 != 0 { id += 2 }
        if self.flags & Self::FLAG_BG_SURFACE_2 != 0 { id += 4 }

        match id {
            0 => return None,
            1..=6 => (),
            _ => panic!("Invalid background surface ID"),
        };
        return Some(&BG_SURFACES[id - 1]);
    }

    pub fn get_cell_color(&self) -> egui::Color32 {
        let rgb = egui::Color32::from_rgb;

        if self.is_mineral_dirt() && self.flags & Self::FLAG_CUSTOM_DATA_0 != 0 {
            return rgb(255, 255, 110);
        }
        return get_content_color(self.content_id);
    }
}

pub fn get_content_color(content_id: u16) -> egui::Color32 {
    let rgb = egui::Color32::from_rgb;
    return match content_id {
        0 => egui::Color32::TRANSPARENT,
        58..=70 => rgb(135, 135, 135),
        71 => rgb(186, 104, 60),
        72 => rgb(186, 104, 60),
        73 => rgb(192, 189, 181),
        74 => rgb(176, 186, 193),
        75 => rgb(125, 123, 116),
        76 => rgb(171, 168, 162),
        77 => rgb(191, 200, 202),
        78 => rgb(191, 200, 202),
        79 => rgb(208, 132, 23),
        80 => rgb(208, 132, 23),
        81 => rgb(182, 181, 191),
        82 => rgb(191, 200, 202),
        83 => rgb(184, 190, 194),
        84 => rgb(191, 200, 202),
        85 => rgb(182, 181, 191),
        86..=118 => rgb(152, 152, 152),
        119 => rgb(143, 111, 79),
        120 => rgb(182, 94, 64),
        121 => rgb(180, 150, 74),
        122 => rgb(53, 50, 47),
        123 => rgb(80, 135, 122),
        124 => rgb(110, 113, 124),
        125 => rgb(204, 171, 151),
        126 => rgb(48, 58, 69),
        127 => rgb(233, 138, 80),
        128 => rgb(246, 223, 149),
        129 => rgb(221, 221, 221),
        130 => rgb(155, 186, 196),
        131 => rgb(207, 244, 242),
        132 => rgb(144, 223, 242),
        133 => rgb(204, 245, 250),
        134 => rgb(87, 87, 87),
        135 => rgb(99, 98, 109),
        136 => rgb(180, 241, 225),
        137 => rgb(229, 229, 229),
        138 => rgb(221, 221, 221),
        139 => rgb(227, 73, 250),
        140 => rgb(254, 247, 180),
        141 => rgb(65, 62, 202),
        142 => rgb(112, 32, 30),
        143 => rgb(163, 40, 59),
        144 => rgb(77, 76, 76),
        145 => rgb(92, 91, 91),
        146 => rgb(230, 140, 140),
        147 => rgb(254, 254, 254),
        148 => rgb(254, 254, 254),
        149 => rgb(186, 104, 55),
        150 => rgb(186, 104, 55),
        151 => rgb(101, 218, 190),
        152 => rgb(101, 218, 190),
        153 => rgb(124, 124, 124),
        154 => rgb(159, 92, 42),
        155 => rgb(159, 92, 42),
        156 => rgb(229, 81, 45),
        157 => rgb(89, 208, 175),
        158 => rgb(40, 113, 158),
        159 => rgb(241, 197, 22),
        160 => rgb(92, 166, 254),
        161 => rgb(215, 215, 215),
        162 => rgb(249, 67, 67),
        163 => rgb(226, 43, 19),
        164 => rgb(254, 195, 101),
        165 => rgb(241, 103, 72),
        166 => rgb(69, 60, 55),
        167 => rgb(52, 41, 35),
        168 => rgb(30, 95, 140),
        169 => rgb(241, 77, 28),
        170 => rgb(203, 65, 3),
        171 => rgb(25, 86, 131),
        172 => rgb(132, 100, 79),
        173..=201 => rgb(254, 254, 254),
        _ => egui::Color32::PLACEHOLDER,
    };
}

pub fn get_content_name(content_id: u16) -> (&'static str, &'static str) {
    return match content_id {
        0 => ("", "Null"),
        1 => ("miniaturizorMK1", "Miniaturizor"),
        2 => ("miniaturizorMK2", "Miniaturizor MK II"),
        3 => ("miniaturizorMK3", "Miniaturizor MK III"),
        4 => ("miniaturizorMK4", "Miniaturizor MK IV"),
        5 => ("miniaturizorMK5", "Miniaturizor MK V"),
        6 => ("miniaturizorUltimate", "Ultimate Miniaturizor"),
        7 => ("potionHp", "Health Potion"),
        8 => ("potionHpRegen", "Health Regeneration Potion"),
        9 => ("potionHpBig", "Large Health Potion"),
        10 => ("potionHpMega", "Mega Health Potion"),
        11 => ("potionArmor", "Armor Potion"),
        12 => ("potionPheromones", "Pheromones Potion"),
        13 => ("potionCritics", "Precision Potion"),
        14 => ("potionInvisibility", "Invisibility Potion"),
        15 => ("potionSpeed", "Speed Potion"),
        16 => ("armorMk1", "Dweller Armor"),
        17 => ("armorMk2", "Black Dweller Armor"),
        18 => ("armorMk3", "Ant Armor"),
        19 => ("armorUltimate", "Ultimate Armor"),
        20 => ("defenseShield", "Defense Shield"),
        21 => ("drone", "Drone"),
        22 => ("droneCombat", "Combat Drone"),
        23 => ("droneWar", "War Drone"),
        24 => ("flashLight", "Flashlight"),
        25 => ("minimapper", "Minimapper"),
        26 => ("effeilGlasses", "Eiffel Glasses"),
        27 => ("metalDetector", "Rare Metals Detector"),
        28 => ("waterDetector", "Water Detector"),
        29 => ("flashLightMK2", "Advanced Flashlight"),
        30 => ("waterBreather", "Rebreather"),
        31 => ("jetpack", "Jetpack"),
        32 => ("invisibilityDevice", "Stealth Bracelet"),
        33 => ("ultimateJetpack", "Ultimate Jetpack"),
        34 => ("ultimateBrush", "Ultimate Dimensional Brush"),
        35 => ("ultimateRebreather", "Ultimate Rebreather"),
        36 => ("gunRifle", "Plasma Rifle"),
        37 => ("gunShotgun", "Plasma Shotgun"),
        38 => ("gunMachineGun", "Plasma Machine Gun"),
        39 => ("gunSnipe", "Plasma Sniper Rifle"),
        40 => ("gunLaser", "Laser Gun"),
        41 => ("gunRocket", "Rocket Launcher"),
        42 => ("gunZF0", "ZF-0"),
        43 => ("gunMegaSnipe", "Overcharged Plasma Gun"),
        44 => ("gunLaserGatling", "Gatling Laser"),
        45 => ("gunStorm", "Storm Gun"),
        46 => ("gunGrenadeLaunch", "Grenade Launcher"),
        47 => ("gunParticlesShotgun", "Particle Shotgun"),
        48 => ("gunParticlesSniper", "Particle Sniper Rifle"),
        49 => ("gunFlamethrower", "Flame Thrower"),
        50 => ("gunLightSword", "-"),
        51 => ("gunUltimateParticlesGatling", "Ultimate Particle Gatling"),
        52 => ("gunUltimateGrenadeLauncher", "Ultimate Grenade Launcher"),
        53 => ("ultimateWaterPistol", "Ultimate Water Pistol"),
        54 => ("ultimateLavaPistol", "Ultimate Lava Pistol"),
        55 => ("ultimateSpongePistol", "Ultimate Sponge Pistol"),
        56 => ("ultimateTotoroGun", "Ultimate Plant Gun"),
        57 => ("ultimateMonstersGun", "Ultimate Monster Gun"),
        58 => ("turret360", "Rotating Turret"),
        59 => ("turretGatling", "Gatling Turret"),
        60 => ("turretReparator", "Auto-Repair Turret"),
        61 => ("turretHeavy", "Heavy Turret"),
        62 => ("turretMine", "Lightning Mine"),
        63 => ("turretSpikes", "Electrified Spikes"),
        64 => ("turretReparatorMK2", "Advanced Auto-Repair Turret"),
        65 => ("turretCeiling", "Death Pulse Turret"),
        66 => ("turretLaser", "Laser Turret"),
        67 => ("turretTesla", "Tesla Turret"),
        68 => ("turretFlame", "Flamethrower Turret"),
        69 => ("turretParticles", "Particle Turret"),
        70 => ("explosive", "Explosive"),
        71 => ("wallWood", "Wooden Wall"),
        72 => ("platform", "Wooden Platform"),
        73 => ("wallConcrete", "Concrete Wall"),
        74 => ("wallIronSupport", "Iron Support"),
        75 => ("backwall", "Concrete Back Wall"),
        76 => ("wallReinforced", "Reinforced Concrete Wall"),
        77 => ("wallDoor", "Armored Door"),
        78 => ("platformSteel", "Steel Platform"),
        79 => ("generatorWater", "Hydroelectric Power Generator"),
        80 => ("waterPump", "Water Pump"),
        81 => ("wallComposite", "Composite Wall"),
        82 => ("wallCompositeSupport", "Composite Metal Support"),
        83 => ("wallCompositeLight", "Composite Light Wall"),
        84 => ("wallCompositeDoor", "Composite Door"),
        85 => ("wallUltimate", "Ultimate Wall"),
        86 => ("autoBuilderMK1", "Auto-Builder"),
        87 => ("autoBuilderMK2", "Auto-Builder MK II"),
        88 => ("light", "Lamp"),
        89 => ("autoBuilderMK3", "Auto-Builder MK III"),
        90 => ("lightSticky", "Wall Light"),
        91 => ("electricWire", "Electric Wire"),
        92 => ("generatorSun", "Solar Panel"),
        93 => ("lightSun", "Sun Light"),
        94 => ("teleport", "Teleporter"),
        95 => ("elecSwitch", "Toggle Switch"),
        96 => ("autoBuilderMK4", "Auto-Builder MK IV"),
        97 => ("autoBuilderMK5", "Auto-Builder MK V"),
        98 => ("elecSwitchPush", "Push Switch"),
        99 => ("elecSwitchRelay", "Relay Switch"),
        100 => ("elecCross", "Wire Crossing"),
        101 => ("elecSignal", "Signal Generator"),
        102 => ("elecClock", "Clock Signal Generator"),
        103 => ("elecToggle", "Flip-flop"),
        104 => ("elecDelay", "Delay Gate"),
        105 => ("elecWaterSensor", "Water Sensor"),
        106 => ("elecProximitySensor", "Proximity Sensor"),
        107 => ("elecDistanceSensor", "Distance Sensor"),
        108 => ("elecAND", "AND Gate"),
        109 => ("elecOR", "OR Gate"),
        110 => ("elecXOR", "XOR Gate"),
        111 => ("elecNOT", "NOT Gate"),
        112 => ("elecLight", "Light Signal"),
        113 => ("elecAlarm", "Alarm Signal"),
        114 => ("reactor", "Thorium Reactor"),
        115 => ("rocketTop", "Rocket Top"),
        116 => ("rocketTank", "Rocket Tank"),
        117 => ("rocketEngine", "Rocket Engine"),
        118 => ("autoBuilderUltimate", "Ultimate Auto-Builder"),
        119 => ("dirt", "Dirt"),
        120 => ("dirtRed", "Red Dirt"),
        121 => ("silt", "Silt"),
        122 => ("dirtBlack", "Black Dirt"),
        123 => ("dirtSky", "Cactus Dirt"),
        124 => ("rock", "Stone"),
        125 => ("iron", "Iron"),
        126 => ("coal", "Coal"),
        127 => ("copper", "Copper"),
        128 => ("gold", "Gold"),
        129 => ("aluminium", "Aluminum"),
        130 => ("rockFlying", "Lightweight Rock"),
        131 => ("rockGaz", "Gas Rock"),
        132 => ("crystal", "Crystals"),
        133 => ("crystalLight", "Light Crystals"),
        134 => ("crystalBlack", "Black Crystals"),
        135 => ("granit", "Granite Rock"),
        136 => ("uranium", "Uranium"),
        137 => ("titanium", "Titanium"),
        138 => ("lightonium", "Lightonium"),
        139 => ("thorium", "Thorium"),
        140 => ("sulfur", "Sulfur"),
        141 => ("sapphire", "Sapphire"),
        142 => ("organicRock", "Strange Rock"),
        143 => ("organicRockHeart", "Strange Rock Heart"),
        144 => ("lava", "Basalt"),
        145 => ("lavaOld", "Ancient Basalt"),
        146 => ("diamonds", "Diamond"),
        147 => ("organicRockDefenseLead", "-"),
        148 => ("organicRockDefense", "Strange Rock Defense"),
        149 => ("wood", "Log"),
        150 => ("woodwater", "Water Tree Log"),
        151 => ("woodSky", "Cactus Log"),
        152 => ("woodGranit", "Deep Caves Log"),
        153 => ("deadPlant", "Dead Plant"),
        154 => ("tree", "Tree Seed"),
        155 => ("treePine", "Tree Pine Seed"),
        156 => ("treeWater", "Sea Tree Seed"),
        157 => ("treeSky", "Giant Cactus Seed"),
        158 => ("treeGranit", "Deep Cave Tree Seed"),
        159 => ("bush", "Bush"),
        160 => ("flowerBlue", "Blue Flower"),
        161 => ("flowerWhite", "White Flower"),
        162 => ("fernRed", "Red Fern"),
        163 => ("waterBush", "Water Flower"),
        164 => ("waterLight", "Flower of Light"),
        165 => ("waterCoral", "Coral"),
        166 => ("blackGrass", "Black Grass"),
        167 => ("blackMushroom", "Black Mushroom"),
        168 => ("skyBush", "Small Cactus"),
        169 => ("lavaFlower", "Lava Bush"),
        170 => ("lavaPlant", "Lava Flower"),
        171 => ("bushGranit", "Deep Caves Bush"),
        172 => ("organicHair", "Hairy Plant"),
        173 => ("metalScrap", "Scrap Metal"),
        174 => ("lightGem", "Blue Energy Gem"),
        175 => ("energyGem", "Red Energy Gem"),
        176 => ("darkGem", "Dark Energy Gem"),
        177 => ("dogHorn", "Hound Horn"),
        178 => ("dogHorn3", "Black Hound Horn"),
        179 => ("moleShell", "Dweller Shell"),
        180 => ("moleShellBlack", "Black Dweller Shell"),
        181 => ("fish2Regen", "Piranha Regen Organ"),
        182 => ("fish3Regen", "Black Piranha Regen Organ"),
        183 => ("bat2Sonar", "Bat Sonar"),
        184 => ("bat3Sonar", "Precise Bat Sonar"),
        185 => ("antShell", "Ant Shell"),
        186 => ("sharkSkin", "Shark Fish Skin"),
        187 => ("unstableGemResidue", "Unstable Gem Residue"),
        188 => ("lootDwellerLord", "Dweller Lord Shell Spike"),
        189 => ("lootParticleGround", "Small Particle Emitor"),
        190 => ("lootParticleBirds", "Particle Emitor"),
        191 => ("lootLargeParticleBirds", "Powerful Particle Emitor"),
        192 => ("lootLavaSpider", "Lava Spider's Igniter"),
        193 => ("lootLavaBat", "Lava Bat's Igniter"),
        194 => ("lootMiniBalrog", "Hell Hound's Horns"),
        195 => ("bloodyFlesh1", "Radioactive Blood"),
        196 => ("bloodyFlesh2", "High Radioactive Blood"),
        197 => ("bloodyFlesh3", "Deadly Radioactive Blood"),
        198 => ("bossMadCrabSonar", "Mad Crab Sonar"),
        199 => ("bossMadCrabMaterial", "Mad Crab Material"),
        200 => ("masterGem", "Energy Master Gem"),
        201 => ("lootBalrog", "Demon's Skin"),
        _ => ("[unknown]", "[Unknown]")
    };
}

impl Color24 {
    pub fn to_color32(self) -> egui::Color32 {
        return egui::Color32::from_rgb(self.r, self.g, self.b);
    }
}
impl std::fmt::Display for Color24 {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        return write!(f, "({}, {}, {})", self.r, self.g, self.b);
    }
}
impl std::ops::Add<Vector2> for Vector2 {
    type Output = Vector2;
    fn add(self, rhs: Vector2) -> Self::Output {
        return Vector2 { x: self.x + rhs.x, y: self.y + rhs.y };
    }
}
impl std::ops::Sub<Vector2> for Vector2 {
    type Output = Vector2;
    fn sub(self, rhs: Vector2) -> Self::Output {
        return Vector2 { x: self.x - rhs.x, y: self.y - rhs.y };
    }
}
impl Vector2 {
    pub const UP: Vector2 = Vector2 { x: 0.0, y: 1.0 };
    pub const DOWN: Vector2 = Vector2 { x: 0.0, y: -1.0 };
    pub const LEFT: Vector2 = Vector2 { x: -1.0, y: 0.0 };
    pub const RIGHT: Vector2 = Vector2 { x: 1.0, y: 0.0 };
}

impl Default for ItemVar {
    fn default() -> Self {
        return Self { time_last_use: f32::MIN, time_activation: f32::MIN, dico: Vec::new() };
    }
}

trait CSBinReaderExt {
    fn check_magic_string(&mut self, expected: &'static str) -> Result<()>;
}
impl CSBinReaderExt for cs_binary::CSBinReader<'_> {
    fn check_magic_string(&mut self, expected: &'static str) -> Result<()> {
        let actual = self.read_string()?;
        if actual != expected {
            bail!("Magic string mismatch: expected \"{expected}\", got \"{actual}\"");
        }
        return Ok(());
    }
}

#[derive(PartialEq)]
pub enum UnitKind {
    Monster,
    Ally,
    Unknown
}

impl Unit {
    pub fn unit_kind(codename: &str) -> UnitKind {
        return match codename {
            "player" | "playerLocal" | "defense" | "drone" | "droneCombat" | "droneWar"
                => UnitKind::Ally,

            "hound" | "firefly" | "fireflyRed" | "dweller" | "fish" | "bat" | "houndBlack" | "fireflyBlack" |
            "dwellerBlack" | "fishBlack" | "batBlack" | "bossMadCrab" | "shark" | "fireflyExplosive" |
            "antClose" | "antDist" | "bossFirefly" | "bossDweller" | "lavaAnt" | "lavaBat" | "particleGround" |
            "particleBird" | "particleBird2" | "balrogMini" | "bossBalrog" | "bossBalrog2"
                => UnitKind::Monster,

            _ => UnitKind::Unknown
        };
    }
}

static ITEM_CODENAME_TO_ID: phf::Map<&'static str, u16> = phf::phf_map! {
    "miniaturizorMK1" => 1,
    "miniaturizorMK2" => 2,
    "miniaturizorMK3" => 3,
    "miniaturizorMK4" => 4,
    "miniaturizorMK5" => 5,
    "miniaturizorUltimate" => 6,
    "potionHp" => 7,
    "potionHpRegen" => 8,
    "potionHpBig" => 9,
    "potionHpMega" => 10,
    "potionArmor" => 11,
    "potionPheromones" => 12,
    "potionCritics" => 13,
    "potionInvisibility" => 14,
    "potionSpeed" => 15,
    "armorMk1" => 16,
    "armorMk2" => 17,
    "armorMk3" => 18,
    "armorUltimate" => 19,
    "defenseShield" => 20,
    "drone" => 21,
    "droneCombat" => 22,
    "droneWar" => 23,
    "flashLight" => 24,
    "minimapper" => 25,
    "effeilGlasses" => 26,
    "metalDetector" => 27,
    "waterDetector" => 28,
    "flashLightMK2" => 29,
    "waterBreather" => 30,
    "jetpack" => 31,
    "invisibilityDevice" => 32,
    "ultimateJetpack" => 33,
    "ultimateBrush" => 34,
    "ultimateRebreather" => 35,
    "gunRifle" => 36,
    "gunShotgun" => 37,
    "gunMachineGun" => 38,
    "gunSnipe" => 39,
    "gunLaser" => 40,
    "gunRocket" => 41,
    "gunZF0" => 42,
    "gunMegaSnipe" => 43,
    "gunLaserGatling" => 44,
    "gunStorm" => 45,
    "gunGrenadeLaunch" => 46,
    "gunParticlesShotgun" => 47,
    "gunParticlesSniper" => 48,
    "gunFlamethrower" => 49,
    "gunLightSword" => 50,
    "gunUltimateParticlesGatling" => 51,
    "gunUltimateGrenadeLauncher" => 52,
    "ultimateWaterPistol" => 53,
    "ultimateLavaPistol" => 54,
    "ultimateSpongePistol" => 55,
    "ultimateTotoroGun" => 56,
    "ultimateMonstersGun" => 57,
    "turret360" => 58,
    "turretGatling" => 59,
    "turretReparator" => 60,
    "turretHeavy" => 61,
    "turretMine" => 62,
    "turretSpikes" => 63,
    "turretReparatorMK2" => 64,
    "turretCeiling" => 65,
    "turretLaser" => 66,
    "turretTesla" => 67,
    "turretFlame" => 68,
    "turretParticles" => 69,
    "explosive" => 70,
    "wallWood" => 71,
    "platform" => 72,
    "wallConcrete" => 73,
    "wallIronSupport" => 74,
    "backwall" => 75,
    "wallReinforced" => 76,
    "wallDoor" => 77,
    "platformSteel" => 78,
    "generatorWater" => 79,
    "waterPump" => 80,
    "wallComposite" => 81,
    "wallCompositeSupport" => 82,
    "wallCompositeLight" => 83,
    "wallCompositeDoor" => 84,
    "wallUltimate" => 85,
    "autoBuilderMK1" => 86,
    "autoBuilderMK2" => 87,
    "light" => 88,
    "autoBuilderMK3" => 89,
    "lightSticky" => 90,
    "electricWire" => 91,
    "generatorSun" => 92,
    "lightSun" => 93,
    "teleport" => 94,
    "elecSwitch" => 95,
    "autoBuilderMK4" => 96,
    "autoBuilderMK5" => 97,
    "elecSwitchPush" => 98,
    "elecSwitchRelay" => 99,
    "elecCross" => 100,
    "elecSignal" => 101,
    "elecClock" => 102,
    "elecToggle" => 103,
    "elecDelay" => 104,
    "elecWaterSensor" => 105,
    "elecProximitySensor" => 106,
    "elecDistanceSensor" => 107,
    "elecAND" => 108,
    "elecOR" => 109,
    "elecXOR" => 110,
    "elecNOT" => 111,
    "elecLight" => 112,
    "elecAlarm" => 113,
    "reactor" => 114,
    "rocketTop" => 115,
    "rocketTank" => 116,
    "rocketEngine" => 117,
    "autoBuilderUltimate" => 118,
    "dirt" => 119,
    "dirtRed" => 120,
    "silt" => 121,
    "dirtBlack" => 122,
    "dirtSky" => 123,
    "rock" => 124,
    "iron" => 125,
    "coal" => 126,
    "copper" => 127,
    "gold" => 128,
    "aluminium" => 129,
    "rockFlying" => 130,
    "rockGaz" => 131,
    "crystal" => 132,
    "crystalLight" => 133,
    "crystalBlack" => 134,
    "granit" => 135,
    "uranium" => 136,
    "titanium" => 137,
    "lightonium" => 138,
    "thorium" => 139,
    "sulfur" => 140,
    "sapphire" => 141,
    "organicRock" => 142,
    "organicRockHeart" => 143,
    "lava" => 144,
    "lavaOld" => 145,
    "diamonds" => 146,
    "organicRockDefenseLead" => 147,
    "organicRockDefense" => 148,
    "wood" => 149,
    "woodwater" => 150,
    "woodSky" => 151,
    "woodGranit" => 152,
    "deadPlant" => 153,
    "tree" => 154,
    "treePine" => 155,
    "treeWater" => 156,
    "treeSky" => 157,
    "treeGranit" => 158,
    "bush" => 159,
    "flowerBlue" => 160,
    "flowerWhite" => 161,
    "fernRed" => 162,
    "waterBush" => 163,
    "waterLight" => 164,
    "waterCoral" => 165,
    "blackGrass" => 166,
    "blackMushroom" => 167,
    "skyBush" => 168,
    "lavaFlower" => 169,
    "lavaPlant" => 170,
    "bushGranit" => 171,
    "organicHair" => 172,
    "metalScrap" => 173,
    "lightGem" => 174,
    "energyGem" => 175,
    "darkGem" => 176,
    "dogHorn" => 177,
    "dogHorn3" => 178,
    "moleShell" => 179,
    "moleShellBlack" => 180,
    "fish2Regen" => 181,
    "fish3Regen" => 182,
    "bat2Sonar" => 183,
    "bat3Sonar" => 184,
    "antShell" => 185,
    "sharkSkin" => 186,
    "unstableGemResidue" => 187,
    "lootDwellerLord" => 188,
    "lootParticleGround" => 189,
    "lootParticleBirds" => 190,
    "lootLargeParticleBirds" => 191,
    "lootLavaSpider" => 192,
    "lootLavaBat" => 193,
    "lootMiniBalrog" => 194,
    "bloodyFlesh1" => 195,
    "bloodyFlesh2" => 196,
    "bloodyFlesh3" => 197,
    "bossMadCrabSonar" => 198,
    "bossMadCrabMaterial" => 199,
    "masterGem" => 200,
    "lootBalrog" => 201,
};

static UNIT_CODENAME_TO_ID: phf::Map<&'static str, u16> = phf::phf_map! {
    "player" => 1,
    "playerLocal" => 2,
    "defense" => 3,
    "drone" => 4,
    "droneCombat" => 5,
    "droneWar" => 6,
    "hound" => 7,
    "firefly" => 8,
    "fireflyRed" => 9,
    "dweller" => 10,
    "fish" => 11,
    "bat" => 12,
    "houndBlack" => 13,
    "fireflyBlack" => 14,
    "dwellerBlack" => 15,
    "fishBlack" => 16,
    "batBlack" => 17,
    "bossMadCrab" => 18,
    "shark" => 19,
    "fireflyExplosive" => 20,
    "antClose" => 21,
    "antDist" => 22,
    "bossFirefly" => 23,
    "bossDweller" => 24,
    "lavaAnt" => 25,
    "lavaBat" => 26,
    "particleGround" => 27,
    "particleBird" => 28,
    "particleBird2" => 29,
    "balrogMini" => 30,
    "bossBalrog" => 31,
    "bossBalrog2" => 32,
};

pub enum AbnormalitiesNode {
    Leaf(String),
    Group { title: String, children: Vec<AbnormalitiesNode> },
}

pub struct SaveAbnormalities {
    nodes: Vec<AbnormalitiesNode>,
}
impl SaveAbnormalities {
    pub fn new() -> Self {
        return Self { nodes: Vec::new() };
    }
    pub fn add(&mut self, message: String) {
        self.nodes.push(AbnormalitiesNode::Leaf(message));
    }
    pub fn check<T>(&mut self, value: &T, pred: impl FnOnce(&T) -> bool, msg_fn: impl FnOnce(&T) -> String) {
        if !pred(value) {
            self.add(msg_fn(value));
        }
    }
    pub fn group(&mut self, title: impl FnOnce() -> String, func: impl FnOnce(&mut SaveAbnormalities)) {
        let mut child = SaveAbnormalities::new();
        func(&mut child);
        if !child.nodes.is_empty() {
            self.nodes.push(AbnormalitiesNode::Group { title: title(), children: child.nodes });
        }
    }
    pub fn len(&self) -> usize {
        return self.nodes.len();
    }
    pub fn nodes(&self) -> &[AbnormalitiesNode] {
        &self.nodes
    }
}

impl SaveModel {
    const fn get_save_offset(x: i32) -> u32 {
        let mut hash = (x as u32).wrapping_mul(0x9e3779b1);
        hash = ((hash >> 15) ^ hash).wrapping_mul(0x85ebca77);
        hash = ((hash >> 13) ^ hash).wrapping_mul(0xc2b2ae3d);
        hash = (hash >> 16) ^ hash;

        let product = (hash as u64).wrapping_mul(10);
        return (product / 0xFFFFFFFF) as u32;
    }

    pub fn deserialize(uncompressed_bytes: &[u8]) -> Result<Self> {
        // should prevent OOM attacks since the array element count can be easily modifed externally
        fn safe_capacity(count: i32) -> usize {
            const MAX_TRUST_CAPACITY: usize = 256;
            assert!(count >= 0, "Negative array element count");
            return (count as usize).min(MAX_TRUST_CAPACITY);
        }

        let mut reader = CSBinReader::new(uncompressed_bytes);

        reader.check_magic_string("SAVE FILE")?;

        let mut save = SaveModel {
            header: Header {
                version: reader.read_float()?,
                build: reader.read_int()?,
                mode_name: reader.read_string()?.to_owned()
            },
            ..Default::default()
        };
        ensure!(save.header.build >= 481, "Invalid game build: {} (must be >= 481)", save.header.build);

        reader.check_magic_string("Header")?;

        save.params = Params::deserialize(&mut reader)?;
        reader.check_magic_string("Game Params Data")?;

        let items_in_save_count = reader.read_int()?;
        ensure!(items_in_save_count > 0, "Invalid number of item codenames: {items_in_save_count} (must be > 0)");
        
        save.foreign_item_id_to_native = Vec::with_capacity(safe_capacity(items_in_save_count));
        save.foreign_item_id_to_native.push(0); // null item

        let mut last_unknown_item_id: Option<u16> = None;
        for _ in 0..items_in_save_count - 1 {
            let item_codename = reader.read_string()?;

            let native_id = if let Some(last_id) = &mut last_unknown_item_id {
                *last_id += 1;
                *last_id
            } else {
                 match ITEM_CODENAME_TO_ID.get(item_codename) {
                    Some(&id) => id,
                    None => {
                        let last_id = *save.foreign_item_id_to_native.last().unwrap_or(&0);
                        last_unknown_item_id = Some(last_id);
                        last_id
                    },
                }
            };
            save.foreign_item_id_to_native.push(native_id);
        }

        let pickups_count = reader.read_int()?;
        ensure!(pickups_count >= 0, "Invalid number of pickups: {pickups_count} (must be >= 0)");
        save.pickups = Vec::with_capacity(safe_capacity(pickups_count));
        for i in 0..pickups_count {
            let foreign_id = reader.read_ushort()?;
            let id = *save.foreign_item_id_to_native.get(foreign_id as usize)
                .with_context(|| format!("Invalid item ID for pickup[{i}]: {foreign_id} (max ID={})", save.foreign_item_id_to_native.len()))?;
            let pos = Vector2 { x: reader.read_float()?, y: reader.read_float()? };
            let creation_time = reader.read_float()?;

            save.pickups.push(Pickup { id, pos, creation_time });
        }
        reader.check_magic_string("Items Data")?;

        let players_count = reader.read_int()?;
        ensure!(players_count >= 0, "Invalid number of players: {players_count} (must be >= 0)");
        save.players = Vec::with_capacity(safe_capacity(players_count));
        for _ in 0..players_count {
            let steam_id = reader.read_ulong()?;
            let name = reader.read_string()?.to_owned();
            let pos = Vector2 { x: reader.read_float()?, y: reader.read_float()? };
            let unit_player_id = reader.read_ushort()?;
            let skin_is_female = reader.read_bool()?;
            let skin_color_skin = reader.read_float()?;
            let skin_hair_style = reader.read_int()?;
            ensure!(matches!(skin_hair_style, -1..=5), "Invalid skin hair style: {} must be in [-1, 5]", skin_hair_style);
            let skin_color_hair = Color24 { r: reader.read_byte()?, g: reader.read_byte()?, b: reader.read_byte()? };
            let skin_color_eyes = Color24 { r: reader.read_byte()?, g: reader.read_byte()?, b: reader.read_byte()? };

            let items_count = reader.read_int()?;
            ensure!(items_count >= 0, "Invalid number of inventory items for player {name:?} ({steam_id}): {items_count} (must be >= 0)");
            let mut items = Vec::with_capacity(safe_capacity(items_count));
            for item_idx in 0..items_count {
                let foreign_id = reader.read_ushort()?;
                let id = *save.foreign_item_id_to_native.get(foreign_id as usize)
                    .with_context(|| format!("Invalid item ID for InventoryItem[{item_idx}]: {foreign_id} (max ID={})", save.foreign_item_id_to_native.len()))?;
                let nb = reader.read_int()?;

                items.push(Item { id, nb });
            }

            let bar_items_count = reader.read_int()?; // usually = 20
            ensure!(bar_items_count >= 0, "Invalid number of bar items for player {name:?} ({steam_id}): {bar_items_count} (must be >= 0)");
            
            let mut bar_items = Vec::with_capacity(safe_capacity(bar_items_count));
            for bar_item_idx in 0..bar_items_count {
                let foreign_id = reader.read_ushort()?;
                let id = *save.foreign_item_id_to_native.get(foreign_id as usize)
                    .with_context(|| format!("Invalid item ID for BarItem[{bar_item_idx}] for player {name:?} ({steam_id}): {foreign_id} (max ID={})", save.foreign_item_id_to_native.len()))?;

                bar_items.push(id);
            }
            // selected item should exists in inventory
            // important note: the game actually doesn't map foreign ID into native one for the selected item (this is possible a bug in a game)
            let item_selected = reader.read_ushort()?;

            let item_vars_count = reader.read_int()?;
            ensure!(item_vars_count >= 0, "Invalid number of item vars for player {name:?} ({steam_id}): {item_vars_count} (must be >= 0)");
            
            let mut item_vars = Vec::with_capacity(safe_capacity(save.foreign_item_id_to_native.len() as i32));
            for item_id in 0..item_vars_count {
                let exists = reader.read_bool()?;
                if !exists { continue; }

                let time_last_use = reader.read_float()?;
                let time_activation = reader.read_float()?;
                let dico_count = reader.read_int()?;

                ensure!(dico_count >= 0, "Invalid dico count for player {name:?} ({steam_id}): {dico_count} (must be >= 0)");
                let mut dico = Vec::with_capacity(safe_capacity(dico_count));
                for _ in 0..dico_count {
                    dico.push(KeyValue {
                        key: reader.read_string()?.to_owned(),
                        value: reader.read_float()?
                    });
                }
                let id = *save.foreign_item_id_to_native.get(item_id as usize)
                    .with_context(|| format!("Invalid item ID for ItemVar[{item_id}] for player {name:?} ({steam_id}): {item_id} (max ID={})", save.foreign_item_id_to_native.len()))?
                    as usize;
                
                if id >= item_vars.len() {
                    item_vars.resize_with(id + 1, ItemVar::default);
                }
                item_vars[id] = ItemVar { time_last_use, time_activation, dico };
            }
            save.players.push(Player {
                steam_id, name, pos, unit_player_id, skin_is_female, skin_color_skin,
                skin_hair_style, skin_color_hair, skin_color_eyes,
                inventory: Inventory {
                    items,
                    bar_items,
                    item_selected,
                },
                item_vars,
            });
        }
        reader.check_magic_string("Players")?;

        let events_count = reader.read_int()?;
        ensure!(events_count >= 0, "Invalid number of events: {events_count} (must be >= 0)");
        save.last_events = Vec::with_capacity(safe_capacity(events_count));
        for _ in 0..events_count {
            save.last_events.push(reader.read_string()?.to_owned());
        }
        reader.check_magic_string("Environments")?;

        const WORLD_WIDTH: usize = 1024;
        const WORLD_HEIGHT: usize = 1024;

        let world_grid_len = WORLD_WIDTH.checked_mul(WORLD_HEIGHT).context("Cell grid length calculation overflowed")?;
        let mut world_grid = Box::<[Cell]>::new_uninit_slice(world_grid_len);
        for i in 0..WORLD_WIDTH {
            reader.skip_bytes(Self::get_save_offset(i as i32) as usize)?;
            for j in 0..WORLD_HEIGHT {
                let flags = reader.read_uint()?;
                let foreign_content_id = reader.read_ushort()?;
                let content_hp = reader.read_ushort()?;
                let water = reader.read_float()?;
                let force_x = reader.read_short()?;
                let force_y = reader.read_short()?;
                let light = Color24 { r: reader.read_byte()?, g: reader.read_byte()?, b: reader.read_byte()? };

                let content_id = *save.foreign_item_id_to_native.get(foreign_content_id as usize)
                    .with_context(|| format!("Invalid item ID for Cell[{i}, {j}]: {foreign_content_id} (max ID={})", save.foreign_item_id_to_native.len()))?;

                world_grid[j + i * WORLD_HEIGHT].write(Cell { flags, content_id, content_hp, water, force_x, force_y, light });
            }
        }
        save.cell_grid = CellGrid::new(unsafe { world_grid.assume_init() }, WORLD_WIDTH, WORLD_HEIGHT)?;
        reader.check_magic_string("World Data")?;

        let units_count = reader.read_int()?;
        ensure!(units_count >= 0, "Invalid number of units: {units_count} (must be >= 0)");
        save.units = Vec::with_capacity(safe_capacity(units_count));
        for _ in 0..units_count {
            let code_name = reader.read_string()?.to_owned();
            let pos = Vector2 { x: reader.read_float()?, y: reader.read_float()? };
            let instance_id = reader.read_ushort()?;
            let desc_id = UNIT_CODENAME_TO_ID.get(&code_name).and_then(|&id| NonZeroU16::new(id));
            let hp = reader.read_float()?;
            let air = reader.read_float()?;

            let monster_data = match Unit::unit_kind(&code_name) {
                UnitKind::Monster => {
                    Some(UnitMonsterData {
                        is_night_spawn: reader.read_bool()?,
                        target_id: reader.read_ushort()?,
                        is_creative_spawn: reader.read_bool()?,
                    })
                },
                UnitKind::Unknown => {
                    bail!("Unknown unit kind for unit with code name: {code_name:?}");
                },
                UnitKind::Ally => None,
            };
            save.units.push(Unit { code_name, pos, instance_id, desc_id, hp, air, monster_data });
        }
        let species_killed_count = reader.read_int()?;
        ensure!(species_killed_count >= 0, "Invalid number of species killed: {species_killed_count} (must be >= 0)");
        save.species_killed = Vec::with_capacity(safe_capacity(species_killed_count));
        for _ in 0..species_killed_count {
            let code_name = reader.read_string()?.to_owned();
            let nb = reader.read_int()?; // should be >0
            let last_kill_time = reader.read_float()?;
            save.species_killed.push(SpeciesKillsInfo { code_name, nb, last_kill_time });
        }
        reader.check_magic_string("Units Data")?;

        save.vars = Vars::deserialize(&mut reader)?;
        reader.check_magic_string("Vars Data")?;

        ensure!(reader.remaining() == 0, "There is remanining {} bytes left", reader.remaining());

        return Ok(save);
    }

    pub fn find_abnormalities(&self) -> SaveAbnormalities {
        let is_valid_int2 = |pos: &Int2| pos.x >= 0 && pos.y >= 0 && pos.x < 1024 && pos.y < 1024;
        let is_valid_vector2 = |pos: &Vector2| pos.x >= 0.0 && pos.y >= 0.0 && pos.x < 1024.0 && pos.y < 1024.0;
        
        let mut abnorms = SaveAbnormalities::new();
        abnorms.check(&self.header.version, |ver| matches!(ver, 0.0..2.0), |ver| format!("Save version should a valid one, got: {ver}"));
        abnorms.check(&self.header.build, |build| matches!(build, 481..1000), |build| format!("Save build should a valid one, got: {build}"));
        abnorms.check(&self.header.mode_name, 
            |x| matches!(x.as_str(), "Solo" | "Multi" | "SkyWorld" | "UnderTheSea" | "Defense"),
            |x| format!("Unknown mode name (should be one of those: \"Solo\", \"Multi\", \"SkyWorld\", \"UnderTheSea\" or \"Defense\"), got: {x:?}"));

        abnorms.check(&self.params.m_difficulty, |x| matches!(x, -1..=3), |x| format!("Params m_difficulty should only be [-1, 3], got: {x}"));
        abnorms.check(&self.params.m_spawnPos, is_valid_int2, |pos| format!("Params m_spawnPos should have coordinates inside world, got: {pos}"));
        abnorms.check(&self.params.m_shipPos, is_valid_int2, |pos| format!("Params m_shipPos should have coordinates inside world, got: {pos}"));
        abnorms.check(&self.params.m_visibility, |x| matches!(x, 0..=2), |x| format!("Params m_visibility should only be [0, 2] (0=public, 1=friends, 2=private), got: {x}"));

        for (i, pickup) in self.pickups.iter().enumerate() {
            let Pickup { id, pos, creation_time } = &pickup;
            abnorms.group(|| format!("Pickup[{i}] with item ID: {id} (at {pos} with creation time {creation_time:+?})"), |ab| {
                ab.check(pos, is_valid_vector2, |pos| format!("Invalid pickup position: {pos}"));
                ab.check(creation_time, |t| t.is_finite() && *t >= 0.0, |t| format!("Pickup creation time should be non-negative, got: {t:+?}"));
            });
        }
        if self.players.is_empty() {
            abnorms.add("There should be at least one player in the world".to_owned());
        }
        for (i, player) in self.players.iter().enumerate() {
            let (steam_id, name) = (&player.steam_id, &player.name);
            abnorms.group(|| format!("Player[{i}] with name: {name:?} (Steam ID: {steam_id})"), |ab| {
                ab.check(steam_id,
                    |&id| id >= 76561197960265728 /*min id*/ && (id >> 52) & 0xF == 1 /*account type*/ && (id >> 56) & 0xFF == 1 /*universe*/,
                    |id| format!("Invalid player's steam ID, got: {id}"));
                ab.check(&player.pos, is_valid_vector2, |pos| format!("Invalid player position: {pos}"));
                // The assignment of player unit IDs starts from 0, while other unit IDs starts from 1000 (and goes up to 65000). See SMisc.SearchFreeId
                ab.check(&player.unit_player_id, |id| *id < 1000, |id| format!("Player unit ID should be less than 1000, got: {id}"));
                ab.check(&player.skin_color_skin, |x| matches!(x, 0.0..=1.0), |x| format!("Player skin color value should be in [0, 1], got: {x}"));

                for (i, item) in player.inventory.items.iter().enumerate() {
                    ab.group(|| format!("Inventory Item[{i}] with ID {}", item.id), |ab| {
                        ab.check(&item.nb, |nb| *nb >= 0, |nb| format!("Negative amount of items in a stack: {nb}"));
                    });
                }
                ab.check(&player.inventory.bar_items.len(), |n| *n == 20, |n| format!("The number of bar items should be 20, got: {n}"));
                for (i, item_id) in player.inventory.bar_items.iter().enumerate() {
                    ab.check(item_id, |&id| id == 0 || player.inventory.items.iter().any(|item| item.id == id),
                        |id| format!("Bar Item[{i}] with ID {id} doesn't exists in inventory"));
                }
            });
        }
        const KNOWN_EVENTS: [&str; 16] = [
            "heatWave", "rainFlood", "meteorShower", "volcanoEruption", "earthquake", "quietNight", "restlessNight", "gravityWaves",
            "acidWater", "drowsiness", "matingSeason", "luckyDay", "sunEclipse", "mist", "emp", "sharkstorm"
        ];
        for (i, event) in self.last_events.iter().enumerate() {
            abnorms.check(event, |env| KNOWN_EVENTS.contains(&env.as_str()), |x| format!("Event[{i}] has unknown name: {x:?}"));
        }

        for (x, y, cell) in self.cell_grid.iter() {
            const ALL_FLAGS: u32 = 0xFFF77;
    
            abnorms.group(|| format!("Cell[{x}, {y}]"), |ab| {
                ab.check(&cell.flags, |&f| f & !ALL_FLAGS == 0, |f| format!("Unknown flag value: {f:b}"));
                // small amount of negative water can still naturally occur
                ab.check(&cell.water, |w| w.is_finite() && *w >= -0.001, |w| format!("Invalid water value: {w:+}"));
            });
        }

        for (i, unit) in self.units.iter().enumerate() {
            abnorms.group(|| format!("Unit[{i}] with codename: \"{}\"", unit.code_name), |ab| {
                ab.check(&unit.pos, is_valid_vector2, |pos| format!("Invalid unit position: {pos}"));
                ab.check(&unit.hp, |hp| hp.is_finite() && *hp >= 0.0, |hp| format!("Health should be finite and non-negative: {hp:+}"));
                ab.check(&unit.air, |air| matches!(air, 0.0..=1.0), |air| format!("Air should be in [0, 1]: {air:+}"));
            });
        }

        abnorms.check(&self.vars.m_simuTimeD, |t| t.is_finite() && *t >= 0.0, |t| format!("GVars.m_simuTimeD should be finite and non-negative: {t:+?}"));
        abnorms.check(&self.vars.m_worldTimeD, |t| t.is_finite() && *t >= 0.0, |t| format!("GVars.m_worldTimeD should be finite and non-negative: {t:+?}"));
        abnorms.check(&self.vars.m_clock, |c| c.is_nan() || matches!(c, 0.0..=1.0), |c| format!("GVars.m_clock should be in [0, 1] or NaN: {c:+?}"));
        // TODO: add more checks
        return abnorms;
    }
}
