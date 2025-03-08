#bankdef data
{
    #bits 4
    #addr 0
    #size 0x100
}

#bankdef program
{
    #addr 0
    #size 0x1000
    #bits 16
    #outp 0
    #fill
}

#fn hi(addr) => (addr >> 4)`4
#fn lo(addr) => addr`4

; TODO: Use a mask to flag conflicts between the two operands
; and the HL mnemonic suffix, as well as immediate values,
; the ALU operation, and a zero page reference

; 4 bits of immediate, 3 bits of hldir/ninchl/addrsel, 3 bits of src
#subruledef movsrc
{
    X => 0`4 @ 0b011 @ 0`3
    ; ALU => 1`3
    H => 0`4 @ 0b011 @ 2`3
    L => 0`4 @ 0b011 @ 3`3
    IO[#{addr:u4}] => addr @ 0b010 @ 4`3
    IO[{hl:internal_hl}] => 0`4 @ hl @ 4`3
    Mem[#{addr:u4}] => addr @ 0b010 @ 5`3
    Mem[{hl:internal_hl}] => 0`4 @ hl @ 5`3
    Flags => 0`4 @ 0b011 @ 6`3
    #{i:u4} => i @ 0b011 @ 7`3
}

; 4 bits of immediate, 3 bits of hldir/ninchl/addrsel, 3 bits of dest
#subruledef dst
{
    X => 0`4 @ 0b011 @ 0`3
    Y => 0`4 @ 0b011 @ 1`3
    H => 0`4 @ 0b011 @ 2`3
    L => 0`4 @ 0b011 @ 3`3
    IO[#{addr:u4}] => addr @ 0b010 @ 4`3
    IO[{hl:internal_hl}] => 0`4 @ hl @ 4`3
    Mem[#{addr:u4}] => addr @ 0b010 @ 5`3
    Mem[{hl:internal_hl}] => 0`4 @ hl @ 5`3
    Flags => 0`4 @ 0b011 @ 6`3
    None => 0`4 @ 0b011 @ 7`3
}

#subruledef hl 
{
    _ => 0b01
    (hl++) => 0b00
    (hl--) => 0b10
}

#subruledef internal_hl
{
    hl => 0b011
    hl++ => 0b001
    hl-- => 0b101
}

#subruledef arithop 
{
    ; alumode Arith
    sub => 6`4
    add => 9`4
}

#subruledef logicop 
{
    ; alumode Logic
    or => 14`4
    and => 11`4
    xor => 6`4
    nor => 1`4
    nand => 4`4
    xnor => 9`4
}

#subruledef unaryxop
{
    shl => 12`4 @ 0b0
    dec => 15`4 @ 0b0
    not => 0`4 @ 0b1
}

#subruledef unaryyop
{
    mov => 10`4 @ 0b1
    not => 5`4 @ 0b1
}

#subruledef jmpcond 
{
    z => 1`3
    e => 1`3
    nz => 2`3
    ne => 2`3
    c => 3`3
    lt => 3`3
    nc => 4`3
    ge => 4`3
    gt => 5`3
    lt => 6`3
    mp => 7`3 ; heh heh
}

#subruledef bcdcarry 
{
    _ => 0b00
    c => 0b10
    d => 0b01
    cd => 0b11
}

#ruledef
{
    ; jump instructions (bit 0 = 0)
    nop => 0x0000

    j{cond: jmpcond} {addr: u12} => addr @ cond @ 0b0

    ; non-jump instructions (bit 0 = 1)
    nop{hl: hl} => 0`4 @ 0b00 @ hl @ 0b0 @ 7`3 @ 0`3 @ 0b1

    mov {d: dst}, {s: movsrc} => asm { mov_ {d}, {s} }
    mov{hl: hl} {d: dst}, {s: movsrc} => {
        hldir = (hl[1:1] | d[5:5] | s[5:5])`1
        ninchl = (hl[0:0] & d[4:4] & s[4:4])`1
        addrsel = (d[3:3] & s[3:3])`1
        imm = (d[9:6] | s[9:6])`4
        imm @ 0b00 @ hldir @ ninchl @ addrsel @ d`3 @ s`3 @ 0b1
    }

    {op:arithop} {d: dst}, x, y => asm { {op}__ {d}, x, y }
    {op:arithop}{dc:bcdcarry} {d: dst}, x, y => asm { {op}{dc}_ {d}, x, y }
    {op:arithop}{dc:bcdcarry}{hl:hl} {d:dst}, x, y => {
        hldir_bcd = (dc[0:0] | hl[1:1] | d[5:5])`1
        ninchl = (hl[0:0] & d[4:4])`1
        addrsel = d[3:3]
        op @ 0b0 @ dc[1:1] @ hldir_bcd @ ninchl @ addrsel @ d`3 @ 1`3 @ 0b1
    }
    
    {op:logicop} {d: dst}, x, y => asm { {op}_ {d}, x, y }
    {op:logicop}{hl:hl} {d:dst}, x, y => {
        hldir = (hl[1:1] | d[5:5])`1
        ninchl = (hl[0:0] & d[4:4])`1
        addrsel = d[3:3]
        op @ 0b10 @ hldir @ ninchl @ addrsel @ d`3 @ 1`3 @ 0b1
    }

    {op:unaryxop} {d: dst}, x => asm { {op}_ {d}, x }
    {op:unaryxop}{hl:hl} {d:dst}, x => {
        hldir = (hl[1:1] | d[5:5])`1
        ninchl = (hl[0:0] & d[4:4])`1
        addrsel = d[3:3]
        op @ 0b0 @ hldir @ ninchl @ addrsel @ d`3 @ 1`3 @ 0b1
    }

    {op:unaryyop} {d: dst}, x => asm { {op}_ {d}, y }
    {op:unaryyop}{hl:hl} {d:dst}, y => {
        hldir = (hl[1:1] | d[5:5])`1
        ninchl = (hl[0:0] & d[4:4])`1
        addrsel = d[3:3]
        op @ 0b0 @ hldir @ ninchl @ addrsel @ d`3 @ 1`3 @ 0b1        
    }
}

