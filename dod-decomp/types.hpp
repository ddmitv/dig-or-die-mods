#pragma once

#include <cstdint>
#include <cstddef>

#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#include <Windows.h> // ::HANDLE

using DelegateCallbackDebug = void __cdecl(const char* str);
using DelegateCallbackGetElecProd = int __cdecl(int i, int j);

struct Color24 {
    uint8_t r; // 0x00
    uint8_t g; // 0x01
    uint8_t b; // 0x02
};
static_assert(sizeof(Color24) == 3);

enum CCell_Flag : uint32_t {
    Flag_CustomData0 = 1 << 0,        // 1
    Flag_CustomData1 = 1 << 1,        // 2
    Flag_CustomData2 = 1 << 2,        // 4
    Flag_UNUSED1 = 1 << 3,            // 8
    Flag_IsXReversed = 1 << 4,        // 16
    Flag_IsBurning = 1 << 5,          // 32
    Flag_IsMapped = 1 << 6,           // 64
    Flag_UNUSED2 = 1 << 7,            // 128
    Flag_BackWall_0 = 1 << 8,         // 256
    Flag_BgSurface_0 = 1 << 9,        // 512
    Flag_BgSurface_1 = 1 << 10,       // 1024
    Flag_BgSurface_2 = 1 << 11,       // 2048
    Flag_WaterFall = 1 << 12,         // 4096
    Flag_StreamLFast = 1 << 13,       // 8192
    Flag_StreamRFast = 1 << 14,       // 16384
    Flag_IsLava = 1 << 15,            // 32768
    Flag_HasWireRight = 1 << 16,      // 65536
    Flag_HasWireTop = 1 << 17,        // 131072
    Flag_ElectricAlgoState = 1 << 18, // 262144
    Flag_IsPowered = 1 << 19,         // 524288
};
constexpr CCell_Flag operator|(CCell_Flag a, CCell_Flag b) noexcept { return CCell_Flag(uint32_t(a) | uint32_t(b)); }
constexpr CCell_Flag& operator|=(CCell_Flag& a, CCell_Flag b) noexcept { return a = CCell_Flag(uint32_t(a) | uint32_t(b)); }
constexpr CCell_Flag operator&(CCell_Flag a, CCell_Flag b) noexcept { return CCell_Flag(uint32_t(a) & uint32_t(b)); }
constexpr CCell_Flag& operator&=(CCell_Flag& a, CCell_Flag b) noexcept { return a = CCell_Flag(uint32_t(a) & uint32_t(b)); }
constexpr CCell_Flag operator~(CCell_Flag a) noexcept { return CCell_Flag(~uint32_t(a)); }

enum class ElecSwitchType : int {
    None = 0,
    ElecCross = 1,
    ElecSwitchRelay = 2,
    ElecSwitch = 3,
    ElecSwitchPush = 4,
};

struct CCell {
    CCell_Flag m_flags;      // 0x00
    uint16_t m_contentId;    // 0x04
    uint16_t m_contentHP;    // 0x06
    int16_t m_forceX;        // 0x08
    int16_t m_forceY;        // 0x0A
    float m_water;           // 0x0C
    Color24 m_light;         // 0x10
    uint8_t m_elecProd;      // 0x13
    uint8_t m_elecCons;      // 0x14
    Color24 m_temp;          // 0x15
};
static_assert(sizeof(CCell) == 24);

struct CItem_PluginData {
    float m_weight;                  // 0x00
    int m_electricValue;             // 0x04
    int m_electricOutletFlags;       // 0x08
    ElecSwitchType m_elecSwitchType; // 0x0C
    int m_elecVariablePower;         // 0x10
    int m_anchor;                    // 0x14
    Color24 m_light;                 // 0x18
    int m_isBlock;                   // 0x1C
    int m_isBlockDoor;               // 0x20
    int m_isReceivingForces;         // 0x24
    int m_isMineral;                 // 0x28
    int m_isDirt;                    // 0x2C
    int m_isPlant;                   // 0x30
    int m_isFireProof;               // 0x34
    int m_isWaterGenerator;          // 0x38
    int m_isWaterPump;               // 0x3C
    int m_isLightGenerator;          // 0x40
    int m_isBasalt;                  // 0x44
    int m_isLightonium;              // 0x48
    int m_isOrganicHeart;            // 0x4C
    int m_isSunLamp;                 // 0x50
    int m_isAutobuilder;             // 0x54
    float m_customValue;             // 0x58
};
static_assert(sizeof(CItem_PluginData) == 92);

struct int2 {
    int x; // 0x00
    int y; // 0x04
};
static_assert(sizeof(int2) == 8);

struct short2 {
    short x; // 0x00
    short y; // 0x02
};
static_assert(sizeof(short2) == 4);

struct RectInt {
    int x;      // 0x00
    int y;      // 0x04
    int width;  // 0x08
    int height; // 0x12
};
static_assert(sizeof(RectInt) == 16);

struct ThreadData {
    ::HANDLE handle;              // 0x00
    int id;                       // 0x04
    ::HANDLE workEvent;           // 0x08
                                  
    bool shouldExit;              // 0x0C
    bool processCellLighting;     // 0x0D
    uint8_t _padding_0E[2];       // 0x0E
                                  
    int startX;                   // 0x10
    int startY;                   // 0x14
    int endX;                     // 0x18
    int endY;                     // 0x1C
                                
    bool processFluidSimulation;  // 0x20
    uint8_t _padding_21[3];       // 0x21
                                
    int fluidSimulationStartX;    // 0x24
    int fluidSimulationEndX;      // 0x28
                                
    bool processWaterSeepage;     // 0x2C
    uint8_t _padding_2D[3];       // 0x2D
    int waterSeepageOffset;       // 0x30
    int waterSeepageIterations;   // 0x34

    uint8_t _padding[512]; // large padding (used to avoid false sharing?)
};
static_assert(sizeof(ThreadData) == 568);

enum class RainMode : int {
    NoRain = 0,
    Rain = 1,
    HeavyRain = 2,
};
