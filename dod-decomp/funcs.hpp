#pragma once

#include <iterator> // std::size
#include <algorithm> // std::max
#include <tuple> // std::tuple
#include <utility> // std::swap

#include "types.hpp"
#include "vars.hpp"

inline void WaitForDebugger() {
    while (!::IsDebuggerPresent()) {
        ::Sleep(200);
    }
    ::DebugBreak();
}

// these functions are basically equivalents of methods in CCell type in Assembly-CSharp
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
inline bool CellHasFlag(const CCell& cell, CCell_Flag flag) {
    return (cell.m_flags & flag) != 0;
}
inline void CellSetFlag(CCell& cell, CCell_Flag flag, bool value) {
    cell.m_flags = value ? cell.m_flags | flag : cell.m_flags & ~flag;
}

// same as SMisc.GetIterators in Assembly-CSharp
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

// FUNCTION: 0x1120
inline void InitGridOrder() {
    const int gridSizeX = g_gridSize.x;

    if (gridSizeX + 1 > 0) {
        int i = 0;
        if (gridSizeX + 1 >= 16) {
            const uint32_t unrollRemainder = (gridSizeX + 1) - (gridSizeX + 1) % 16;
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
        for (; i < gridSizeX + 1; ++i) {
            g_gridOrder[i] = i;
        }
    }
    // Fisher-Yates shuffle algorithm
    for (int i = 10; i < gridSizeX - 9; ++i) {
        const int index = (::rand() * (gridSizeX - 19)) / RAND_MAX;
        std::swap(g_gridOrder[i], g_gridOrder[index + 10]);
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

// FUNCTION: 0x2720
inline bool SunLampLightStep(CCell* grid, const CItem_PluginData* itemsData, float x, float y) {
    CCell& cell = grid[(int)::roundf(y) + (int)::roundf(x) * g_gridSize.y];
    if (!IsCellPassable(cell, itemsData)) {
        return false;
    }
    cell.m_light.r = std::max<uint8_t>(134, cell.m_light.r);
    cell.m_light.g = std::max<uint8_t>(134, cell.m_light.g);
    cell.m_light.b = std::max<uint8_t>(134, cell.m_light.b);
    cell.m_temp = cell.m_light;
    return true;
}

// FUNCTION: 0x31a0
inline void ProcessLightPropagation(int startX, int startY, int endX, int endY) {
    CCell* const grid = g_grid;
    const int gridSizeY = g_gridSize.y;

    const auto fourthPower = [](float x) { return x * x * x * x; };

    for (int x = startX; x < endX; ++x) {
        for (int y = startY; y < endY; ++y) {
            if (x >= g_skipYMax.x && x <= g_skipYMax.width + g_skipYMax.x &&
                y >= g_skipYMax.y && y <= g_skipYMax.height + g_skipYMax.y) {
                continue;
            }
            const int gridIdx = x * gridSizeY + y;
            CCell& centerCell = grid[gridIdx];
            if (centerCell.m_temp.r == 0 && centerCell.m_temp.g == 0 && centerCell.m_temp.b == 0) {
                continue;
            }

            const int rightIdx = (x + 1) * gridSizeY + y;
            const int leftIdx = (x - 1) * gridSizeY + y;

            CCell& rightCell = grid[rightIdx];                // x+1, y
            CCell& leftCell = grid[leftIdx];                  // x-1, y
            CCell& topCell = grid[gridIdx + 1];               // x,   y+1
            CCell& bottomCell = grid[gridIdx - 1];            // x,   y-1
            const CCell& topRightCell = grid[rightIdx + 1];   // x+1, y+1
            const CCell& bottomLeftCell = grid[leftIdx - 1];  // x-1, y-1  
            const CCell& topLeftCell = grid[leftIdx + 1];     // x-1, y+1
            const CCell& bottomRightCell = grid[rightIdx - 1];// x+1, y-1

            // normalized colors from [0,256] -> [0,1]
            // 0.00390625 = 1/256

            // @ @ @
            // @ X @
            // @ @ @
            const float currentRed = centerCell.m_temp.r * 0.00390625f;
            const float currentGreen = centerCell.m_temp.g * 0.00390625f;
            const float currentBlue = centerCell.m_temp.b * 0.00390625f;

            // @ @ @
            // @ @ X
            // @ @ @
            const float rightRed = rightCell.m_temp.r * 0.00390625f;
            const float rightGreen = rightCell.m_temp.g * 0.00390625f;
            const float rightBlue = rightCell.m_temp.b * 0.00390625f;

            // @ @ @
            // X @ @
            // @ @ @
            const float leftRed = leftCell.m_temp.r * 0.00390625f;
            const float leftGreen = leftCell.m_temp.g * 0.00390625f;
            const float leftBlue = leftCell.m_temp.b * 0.00390625f;

            // @ X @
            // @ @ @
            // @ @ @
            const float topRed = topCell.m_temp.r * 0.00390625f;
            const float topGreen = topCell.m_temp.g * 0.00390625f;
            const float topBlue = topCell.m_temp.b * 0.00390625f;

            // @ @ @
            // @ @ @
            // @ X @
            const float bottomRed = bottomCell.m_temp.r * 0.00390625f;
            const float bottomGreen = bottomCell.m_temp.g * 0.00390625f;
            const float bottomBlue = bottomCell.m_temp.b * 0.00390625f;

            // @ @ X
            // @ @ @
            // @ @ @
            const float topRightRed = topRightCell.m_temp.r * 0.00390625f;
            const float topRightGreen = topRightCell.m_temp.g * 0.00390625f;
            const float topRightBlue = topRightCell.m_temp.b * 0.00390625f;

            // @ @ @
            // @ @ @
            // X @ @
            const float bottomLeftRed = bottomLeftCell.m_temp.r * 0.00390625f;
            const float bottomLeftGreen = bottomLeftCell.m_temp.g * 0.00390625f;
            const float bottomLeftBlue = bottomLeftCell.m_temp.b * 0.00390625f;

            // X @ @
            // @ @ @
            // @ @ @
            const float topLeftRed = topLeftCell.m_temp.r * 0.00390625f;
            const float topLeftGreen = topLeftCell.m_temp.g * 0.00390625f;
            const float topLeftBlue = topLeftCell.m_temp.b * 0.00390625f;

            // @ @ @
            // @ @ @
            // @ @ X
            const float bottomRightRed = bottomRightCell.m_temp.r * 0.00390625f;
            const float bottomRightGreen = bottomRightCell.m_temp.g * 0.00390625f;
            const float bottomRightBlue = bottomRightCell.m_temp.b * 0.00390625f;

            // Calculate combined electrical components using weighted averages
            // Red channel calculation
            const double redComponent = std::sqrt(std::sqrt(
                (fourthPower(rightRed) + fourthPower(currentRed) + fourthPower(leftRed) + fourthPower(topRed) + fourthPower(bottomRed) +
                 0.7f * fourthPower(topRightRed) + 0.7f * fourthPower(bottomLeftRed) + 0.7f * fourthPower(topLeftRed) + 0.7f * fourthPower(bottomRightRed)
                ) / 7.8f));

            const double greenComponent = std::sqrt(std::sqrt(
                (fourthPower(rightGreen) + fourthPower(currentGreen) + fourthPower(leftGreen) + fourthPower(topGreen) + fourthPower(bottomGreen) +
                    0.7f * fourthPower(topRightGreen) + 0.7f * fourthPower(bottomLeftGreen) + 0.7f * fourthPower(topLeftGreen) + 0.7f * fourthPower(bottomRightGreen)
                    ) / 7.8f));

            const double blueComponent = std::sqrt(std::sqrt(
                (fourthPower(rightBlue) + fourthPower(currentBlue) + fourthPower(leftBlue) + fourthPower(topBlue) + fourthPower(bottomBlue) +
                    0.7f * fourthPower(topRightBlue) + 0.7f * fourthPower(bottomLeftBlue) + 0.7f * fourthPower(topLeftBlue) + 0.7f * fourthPower(bottomRightBlue)
                    ) / 7.8f));

            // apply passability attenuation
            const float attenuation = IsCellPassable(centerCell, g_itemsData) ? 0.96f : 0.75f;

            centerCell.m_light.r = uint8_t(float(redComponent) * attenuation * 256.0f);
            centerCell.m_light.g = uint8_t(float(greenComponent) * attenuation * 256.0f);
            centerCell.m_light.b = uint8_t(float(blueComponent) * attenuation * 256.0f);

            // propagate minimal light to neighbors if above threshold
            if (float(redComponent) * attenuation > 0.005f ||
                float(greenComponent) * attenuation > 0.005f ||
                float(blueComponent) * attenuation > 0.005f) {

                rightCell.m_light.r = std::max<uint8_t>(rightCell.m_light.r, 1);
                rightCell.m_light.g = std::max<uint8_t>(rightCell.m_light.g, 1);
                rightCell.m_light.b = std::max<uint8_t>(rightCell.m_light.b, 1);

                leftCell.m_light.r = std::max<uint8_t>(leftCell.m_light.r, 1);
                leftCell.m_light.g = std::max<uint8_t>(leftCell.m_light.g, 1);
                leftCell.m_light.b = std::max<uint8_t>(leftCell.m_light.b, 1);

                topCell.m_light.r = std::max<uint8_t>(topCell.m_light.r, 1);
                topCell.m_light.g = std::max<uint8_t>(topCell.m_light.g, 1);
                topCell.m_light.b = std::max<uint8_t>(topCell.m_light.b, 1);

                bottomCell.m_light.r = std::max<uint8_t>(bottomCell.m_light.r, 1);
                bottomCell.m_light.g = std::max<uint8_t>(bottomCell.m_light.g, 1);
                bottomCell.m_light.b = std::max<uint8_t>(bottomCell.m_light.b, 1);
            }
        }
    }
}

// FUNCTION: 0x3e00
inline void ProcessFluidSimulation(int startX, int endX, int offset, int iterations) {
    const int gridHeight = g_gridSize.y;

    for (int iteration = 0; iteration < iterations; ++iteration) {
        const int currentY = (g_gridSize.y - 2 + (offset + iteration) % (g_gridSize.y - 2)) % (g_gridSize.y - 2) + 1;
        g_infiltrationTimes[currentY] = g_simuTime;
        g_lavaMovingTimes[currentY] = std::max(g_lavaMovingTimes[currentY], 1.);

        // rain logic
        if (currentY == g_yRain - 100 && g_rainMode != RainMode::NoRain) {
            for (int x = startX; x < endX; ++x) {
                CCell& cell = g_grid[x * gridHeight + g_yRain - 2];
                const float cloudFactor = std::max(0.f, 1.f - std::abs(float(x) - g_cloudCenter) / float(g_cloudRadius));
                float rainIntensity = cloudFactor * g_waterSpeed;

                if (rainIntensity < 0.0025f) {
                    rainIntensity = 0.f;
                }
                if (g_rainMode == RainMode::HeavyRain) {
                    rainIntensity = 0.015f;
                }
                if (rainIntensity > 0.f) {
                    cell.m_water = std::min(1.f, cell.m_water + rainIntensity);
                    CellSetFlag(cell, Flag_IsLava, false);
                }
            }
        }

        // fast water evaporation at underground ocean level
        if (currentY == 420) {
            for (int i = startX; i < endX; ++i) {
                CCell& cell = g_grid[i * gridHeight + 420];
                if (cell.m_water > 0.2f && (cell.m_flags & Flag_BackWall_0) == 0) {
                    cell.m_water -= 0.01f;
                }
            }
        } else if (currentY == 3 || currentY == 4 || currentY == 5) { // lava creation (for volcano)
            for (int i = startX; i < endX; ++i) {
                CCell& cell = g_grid[i * gridHeight + currentY];
                if (IsCellPassable(cell, g_itemsData)) {
                    cell.m_water = g_lavaPressure;
                    CellSetFlag(cell, Flag_IsLava, true);
                }
            }
        }
        const auto [xStart, xEnd, xStep] = g_waterSimulationDir != 0 ? std::tuple(startX, endX, 1) : std::tuple(endX - 1, startX - 1, -1);

        for (int currentX = xStart; currentX != xEnd; currentX += xStep) {
            const int cellIdx = currentX * gridHeight + currentY;
            CCell& centerCell = g_grid[cellIdx];

            float waterAmount = centerCell.m_water;
            if (waterAmount < 0.001f || !IsCellPassable(centerCell, g_itemsData)) {
                continue;
            }
            const int leftCellIdx = (currentX - 1) * gridHeight + currentY;
            const int rightCellIdx = (currentX + 1) * gridHeight + currentY;

            CCell& leftCell = g_grid[leftCellIdx];
            CCell& rightCell = g_grid[rightCellIdx];
            CCell& bottomCell = g_grid[cellIdx - 1];
            CCell& topCell = g_grid[cellIdx + 1];

            const bool centerCellHasLava = CellHasFlag(centerCell, Flag_IsLava);

            // spread lava flag to neighbors
            if (leftCell.m_water < 0.001 && IsCellPassable(leftCell, g_itemsData)) {
                CellSetFlag(leftCell, Flag_IsLava, centerCellHasLava);
            }
            if (rightCell.m_water < 0.001 && IsCellPassable(rightCell, g_itemsData)) {
                CellSetFlag(rightCell, Flag_IsLava, centerCellHasLava);
            }
            if (bottomCell.m_water < 0.001 && IsCellPassable(bottomCell, g_itemsData)) {
                CellSetFlag(bottomCell, Flag_IsLava, centerCellHasLava);
            }
            if (topCell.m_water < 0.001 && IsCellPassable(topCell, g_itemsData)) {
                CellSetFlag(topCell, Flag_IsLava, centerCellHasLava);
            }
            // lava interaction with water
            if (centerCellHasLava) {
                if (!CellHasFlag(leftCell, Flag_IsLava)
                    && leftCell.m_water > 0.001f
                    && IsCellPassable(leftCell, g_itemsData)) {
                    if (leftCell.m_water > 0.05f) {
                        if (waterAmount <= 0.3f) {
                            waterAmount = std::max(0.f, waterAmount - 0.01f);
                        } else {
                            g_lastChangedCellPos = leftCellIdx;
                        }
                    }
                    leftCell.m_water = std::max(0.f, leftCell.m_water - (waterAmount + 1.f) * 0.01f);
                }
                if (!CellHasFlag(rightCell, Flag_IsLava)
                    && rightCell.m_water > 0.001f
                    && IsCellPassable(rightCell, g_itemsData)) {
                    if (rightCell.m_water > 0.05f) {
                        if (waterAmount <= 0.3f) {
                            waterAmount = std::max(0.f, waterAmount - 0.01f);
                        } else {
                            g_lastChangedCellPos = rightCellIdx;
                        }
                    }
                    rightCell.m_water = std::max(0.f, rightCell.m_water - (waterAmount + 1.f) * 0.01f);
                }
                if (!CellHasFlag(bottomCell, Flag_IsLava)
                    && bottomCell.m_water > 0.001f
                    && IsCellPassable(bottomCell, g_itemsData)) {
                    if (bottomCell.m_water > 0.05f) {
                        if (waterAmount <= 0.3f) {
                            waterAmount = std::max(0.f, waterAmount - 0.01f);
                        } else {
                            g_lastChangedCellPos = cellIdx - 1;
                        }
                    }
                    bottomCell.m_water = std::max(0.f, bottomCell.m_water - (waterAmount + 1.f) * 0.01f);
                }
                if (!CellHasFlag(topCell, Flag_IsLava)
                    && topCell.m_water > 0.001f
                    && IsCellPassable(topCell, g_itemsData)) {
                    if (topCell.m_water > 0.05f) {
                        if (waterAmount <= 0.3f) {
                            waterAmount = std::max(0.f, waterAmount - 0.01f);
                        } else {
                            g_lastChangedCellPos = cellIdx + 1;
                        }
                    }
                    topCell.m_water = std::max(0.f, topCell.m_water - (waterAmount + 1.f) * 0.01f);
                }
            }
            if (waterAmount > 0.2f) {
                CCell& left2Cell = g_grid[(currentX - 2) * gridHeight + currentY];
                CCell& right2Cell = g_grid[(currentX + 2) * gridHeight + currentY];

                // water generator logic
                if (g_itemsData[leftCell.m_contentId].m_isWaterGenerator != 0
                    && IsCellPassable(left2Cell, g_itemsData)
                    && waterAmount > 1.2f) {

                    const float left2Water = std::max(1.f, left2Cell.m_water);
                    if (waterAmount > left2Water + 0.3f || (waterAmount > left2Water + 0.2f && int(g_simuTime) % 6 < 3)) {
                        left2Cell.m_water += 0.05f;
                        waterAmount -= 0.05f;
                        CellSetFlag(left2Cell, Flag_IsLava, centerCellHasLava);
                    }
                }
                if (g_itemsData[rightCell.m_contentId].m_isWaterGenerator != 0
                    && IsCellPassable(right2Cell, g_itemsData)
                    && waterAmount > 1.2f) {

                    const float right2Water = std::max(1.f, right2Cell.m_water);
                    if (waterAmount > right2Water + 0.3f || (waterAmount > right2Water + 0.2f && int(g_simuTime) % 6 < 3)) {
                        right2Cell.m_water += 0.05f;
                        waterAmount -= 0.05f;
                        CellSetFlag(right2Cell, Flag_IsLava, centerCellHasLava);
                    }
                }
                // water pump logic
                if (g_itemsData[leftCell.m_contentId].m_isWaterPump != 0
                    && CellHasFlag(leftCell, Flag_IsPowered)
                    && CellHasFlag(leftCell, Flag_IsXReversed)
                    && IsCellPassable(left2Cell, g_itemsData)) {

                    const float waterDiff = left2Cell.m_water - waterAmount;
                    const float pumpAmount = 0.03f / (std::max(0.f, waterDiff) * 0.74f + 1.f);
                    left2Cell.m_water += pumpAmount;
                    waterAmount -= pumpAmount;

                    CellSetFlag(left2Cell, Flag_IsLava, centerCellHasLava);
                }
                if (g_itemsData[rightCell.m_contentId].m_isWaterPump != 0
                    && CellHasFlag(rightCell, Flag_IsPowered)
                    && !CellHasFlag(rightCell, Flag_IsXReversed)
                    && IsCellPassable(right2Cell, g_itemsData)) {

                    const float waterDiff = right2Cell.m_water - waterAmount;
                    const float pumpAmount = 0.03f / (std::max(0.f, waterDiff) * 0.74f + 1.f);
                    right2Cell.m_water += pumpAmount;
                    waterAmount -= pumpAmount;

                    CellSetFlag(right2Cell, Flag_IsLava, centerCellHasLava);
                }
            }
            // horizontal flow
            const bool leftCompatible = IsCellPassable(leftCell, g_itemsData) && CellHasFlag(leftCell, Flag_IsLava) == centerCellHasLava;
            const bool rightCompatible = IsCellPassable(rightCell, g_itemsData) && CellHasFlag(rightCell, Flag_IsLava) == centerCellHasLava;

            const int compatibleCount = (leftCompatible ? 1 : 0) + (rightCompatible ? 1 : 0) + 1;
            if (compatibleCount > 1 && (!CellHasFlag(centerCell, Flag_WaterFall) || waterAmount > 0.05f)) {
                const float flowFactor = !CellHasFlag(centerCell, Flag_WaterFall) ? (centerCellHasLava ? 1.f : 0.5f) : 0.01f;
                const float baseFlow = flowFactor * ((
                    (leftCompatible ? leftCell.m_water : 0.f)
                    + waterAmount
                    + (rightCompatible ? rightCell.m_water : 0.f)
                ) / float(compatibleCount));
                waterAmount = waterAmount * (1.f - flowFactor) + baseFlow;

                if (leftCompatible) {
                    leftCell.m_water = leftCell.m_water * (1.f - flowFactor) + baseFlow;
                }
                if (rightCompatible) {
                    rightCell.m_water = rightCell.m_water * (1.f - flowFactor) + baseFlow;
                }
            }
            // vertical flow
            const bool bottomCompatible = IsCellPassable(bottomCell, g_itemsData) && CellHasFlag(bottomCell, Flag_IsLava) == centerCellHasLava;
            const bool topCompatible = IsCellPassable(topCell, g_itemsData) && CellHasFlag(topCell, Flag_IsLava) == centerCellHasLava;

            if (bottomCompatible && topCompatible) {
                float totalWater = bottomCell.m_water + waterAmount + topCell.m_water;

                if (totalWater <= 3.3f) {
                    if (totalWater <= 2.1f) {
                        if (totalWater > 1.f) {
                            topCell.m_water = 0.f;
                            waterAmount = (totalWater - 1.f) * 10.f / 11.f;
                            bottomCell.m_water = (totalWater - 1.f) / 11.f + 1.f;
                        }
                    } else {
                        const float baseWater = (totalWater - 2.1f) / 12.f;
                        topCell.m_water = (totalWater - 2.1f) * 10.f / 12.f;
                        waterAmount = baseWater + 1.f;
                        bottomCell.m_water = baseWater + 1.1f;
                    }
                } else {
                    waterAmount = totalWater / 3.f;

                    CCell& bottom2Cell = g_grid[cellIdx - 2];
                    CCell& bottom3Cell = g_grid[cellIdx - 3];
                    CCell& bottom4Cell = g_grid[cellIdx - 4];
                    CCell& top2Cell = g_grid[cellIdx + 2];
                    CCell& top3Cell = g_grid[cellIdx + 3];
                    CCell& top4Cell = g_grid[cellIdx + 4];

                    if (IsCellPassable(bottom2Cell, g_itemsData) && CellHasFlag(bottom2Cell, Flag_IsLava) == centerCellHasLava
                        && IsCellPassable(top2Cell, g_itemsData) && CellHasFlag(top2Cell, Flag_IsLava) == centerCellHasLava
                        && IsCellPassable(bottom3Cell, g_itemsData) && CellHasFlag(bottom3Cell, Flag_IsLava) == centerCellHasLava
                        && IsCellPassable(top3Cell, g_itemsData) && CellHasFlag(top3Cell, Flag_IsLava) == centerCellHasLava
                        && IsCellPassable(bottom4Cell, g_itemsData) && CellHasFlag(bottom4Cell, Flag_IsLava) == centerCellHasLava
                        && IsCellPassable(top4Cell, g_itemsData) && CellHasFlag(top4Cell, Flag_IsLava) == centerCellHasLava)
                    {
                        totalWater += bottom2Cell.m_water + bottom3Cell.m_water + bottom4Cell.m_water
                                      + top2Cell.m_water + top3Cell.m_water + top4Cell.m_water;

                        if (totalWater > 12.6f) {
                            waterAmount = totalWater / 9.f;
                            if (std::abs((top4Cell.m_water - waterAmount) + 0.4f) > 0.02f) {
                                if (IsCellPassable(centerCell, g_itemsData) && centerCellHasLava) {
                                    g_lavaMovingTimes[currentY] = g_simuTime;
                                }
                                top4Cell.m_water = waterAmount - 0.4f;
                                top3Cell.m_water = waterAmount - 0.3f;
                                top2Cell.m_water = waterAmount - 0.2f;
                                bottom2Cell.m_water = waterAmount + 0.2f;
                                bottom3Cell.m_water = waterAmount + 0.3f;
                                bottom4Cell.m_water = waterAmount + 0.4f;
                            }
                        }
                    }
                    if (std::abs((topCell.m_water - waterAmount) + 0.1f) > 0.02f
                        && IsCellPassable(centerCell, g_itemsData) && centerCellHasLava) {
                        g_lavaMovingTimes[currentY] = g_simuTime;
                    }
                    topCell.m_water = waterAmount - 0.1f;
                    bottomCell.m_water = waterAmount + 0.1f;
                }
            } else if (topCompatible) {
                const float totalWater = topCell.m_water + waterAmount;
                if (totalWater > 1.f) {
                    if (totalWater <= 2.1f) {
                        topCell.m_water = (totalWater - 1.f) * 10.f / 11.f;
                        waterAmount = (totalWater - 1.f) / 11.f + 1.f;
                    } else {
                        waterAmount = totalWater * 0.5f + 0.05f;
                        topCell.m_water = totalWater * 0.5f - 0.05f;
                    }
                }
            } else if (bottomCompatible) {
                const float totalWater = bottomCell.m_water + waterAmount;
                if (totalWater > 1.f) {
                    if (totalWater <= 2.1f) {
                        waterAmount = (totalWater - 1.f) * 10.f / 11.f;
                        bottomCell.m_water = (totalWater - 1.f) / 11.f + 1.f;
                    } else {
                        bottomCell.m_water = totalWater * 0.5f + 0.05f;
                        waterAmount = totalWater * 0.5f - 0.05f;
                    }
                }
            }
            // waterfall flag marking
            bool hasWaterfall = false;
            if (bottomCompatible) {
                const float maxTransfer = std::min(1.f - bottomCell.m_water, std::min(waterAmount * 0.7f + 0.005f, waterAmount));
                if (maxTransfer > 0.001f) {
                    if (maxTransfer >= waterAmount * 0.4f && bottomCell.m_water < 0.9f && centerCell.m_water < 0.9f) {
                        hasWaterfall = true;
                    }
                    bottomCell.m_water += maxTransfer;
                    waterAmount -= maxTransfer;
                }
                if (waterAmount < 0.1f && bottomCell.m_water < 1.f) {
                    hasWaterfall = true;
                }
            }
            // general water evaporation
            if (waterAmount > 0.0002f && waterAmount < 1.01f) {
                const float lightFactor = (centerCell.m_light.r + centerCell.m_light.g + centerCell.m_light.b) / 768.f;
                waterAmount -= lightFactor * 0.0002f;

                if (currentY < g_fastEvaporationYMax && waterAmount > 0.002f) {
                    waterAmount -= 0.003f;
                }
            }

            if (centerCell.m_water != waterAmount) {
                centerCell.m_water = waterAmount;
            }
            if (CellHasFlag(centerCell, Flag_WaterFall) != hasWaterfall) {
                CellSetFlag(centerCell, Flag_WaterFall, hasWaterfall);
            }
        }
    }
}

// FUNCTION: 0x5150
inline void ProcessWaterSeepage(int offset, int iterations) {
    const int gridSizeX = g_gridSize.x;
    const int gridSizeY = g_gridSize.y;

    for (int iteration = 0; iteration < iterations; ++iteration) {
        const int gridOrderIndex = (iteration + offset) % (gridSizeX - 2);
        const int x = g_gridOrder[(gridOrderIndex + (gridOrderIndex & 1 ? 4 : 0)) % (gridSizeX - 2) + 1];

        for (int y = 2; y < gridSizeY - 2; ++y) {
            const int cellIdx = x * gridSizeY + y;
            CCell& centerCell = g_grid[cellIdx];

            float waterAmount = centerCell.m_water;
            if (waterAmount >= 0.0005f || waterAmount == 0.f) {
                if (waterAmount < 0.001f) {
                    CellSetFlag(centerCell, Flag_IsLava, false);
                }
            } else {
                waterAmount = 0.f;
                centerCell.m_water = 0.f;
                CellSetFlag(centerCell, Flag_IsLava, false);
            }
            if (waterAmount <= 0.001f || CellHasFlag(centerCell, Flag_IsLava)) {
                continue;
            }
            const int leftCellIdx = (x - 1) * gridSizeY + y;
            const int rightCellIdx = (x + 1) * gridSizeY + y;

            CCell& leftCell = g_grid[leftCellIdx];
            CCell& rightCell = g_grid[rightCellIdx];
            CCell& topCell = g_grid[cellIdx + 1];
            CCell& bottomCell = g_grid[cellIdx - 1];

            if (waterAmount <= 0.2f
                && g_itemsData[centerCell.m_contentId].m_isDirt != 0
                && IsCellPassable(topCell, g_itemsData)) {
                continue;
            }
            float waterDiffBottom = -1.f;
            if (g_itemsData[bottomCell.m_contentId].m_isDirt != 0 && bottomCell.m_water < 0.3f) {
                waterDiffBottom = waterAmount - bottomCell.m_water;
            }
            if (!IsCellPassable(centerCell, g_itemsData) && IsCellPassable(bottomCell, g_itemsData)) {
                waterDiffBottom = waterAmount;
            }

            float waterDiffTop = -1.f;
            if (g_itemsData[topCell.m_contentId].m_isDirt != 0 && topCell.m_water < 0.3f) {
                waterDiffTop = waterAmount - topCell.m_water;
            }
            float waterDiffLeft = -1.f;
            if (g_itemsData[leftCell.m_contentId].m_isDirt != 0 && leftCell.m_water < 0.3f) {
                waterDiffLeft = waterAmount - leftCell.m_water;
            }
            float waterDiffRight = -1.f;
            if (g_itemsData[rightCell.m_contentId].m_isDirt != 0 && rightCell.m_water < 0.3f) {
                waterDiffRight = waterAmount - rightCell.m_water;
            }

            float bottomFlow = std::min(waterDiffBottom * 0.1f, 0.65f);
            if (IsCellPassable(bottomCell, g_itemsData)) {
                bottomFlow = std::max(std::min(waterDiffBottom, 0.01f), waterDiffBottom * 0.1f);
            }
            if (waterDiffBottom > 0.f && bottomFlow <= waterDiffBottom && bottomCell.m_water + bottomFlow < 1.f) {
                bottomCell.m_water += bottomFlow;
                waterAmount -= bottomFlow;
            }
            const float leftFlow = waterDiffLeft * 0.04f;
            if (waterDiffLeft > 0.f && leftFlow <= waterDiffLeft && leftCell.m_water + leftFlow < 1.f) {
                leftCell.m_water += leftFlow;
                waterAmount -= leftFlow;
            }
            const float rightFlow = waterDiffRight * 0.04f;
            if (waterDiffRight > 0.f && rightFlow <= waterDiffRight && rightCell.m_water + rightFlow < 1.f) {
                rightCell.m_water += rightFlow;
                waterAmount -= rightFlow;
            }
            const float topFlow = waterDiffTop * 0.02f;
            if (waterDiffTop > 0.f && topFlow <= waterDiffTop && topCell.m_water + topFlow < 1.f) {
                topCell.m_water += topFlow;
                waterAmount -= topFlow;
            }
            centerCell.m_water = waterAmount;
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
        if (threadData.processFluidSimulation /*0x20*/) {
            threadData.processFluidSimulation /*0x20*/ = false;
            ProcessFluidSimulation(threadData.fluidSimulationStartX, threadData.fluidSimulationEndX, g_fluidSimulationOffset, g_fluidSimulationIterations);
        } else if (threadData.processWaterSeepage /*0x2C*/) {
            threadData.processWaterSeepage /*0x2C*/ = false;
            ProcessWaterSeepage(threadData.waterSeepageOffset, threadData.waterSeepageIterations);
        } else if (threadData.processCellLighting /*0xD*/) {
            threadData.processCellLighting /*0xD*/ = false;
            ProcessLightPropagation(threadData.startX, threadData.startY, threadData.endX, threadData.endY);
        }
        ::SetEvent(g_threadEvents[threadData.id /*0x4*/]);
        ::WaitForSingleObject(threadData.workEvent /*0x8*/, 1000);
    }
}

// FUNCTION: 0x1750
inline void UpdateCellPowerState(
    CCell* const grid, const CItem_PluginData* const itemsData, int x, int y, uint32_t direction, const int& totalProduction, int& remainingPowerBudget
) {
    CCell& currentCell = grid[x * g_gridSize.y + y];
    const CCell& rightCell = grid[(x + 1) * g_gridSize.y + y];
    const CCell& topCell = grid[x * g_gridSize.y + (y + 1)];
    const CCell& topRightCell = grid[(x + 1) * g_gridSize.y + (y + 1)];

    // in orig code the CItem_PluginData struct is copied
    const CItem_PluginData& itemData = itemsData[currentCell.m_contentId];

    if (itemData.m_electricValue > -1) {
        return;
    }
    if ((direction & itemData.m_electricOutletFlags) == 0) {
        return;
    }
    bool shouldPowerCell = false;
    // true for items: elecLight, elecAlarm, wallDoor and wallCompositeDoor
    if (itemData.m_electricValue == -255) {
        if (totalProduction != 0 || remainingPowerBudget != 0) {
            shouldPowerCell = true;
        }
    } else {
        if (remainingPowerBudget + itemData.m_electricValue >= 0 && remainingPowerBudget != 255) {
            remainingPowerBudget += itemData.m_electricValue;
            shouldPowerCell = true;
        }
    }
    if (itemData.m_isBlockDoor != 0) {
        bool hasConnectedWire =
            (currentCell.m_flags & (Flag_HasWireTop | Flag_HasWireRight)) != 0 ||
            (topCell.m_flags & Flag_HasWireRight) != 0 ||
            (rightCell.m_flags & Flag_HasWireTop) != 0;
        bool hasConnectedWireCrossing =
            itemsData[topCell.m_contentId].m_elecSwitchType == ElecSwitchType::ElecCross ||
            itemsData[rightCell.m_contentId].m_elecSwitchType == ElecSwitchType::ElecCross ||
            itemsData[topRightCell.m_contentId].m_elecSwitchType == ElecSwitchType::ElecCross;

        if (hasConnectedWire || hasConnectedWireCrossing) {
            CellSetFlag(currentCell, Flag_CustomData0, currentCell.m_elecProd != 0);
        }
    }
    CellSetFlag(currentCell, Flag_IsPowered, shouldPowerCell);
}

// FUNCTION: 0x1670
inline void UpdateCellElectricity(CCell* const grid, const CItem_PluginData* const itemsData, int x, int y, uint32_t direction, int& totalConsumption, int& totalProduction) {
    CCell& currentCell = grid[x * g_gridSize.y + y];

    // in orig code the CItem_PluginData struct is copied
    const CItem_PluginData& itemData = itemsData[currentCell.m_contentId];

    if ((direction & itemData.m_electricOutletFlags) == 0) {
        return;
    }
    if (itemData.m_electricValue < 0) {
        if (itemData.m_electricValue == -255) {
            if (totalConsumption == 0) {
                totalConsumption = 255;
            }
        } else {
            if (totalConsumption != 255) {
                totalConsumption += -itemData.m_electricValue;
            }
        }
    } else if (itemData.m_electricValue > 0) {
        const int electricityProd = (*g_callbackGetElecProd)(x, y);
        CellSetFlag(currentCell, Flag_IsPowered, electricityProd != 0);

        if (electricityProd == 255) {
            if (totalProduction == 0) {
                totalProduction = 255;
            }
        } else if (electricityProd != 0) {
            totalProduction = std::min(250, (totalProduction == 255 ? 0 : totalProduction) + electricityProd);
        }
    }
}

// FUNCTION: 0x1860
inline void PropagateElectricity(CCell* const grid, const CItem_PluginData* const itemsData, int startX, int startY) {
    const int gridSizeY = g_gridSize.y;

    g_elecProcessedCells.clear();

    g_elecPropagationQueue.clear();
    g_elecPropagationQueue.emplace_back(startX, startY);

    CCell& startCell = grid[startX * gridSizeY + startY];
    CellSetFlag(startCell, Flag_ElectricAlgoState, g_elecAlgoState != 0);

    int totalConsumption = 0;
    int totalProduction = 0;
    int iterationCount = 0;

    while (!g_elecPropagationQueue.empty() && iterationCount < 10'000) {
        iterationCount += 1;

        const short2 currentCoord = g_elecPropagationQueue.back();
        g_elecPropagationQueue.pop_back();

        const int x = currentCoord.x;
        const int y = currentCoord.y;

        g_elecProcessedCells.push_back(currentCoord);

        CCell& currentCell = grid[x * gridSizeY + y];
        CCell& topCell = grid[x * gridSizeY + (y + 1)];
        const CCell& rightCell = grid[(x + 1) * gridSizeY + y];
        const CCell& leftCell = grid[(x - 1) * gridSizeY + y];
        const CCell& topRightCell = grid[(x + 1) * gridSizeY + (y + 1)];
        const CCell& bottomLeftCell = grid[(x - 1) * gridSizeY + (y - 1)];

        UpdateCellElectricity(grid, itemsData, x, y, 2, totalConsumption, totalProduction);
        UpdateCellElectricity(grid, itemsData, x, y + 1, 1, totalConsumption, totalProduction);

        for (int dir = 0; dir < 8; ++dir) {
            // neighbor pos based on dir:
            // 3 2 1
            // 4 # 0
            // 5 6 7

            const short neighborX = x + g_elecDirOffsetsX[dir * 2];
            const short neighborY = y + g_elecDirOffsetsY[dir * 2];

            CCell& neighborCell = grid[neighborX * gridSizeY + neighborY];

            if (int(CellHasFlag(neighborCell, Flag_ElectricAlgoState)) == g_elecAlgoState) {
                continue;
            }
            // 0, 4 - general electricity propagation
            // 2, 6 - handles relays and switches (toggle and push)
            // 1, 3, 5, 7 - handles wire crossings
            if (dir == 0) {
                if (!CellHasFlag(rightCell, Flag_HasWireTop)) {
                    continue;
                }
            } else if (dir == 1) {
                if (itemsData[topRightCell.m_contentId].m_elecSwitchType != ElecSwitchType::ElecCross) {
                    continue;
                }
            } else if (dir == 2) {
                if (!CellHasFlag(topCell, Flag_HasWireRight)) {
                    if (itemsData[topCell.m_contentId].m_elecSwitchType == ElecSwitchType::ElecSwitchRelay) {
                        if (leftCell.m_elecProd == 0) { continue; }
                    } else if (itemsData[topCell.m_contentId].m_elecSwitchType >= ElecSwitchType::ElecSwitch) {
                        if (!CellHasFlag(topCell, Flag_CustomData0)) { continue; }
                    } else {
                        continue;
                    }
                }
            } else if (dir == 3) {
                if (itemsData[topCell.m_contentId].m_elecSwitchType != ElecSwitchType::ElecCross) {
                    continue;
                }
            } else if (dir == 4) {
                if (!CellHasFlag(currentCell, Flag_HasWireTop)) {
                    continue;
                }
            } else if (dir == 5) {
                if (itemsData[currentCell.m_contentId].m_elecSwitchType != ElecSwitchType::ElecCross) {
                    continue;
                }
            } else if (dir == 6) {
                if (!CellHasFlag(currentCell, Flag_HasWireRight)) {
                    if (itemsData[currentCell.m_contentId].m_elecSwitchType == ElecSwitchType::ElecSwitchRelay) {
                        if (bottomLeftCell.m_elecProd == 0) { continue; }
                    } else if (itemsData[currentCell.m_contentId].m_elecSwitchType >= ElecSwitchType::ElecSwitch) {
                        if (!CellHasFlag(currentCell, Flag_CustomData0)) { continue; }
                    } else {
                        continue;
                    }
                }
            } else if (dir == 7) {
                if (itemsData[rightCell.m_contentId].m_elecSwitchType != ElecSwitchType::ElecCross) {
                    continue;
                }
            } else {
                continue;
            }
            g_elecPropagationQueue.emplace_back(neighborX, neighborY);
            CellSetFlag(neighborCell, Flag_ElectricAlgoState, g_elecAlgoState != 0);

            if (dir == 2) {
                if (itemsData[topCell.m_contentId].m_elecSwitchType == ElecSwitchType::ElecSwitchPush && CellHasFlag(topCell, Flag_CustomData0)) {
                    CellSetFlag(topCell, Flag_CustomData0, false);
                }
            } else if (dir == 6) {
                if (itemsData[currentCell.m_contentId].m_elecSwitchType == ElecSwitchType::ElecSwitchPush && CellHasFlag(currentCell, Flag_CustomData0)) {
                    CellSetFlag(currentCell, Flag_CustomData0, false);
                }
            }
        }
    }
    const int processedStart = (::rand() * g_elecProcessedCells.size()) / RAND_MAX;
    int remainingPowerBudget = totalProduction;
    for (int i = 0; i < g_elecProcessedCells.size(); ++i) {
        const short2 pos = g_elecProcessedCells[(processedStart + i) % g_elecProcessedCells.size()];
            
        CCell& cell = grid[pos.x * gridSizeY + pos.y];
        cell.m_elecCons = uint8_t(totalConsumption);
        cell.m_elecProd = uint8_t(totalProduction);

        UpdateCellPowerState(grid, itemsData, pos.x, pos.y, 2, totalProduction, remainingPowerBudget);
        UpdateCellPowerState(grid, itemsData, pos.x, pos.y + 1, 1, totalProduction, remainingPowerBudget);
    }
}
