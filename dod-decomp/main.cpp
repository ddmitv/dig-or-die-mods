#include <stdio.h>
#include <cstdarg>
#include <process.h>
#include <stdlib.h>
#include <time.h>
#include <iterator>
#include <cmath>
#include <algorithm>

#include "types.hpp"
#include "vars.hpp"
#include "funcs.hpp"

extern "C" {

// FUNCTION: 0x1470
__declspec(dllexport) void DllSetCallbacks(DelegateCallbackDebug* fp1, DelegateCallbackGetElecProd* fp2) {
    g_callbackDebug = fp1;
    g_callbackGetElecProd = fp2;
}
// FUNCTION: 0x13b0
__declspec(dllexport) void DllClose() {
    g_callbackDebug("- Stopping threads...");

    for (int i = 0; i < g_nbThreads; ++i) {
        g_threadData[i].shouldExit = true;
        ::SetEvent(g_threadData[i].workEvent);
    }
    ::Sleep(100);
    for (int i = 0; i < g_nbThreads; ++i) {
        DWORD res = ::WaitForSingleObject(g_threadData[i].handle, 1000);
        if (res != WAIT_OBJECT_0) {
            g_callbackDebug("- Error stopping thread.");
        }
        ::CloseHandle(g_threadData[i].handle);
        ::CloseHandle(g_threadData[i].workEvent);
        ::CloseHandle(g_threadEvents[i]);
    }
    g_callbackDebug("- Threads stopped.");
}
// FUNCTION: 0x1470
__declspec(dllexport) uint32_t DllGetSaveOffset(int32_t build, int32_t x) {
    if (build < 198) {
        return 0;
    }
    uint32_t hash = x * 0x9e3779b1U; // Golden Ratio's fractional part
    hash = ((hash >> 15) ^ hash) * 0x85ebca77U; // Some prime number
    hash = ((hash >> 13) ^ hash) * 0xc2b2ae3dU; // Some prime number too
    hash = (hash >> 16) ^ hash;

    // ensures that return value is between [0, 10]
    uint64_t product = (uint64_t)hash * 10ULL;
    return (uint32_t)(product / 0xFFFFFFFFULL);
}
// FUNCTION: 0x1270
__declspec(dllexport) void DllInit(int nbThreads) {
    // WaitForDebugger();
    // nbThreads = 1;

    DebugLogFormat(g_formatBuffer, "- Dll initialisation. Creating %d threads...", nbThreads);
    g_callbackDebug(g_formatBuffer);

    if (nbThreads > 32) { nbThreads = 32; }
    g_nbThreads = nbThreads;

    ThreadData* threadData = g_threadData;
    for (int i = 0; i < nbThreads; ++i, ++threadData) {
        g_threadEvents[i] = ::CreateEventW(nullptr, false, false, nullptr);

        threadData->id = i;
        threadData->shouldExit = 0;

        threadData->workEvent = ::CreateEventW(nullptr, false, false, nullptr);

        uintptr_t hThread = ::_beginthreadex(
            nullptr, 0, &WorkerThread, threadData, 0, nullptr
        );
        threadData->handle = reinterpret_cast<HANDLE>(hThread);
        if (hThread == 0) {
            DebugLogFormat(g_formatBuffer, "- Error creating thread %d.", i);
            g_callbackDebug(g_formatBuffer);
        }
    }
    g_callbackDebug("- Dll successfuly initialized.");
    ::srand(uint32_t(::_time64(nullptr)));
}
// FUNCTION: 0x1cb0
__declspec(dllexport) int DllProcessElectricity(
    CCell* grid, CItem_PluginData* itemsData, double simuTime, double simuDeltaTime
) {
    // g_callbackDebug("dll: inside DllProcessElectricity");

    const int gridSizeY = g_gridSize.y;
    const int gridSizeX = g_gridSize.x;

    int startOffset = 0;
    int numIterations = 0;
    GetIterators(gridSizeX - 2, simuTime, simuDeltaTime, 1.f/3.f, startOffset, numIterations);

    for (int i = 0; i < numIterations; ++i) {
        // since (i + startOffset) is always >= 0, the expression can be simplied to just taking a modulo:
        // const int x = ((i + startOffset) % (gridSizeX - 2) + (gridSizeX - 2)) % (gridSizeX - 2) + 1;
        const int x = (i + startOffset) % (gridSizeX - 2) + 1;
        if (x == 1) {
            g_elecAlgoState = !g_elecAlgoState;
        }
        for (int y = 1; y < gridSizeY - 1; ++y) {
            CCell& currentCell = grid[x * gridSizeY + y];
            const CCell& topCell = grid[x * gridSizeY + (y + 1)];
            const CCell& rightCell = grid[(x + 1) * gridSizeY + y];
            const CCell& topRightCell = grid[(x + 1) * gridSizeY + (y + 1)];

            if (int(CellHasFlag(currentCell, Flag_ElectricAlgoState)) != g_elecAlgoState) {
                currentCell.m_elecProd = 0;
                if (CellHasFlag(currentCell, Flag_HasWireTop) || CellHasFlag(currentCell, Flag_HasWireRight)
                    || CellHasFlag(topCell, Flag_HasWireRight)
                    || CellHasFlag(rightCell, Flag_HasWireTop)
                    || (itemsData[topCell.m_contentId].m_electricValue != 0 && (itemsData[topCell.m_contentId].m_electricOutletFlags & 1) != 0)
                    || (itemsData[currentCell.m_contentId].m_electricValue != 0 && (itemsData[currentCell.m_contentId].m_electricOutletFlags & 2) != 0)
                    || itemsData[topCell.m_contentId].m_elecSwitchType > ElecSwitchType::None
                    || itemsData[topRightCell.m_contentId].m_elecSwitchType == ElecSwitchType::ElecCross
                    ) {
                    PropagateElectricity(grid, itemsData, x, y);
                }
            }
        }
    }
    return 0;
}
// FUNCTION: 0x2240
__declspec(dllexport) int DllProcessForces(
    CCell* grid, CItem_PluginData* itemsData, int* cellsWithForces, int nbCells, float weightMult
) {
    // g_callbackDebug("dll: inside DllProcessForces");

    for (int i = 0; i < nbCells; ++i) {
        const int cellIdx = cellsWithForces[i];
        if (cellIdx == INT_MIN) { continue; }

        const int topCellIdx = cellIdx + g_gridSize.y;
        grid[topCellIdx].m_forceX  = int16_t(::roundf((float)grid[topCellIdx].m_forceX * 0.995f));
        grid[cellIdx].m_forceX     = int16_t(::roundf((float)grid[cellIdx].m_forceX * 0.995f));
        grid[cellIdx + 1].m_forceY = int16_t(::roundf((float)grid[cellIdx + 1].m_forceY * 0.995f));
        grid[cellIdx].m_forceY     = int16_t(::roundf((float)grid[cellIdx].m_forceY * 0.995f));
    }

    for (int iterationCount = 20; iterationCount != 0; --iterationCount) {
        for (int i = 0; i < nbCells; ++i) {
            const int cellIdx = cellsWithForces[i];
            if (cellIdx == INT_MIN) { continue; }

            CCell& centerCell = grid[cellIdx];
            CCell& bottomCell = grid[cellIdx - g_gridSize.y];
            CCell& topCell = grid[cellIdx + g_gridSize.y];
            CCell& leftCell = grid[cellIdx - 1];
            CCell& rightCell = grid[cellIdx + 1];

            float excessWaterTop = 0;
            float excessWaterBottom = 0;
            float excessWaterLeft = 0;
            float excessWaterRight = 0;

            if (!IsCellPassable(centerCell, itemsData)) {
                if (IsCellPassable(topCell, itemsData) && topCell.m_water > 1.f) {
                    excessWaterTop = topCell.m_water - 1.f;
                }
                if (IsCellPassable(bottomCell, itemsData) && bottomCell.m_water > 1.f) {
                    excessWaterBottom = bottomCell.m_water - 1.f;
                }
                if (IsCellPassable(leftCell, itemsData) && leftCell.m_water > 1.f) {
                    excessWaterLeft = leftCell.m_water - 1.f;
                }
                if (IsCellPassable(rightCell, itemsData) && rightCell.m_water > 1.f) {
                    excessWaterRight = rightCell.m_water - 1.f;
                }
            }
            const bool isReceivingForcesTop = itemsData[topCell.m_contentId].m_isReceivingForces != 0;
            const bool isReceivingForcesBottom = itemsData[bottomCell.m_contentId].m_isReceivingForces != 0;
            const bool isReceivingForcesLeft = itemsData[leftCell.m_contentId].m_isReceivingForces != 0;
            const bool isReceivingForcesRight = itemsData[rightCell.m_contentId].m_isReceivingForces != 0;

            const float forceTop = isReceivingForcesTop ? topCell.m_forceX : 0.f;
            const float forceBottom = isReceivingForcesBottom ? -centerCell.m_forceX : 0.f;
            const float forceLeft = isReceivingForcesLeft ? -centerCell.m_forceY : 0.f;
            const float forceRight = isReceivingForcesRight ? rightCell.m_forceY : 0.f;

            const float maxExcessDiff = std::max(
                ::fabsf(excessWaterTop - excessWaterBottom),
                ::fabsf(excessWaterLeft - excessWaterRight)
            );
            const int receivingForceNumDirections = int(isReceivingForcesTop) + int(isReceivingForcesBottom) +
                                                    int(isReceivingForcesLeft) + int(isReceivingForcesRight);

            const int16_t forceAdjustment = (
                (
                    ( (2 - (isReceivingForcesLeft ? 1 : 0)) * itemsData[centerCell.m_contentId].m_weight
                    + maxExcessDiff * 512.f ) * weightMult
                    - (forceTop + forceBottom + forceRight + forceLeft)
                ) / float(receivingForceNumDirections)
            );

            if (isReceivingForcesTop) {
                topCell.m_forceX += forceAdjustment;
            }
            if (isReceivingForcesBottom) {
                centerCell.m_forceX -= forceAdjustment;
            }
            if (isReceivingForcesRight) {
                rightCell.m_forceY += forceAdjustment;
            }
            if (isReceivingForcesLeft) {
                centerCell.m_forceY -= forceAdjustment;
            }
        }
    }
    return 1;
}
// FUNCTION: 0x27f0
__declspec(dllexport) int DllProcessLightingSquare(
    CCell* const grid, CItem_PluginData* const itemsData,
    int2 posMin, int2 posMax, float sunlight, const RectInt skipYMax,
    int sunLightYMin, int sunLightYMax
) {
    // g_callbackDebug("dll: inside DllProcessLightingSquFare");

    g_grid = grid;
    g_itemsData = itemsData;
    g_skipYMax = skipYMax;
    const int startY = std::clamp(posMin.y - 1, 1, g_gridSize.y - 2);
    const int startX = std::clamp(posMin.x - 1, 1, g_gridSize.x - 2);
    const int endY = std::clamp(posMax.y + 2, 1, g_gridSize.y - 2);
    const int endX = std::clamp(posMax.x + 1, 1, g_gridSize.x - 2);
    const float heightRange = sunLightYMax == sunLightYMin ? 1 : sunLightYMax - sunLightYMin;

    for (int x = startX; x < endX; ++x) {
        const int baseIndex = x * g_gridSize.y;
        for (int y = startY; y < endY; ++y) {
            if (x >= skipYMax.x && x <= skipYMax.x + skipYMax.width &&
                y >= skipYMax.y && y <= skipYMax.y + skipYMax.height) {
                continue;
            }
            const int cellIdx = baseIndex + y;
            CCell& cell = grid[cellIdx];

            if (IsCellPassable(cell, itemsData)
                && (cell.m_flags & (Flag_BgSurface_2 | Flag_BgSurface_1 | Flag_BgSurface_0)) == 0
                && y > sunLightYMin) {
                const float heightOffset = y - sunLightYMin;

                const float waterFactor = std::max(0.f, 1.f - cell.m_water);
                const float heightFactor = std::min(1.0f, heightOffset / heightRange);
                const uint8_t light = uint8_t(waterFactor * sunlight * heightFactor);

                cell.m_light.r = std::max(cell.m_light.r, light);
                cell.m_light.g = std::max(cell.m_light.g, light);
                cell.m_light.b = std::max(cell.m_light.b, light);
            }
            if ((cell.m_flags & Flag_IsLava) != 0 && cell.m_water > 0.01f) {
                const float waterBasedRed = cell.m_water * 50.f * 255.f;

                if (cell.m_light.r <= waterBasedRed) {
                    const double timePhase = std::cos(x) * 6.0 + g_simuTime * 5.0;
                    const double combinedNoise = std::cos(std::cos(y) * 6.0 + timePhase);
                    const double noiseRedValue = 255.0 - (combinedNoise * 0.5 + 0.5) * 100.0;

                    cell.m_light.r = uint8_t(std::min<double>(noiseRedValue, waterBasedRed));
                }
            }
            if ((cell.m_flags & Flag_IsBurning) != 0) {
                cell.m_light.r = 0xff;
                cell.m_light.g = std::max<uint8_t>(cell.m_light.g, 192);
                // the code in disassembly seems to do no-op with blue color
                // possible the correct statement is `grid[iVar5].m_light.b = 0` ?
            }
            const CItem_PluginData& itemData = itemsData[cell.m_contentId];
            if ((itemData.m_light.r != 0 || itemData.m_light.g != 0 || itemData.m_light.b != 0) && (itemData.m_electricValue > -1 || (cell.m_flags & Flag_IsPowered) != 0)) {
                cell.m_light.r = std::max(itemData.m_light.r, cell.m_light.r);
                cell.m_light.g = std::max(itemData.m_light.g, cell.m_light.g);
                cell.m_light.b = std::max(itemData.m_light.b, cell.m_light.b);

                if (itemData.m_isLightonium != 0) {
                    CCell& prevCell = grid[cellIdx - 1];
                    prevCell.m_light = { .r = 255, .g = 255, .b = 255 };
                    prevCell.m_temp = { .r = 255, .g = 255, .b = 255 };

                    CCell& prevPrevCell = grid[cellIdx - 2];
                    prevPrevCell.m_light = { .r = 255, .g = 255, .b = 255 };
                    prevPrevCell.m_temp = { .r = 255, .g = 255, .b = 255 };
                } else if (itemData.m_isSunLamp != 0) {
                    float offset = 0.0f;
                    do {
                        if (!SunLampLightStep(grid, itemsData, x - offset * 0.26f, y - offset)) {
                            break;
                        }
                        offset += 1.f;
                    } while (offset < 7.0f);
                    offset = 0.f;
                    do {
                        if (!SunLampLightStep(grid, itemsData, x + offset * 0.26f, y - offset)) {
                            break;
                        }
                        offset += 1.f;
                    } while (offset < 7.0f);
                    offset = 0.f;
                    do {
                        if (!SunLampLightStep(grid, itemsData, x + offset, y - offset * 0.35f)) {
                            break;
                        }
                        offset += 1.f;
                    } while (offset < 11.0f);
                    offset = 0.f;
                    do {
                        if (!SunLampLightStep(grid, itemsData, x + offset, y - offset * 0.65f)) {
                            break;
                        }
                        offset += 1.f;
                    } while (offset < 11.0f);
                    offset = 0.f;
                    do {
                        if (!SunLampLightStep(grid, itemsData, x - offset, y - offset * 0.35f)) {
                            break;
                        }
                        offset += 1.f;
                    } while (offset < 11.0f);
                    offset = 0.f;
                    do {
                        if (!SunLampLightStep(grid, itemsData, x - offset, y - offset * 0.65f)) {
                            break;
                        }
                        offset += 1.f;
                    } while (offset < 11.0f);
                } else if (itemData.m_isOrganicHeart != 0) {
                    const double cosValue = std::cos(::clock() * 0.00628 * 0.3 * 2);
                    const float pulseMultiplier = float((cosValue * 0.5 + 0.5) * 1.5 + 1.0);

                    cell.m_light.r = std::max(cell.m_light.r, uint8_t(itemData.m_light.r * pulseMultiplier));
                    cell.m_light.g = std::max(cell.m_light.g, uint8_t(itemData.m_light.g * pulseMultiplier));
                    cell.m_light.b = std::max(cell.m_light.b, uint8_t(itemData.m_light.b * pulseMultiplier));
                }
            }
            cell.m_temp = cell.m_light;
        }
    }
    const int processStartY = std::clamp(posMin.y, 1, g_gridSize.y - 2);
    const int processStartX = std::clamp(posMin.x, 1, g_gridSize.x - 2);
    const int processEndY = std::clamp(posMax.y + 1, 1, g_gridSize.y - 2);
    const int processEndX = std::clamp(posMax.x, 1, g_gridSize.x - 2);

    const double totalColumns = double(processEndX - processStartX);
    for (int i = g_nbThreads - 1; i >= 0; --i) {
        ThreadData& threadData = g_threadData[i];

        threadData.startX /*-0x8*/ = processStartX + int(double(i) * totalColumns / g_nbThreads);
        threadData.endX /*0x0*/ = processStartX + int(double(i + 1) * totalColumns / g_nbThreads);
        threadData.startY /*-0x4*/ = processStartY;
        threadData.endY /*0x4*/ = processEndY;
        threadData.processCellLighting /*-0xB*/ = true;
        ::SetEvent(threadData.workEvent /*-0x10*/);
    }

    const DWORD waitResult = ::WaitForMultipleObjects(g_nbThreads, g_threadEvents, /*bWaitAll*/ true, /*dwMilliseconds*/ 2000);
    if (waitResult != WAIT_OBJECT_0) {
        DebugLogFormat(g_formatBuffer, "- Threads eventWorkFinished timeout: %ld", waitResult);
        g_callbackDebug(g_formatBuffer);
    }

    return 1;
}
// FUNCTION: 0x3a40
__declspec(dllexport) int DllProcessWaterMT(
    CCell* grid, CItem_PluginData* itemsData, double simuTime, double simuDeltaTime, int isRaining,
    int yRain, int fastEvaporationYMax, float cloudCenter, int cloudRadius, float lavaPressure, float waterSpeed,
    int* changeCellPos, double* infiltrationTimes, double* lavaMovingTimes, short* nbCellsUpdated
) {
    // g_callbackDebug("dll: inside DllProcessWaterMT");

    if (double(::rand()) < (double(RAND_MAX) * 0.05)) { // 5% chance
        InitGridOrder();
    }
    g_waterSimulationDir = (g_waterSimulationDir == 0);
    g_cloudCenter = cloudCenter;
    g_grid = grid;
    g_itemsData = itemsData;
    g_lavaPressure = lavaPressure;
    g_rainMode = RainMode(isRaining);
    g_waterSpeed = waterSpeed;
    g_yRain = yRain;
    g_fastEvaporationYMax = fastEvaporationYMax;
    g_cloudRadius = cloudRadius;
    g_infiltrationTimes = infiltrationTimes;
    g_lavaMovingTimes = lavaMovingTimes;
    g_simuTime = simuTime;
    g_simuDeltaTime = simuDeltaTime;
    g_lastChangedCellPos = -1;
    g_nbCellsUpdated = nbCellsUpdated;

    int verticalStartOffset = 0;
    int verticalIterations = 0;
    GetIterators(g_gridSize.y - 2, simuTime, simuDeltaTime, 1.f/20.f, verticalStartOffset, verticalIterations);

    g_verticalWaterOffset = verticalStartOffset;
    g_verticalWaterIterations = verticalIterations;

    // first phase: process vertical water simulation across threads
    for (int threadIndex = g_nbThreads - 1; threadIndex >= 0; --threadIndex) {
        ThreadData& threadData = g_threadData[threadIndex];

        // for some reason, in the original the double type is used in calculation but casted to int (mistakenly 2.0 literal was used?)
        threadData.verticalFlowStart = ((g_gridSize.x - 2) * threadIndex) / g_nbThreads + 1;
        threadData.processVerticalWater = true;
        // again, in the original the double is used in calculation but casted to int
        threadData.verticalFlowEnd = ((g_gridSize.x - 2) * (threadIndex + 1)) / g_nbThreads + 1;

        ::SetEvent(threadData.workEvent);
    }
    // wait for all threads to complete first phase
    const DWORD waitResult1 = ::WaitForMultipleObjects(g_nbThreads, g_threadEvents, /*bWaitAll*/ true, /*dwMilliseconds*/ 1000);
    if (waitResult1 != WAIT_OBJECT_0) {
        DebugLogFormat(g_formatBuffer, "Threads eventWorkFinished timeout: %ld", waitResult1);
        g_callbackDebug(g_formatBuffer);
    }
    // second phase: process horizontal water flow
    int horizontalStartOffset = 0;
    int horizontalIterations = 0;
    GetIterators(g_gridSize.x - 2, g_simuTime, g_simuDeltaTime, 4.0f, horizontalStartOffset, horizontalIterations);

    // distribute horizontal water flow work between threads
    for (int threadIndex = 0; threadIndex < g_nbThreads; ++threadIndex) {
        ThreadData& threadData = g_threadData[threadIndex];

        threadData.processHorizontalFlow = true;
        // again, in the original the double is used in calculation but casted to int
        threadData.flowStartOffset = horizontalStartOffset + (threadIndex * horizontalIterations) / g_nbThreads;
        // again, in the original the double is used in calculation but casted to int
        threadData.flowIterations = ((threadIndex + 1) * horizontalIterations) / g_nbThreads - (threadIndex * horizontalIterations) / g_nbThreads;

        ::SetEvent(threadData.workEvent);
    }
    const DWORD waitResult2 = ::WaitForMultipleObjects(g_nbThreads, g_threadEvents, /*bWaitAll*/ true, /*dwMilliseconds*/ 1000);
    if (waitResult2 != WAIT_OBJECT_0) {
        DebugLogFormat(g_formatBuffer, "- Threads eventWorkFinished timeout: %ld", waitResult2);
        g_callbackDebug(g_formatBuffer);
    }
    changeCellPos[0] = g_lastChangedCellPos;

    PostProcessWater(changeCellPos);

    return 1;
}
// FUNCTION: 0x1360
__declspec(dllexport) void DllResetSimu(int2 gs, float gridBorderNoCam) {
    // g_callbackDebug("dll: inside DllResetSimu");

    g_gridSize = gs;
    g_gridBorderNoCam = gridBorderNoCam;

    // FIXME: if gridOrderSize overflowed clamp at max value
    g_gridOrder = new int[gs.x + 1]; // memory leak
    InitGridOrder();
}
// FUNCTION: 0x14c0
__declspec(dllexport) int GetBestSpawnPoint(CCell* grid, const CItem_PluginData* itemsData, int2* pos) {
    // g_callbackDebug("dll: inside GetBestSpawnPoint");

    const int2 origPos = *pos;
    float maxScore = -10'000'000.0f;

    if (g_gridSize.x < 1) { return 0; }
    int spawnPointsFound = 0;

    for (int i = 0; i < g_gridSize.x; ++i) {
        for (int j = 0; j < g_gridSize.y; ++j) {
            const CItem_PluginData& itemData = itemsData[grid[j + i * g_gridSize.y].m_contentId];
            if (!itemData.m_isAutobuilder) { continue; }

            spawnPointsFound += 1;

            const int deltaX = i - origPos.x;
            const int deltaY = j - origPos.y;
            const float distSqr = float(deltaX * deltaX + deltaY * deltaY);

            const float autobuilderRank = itemData.m_customValue;
            const float score = autobuilderRank * 1'000'000.f - distSqr;
            if (score > maxScore) {
                pos->x = deltaX;
                pos->y = deltaY;
                maxScore = score;
            }
        }
    }
    return spawnPointsFound;
}

}
