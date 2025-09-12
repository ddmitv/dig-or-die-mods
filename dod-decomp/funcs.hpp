#pragma once

#include <iterator> // std::size
#include <algorithm> // std::max

#include "types.hpp"
#include "vars.hpp"

inline void WaitForDebugger() {
    while (!::IsDebuggerPresent()) {
        ::Sleep(200);
    }
    ::DebugBreak();
}

inline bool IsCellPassable(const CCell& cell, const CItem_PluginData* itemsData) {
    return itemsData[cell.m_contentId].m_isBlock == 0
        || (itemsData[cell.m_contentId].m_isBlockDoor != 0 && (cell.m_flags & Flag_CustomData0) != 0);
}
inline bool CellHasLava(const CCell& cell) {
    return (cell.m_flags & Flag_IsLava) != 0;
}
inline bool CellHasLavaAtLeast(const CCell& cell) {
    return (cell.m_flags & Flag_IsLava) != 0;
}
inline bool CellHasBgSurface(const CCell& cell) {
    return (cell.m_flags & (Flag_BgSurface_2 | Flag_BgSurface_1 | Flag_BgSurface_0)) != 0;
}
inline bool CellHasGrass(const CCell& cell, const CItem_PluginData* const itemsData) {
    return itemsData[cell.m_contentId].m_isDirt != 0 && (cell.m_flags & Flag_CustomData0) != 0;
}
inline bool AlmostEqual(const float x, const float y, const float tolerance = 0.001f) {
    return std::abs(x - y) <= tolerance;
}
inline bool IsCellPassableWithLava(const CCell& cell, const CItem_PluginData* itemsData) {
    return IsCellPassable(cell, itemsData) && (cell.m_flags & Flag_IsLava) != 0;
}
inline void GetIterators(int n, double t, double dt, float period, int& outStartOffset, int& outNumIterations) {
    int64_t time1 = int64_t((t - dt) * n);
    int64_t time2 = int64_t(t * n);
    
    outStartOffset = std::max(0, int(float(time1) / period) % n);
    outNumIterations = std::max(0, int(float(time2) / period - float(time1) / period));
}


// https://github.com/wine-mirror/wine/blob/master/include/msvcrt/stdio.h
// FUNCTION: 0x1630
inline int DebugLogFormat(char* buffer, const char* format, ...) {
    va_list args;
    va_start(args, format);
    int res = ::__stdio_common_vsprintf_s(
        _CRT_INTERNAL_LOCAL_PRINTF_OPTIONS, buffer, std::size(g_formatBuffer), format, nullptr, args
    );
    va_end(args);
    return res < 0 ? -1 : res;
}

// FUNCTION: 0x5150
inline void UpdateWaterFlow(int startOffset, int numIterations) {
    CItem_PluginData* itemsData = g_itemsData;
    CCell* grid = g_grid;
    int gridHeight = g_gridSize.y;
    int gridWidthMinus2 = g_gridSize.x - 2;
    int gridHeightMinus2 = g_gridSize.y - 2;

    for (int iter = 0; iter < numIterations; iter++) {
        int gridOrderIndex = (gridWidthMinus2 + (iter + startOffset) % gridWidthMinus2) % gridWidthMinus2;

        // assuming gridOrderIndex is never negative
        uint32_t orderOffset = gridOrderIndex + 4 * (gridOrderIndex & 1);
        int x = g_gridOrder[(orderOffset % gridWidthMinus2) + 1];

        CCell* currentCell = &grid[x * gridHeight + 1];
        for (int y = 2; y < gridHeightMinus2; ++y, ++currentCell) {
            
        }
    }
}

