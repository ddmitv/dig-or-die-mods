cmake_minimum_required(VERSION 4.0 FATAL_ERROR)

project(dod-decomp VERSION 0.0.0 LANGUAGES CXX)

set(GAME_DATA_PATH "$ENV{ProgramFiles\(x86\)}/Steam/steamapps/common/Dig or Die" CACHE PATH "")

add_library(dod-dll-decomp SHARED
	main.cpp
    types.hpp
)

target_compile_features(dod-dll-decomp PUBLIC cxx_std_20)
set_target_properties(dod-dll-decomp PROPERTIES
	# COMPILE_WARNING_AS_ERROR TRUE
	LINK_WARNING_AS_ERROR TRUE)

if (EXISTS "${GAME_DATA_PATH}")
    cmake_path(APPEND GAME_DATA_PATH "DigOrDie_Data/Plugins/DigOrDie.dll")
    add_custom_command(TARGET dod-dll-decomp POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy "$<TARGET_FILE:dod-dll-decomp>" "${GAME_DATA_PATH}"
        COMMENT "Copying DLL plugin to: ${GAME_DATA_PATH}"
        VERBATIM)
endif()
