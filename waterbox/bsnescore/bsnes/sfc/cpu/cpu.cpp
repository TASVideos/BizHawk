#include <sfc/sfc.hpp>

namespace SuperFamicom {

CPU cpu;
#include "dma.cpp"
#include "memory.cpp"
#include "io.cpp"
#include "timing.cpp"
#include "irq.cpp"
#include "serialization.cpp"

auto CPU::synchronizeSMP() -> void {
  if(smp.clock < 0) scheduler.resume(smp.thread);
}

auto CPU::synchronizePPU() -> void {
  if(ppu.clock < 0) scheduler.resume(ppu.thread);
}

auto CPU::synchronizeCoprocessors() -> void {
  for(auto coprocessor : coprocessors) {
    if(coprocessor->clock < 0) scheduler.resume(coprocessor->thread);
  }
}

auto CPU::Enter() -> void {
  while(true) {
    scheduler.synchronize();
    cpu.main();
    if (scheduler.StepOnce) scheduler.leave(Scheduler::Event::Desynchronized);
  }
}

auto CPU::main() -> void {
  if(r.wai) return instructionWait();
  if(r.stp) return instructionStop();
  if(!status.interruptPending) {
    platform->execHook(cpu.r.pc.d);
    if (platform->traceEnabled) {
      vector<string> disassembly = disassemble();
      disassembly[1].append(" V:", hex(cpu.vcounter(), 3), " H:", hex(cpu.hdot(), 3));
      platform->cpuTrace(disassembly);
    }
    return instruction();
  }

  if(status.nmiPending) {
    status.nmiPending = 0;
    r.vector = r.e ? 0xfffa : 0xffea;
    return interrupt();
  }

  if(status.irqPending) {
    status.irqPending = 0;
    r.vector = r.e ? 0xfffe : 0xffee;
    return interrupt();
  }

  if(status.resetPending) {
    status.resetPending = 0;
    for(uint repeat : range(22)) step<6,0>();  //step(132);
    r.vector = 0xfffc;
    return interrupt();
  }

  status.interruptPending = 0;
}

auto CPU::load() -> bool {
  version = configuration.system.cpu.version;
  if(version < 1) version = 1;
  if(version > 2) version = 2;
  return true;
}

auto CPU::power(bool reset) -> void {
  WDC65816::power();
  Thread::create(Enter, system.cpuFrequency());
  coprocessors.reset();
  PPUcounter::reset();
  PPUcounter::scanline = {&CPU::scanline, this};

  function<uint8 (uint, uint8)> reader;
  function<void  (uint, uint8)> writer;

  reader = {&CPU::readRAM, this};
  writer = {&CPU::writeRAM, this};
  bus.map(reader, writer, "00-3f,80-bf:0000-1fff", 0x2000);
  bus.map(reader, writer, "7e-7f:0000-ffff", 0x20000);

  reader = {&CPU::readAPU, this};
  writer = {&CPU::writeAPU, this};
  bus.map(reader, writer, "00-3f,80-bf:2140-217f");

  reader = {&CPU::readCPU, this};
  writer = {&CPU::writeCPU, this};
  bus.map(reader, writer, "00-3f,80-bf:2180-2183,4016-4017,4200-421f");

  reader = {&CPU::readDMA, this};
  writer = {&CPU::writeDMA, this};
  bus.map(reader, writer, "00-3f,80-bf:4300-437f");

  if(!reset) random.array(wram, sizeof(wram));

  if(configuration.hacks.hotfixes) {
    //Dirt Racer (Europe) relies on uninitialized memory containing certain values to boot without freezing.
    //the game itself is broken and will fail to run sometimes on real hardware, but for the sake of expedience,
    //WRAM is initialized to a constant value that will allow this game to always boot in successfully.
    if(cartridge.headerTitle() == "DIRT RACER") {
      for(auto& byte : wram) byte = 0xff;
    }
  }

  for(uint n : range(8)) {
    channels[n] = {};
    if(n != 7) channels[n].next = channels[n + 1];
  }

  counter = {};
  io = {};
  alu = {};

  status = {};
  status.dramRefreshPosition = (version == 1 ? 530 : 538);
  status.hdmaSetupPosition = (version == 1 ? 12 + 8 - dmaCounter() : 12 + dmaCounter());
  status.hdmaPosition = 1104;
  status.resetPending = 1;
  status.interruptPending = 1;
}

}
