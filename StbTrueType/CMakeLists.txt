# CMake Project for StbTrueType
cmake_minimum_required(VERSION 2.8.12)
project(StbTrueType CXX)

# Options
option(BUILD_SHARED_LIBS "Build shared library" ON)

# Version
SET(LIB_MAJOR_VERSION "0")
SET(LIB_MINOR_VERSION "0")
SET(LIB_REVISION "0")
SET(LIB_VERSION "${LIB_MAJOR_VERSION}.${LIB_MINOR_VERSION}.${LIB_REVISION}")

# Build Type
if(NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
	# By default, we use Release
	message(STATUS "Setting build type to 'Release' as none was specified.")
	set(CMAKE_BUILD_TYPE "Release" CACHE
		STRING "Choose the type of build." FORCE
	)

	# Set the possible values of build type for cmake-gui
	set_property(CACHE CMAKE_BUILD_TYPE PROPERTY
		STRINGS "Debug" "Release" "RelWithDebInfo"
	)
endif()

# Platform Flags
if(APPLE)
	set(CMAKE_MACOSX_RPATH ON)
	set(CMAKE_OSX_DEPLOYMENT_TARGET 10.9)
	set(LOBJC "objc")
elseif(WIN32)
	# "StbTrueType.dll", not "libStbTrueType.dll"
	set(CMAKE_SHARED_LIBRARY_PREFIX "")
endif()

# Defines
add_definitions(
	-DBW_LINUX
)

# Source lists
add_library(StbTrueType
	# Public Headers
	stb_truetype.h
	# Internal Headers
	Shared.hpp
	# Source Files
	StbTrueType.cpp
)

# Build flags
if(NOT MSVC)
	set_property(TARGET StbTrueType PROPERTY COMPILE_FLAGS "-Wno-strict-aliasing")
endif()

# StbTrueType folders as includes, for other targets to consume
target_include_directories(StbTrueType PUBLIC
	$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}>
)

# MinGW builds should statically link libgcc
if(MINGW)
	target_link_libraries(StbTrueType PRIVATE -static-libgcc)
endif()

# Soname
set_target_properties(StbTrueType PROPERTIES OUTPUT_NAME "StbTrueType"
	VERSION ${LIB_VERSION}
	SOVERSION ${LIB_MAJOR_VERSION}
)
