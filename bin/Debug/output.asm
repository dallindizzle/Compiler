ZERO .INT 0
ONE .INT 1
A0 .INT 1
A1 .INT 0
A2 .INT -99
H5 .BYT 'y'
LDR R0 A2
MOV R1 FP
MOV FP SP
ADI SP -4
STR R1 SP
ADI SP -4
STR R0 SP
ADI SP -4
MOV R0 PC
ADI R0 36
STR R0 FP
JMP F7
TRP 0
X6 ADI SP -8
MOV R0 FP
ADI R0 -8
LDR R0 R0
MOV R1 FP
MOV FP SP
ADI SP -4
STR R1 SP
ADI SP -4
STR R0 SP
ADI SP -4
MOV R0 PC
ADI R0 36
STR R0 FP
JMP S0
LDR R0 FP
MOV R1 FP
ADI R1 -4
LDR FP R1
JMR R0
S0 ADI SP -0
MOV R1 FP
ADI R1 -8
LDR R2 R1
ADI R2 0
LDB R3 H5
STB R3 R2
LDR R0 FP
MOV R1 FP
ADI R1 -4
LDR FP R1
JMR R0
F7 ADI SP -16
MOV R0 SL
ADI SL 1
MOV R1 FP
ADI R1 -16
STR R0 R1
MOV R0 FP
ADI R0 -16
LDR R1 R0
MOV R2 FP
MOV FP SP
ADI SP -4
STR R2 SP
ADI SP -4
STR R1 SP
ADI SP -4
MOV R0 PC
ADI R0 36
STR R0 FP
JMP X6
MOV R0 SP
ADI R0 4
MOV R2 FP
ADI R2 -20
LDR R1 R0
STR R1 R2
MOV R0 FP
ADI R0 -12
MOV R1 FP
ADI R1 -20
LDR R2 R1
STR R2 R0
MOV R0 FP
ADI R0 -12
LDR R1 R0
ADI R1 0
LDR R1 R1
MOV R2 FP
ADI R2 -24
STR R1 R2
TRP 99
MOV R0 FP
ADI R0 -24
LDB R1 R0
MOV R3 R1
TRP 3
LDR R0 FP
MOV R1 FP
ADI R1 -4
LDR FP R1
JMR R0