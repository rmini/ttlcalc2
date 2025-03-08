# TtlCalc2 Project

## Overview

The TtlCalc2 project is a recreation of the original (now TtlCalc1) project from 2005 that implemented a four-function floating point calculator in TTL logic without a microprocessor.

This version uses multilayer PCBs and Nixie tube and neon indicators for the display, whereas the original used wire wrap boards and LED 7-segment displays. Both versions share the same architecture, with separate (Harvard architecture) 8-bit data and 12-bit instruction address spaces, a 4-bit data path, and a 16-bit instruction word.

The goal of the original was to gain experience designing and implementing a processor with these techniques with an eye towards taking on the design of a more capable minicomputer. 

This repository contains the hardware and software components for the TtlCalc2 project, including simulation tools, assembly code, and documentation.

## Directory Structure

 * `hw/`: KiCad schematic and PCB designs for the hardware
 * `sw/`: Software for the calculator (microinstruction ROMs)
 * `doc/`: Design and implementation documentation
 * `TtlCalcSim/`: Simulator for the calculator hardware

## Hardware

The `hw` directory contains KiCad project files for the TtlCalc2 hardware design.

- `ttlcalc2/`: Logic board hardware design files.
- `ttlcalc2_display/`: Display driver module design files.
- `ttlcalc2_front/`: Front panel design files.

TODO: The keyboard and keyboard interface still needs to be designed.

### Gerber Files

The `hw/ttlcalc2/gerbers` directory contains the Gerber files for PCB manufacturing.

## Software

The `sw` directory contains assembly code for the TtlCalc2 project. This project uses the [customasm](https://github.com/hlorenzi/customasm) assembler to generate the microcode stream.

- `ttlcalc2.asm`: Mnemonic definitions for the TtlCalc2 microcode.
- `fib.asm`: Assembly code for calculating Fibonacci numbers.

TODO: Port original ttlcalc assembly source code to new customasm syntax.

### Assembling

For example, to build the Fibonacci code, run:
```sh
customasm fib.asm
```
This generates `fib.bin` which you can then load into the simulator and run.

## Documentation

The `doc` directory contains documentation for the TtlCalc2 project.

## Simulation

The `TtlCalcSim` directory contains the simulation tools for the TtlCalc2 project. The simulator is implemented as a C#/.NET 9.0 console application.

- `TtlCalcSim/`: Core simulation library.
- `TtlCalcSim.ConsoleUI/`: Console-based user interface for the simulator.
- `TtlCalcSim.Tests/`: Unit tests for the simulation library.

### Building the Simulator

To build the simulator, open the TtlCalcSim.sln solution file in Visual Studio and build the project, or use the following command from the TtlCalcSim directory:
```sh
dotnet build
```

### Running Tests

To run the unit tests, use the following command from the TtlCalcSim directory:
```sh
dotnet test
```

### Running the simulator

Either run the simulator executable after building, or run it via the `dotnet` command:
```sh
dotnet run --project TtlCalcSim.ConsoleUI
```

## License

This project is licensed under the MIT License. See the LICENSE file for details.
