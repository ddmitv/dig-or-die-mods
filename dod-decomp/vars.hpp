#include "types.hpp"

inline DelegateCallbackDebug* g_callbackDebug;
inline DelegateCallbackGetElecProd* g_callbackGetElecProd;

inline int2 g_gridSize;
inline float g_gridBorderNoCam;
inline int* g_gridOrder;
inline double g_simuTime;
inline double g_simuDeltaTime;
inline int g_isRaining;
inline int g_yRain;
inline int g_fastEvaporationYMax;
inline float g_cloudCenter;
inline int g_cloudRadius;

inline float g_lavaPressure;
inline int g_someFlag1; // the only first byte is used
inline int g_someCellPos1;

inline float g_waterSpeed;
inline double* g_infiltrationTimes;
inline double* g_lavaMovingTimes;
inline short* g_nbCellsUpdated;
inline CCell* g_grid;
inline CItem_PluginData* g_itemsData;
inline RectInt g_skipYMax;

inline int g_nbThreads;

inline char g_formatBuffer[512];

inline ThreadData g_threadData[32];
inline HANDLE g_threadEvents[32];
