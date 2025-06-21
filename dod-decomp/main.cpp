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
        ::SetEvent(g_threadData[i].secondEvent);
    }
    ::Sleep(100);
    for (int i = 0; i < g_nbThreads; ++i) {
        DWORD res = ::WaitForSingleObject(g_threadData[i].handle, 1000);
        if (res != WAIT_OBJECT_0) {
            g_callbackDebug("- Error stopping thread.");
        }
        ::CloseHandle(g_threadData[i].handle);
        ::CloseHandle(g_threadData[i].secondEvent);
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

    DebugLogFormat(g_formatBuffer, "- Dll initialisation. Creating %d threads...", nbThreads);
    g_callbackDebug(g_formatBuffer);

    if (nbThreads > 32) { nbThreads = 32; }
    g_nbThreads = nbThreads;

    ThreadData* threadData = g_threadData;
    for (int i = 0; i < nbThreads; ++i, ++threadData) {
        g_threadEvents[i] = ::CreateEventW(nullptr, false, false, nullptr);

        threadData->id = i;
        threadData->shouldExit = 0;

        threadData->secondEvent = ::CreateEventW(nullptr, false, false, nullptr);

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
    g_callbackDebug("dll: inside DllProcessElectricity");
    return 1;
}
// FUNCTION: 0x2240
__declspec(dllexport) int DllProcessForces(
    CCell* grid, CItem_PluginData* itemsData, int* cellsWithForces, int nbCells, float weightMult
) {
    // g_callbackDebug("dll: inside DllProcessForces");

    for (int i = 0; i < nbCells; ++i) {
        int cellIdx = cellsWithForces[i];
        if (cellIdx == INT_MIN) { continue; }

        int topCellIdx = cellIdx + g_gridSize.y;
        grid[topCellIdx].m_forceX  = (int16_t)::roundf((float)grid[topCellIdx].m_forceX * 0.995f);
        grid[cellIdx].m_forceX     = (int16_t)::roundf((float)grid[cellIdx].m_forceX * 0.995f);
        grid[cellIdx + 1].m_forceY = (int16_t)::roundf((float)grid[cellIdx + 1].m_forceY * 0.995f);
        grid[cellIdx].m_forceY     = (int16_t)::roundf((float)grid[cellIdx].m_forceY * 0.995f);
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
    CCell* grid, CItem_PluginData* itemsData, int2 posMin, int2 posMax, float sunlight, RectInt skipYMax,
    int sunLightYMin, int sunLightYMax
) {
    g_callbackDebug("dll: inside DllProcessLightingSquare");

    g_grid = grid;
    g_itemsData = itemsData;
    g_skipYMax = skipYMax;

    // INCOMPLETE

    for (int i = g_nbThreads - 1; i >= 0; --i) {
        ThreadData& threadData = g_threadData[i];

        // threadData.cellParam2 /*-0x4*/ = ...
        // threadData.cellParam1 /*-0x8*/ = ...
        threadData.processCellLighting /*-0xB*/ = true;
        // threadData.cellParam3 /*0x0*/ = ...
        // threadData.cellParam4 /*+0x4*/ = ...
        ::SetEvent(threadData.secondEvent /*-0x10*/);
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
    g_callbackDebug("dll: inside DllProcessWaterMT");

    g_someFlag1 = g_someFlag1 == 0;
    g_cloudCenter = cloudCenter;
    g_grid = grid;
    g_itemsData = itemsData;
    g_lavaPressure = lavaPressure;
    g_isRaining = isRaining;
    g_waterSpeed = waterSpeed;
    g_yRain = yRain;
    g_fastEvaporationYMax = fastEvaporationYMax;
    g_cloudRadius = cloudRadius;
    g_infiltrationTimes = infiltrationTimes;
    g_lavaMovingTimes = lavaMovingTimes;
    g_simuTime = simuTime;
    g_simuDeltaTime = simuDeltaTime;
    g_someCellPos1 = -1;

    // INCOMPLETE

    for (int i = 0; i < g_nbThreads; ++i) {
        ThreadData& threadData = g_threadData[i];
        threadData.flag_0x2C /*-0x8*/ = true;
        // threadData.processWaterFlowStartOffset /*-0x4*/ = ...
        // threadData.processWaterFlowNumIterations /*0x0*/ = ...
        ::SetEvent(threadData.secondEvent /*-0x2C*/);
    }

    DWORD waitResult = ::WaitForMultipleObjects(g_nbThreads, g_threadEvents, /*bWaitAll*/ true, /*dwMilliseconds*/ 1000);
    if (waitResult != WAIT_OBJECT_0) {
        DebugLogFormat(g_formatBuffer, "- Threads eventWorkFinished timeout: %ld", waitResult);
        g_callbackDebug(g_formatBuffer);
    }
    changeCellPos[0] = g_someCellPos1;
    // INCOMPLETE: CALL TO FUNCTION 0x54d0
    return 1;
}
// FUNCTION: 0x1360
__declspec(dllexport) void DllResetSimu(int2 gs, float gridBorderNoCam) {
    g_callbackDebug("dll: inside DllResetSimu");

    g_gridSize = gs;
    g_gridBorderNoCam = gridBorderNoCam;

    // FIXME: if gridOrderSize overflowed clamp at max value
    g_gridOrder = new int[gs.x + 1]; // memory leak
    InitGridOrder();
}
// FUNCTION: 0x14c0
__declspec(dllexport) int GetBestSpawnPoint(CCell* grid, const CItem_PluginData* itemsData, int2* pos) {
    g_callbackDebug("dll: inside GetBestSpawnPoint");

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
