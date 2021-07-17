/******************************************************************************/
/* Mednafen Virtual Boy Emulation Module                                      */
/******************************************************************************/
/* vb.h:
**  Copyright (C) 2010-2016 Mednafen Team
**
** This program is free software; you can redistribute it and/or
** modify it under the terms of the GNU General Public License
** as published by the Free Software Foundation; either version 2
** of the License, or (at your option) any later version.
**
** This program is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License
** along with this program; if not, write to the Free Software Foundation, Inc.,
** 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

#pragma once

#include <cstdint>
#include <cstddef>
#include <cstring>
#include <algorithm>
#include <memory>
#include <cassert>
#include <cstdio>

typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef uint64_t uint64;
typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;
typedef int64_t int64;

#define MDFN_FASTCALL
#define INLINE inline
#define MDFN_COLD
#define NO_INLINE
//#define MDFN_ASSUME_ALIGNED(p, align) ((decltype(p))__builtin_assume_aligned((p), (align)))
#define MDFN_ASSUME_ALIGNED(p, align) (p)
#define trio_snprintf snprintf
#define TRUE true
#define FALSE false
#ifndef __alignas_is_defined
#define alignas(p)
#endif

struct MyFrameInfo
{
	uint32_t* VideoBuffer;
	int16_t* SoundBuffer;
	int64_t Cycles;
	int32_t Width;
	int32_t Height;
	int32_t Samples;
	int32_t Lagged;
	int32_t Buttons;
};

#include "endian.h"
#include "math_ops.h"
#include "blip/Blip_Buffer.h"
#include "v810/v810_fp_ops.h"
#include "v810/v810_cpu.h"

#include "git.h"

#include "vsu.h"
#include "vip.h"
#include "timer.h"
#include "input.h"


namespace MDFN_IEN_VB
{

enum
{
	VB3DMODE_ANAGLYPH = 0,
	VB3DMODE_CSCOPE = 1,
	VB3DMODE_SIDEBYSIDE = 2,
	VB3DMODE_OVERUNDER = 3,
	VB3DMODE_VLI,
	VB3DMODE_HLI,
	VB3DMODE_ONLYLEFT,
	VB3DMODE_ONLYRIGHT
};

#define VB_MASTER_CLOCK 20000000.0

enum
{
	VB_EVENT_VIP = 0,
	VB_EVENT_TIMER,
	VB_EVENT_INPUT,
	// VB_EVENT_COMM
};

#define VB_EVENT_NONONO 0x7fffffff

void VB_SetEvent(const int type, const v810_timestamp_t next_timestamp);

#define VBIRQ_SOURCE_INPUT 0
#define VBIRQ_SOURCE_TIMER 1
#define VBIRQ_SOURCE_EXPANSION 2
#define VBIRQ_SOURCE_COMM 3
#define VBIRQ_SOURCE_VIP 4

void VBIRQ_Assert(int source, bool assert);

void VB_ExitLoop(void);

void ForceEventUpdates(const v810_timestamp_t timestamp);

uint8 MDFN_FASTCALL MemRead8(v810_timestamp_t &timestamp, uint32 A);
uint16 MDFN_FASTCALL MemRead16(v810_timestamp_t &timestamp, uint32 A);

void MDFN_FASTCALL MemWrite8(v810_timestamp_t &timestamp, uint32 A, uint8 V);
void MDFN_FASTCALL MemWrite16(v810_timestamp_t &timestamp, uint32 A, uint16 V);
}
