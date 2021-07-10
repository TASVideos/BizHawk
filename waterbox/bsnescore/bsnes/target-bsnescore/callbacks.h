#ifndef CALLBACKS_H
#define CALLBACKS_H

#include <stdint.h>

typedef void (*snes_input_poll_t)(void);
typedef int16_t (*snes_input_state_t)(int port, int index, int id);
typedef void (*snes_no_lag_t)(void);
typedef void (*snes_video_frame_t)(const uint16_t* data, int width, int height, int pitch);
typedef void (*snes_audio_sample_t)(int16_t left, int16_t right);
typedef char* (*snes_path_request_t)(int slot, const char* hint, int required);
typedef void (*snes_trace_t)(const char* disassembly, const char* register_info);
typedef void (*snes_read_hook_t)(uint32_t address);
typedef void (*snes_write_hook_t)(uint32_t address, uint8_t value);
typedef void (*snes_exec_hook_t)(uint32_t address);

struct SnesCallbacks {
    snes_input_poll_t snes_input_poll;
    snes_input_state_t snes_input_state;
    snes_no_lag_t snes_no_lag;
    snes_video_frame_t snes_video_frame;
    snes_audio_sample_t snes_audio_sample;
    snes_path_request_t snes_path_request;
    snes_trace_t snes_trace;
    snes_read_hook_t snes_read_hook;
    snes_write_hook_t snes_write_hook;
    snes_exec_hook_t snes_exec_hook;
};

extern SnesCallbacks snesCallbacks;

#endif
