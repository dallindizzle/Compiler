ZERO .INT 0
ONE .INT 1
A0 .INT 1
A1 .INT 0
A2 .INT -99
N5 .INT 3
N7 .INT 5
MOV R0 FP
MOV FP SP
ADI SP -4
STR R0 SP
ADI SP -4
STR R7 SP
ADI SP -4
MOV R0 PC
ADI R0 36
STR R0 FP
JMP F3
F3 ADI SP -12
MOV R0 FP
ADI R0 -12
LDR R1 N5
STR R1 R0
MOV R0 FP
ADI R0 -16
LDR R1 N7
STR R1 R0
MOV R0 FP
ADI R0 -12
LDR R1 R0
MOV R2 FP
ADI R2 -16
LDR R3 R2
CMP R1 R3
BGT R1 L1
MOV R4 ZERO
MOV R5 FP
ADI R5 -20
STR R4 R5
JMP L2
L1 MOV R4 ONE
STR R4 R5
L2 MOV R0 FP
ADI R0 -20
LDR R1 R0
BRZ R1 SKIPIF1
MOV R0 FP
ADI R0 -12
LDR R3 R0
TRP 1
JMP SKIPELSE2
SKIPIF1 MOV R0 FP
ADI R0 -16
LDR R3 R0
TRP 1
SKIPELSE2 TRP 0  
