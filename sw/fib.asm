#include "ttlcalc2.asm"

#bank data

src1l: #res 1
src1h: #res 1
src2l: #res 1
src2h: #res 1
dstl: #res 1
dsth: #res 1

digcount: #res 1
tmpfl: #res 1

#bank program

start:
    ; turn on busy light, turn off rest
    mov h, #0
    mov l, #1
    mov(hl--) IO[#3], l
    mov IO[hl++], #0xf
    mov IO[hl++], #0xf
    mov IO[hl++], #0xf
    
    ; initialize RAM
    mov x, #0
    mov y, #0
    mov l, #0

.loop:
    mov Mem[hl++], #0
    mov x, l
    sub None, x, y
    jne .loop
    mov x, h
    sub None, x, y
    jne .loop

    ; initialize first two numbers as 1
    mov h, #1
    mov l, #8
    mov Mem[hl], #1
    mov l, #0
    mov Mem[hl], #1

    ; src1h:l = #10
    ; src2h:l = #18
    ; dsth:l = #20
    mov x, #0
    mov l, #1
    mov Mem[#src1l], x
    mov Mem[#src1h], l
    mov Mem[#src2h], l
    mov Mem[#dstl], x
    mov x, #8
    mov(hl++) Mem[#src2l], x
    mov Mem[#dsth], l

numloop:
    mov x, #0
    mov Mem[#tmpfl], x
    mov x, #8
    mov Mem[#digcount], x 
digloop:
    mov x, Mem[#digcount]
    dec x, x
    je .digsdone
    mov Mem[#digcount], x

    mov h, Mem[#src1h]
    mov l, Mem[#src1l]
    mov x, Mem[hl++]
    mov Mem[#src1h], h
    mov Mem[#src1l], l
    mov h, Mem[#src2h]
    mov l, Mem[#src2l]
    mov y, Mem[hl++]
    mov Mem[#src2h], h
    mov Mem[#src2l], l
    mov h, Mem[#dsth]
    mov l, Mem[#dstl]
    mov Flags, Mem[#tmpfl]
    addcd Mem[hl], x, y
    mov(hl++) Mem[#tmpfl], Flags
    mov Mem[#dsth], h
    mov Mem[#dstl], l
    jmp digloop
.digsdone:
    mov x, h
    dec None, x
    jne numloop
