
#include "types.hpp"
#include "vars.hpp"

inline void WaitForDebugger() {
    while (!::IsDebuggerPresent()) {
        ::Sleep(200);
    }
    ::DebugBreak();
}

[[msvc::forceinline]]
inline bool IsCellPassable(const CCell& cell, const CItem_PluginData* itemsData) {
    if (itemsData[cell.m_contentId].m_isBlock == 0) {
        return true;
    }
    if (itemsData[cell.m_contentId].m_isBlockDoor != 0 && (cell.m_flags & Flag_CustomData0) != 0) {
        return true;
    }
    return false;
}

// https://github.com/wine-mirror/wine/blob/master/include/msvcrt/stdio.h
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

inline unsigned int __stdcall WorkerThread(void* threadDataRaw) {
    ThreadData* const threadData = static_cast<ThreadData*>(threadDataRaw);

    ::WaitForSingleObject(threadData->secondEvent /*0x8*/, 1000);

    while (true) {
        if (threadData->shouldExit /*0xC*/) {
            return 0;
        }
        if (!threadData->processWaterSim /*0x20*/) {
            if (threadData->flag_0x2C /*0x2C*/) {
                threadData->flag_0x2C /*0x2C*/ = false;
                // FUN_10005150(...);
                goto finish;
            }
            if (threadData->processCellLighting /*0xD*/) {
                threadData->processCellLighting /*0xD*/ = false;
                // ...;
                goto finish;
            }
        } else {
            threadData->processWaterSim /*0x20*/ = false;
            // ...;
        finish:
            ::SetEvent(g_threadEvents[threadData->id /*0x4*/]);
        }
        ::WaitForSingleObject(threadData->secondEvent /*0x8*/, 1000);
    }
}
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
