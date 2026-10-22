# Software for Ttlcalc2

## Assembling

Use customasm v0.14.0 or later from https://github.com/hlorenzi/customasm. `ttlcalc2.asm` uses `struct` values and `$`-prefixed built-ins, which older versions don't support.

For example, to build the Fibonacci code, run `customasm fib.asm`. This generates `fib.bin` which you can then load into the simulator and run.