// FUNCTION: 0x1060
inline unsigned int __stdcall WorkerThread(void* threadDataRaw) {
    ThreadData& threadData = *static_cast<ThreadData*>(threadDataRaw);

    ::WaitForSingleObject(threadData.workEvent /*0x8*/, 1000);

    while (true) {
        if (threadData.shouldExit /*0xC*/) {
            return 0;
        }
        if (threadData.processVerticalWater /*0x20*/) {
            threadData.processVerticalWater /*0x20*/ = false;
            // ...;
        } else if (threadData.processHorizontalFlow /*0x2C*/) {
            threadData.processHorizontalFlow /*0x2C*/ = false;
            // FUN_10005150(...);
        } else if (threadData.processCellLighting /*0xD*/) {
            threadData.processCellLighting /*0xD*/ = false;
            // ...;
        }
        ::SetEvent(g_threadEvents[threadData.id /*0x4*/]);
        ::WaitForSingleObject(threadData.workEvent /*0x8*/, 1000);
    }
}
// FUNCTION: 0x1120
inline void InitGridOrder() {
    if (g_gridSize.x + 1 > 0) {
        int i = 0;
        if (g_gridSize.x + 1 >= 16) {
            const uint32_t unrollRemainder = (g_gridSize.x + 1) - (g_gridSize.x + 1) % 16;
            for (; i < unrollRemainder; i += 16) {
                g_gridOrder[i + 0] = i + 0;
                g_gridOrder[i + 1] = i + 1;
                g_gridOrder[i + 2] = i + 2;
                g_gridOrder[i + 3] = i + 3;
                g_gridOrder[i + 4] = i + 4;
                g_gridOrder[i + 5] = i + 5;
                g_gridOrder[i + 6] = i + 6;
                g_gridOrder[i + 7] = i + 7;
                g_gridOrder[i + 8] = i + 8;
                g_gridOrder[i + 9] = i + 9;
                g_gridOrder[i + 10] = i + 10;
                g_gridOrder[i + 11] = i + 11;
                g_gridOrder[i + 12] = i + 12;
                g_gridOrder[i + 13] = i + 13;
                g_gridOrder[i + 14] = i + 14;
                g_gridOrder[i + 15] = i + 15;
            }
        }
        for (; i < g_gridSize.x + 1; ++i) {
            g_gridOrder[i] = i;
        }
    }
    for (int i = 10; i < g_gridSize.x - 9; ++i) {
        int idx = (::rand() * (g_gridSize.x - 19)) / RAND_MAX;
        int temp = g_gridOrder[idx];
        g_gridOrder[i] = g_gridOrder[idx + 10];
        g_gridOrder[i + 10] = temp;
    }
}
// FUNCTION: 0x54d0
inline void PostProcessWater(int* changeCellPos) {
    // changeCellPos encoding: cellY * gsY + cellX + changeType * 10000000
    // changeType = 0: places basalt and changes background to basalt
    // changeType = 1: melt basalt back to lava
    // changeType = 2: damage cell and set on fire (if possible)

    int changedCellCount = 1;

    int startOffset = 0;
    int numIterations = 0;
    GetIterators(g_gridSize.y - 4, g_simuTime, g_simuDeltaTime, 0.5f, startOffset, numIterations);

    for (int cellIdx = 0; cellIdx < numIterations; ++cellIdx) {
        int cellY = (cellIdx + startOffset) % (g_gridSize.y - 4) + 2; // simplified from 2 mod operations -> 1 mod operation
        g_nbCellsUpdated[cellY] = 10;
        for (int cellX = 1; cellX < g_gridSize.x + -1; ++cellX) {
            const int centerIndex = cellX * g_gridSize.y + cellY;
            const int aboveIndex = (cellX + -1) * g_gridSize.y + cellY;
            const int belowIndex = (cellX + 1) * g_gridSize.y + cellY;

            CCell& centerCell = g_grid[centerIndex];
            const CCell_Flag centerFlags = centerCell.m_flags;
            const CItem_PluginData& centerItem = g_itemsData[centerCell.m_contentId];

            if ((((centerFlags & Flag_IsMapped) != 0) && (g_gridBorderNoCam < float(cellX))) &&
                (float(cellX) < float(g_gridSize.x) - g_gridBorderNoCam)) {
                g_nbCellsUpdated[cellY] += 1;
            }
            if (IsCellPassable(centerCell, g_itemsData) && CellHasLava(centerCell) && centerCell.m_water > 0.3 && (changedCellCount < 1000)) {
                if ((centerCell.m_water < 1.5) && double(::rand()) < (double(RAND_MAX) * 0.005)
                    && (!IsCellPassable(g_grid[centerIndex + -1], g_itemsData)
                        || !IsCellPassable(g_grid[centerIndex + 1], g_itemsData)
                        || !IsCellPassable(g_grid[aboveIndex], g_itemsData)
                        || !IsCellPassable(g_grid[belowIndex], g_itemsData))
                    && (CellHasBgSurface(centerCell)
                        || (CellHasBgSurface(g_grid[centerIndex + -1])
                            && CellHasBgSurface(g_grid[aboveIndex + -1])
                            && CellHasBgSurface(g_grid[belowIndex + -1])))
                    && (!IsCellPassableWithLava(g_grid[centerIndex - 1], g_itemsData)
                        || centerCell.m_water >= 1.0f || AlmostEqual(g_grid[centerIndex - 1].m_water, centerCell.m_water / 10.f + 1.f))
                    && (!IsCellPassableWithLava(g_grid[centerIndex - 1], g_itemsData)
                        || centerCell.m_water < 1.0f || AlmostEqual(g_grid[centerIndex - 1].m_water, centerCell.m_water + 0.1f))
                    && (!IsCellPassableWithLava(g_grid[centerIndex + 1], g_itemsData)
                        || centerCell.m_water <= 1.0f || centerCell.m_water >= 1.1f || AlmostEqual(centerCell.m_water, g_grid[centerIndex + 1].m_water / 10.f + 1.f))
                    && (!IsCellPassableWithLava(g_grid[centerIndex + 1], g_itemsData)
                        || centerCell.m_water < 1.1f || AlmostEqual(centerCell.m_water, g_grid[centerIndex + 1].m_water + 0.1f))
                    && (!IsCellPassableWithLava(g_grid[aboveIndex], g_itemsData)
                        || AlmostEqual(centerCell.m_water, g_grid[aboveIndex].m_water))
                    && (!IsCellPassableWithLava(g_grid[belowIndex], g_itemsData)
                        || AlmostEqual(centerCell.m_water, g_grid[belowIndex].m_water))
                    )
                {
                    changeCellPos[changedCellCount] = centerIndex + changeCellType::basaltFormation;
                    changedCellCount += 1;
                }
                if (centerCell.m_water > 2.f) {
                    const float basaltMeltChance = (double)(centerCell.m_water / 10.f) + 0.2f;
                    const float clampedMeltChance = basaltMeltChance <= 1.f ? basaltMeltChance : 1.f;
                    if (double(::rand()) < clampedMeltChance * double(RAND_MAX)) {
                        if ((g_itemsData[g_grid[centerIndex - 1].m_contentId].m_isBasalt != 0) && changedCellCount < 1000) {
                            changeCellPos[changedCellCount] = (centerIndex - 1) + changeCellType::basaltMelt;
                            changedCellCount += 1;
                        }
                        if ((g_itemsData[g_grid[centerIndex + 1].m_contentId].m_isBasalt != 0) && changedCellCount < 1000) {
                            changeCellPos[changedCellCount] = (centerIndex + 1) + changeCellType::basaltMelt;
                            changedCellCount += 1;
                        }
                        if ((g_itemsData[g_grid[aboveIndex].m_contentId].m_isBasalt != 0) && changedCellCount < 1000) {
                            changeCellPos[changedCellCount] = aboveIndex + changeCellType::basaltMelt;
                            changedCellCount += 1;
                        }
                        if ((g_itemsData[g_grid[belowIndex].m_contentId].m_isBasalt != 0) && changedCellCount < 1000) {
                            changeCellPos[changedCellCount] = belowIndex + changeCellType::basaltMelt;
                            changedCellCount += 1;
                        }
                    }
                }
                if (centerCell.m_water > 0.3f && double(::rand()) < double(RAND_MAX) * 0.2) {
                    for (int i = 0; i < 5; ++i) {
                        if (((g_itemsData[g_grid[(g_dirt_spread_offsets_x[i] + cellX) * g_gridSize.y +
                            (g_dirt_spread_offsets_y[i] + cellY)].m_contentId].m_isDirt != 0)
                            && (changedCellCount < 1000))
                            && (centerCell.m_water > 3.f || double(::rand()) < double(RAND_MAX) * 0.3)) {
                            changeCellPos[changedCellCount] = (g_dirt_spread_offsets_x[i] + cellX) * g_gridSize.y + (g_dirt_spread_offsets_y[i] + cellY) + changeCellType::basaltFormation;
                            changedCellCount += 1;
                        }
                    }
                }
            }
            if (IsCellPassable(centerCell, g_itemsData)
                && ((centerFlags & Flag_IsLava) != 0 && centerCell.m_water > 0.001f)) {
                for (int i = 0; i < 5; ++i) {
                    CCell& dirtCell = g_grid[(g_dirt_spread_offsets_x[i] + cellX) * g_gridSize.y + (g_dirt_spread_offsets_y[i] + cellY)];
                    int dirtCellContentId = dirtCell.m_contentId;
                    if ((dirtCellContentId != 0 && g_itemsData[dirtCellContentId].m_isMineral == 0 &&
                        g_itemsData[dirtCellContentId].m_isFireProof == 0) || CellHasGrass(dirtCell, g_itemsData)) {
                        dirtCell.m_flags |= Flag_IsBurning;
                    }
                }
            }
            if ((centerFlags & Flag_IsBurning) != 0) {
                if (centerCell.m_contentId != 0 && (
                       (IsCellPassable(centerCell, g_itemsData) && ((centerFlags & Flag_IsLava) != 0))
                    || ((!IsCellPassable(centerCell, g_itemsData) || centerCell.m_water <= 0.002f)
                        && (!CellHasGrass(centerCell, g_itemsData) || g_grid[centerIndex + 1].m_water <= 0.002f)))) {
                    if (changedCellCount < 1000) {
                        changeCellPos[changedCellCount] = centerIndex + changeCellType::damageCell;
                        changedCellCount += 1;
                        for (int i = 0; i < 12; ++i) {
                            CCell& currentCell = g_grid[(g_fire_spread_x_offsets[i] + cellX) * g_gridSize.y + g_fire_spread_y_offsets[i] + cellY];
                            if ((((g_itemsData[currentCell.m_contentId].m_isPlant != 0) &&
                                (g_itemsData[currentCell.m_contentId].m_isFireProof == 0)) &&
                                (((currentCell.m_flags & Flag_CustomData0) == 0 &&
                                    (double(::rand()) < double(RAND_MAX) * 0.2 ||
                                        g_fire_spread_y_offsets[i] > 0)))) ||
                                (CellHasGrass(currentCell, g_itemsData) && double(::rand()) < double(RAND_MAX) * 0.1)) {
                                currentCell.m_flags |= Flag_IsBurning;
                            }
                        }
                    }
                } else {
                    centerCell.m_flags &= ~Flag_IsBurning;
                }
            }
        }
    }
}
