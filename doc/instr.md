# Instruction Format

Instruction bits are numbered from least-significant bit (0) to most-significant bit (15).

## Branch instructions
| 15..4 | 3..1 | 0 |
|-------|------|---|
| Destination address | Condition code | 0 |

### Condition codes
| Code | Mnemonic | Branch condition |
|--|--|--|
| 0 | NOP | Never |
| 1 | JZ | EF==1 |
|   | JE | |
| 2 | JNZ | EF==0 |
|   | JNE | |
| 3 | JC | CF==1 |
| | JLT | |
| 4 | JNC | CF==0 |
| | JGE | |
| 5 | JGT | CF==0 && EF==0 |
| 6 | JLT | CF==1 \|\| EF==1 |
| 7 | JMP | Always |

The comparisons are meant to be used with the `SUB` instruction with the incoming carry flag clear.

## Data movement instructions

Data movement instructions are used to transfer data between registers, memory, and I/O ports, as well as to perform arithmetic and logic operations.

| Bit position(s) | Description |
|-----------------|-------------|
| 15..12 | Immediate value OR ALU operation code |
| 11 | ALU mode: 0=Arithmetic, 1=Logic |
| 10 | Carry source: 0=No carry (0), 1=Carry flag |
| 9 | 0: Hex ALU (non-BCD), HL increment; 1: Decimal ALU (BCD), HL decrement |
| 8 | HL post increment/decrement: 0=Enabled; 1=Disabled |
| 7 | Address source: 0=Immediate (zero-page); 1=HL registers |
| 6..4 | Destination |
| 3..1 | Source |
| 0 | 1 |

Note that a few fields are overloaded. The immediate value field is overloaded to form an immediate data value, an immediate address, and the ALU operation code. Bit 9 controls both the ALU BCD mode as well as the HL post-increment/decrement direction. This means some combinations of features do not make sense.

Conflict examples:
* Storing an immediate to a zero-page destination (unless they coincidentally use the same value)
* Using a zero-page destination for an ALU instruction
* Incrementing HL and performing a BCD ALU operation
* Decrementing HL and performing a non-BCD ALU operation

### ALU operations

Some possible operations as defined by the 74LS181 data sheet are omitted here. The omitted operations include constant values (e.g., 0, 1, -1, etc.) that could be provided by an immediate source, and unusual combinations of operations and operands such as `(A & B) + (A | ~B)`, which are not typically useful in practical applications.

#### Arithmetic mode (bit 11=0)
| Mnemonic | ALU operation | Code | Description |
|----------|---------------|------|-------------|
| ADD | X + Y + C | 9 | Add |
| SUB | X - Y - 1 + C | 6 | Subtract |
| SHL | X + X + C | 12 | Shift left (add X to itself) |
| DEC | X - 1 + C | 15 | Decrement X |

#### Logic mode (bit 11=1)
| Mnemonic | ALU operation | Code | Description |
|----------|---------------|------|-------------|
| OR | X \| Y | 14 | Bitwise OR |
| AND | X & Y | 11 | Bitwise AND |
| XOR | X ^ Y | 6 | Bitwise XOR (exclusive-OR) |
| NOR | ~(X \| Y) | 1 | Bitwise NOR (not-OR) |
| NAND | ~(X & Y) | 4 | Bitwise NAND (not-AND) |
| XNOR | ~(X ^ Y) | 9 | Bitwise NXOR (not-exclusive-OR) |
| NOT | ~X | 0 | Bitwise NOT X |
| NOT | ~Y | 5 | Bitwise NOT Y |
| MOV* | Y | 10 | Pass Y through |

\* The MOV mnemonic here is used even though the ALU is the actual source as the Y register cannot drive the data bus directly.

### Data destinations
Instruction bits 6..4 control which destination is set to the value on the data bus:

| Code | Destination |
|------|-------------|
| 0 | X register |
| 1 | Y register |
| 2 | H register |
| 3 | L register |
| 4 | I/O output |
| 5 | Memory write |
| 6 | Flags register\* |
| 7 | None (discard)\*\* |

\* On ALU operations (source=1), the ALU output has priority over the data bus for setting the flags register.

\*\* With ALU operations, the None destination still sets the Flags register, so it is useful for comparisons. Otherwise this functions as a NOP (although if bit 8 is set to 0, HL increments/decrements as normal).

### Data sources
Instruction bits 3..1 control which source drives the data bus:
| Code | Source |
|------|--------|
| 0 | X register |
| 1 | ALU (also sets Flags register) |
| 2 | H register |
| 3 | L register |
| 4 | I/O input |
| 5 | Memory read |
| 6 | Flags |
| 7 | Immediate (instructions bits 15..12) |
