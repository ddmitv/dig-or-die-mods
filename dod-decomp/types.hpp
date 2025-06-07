#pragma once

#include <cstdint>
#include <cstddef>

#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

using DelegateCallbackDebug = void __cdecl(const char* str);
using DelegateCallbackGetElecProd = int __cdecl(int i, int j);

struct Color24 {
    uint8_t r;
    uint8_t g;
    uint8_t b;
};
static_assert(sizeof(Color24) == 3);

enum CCell_Flag : uint32_t {
    Flag_CustomData0 = 1,
    Flag_CustomData1 = 2,
    Flag_CustomData2 = 4,
    Flag_IsXReversed = 16,
    Flag_IsBurning = 32,
    Flag_IsMapped = 64,
    Flag_UNUSED1 = 128,
    Flag_BackWall_0 = 256,
    Flag_BgSurface_0 = 512,
    Flag_BgSurface_1 = 1024,
    Flag_BgSurface_2 = 2048,
    Flag_WaterFall = 4096,
    Flag_StreamLFast = 8192,
    Flag_StreamRFast = 16384,
    Flag_IsLava = 32768,
    Flag_HasWireRight = 65536,
    Flag_HasWireTop = 131072,
    Flag_ElectricAlgoState = 262144,
    Flag_IsPowered = 524288
};
DEFINE_ENUM_FLAG_OPERATORS(CCell_Flag);

struct CCell {
    CCell_Flag m_flags;
    uint16_t m_contentId;
    uint16_t m_contentHP;
    int16_t m_forceX;
    int16_t m_forceY;
    float m_water;
    Color24 m_light;
    uint8_t m_elecProd;
    uint8_t m_elecCons;
    Color24 m_temp;
};
static_assert(sizeof(CCell) == 24);

struct CItem_PluginData {
    float m_weight;
    int m_electricValue;
    int m_electricOutletFlags;
    int m_elecSwitchType;
    int m_elecVariablePower;
    int m_anchor;
    Color24 m_light;
    int m_isBlock;
    int m_isBlockDoor;
    int m_isReceivingForces;
    int m_isMineral;
    int m_isDirt;
    int m_isPlant;
    int m_isFireProof;
    int m_isWaterGenerator;
    int m_isWaterPump;
    int m_isLightGenerator;
    int m_isBasalt;
    int m_isLightonium;
    int m_isOrganicHeart;
    int m_isSunLamp;
    int m_isAutobuilder;
    float m_customValue;
};
static_assert(sizeof(CItem_PluginData) == 92);

struct int2 {
    int x;
    int y;
};
static_assert(sizeof(int2) == 8);

struct RectInt {
    int x;
    int y;
    int width;
    int height;
};
static_assert(sizeof(RectInt) == 16);

struct ThreadData {
    HANDLE handle;                     // 0x00
    int id;                            // 0x04
    HANDLE secondEvent;                // 0x08
                                       
    bool shouldExit;                   // 0x0C
    bool processCellLighting;          // 0x0D
    uint8_t _padding_0E[2];            // 0x0E
                                       
    int cellParam1;                    // 0x10
    int cellParam2;                    // 0x14
    int cellParam3;                    // 0x18
    int cellParam4;                    // 0x1C
                                       
    bool processWaterSim;              // 0x20
    uint8_t padding_21[3];             // 0x21
                                       
    int waterParam1;                   // 0x24
    int waterParam2;                   // 0x1C
                                       
    bool flag_0x2C;                    // 0x2C
    uint8_t padding_2D[3];             // 0x2D
    int processWaterFlowStartOffset;   // 0x30
    int processWaterFlowNumIterations; // 0x34

    uint8_t _padding[512]; // large padding is possible used to avoid false sharing
};
static_assert(sizeof(ThreadData) == 568);
