#pragma once

#include "types.hpp"

inline DelegateCallbackDebug* g_callbackDebug; // GLOBAL: 0x26548
inline DelegateCallbackGetElecProd* g_callbackGetElecProd; // GLOBAL: 0x2654c

inline int2 g_gridSize; // GLOBAL: 0x26550
inline float g_gridBorderNoCam; // GLOBAL: 0x26558
inline int* g_gridOrder; // GLOBAL: 0x2655c
inline double g_simuTime; // GLOBAL: 0x26560
inline double g_simuDeltaTime; // GLOBAL: 0x26568
inline int g_isRaining; // GLOBAL: 0x26570
inline int g_yRain; // GLOBAL: 0x26574
inline int g_fastEvaporationYMax; // GLOBAL: 0x26578
inline float g_cloudCenter; // GLOBAL: 0x2657c
inline int g_cloudRadius; // GLOBAL: 0x26580

inline float g_lavaPressure; // GLOBAL: 0x2658c
inline int g_someFlag1; // GLOBAL: 0x26590 (the only first byte is used)
inline int g_someCellPos1; // GLOBAL: 0x26594
inline float g_waterSpeed; // GLOBAL: 0x26598
inline double* g_infiltrationTimes; // GLOBAL: 0x2659c
inline double* g_lavaMovingTimes; // GLOBAL: 0x265a0
inline short* g_nbCellsUpdated; // GLOBAL: 0x265a4
inline CCell* g_grid; // GLOBAL: 0x265a8
inline CItem_PluginData* g_itemsData; // GLOBAL: 0x265ac
inline RectInt g_skipYMax; // GLOBAL: 0x265b0

inline int g_nbThreads; // GLOBAL: 0x2acc0

inline char g_formatBuffer[512]; // GLOBAL: 0x26340

inline ThreadData g_threadData[32]; // GLOBAL: 0x265c0
inline HANDLE g_threadEvents[32]; // GLOBAL: 0x2acc0
