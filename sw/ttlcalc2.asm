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

; immset = [12:12]
; imm = [11:8]
; hlincset = [7:7]
; addrselset = [6:6]
; hldir = [5:5]; ninchl = [4:4]
; addrsel = [3:3]
; src = [2:0]
#subruledef movsrc
{
    X => 0b0 @ 0`4 @ 0b00 @ 0b011 @ 0`3
    ; ALU => 1`3
    H => 0b0 @ 0`4 @ 0b00 @ 0b011 @ 2`3
    L => 0b0 @ 0`4 @ 0b00 @ 0b011 @ 3`3
    IO[#{addr:u4}] => 0b1 @ addr @ 0b01 @ 0b010 @ 4`3
    IO[{hl:internal_hl}] => 0b0 @ 0`4 @ hl @ 4`3
    Mem[#{addr:u4}] => 0b1 @ addr @ 0b01 @ 0b010 @ 5`3
    Mem[{hl:internal_hl}] => 0b0 @ 0`4 @ hl @ 5`3
    Flags => 0b0 @ 0`4 @ 0b011 @ 6`3
    #{i:u4} => 0b1 @ i @ 0b011 @ 7`3
}

; immset = [12:12]
; imm = [11:8]
; hlincset = [7:7]
; addrselset = [6:6]
; hldir = [5:5]; ninchl = [4:4]
; addrsel = [3:3]
; dst = [2:0]
#subruledef dst
{
    X => 0b0 @ 0`4 @ 0b00 @ 0b011 @ 0`3
    Y => 0b0 @ 0`4 @ 0b00 @ 0b011 @ 1`3
    H => 0b0 @ 0`4 @ 0b00 @ 0b011 @ 2`3
    L => 0b0 @ 0`4 @ 0b00 @ 0b011 @ 3`3
    IO[#{addr:u4}] => 0b1 @ addr @ 0b01 @ 0b010 @ 4`3
    IO[{hl:internal_hl}] => 0b0 @ 0`4 @ hl @ 4`3
    Mem[#{addr:u4}] => 0b1 @ addr @ 0b01 @ 0b010 @ 5`3
    Mem[{hl:internal_hl}] => 0b0 @ 0`4 @ hl @ 5`3
    Flags => 0b0 @ 0`4 @ 0b00 @ 0b011 @ 6`3
    None => 0b0 @ 0`4 @ 0b00 @ 0b011 @ 7`3
}

; hlincset = [2:2]
; hldir = [1:1]
; ninchl = [0:0]
#subruledef hl 
{
    _ => 0b001
    (hl++) => 0b100
    (hl--) => 0b110
}

; hlincset = [4:4]
; addrselset = [3:3]
; hldir = [2:2]
; ninchl = [1:1]
; addrsel = [0:0]
#subruledef internal_hl
{
    hl => 0b01 @ 0b011
    hl++ => 0b11 @ 0b001
    hl-- => 0b11 @ 0b101
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
    shl => 0
    dec => 1
    not => 2
}

#subruledef unaryyop
{
    mov => 0
    not => 1
}

#subruledef unaryop
{
    shlx => 12`4 @ 0b0
    decx => 15`4 @ 0b0
    notx => 0`4 @ 0b1
    movy => 10`4 @ 0b1
    noty => 5`4 @ 0b1
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
    nop{hl: hl} => 0`4 @ 0b00 @ hl[1:0] @ 0b0 @ 7`3 @ 0`3 @ 0b1

    mov {d: dst}, {s: movsrc} => asm { mov_ {d}, {s} }
    mov{hl: hl} {d: dst}, {s: movsrc} => {
        hl_hlincset = hl[2:2]
        d_hlincset = d[7:7]
        s_hlincset = s[7:7]

        hl_hldir = hl[1:1]
        d_hldir = d[5:5]
        s_hldir = s[5:5]

        hl_ninchl = hl[0:0]
        d_ninchl = d[4:4]
        s_ninchl = s[4:4]

        assert((hl_hlincset == 0 && d_hlincset == 0) || (hl_hlincset != d_hlincset) || (hl_hldir == d_hldir), "Instruction suffix and destination cannot both increment or decrement HL")
        assert((hl_hlincset == 0 && s_hlincset == 0) || (hl_hlincset != s_hlincset) || (hl_hldir == s_hldir), "Instruction suffix and source cannot both increment or decrement HL")
        assert((d_hlincset == 0 && s_hlincset == 0) || (d_hlincset != s_hlincset) || (d_hldir == s_hldir), "Destination and source cannot both increment or decrement HL")

        d_addrselset = d[6:6]
        s_addrselset = s[6:6]

        d_addrsel = d[3:3]
        s_addrsel = s[3:3]

        assert((d_addrselset == 0 && s_addrselset == 0) || (d_addrselset != s_addrselset) || (d_addrsel == s_addrsel), "Destination and source memory address selection must match")

        d_immset = d[12:12]
        s_immset = s[12:12]

        d_imm = d[11:8]
        s_imm = s[11:8]
        assert((d_immset == 0 && s_immset == 0) || (d_immset != s_immset) || (d_imm == s_imm), "Destination and source immediate values must match")

        hldir = (hl_hldir | d_hldir | s_hldir)`1
        ninchl = (hl_ninchl & d_ninchl & s_ninchl)`1
        addrsel = (d_addrsel & s_addrsel)`1
        imm = (d[9:6] | s[9:6])`4
        imm @ 0b00 @ hldir @ ninchl @ addrsel @ d`3 @ s`3 @ 0b1
    }

    {op:arithop} {d: dst}, x, y => asm { {op}__ {d}, x, y }
    {op:arithop}{dc:bcdcarry} {d: dst}, x, y => asm { {op}{dc}_ {d}, x, y }
    {op:arithop}{dc:bcdcarry}{hl:hl} {d:dst}, x, y => {
        hl_hlincset = hl[2:2]
        d_hlincset = d[7:7]

        hl_hldir = hl[1:1]
        d_hldir = d[5:5]

        bcd = dc[0:0]
        assert((hl_hlincset == 0 && d_hlincset == 0) || (hl_hlincset != d_hlincset) || (hl_hldir == d_hldir), "Instruction suffix and destination cannot both increment or decrement HL")
        assert((hl_hlincset == 0) || (hl_hldir == bcd), "Instruction suffix HL increment/decrement must match BCD carry mode")
        assert((d_hlincset == 0) || (d_hldir == bcd), "Destination HL increment/decrement must match BCD carry mode")

        d_immset = d[12:12]
        d_imm = d[11:8]
        assert((d_immset == 0) || (d_imm == op), "Destination immediate value must match ALU operation")

        ninchl = (hl[0:0] & d[4:4])`1
        addrsel = d[3:3]
        op @ 0b0 @ dc[1:1] @ bcd @ ninchl @ addrsel @ d`3 @ 1`3 @ 0b1
    }
    
    {op:logicop} {d: dst}, x, y => asm { {op}_ {d}, x, y }
    {op:logicop}{hl:hl} {d:dst}, x, y => {
        hl_hlincset = hl[2:2]
        d_hlincset = d[7:7]

        hl_hldir = hl[1:1]
        d_hldir = d[5:5]
        assert((hl_hlincset == 0 && d_hlincset == 0) || (hl_hldir == d_hldir) , "Instruction suffix and destination cannot both increment or decrement HL")
        assert((hl_hlincset == 0) || (hl_hldir == 0), "Instruction suffix HL decrement not supported with logic operation (BCD conflict)")
        assert((d_hlincset == 0) || (d_hldir == 0), "Destination HL decrement not supported with logic operation (BCD conflict)")

        d_immset = d[12:12]
        d_imm = d[11:8]
        assert((d_immset == 0) || (d_imm == op), "Destination immediate value must match ALU operation")

        hldir = (hl[1:1] | d[5:5])`1
        ninchl = (hl[0:0] & d[4:4])`1
        addrsel = d[3:3]
        op @ 0b10 @ hldir @ ninchl @ addrsel @ d`3 @ 1`3 @ 0b1
    }

    {op:unaryxop} {d: dst}, x => asm { {op}_ {d}, x }
    {op:unaryxop}{hl:hl} {d:dst}, x => asm { __unaryop {op}x {hl} {d} }

    {op:unaryyop} {d: dst}, y => asm { {op}_ {d}, y }
    {op:unaryyop}{hl:hl} {d:dst}, y => asm { __unaryop {op}y {hl} {d} }

    __unaryop {op:unaryop} {hl:hl} {d: dst} => {
        hl_hlincset = hl[2:2]
        d_hlincset = d[7:7]

        hl_hldir = hl[1:1]
        d_hldir = d[5:5]

        assert((hl_hlincset == 0 && d_hlincset == 0) || (hl_hldir == d_hldir) , "Instruction suffix and destination cannot both increment or decrement HL")
        assert((hl_hlincset == 0) || (hl_hldir == 0), "Instruction suffix HL decrement not supported with unary operation (BCD conflict)")
        assert((d_hlincset == 0) || (d_hldir == 0), "Destination HL decrement not supported with unary operation (BCD conflict)")

        d_immset = d[12:12]
        d_imm = d[11:8]
        assert((d_immset == 0) || (d_imm == op), "Destination immediate value must match ALU operation")

        hldir = (hl[1:1] | d[5:5])`1
        ninchl = (hl[0:0] & d[4:4])`1
        addrsel = d[3:3]
        op @ 0b0 @ hldir @ ninchl @ addrsel @ d`3 @ 1`3 @ 0b1                
    }
}

