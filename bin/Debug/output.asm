ZERO .INT 0
ONE .INT 1
A0 .INT 1
A1 .INT 0
A2 .INT -99
N8 .INT 7
N13 .INT 9
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
JMP F11
TRP 0
X5 ADI SP -8
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
JMP S2
MOV R0 FP
ADI R0 -8
LDR R1 R0
LDR R2 FP
MOV R3 FP
ADI R3 -4
LDR FP R3
STR R1 SP
JMR R2
S2 ADI SP -0
MOV R0 SL
ADI SL 4
MOV R1 FP
ADI R1 -12
STR R0 R1
MOV R0 FP
ADI R0 -12
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
JMP X9
MOV R2 FP
ADI R2 -16
LDR R1 SP
STR R1 R2
MOV R1 FP
ADI R1 -8
LDR R2 R1
ADI R2 0
MOV R3 FP
ADI R3 -16
LDR R4 R3
STR R4 R2
LDR R0 FP
MOV R1 FP
ADI R1 -4
LDR FP R1
JMR R0
X9 ADI SP -12
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
JMP S3
MOV R0 FP
ADI R0 -8
LDR R1 R0
LDR R2 FP
MOV R3 FP
ADI R3 -4
LDR FP R3
STR R1 SP
JMR R2
M10 ADI SP -0
MOV R1 FP
ADI R1 -8
LDR R2 R1
ADI R2 0
LDR R0 R2
LDR R3 FP
MOV R4 FP
ADI R4 -4
LDR FP R4
STR R0 SP
JMR R3
LDR R0 FP
MOV R1 FP
ADI R1 -4
LDR FP R1
JMR R0
S3 ADI SP -0
MOV R1 FP
ADI R1 -8
LDR R2 R1
ADI R2 0
LDR R3 N8
STR R3 R2
LDR R0 FP
MOV R1 FP
ADI R1 -4
LDR FP R1
JMR R0
F11 ADI SP -32
MOV R0 SL
ADI SL 4
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
JMP X5
MOV R2 FP
ADI R2 -20
LDR R1 SP
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
MOV R2 FP
ADI R2 -24
STR R1 R2
MOV R0 FP
ADI R0 -24
LDR R1 R0
LDR R1 R1
ADI R1 0
MOV R2 FP
ADI R2 -28
STR R1 R2
LDR R0 N13
MOV R1 FP
ADI R1 -28
LDR R2 R1
LDR R2 R2
ADD R0 R2
MOV R3 FP
ADI R3 -32
STR R0 R3
MOV R0 FP
ADI R0 -28
LDR R1 R0
MOV R2 FP
ADI R2 -32
LDR R3 R2
STR R3 R1
MOV R0 FP
ADI R0 -24
LDR R1 R0
LDR R1 R1
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
JMP M10
MOV R2 FP
ADI R2 -40
LDR R1 SP
STR R1 R2
MOV R0 FP
ADI R0 -40
LDR R1 R0
MOV R3 R1
TRP 1
LDR R0 FP
MOV R1 FP
ADI R1 -4
LDR FP R1
JMR R0
