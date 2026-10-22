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

#fn bit(b) => b ? 1`1 : 0`1

; HL side effect
;   set: HL is incremented/decremented
;   dir: 0 = increment, 1 = decrement
#fn hl_none() => struct { set = false, dir = 0`1 }
#fn hl_step(dir) => struct { set = true, dir = dir`1 }
#fn hl_ok(a, b) => !(a.set && b.set) || a.dir == b.dir

; Operand (source or destination)
;   opsel:   3-bit operand select (destination/source code)
;   immset:  operand uses the immediate field
;   imm:     4-bit immediate value
;   hl:      HL side effect
;   addrset: operand drives address selection
;   addrsel: 0 = immediate address, 1 = HL address
#fn operand(opsel, immset, imm, hl, addrset, addrsel) => struct {
    opsel = opsel`3, immset = immset, imm = imm`4,
    hl = hl, addrset = addrset, addrsel = addrsel`1
}
#fn op_reg(opsel)         => operand(opsel, false, 0, hl_none(), false, 1)
#fn op_imm(opsel, i)      => operand(opsel, true,  i, hl_none(), false, 1)
#fn op_absaddr(opsel, a)  => operand(opsel, true,  a, hl_none(), true,  0)
#fn op_hladdr(opsel, hl)  => operand(opsel, false, 0, hl,        true,  1)

#subruledef hl
{
    (hl++) => hl_step(0)
    (hl--) => hl_step(1)
}

#subruledef addr_hl
{
    hl => hl_none()
    hl++ => hl_step(0)
    hl-- => hl_step(1)
}

; operands valid as both source and destination
#subruledef operand
{
    X => op_reg(0)
    ; ALU => 1
    H => op_reg(2)
    L => op_reg(3)
    IO[#{addr:u4}] => op_absaddr(4, addr)
    IO[{hl:addr_hl}] => op_hladdr(4, hl)
    Mem[#{addr:u4}] => op_absaddr(5, addr)
    Mem[{hl:addr_hl}] => op_hladdr(5, hl)
    Flags => op_reg(6)
}

#subruledef movsrc
{
    {o:operand} => o
    #{i:u4} => op_imm(7, i)
}

#subruledef dst
{
    {o:operand} => o
    Y => op_reg(1)
    None => op_reg(7)
}

; ALU operation
;   alu:   4-bit ALU function select
;   logic: 0 = arithmetic mode, 1 = logic mode
#subruledef arithop
{
    sub => struct { alu = 6`4, logic = 0`1 }
    add => struct { alu = 9`4, logic = 0`1 }
}

#subruledef logicop
{
    or   => struct { alu = 14`4, logic = 1`1 }
    and  => struct { alu = 11`4, logic = 1`1 }
    xor  => struct { alu = 6`4,  logic = 1`1 }
    nor  => struct { alu = 1`4,  logic = 1`1 }
    nand => struct { alu = 4`4,  logic = 1`1 }
    xnor => struct { alu = 9`4,  logic = 1`1 }
}

#subruledef unaryxop
{
    shl => struct { alu = 12`4, logic = 0`1 }
    dec => struct { alu = 15`4, logic = 0`1 }
    not => struct { alu = 0`4,  logic = 1`1 }
}

#subruledef unaryyop
{
    mov => struct { alu = 10`4, logic = 1`1 }
    not => struct { alu = 5`4,  logic = 1`1 }
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
    le => 6`3
    mp => 7`3 ; heh heh
}

; carry: carry in from previous op
; bcd:   BCD mode (also the required HL direction)
#fn dc_none() => struct { carry = 0`1, bcd = 0`1 }
#subruledef bcdcarry
{
    c  => struct { carry = 1`1, bcd = 0`1 }
    d  => struct { carry = 0`1, bcd = 1`1 }
    cd => struct { carry = 1`1, bcd = 1`1 }
}

#fn mov_encode(hl, d, s) => {
    $assert(hl_ok(hl, d.hl), "Instruction suffix and destination cannot both increment or decrement HL")
    $assert(hl_ok(hl, s.hl), "Instruction suffix and source cannot both increment or decrement HL")
    $assert(hl_ok(d.hl, s.hl), "Destination and source cannot both increment or decrement HL")
    $assert(!(d.addrset && s.addrset) || d.addrsel == s.addrsel, "Destination and source memory address selection must match")
    $assert(!(d.immset && s.immset) || d.imm == s.imm, "Destination and source immediate values must match")

    imm = d.immset ? d.imm : s.imm
    hldir = hl.dir | d.hl.dir | s.hl.dir
    ninchl = bit(!(hl.set || d.hl.set || s.hl.set))
    addrsel = d.addrsel & s.addrsel
    imm @ 0b00 @ hldir`1 @ ninchl @ addrsel`1 @ d.opsel @ s.opsel @ 0b1
}

; bit 10 is carry, bit 9 is BCD mode / HL direction
#fn alu_encode(op, dc, hl, d) => {
    $assert(hl_ok(hl, d.hl), "Instruction suffix and destination cannot both increment or decrement HL")
    $assert(!hl.set || hl.dir == dc.bcd, "Instruction suffix HL increment/decrement must match BCD mode")
    $assert(!d.hl.set || d.hl.dir == dc.bcd, "Destination HL increment/decrement must match BCD mode")
    $assert(!d.immset || d.imm == op.alu, "Destination immediate value must match ALU operation")

    ninchl = bit(!(hl.set || d.hl.set))
    op.alu @ op.logic @ dc.carry @ dc.bcd @ ninchl @ d.addrsel @ d.opsel @ 1`3 @ 0b1
}

#ruledef
{
    ; jump instructions (bit 0 = 0)
    nop => 0x0000

    j{cond: jmpcond} {addr: u12} => addr @ cond @ 0b0

    ; non-jump instructions (bit 0 = 1)
    ; nop(hl++) = mov(hl++) None, X
    nop{hl: hl} => mov_encode(hl, op_reg(7), op_reg(0))

    mov       {d: dst}, {s: movsrc} => mov_encode(hl_none(), d, s)
    mov{hl:hl} {d: dst}, {s: movsrc} => mov_encode(hl, d, s)

    {op:arithop}                     {d:dst}, x, y => alu_encode(op, dc_none(), hl_none(), d)
    {op:arithop}{hl:hl}              {d:dst}, x, y => alu_encode(op, dc_none(), hl, d)
    {op:arithop}{dc:bcdcarry}        {d:dst}, x, y => alu_encode(op, dc, hl_none(), d)
    {op:arithop}{dc:bcdcarry}{hl:hl} {d:dst}, x, y => alu_encode(op, dc, hl, d)

    {op:logicop}         {d:dst}, x, y => alu_encode(op, dc_none(), hl_none(), d)
    {op:logicop}{hl:hl}  {d:dst}, x, y => alu_encode(op, dc_none(), hl, d)

    {op:unaryxop}        {d:dst}, x => alu_encode(op, dc_none(), hl_none(), d)
    {op:unaryxop}{hl:hl} {d:dst}, x => alu_encode(op, dc_none(), hl, d)

    {op:unaryyop}        {d:dst}, y => alu_encode(op, dc_none(), hl_none(), d)
    {op:unaryyop}{hl:hl} {d:dst}, y => alu_encode(op, dc_none(), hl, d)
}
