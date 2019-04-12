using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Compiler
{
    class Assembler
    {
        Dictionary<string, int> symbols;
        public int SIZE;
        public int PC;
        public int code;
        bool codeSet;
        public byte[] mem;
        string[] instSym = new string[] { "ADD", "ADI", "SUB", "MUL", "DIV", "AND", "OR", "CMP", "TRP", "MOV", "LDA", "STR", "LDR", "LDB", "JMP", "JMR", "BRZ", "BNZ", "BGT", "STB", "BLT", "LCK", "ULK", "END", "BLK", "RUN" };
        string[] regSym = new string[] { "PC", "SL", "SP", "FP", "SB" };

        public Assembler(int size)
        {
            symbols = new Dictionary<string, int>();
            PC = 0;
            code = 0;
            codeSet = false;
            SIZE = size;
            mem = new byte[SIZE];
        }

        public void PassOne(string file)
        {
            StreamReader reader = new StreamReader(file);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // This parses the line of assembly code by splitting by the a comment (#) and then trimming the line, then splitting by whitepace
                var tokens = line.Split('#')[0].Trim().Split(' ');

                tokens = tokens.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                if (tokens[0] == "BLK" || tokens[0] == "END" || tokens[1] == "BLK" || tokens[1] == "END")
                {
                    if (tokens.Length > 1)
                    {
                        if (tokens[1] == "END" || tokens[1] == "BLK")
                        {
                            symbols.Add(tokens[0], PC);
                        }
                    }
                    PC += 12;
                    continue;
                }

                // Check if line is a Directive by checking the length of the line and checking if it has an operation at the begining
                if (tokens.Length <= 3 && (!instSym.Contains(tokens[0])) && (!instSym.Contains(tokens[1])))
                {
                    // If the directive has a label then add it to the symbol table
                    if (tokens[0] != ".INT" && tokens[0] != ".BYT")
                    {
                        symbols.Add(tokens[0], PC);
                        PC += (tokens[1] == ".INT") ? 4 : 1;
                        continue;
                    }


                    // Incremenet PC counter by either 4 if INT or 1 if BYTE
                    PC += (tokens[0] == ".INT") ? 4 : 1;
                }
                else
                {
                    if (!codeSet) { code = PC; codeSet = true; }
                    if (!instSym.Contains(tokens[0]))
                    {
                        symbols.Add(tokens[0], PC);
                    }
                    PC += 12;
                }
            }

        }

        public void PassTwo(string file)
        {
            PC = 0;

            StreamReader reader = new StreamReader(file);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // This parses the line of assembly code by splitting by the a comment (#) and then trimming the line, then splitting by whitepace
                var tokens = line.Split('#')[0].Trim().Split(' ');

                tokens = tokens.Where(x => !string.IsNullOrEmpty(x)).ToArray();

                if (tokens[0] == "BLK" || tokens[0] == "END" || tokens[1] == "BLK" || tokens[1] == "END")
                {
                    byte[] inst;

                    if (tokens[0] == "BLK" || tokens[0] == "END")
                    {
                        inst = CreateInstruction(tokens[0], "", "");
                    }
                    else
                    {
                        inst = CreateInstruction(tokens[1], "", "");
                    }
                    InsertMem(inst, PC);
                    PC += 12;
                    continue;
                }

                if (tokens.Length <= 3 && (!instSym.Contains(tokens[0])) && (!instSym.Contains(tokens[1])))
                {

                    // If the directive has a label then add it to the symbol table
                    if (tokens[0] != ".INT" && tokens[0] != ".BYT")
                    {
                        if (tokens[1] == ".INT") InsertMem(int.Parse(tokens[2]), symbols[tokens[0]]);
                        else
                        {
                            //Insert the value into memory using the symbols table
                            if (int.TryParse(tokens[2], out int i)) InsertChar(int.Parse(tokens[2]), symbols[tokens[0]]);
                            else
                            {
                                if (tokens[2][0] == '\'' && tokens[2].Length > 1) InsertMem(tokens[2][1], symbols[tokens[0]]);
                                else InsertMem(tokens[2][0], symbols[tokens[0]]);
                            }
                        }
                        PC += (tokens[1] == ".INT") ? 4 : 1;
                        continue;
                    }

                    if (tokens[0] == ".INT") InsertMem(int.Parse(tokens[1]), PC);
                    else
                    {
                        if (int.TryParse(tokens[1], out int i)) InsertChar(int.Parse(tokens[1]), PC);
                        else InsertMem(tokens[1][0], PC);
                    }

                    PC += (tokens[0] == ".INT") ? 4 : 1;

                }
                else
                {
                    // Insert the intruction into memory at the PC counter and then increment the PC counter by 12

                    byte[] inst;
                    if (tokens.Length > 2)
                    {
                        if (!instSym.Contains(tokens[0]) && tokens.Length == 3) inst = CreateInstruction(tokens[1], tokens[2]);
                        else if (!instSym.Contains(tokens[0])) inst = CreateInstruction(tokens[1], tokens[2], tokens[3]);
                        else inst = CreateInstruction(tokens[0], tokens[1], tokens[2]);
                    }
                    else inst = CreateInstruction(tokens[0], tokens[1]);

                    InsertMem(inst, PC);
                    PC += 12;
                }
            }

        }

        // Insert an INT into memory
        void InsertMem(int input, int location)
        {
            var bytes = BitConverter.GetBytes(input);

            int j = 0;
            for (int i = location; i < location + 4; i++)
            {
                mem[i] = bytes[j++];
            }
        }

        // Insert a CHAR into memory
        void InsertMem(char input, int location)
        {
            var bytes = (byte)input;
            mem[location] = bytes;
        }

        // Insert an ASCII char into memory
        void InsertChar(int input, int location)
        {
            var bytes = (byte)input;
            mem[location] = bytes;
        }

        // Insert byte array into memory. I'm pretty sure this is used to insert instructions into memeory
        void InsertMem(byte[] input, int location)
        {
            input.CopyTo(mem, location);
        }

        byte[] CreateInstruction(string op, string op1, string op2 = "")
        {
            int opInt = 0;
            int op1Int;
            int op2Int;
            switch (op)
            {
                case "JMP":
                    opInt = 1;
                    break;

                case "JMR":
                    opInt = 2;
                    break;

                case "BNZ":
                    opInt = 3;
                    break;

                case "BGT":
                    opInt = 4;
                    break;

                case "BLT":
                    opInt = 5;
                    break;

                case "BRZ":
                    opInt = 6;
                    break;

                case "MOV":
                    opInt = 7;
                    break;

                case "LDA":
                    opInt = 8;
                    break;

                case "STR":
                    if (op2[0] == 'R' || regSym.Contains(op2)) opInt = 22;
                    else opInt = 9;
                    break;

                case "STB":
                    if (op2[0] == 'R' || regSym.Contains(op2)) opInt = 24;
                    else opInt = 11;
                    break;

                case "LDR":
                    if (op2[0] == 'R' || regSym.Contains(op2)) opInt = 23;
                    else opInt = 10;
                    break;

                case "ADI":
                    opInt = 14;
                    break;

                case "LDB":
                    if (op2[0] == 'R') opInt = 25;
                    else opInt = 12;
                    break;

                case "TRP":
                    opInt = 21;
                    break;

                case "ADD":
                    opInt = 13;
                    break;

                case "SUB":
                    opInt = 15;
                    break;

                case "MUL":
                    opInt = 16;
                    break;

                case "DIV":
                    opInt = 17;
                    break;

                case "CMP":
                    opInt = 20;
                    break;

                case "RUN":
                    opInt = 26;
                    break;

                case "END":
                    opInt = 27;
                    break;

                case "BLK":
                    opInt = 28;
                    break;

                case "LCK":
                    opInt = 29;
                    break;

                case "ULK":
                    opInt = 30;
                    break;

                case "OR":
                    opInt = 31;
                    break;

                case "AND":
                    opInt = 32;
                    break;
            }

            if (opInt == 27 || opInt == 28)
            {
                op1Int = 0;
                op2Int = 0;

                var m = BitConverter.GetBytes(opInt);
                byte[] mbytes = new byte[12];
                m.CopyTo(mbytes, 0);
                return mbytes;
            }

            // If the op is a TRP then we do this different thing here
            if (opInt == 21)
            {
                op1Int = int.Parse(op1);
                var t = BitConverter.GetBytes(opInt);
                var t1 = BitConverter.GetBytes(op1Int);
                byte[] tBytes = new byte[12];
                t.CopyTo(tBytes, 0);
                t1.CopyTo(tBytes, 4);
                return tBytes;
            }
            else if (opInt == 1 || opInt == 2 || opInt == 29 || opInt == 30)
            {
                if (op1[0] == 'R') op1Int = (int)char.GetNumericValue(op1[1]);
                else if (op1 == "PC")
                    op1Int = 8;
                else if (op1 == "SL")
                    op1Int = 9;
                else if (op1 == "SP")
                    op1Int = 10;
                else if (op1 == "FP")
                    op1Int = 11;
                else if (op1 == "SB")
                    op1Int = 12;
                else op1Int = symbols[op1];

                byte[] jBytes = new byte[12];
                var j = BitConverter.GetBytes(opInt);
                var j1 = BitConverter.GetBytes(op1Int);
                j.CopyTo(jBytes, 0);
                j1.CopyTo(jBytes, 4);
                return jBytes;
            }

            if (op1[0] == 'R') op1Int = (int)char.GetNumericValue(op1[1]);
            else if (op1 == "PC")
                op1Int = 8;
            else if (op1 == "SL")
                op1Int = 9;
            else if (op1 == "SP")
                op1Int = 10;
            else if (op1 == "FP")
                op1Int = 11;
            else if (op1 == "SB")
                op1Int = 12;
            else op1Int = symbols[op1];

            if (opInt == 14)
                //op2Int = (int)char.GetNumericValue(op2[0]);
                op2Int = Int32.Parse(op2);
            else
            {
                if (op2[0] == 'R') op2Int = (int)char.GetNumericValue(op2[1]);
                else if (op2 == "PC")
                    op2Int = 8;
                else if (op2 == "SL")
                    op2Int = 9;
                else if (op2 == "SP")
                    op2Int = 10;
                else if (op2 == "FP")
                    op2Int = 11;
                else if (op2 == "SB")

                    op2Int = 12;
                else op2Int = symbols[op2];
            }

            var b = BitConverter.GetBytes(opInt);
            var b1 = BitConverter.GetBytes(op1Int);
            var b2 = BitConverter.GetBytes(op2Int);

            byte[] bytes = new byte[12];
            b.CopyTo(bytes, 0);
            b1.CopyTo(bytes, 4);
            b2.CopyTo(bytes, 8);

            return bytes;
        }
    }

    class VM
    {
        static public void go(string file)
        {
            //if (args.Length < 1) { Console.WriteLine("No arguments"); return; }

            //string file = "proj4.asm";

            Assembler assembler = new Assembler(100000);
            assembler.PassOne(file);
            assembler.PassTwo(file);
            //assembler.PassOne(args[0]);
            //assembler.PassTwo(args[0]);

            VM vm = new VM(assembler.code, assembler.PC, assembler.SIZE, assembler.mem);
            //try
            //{
                vm.Run();
            //}
            //catch (Exception e)
            //{
                //Console.WriteLine(e.Message);
            //}
            //Console.ReadKey();
        }

        int[] reg;
        byte[] mem;
        int memSize;
        bool[] threads;
        int curThread;
        int threadSize;
        bool blk;

        VM(int startIndex, int stackLimit, int size, byte[] memory)
        {
            reg = new int[13];

            memSize = size - 4;

            threads = new bool[5];

            for (int i = 1; i < threads.Length; i++)
            {
                threads[i] = false;
            }

            curThread = 0;

            threads[0] = true; // this is our MAIN THREAD

            threadSize = 10000;

            blk = false; // If this is true, then the main thread will be blocked from running until other threads are done.

            // Register 8 will be the PC register
            reg[8] = startIndex;

            // Register 9 is the Stack Limit register
            reg[9] = memSize - (threadSize * curThread) - threadSize;

            // Register 10 will be the the Stack Pointer which will point at the top of the stack. Right now there is nothing on the top of the stack so this points to the "bottom"
            reg[10] = size - 56;

            // Register 11 is the Frame Pointer which points to the bottom of the current frame
            reg[11] = size - 56;

            //Register 12 will be the Stack "Bottom"
            reg[12] = size - 56;
            mem = memory;
        }

        void Run()
        {

            bool running = true;

            while (running)
            {
                var inst = Fetch();

                switch (inst[0])
                {
                    case 1:
                        JMP(inst);
                        break;

                    case 2:
                        JMR(inst);
                        break;

                    case 3:
                        BNZ(inst);
                        break;

                    case 4:
                        BGT(inst);
                        break;

                    case 5:
                        BLT(inst);
                        break;

                    case 6:
                        BRZ(inst);
                        break;

                    case 7:
                        MOV(inst);
                        break;

                    case 8:
                        LDA(inst);
                        break;

                    case 9:
                        STR(inst);
                        break;

                    case 10:
                        LDR(inst);
                        break;

                    case 11:
                        STB(inst);
                        break;

                    case 12:
                        LDB(inst);
                        break;

                    case 13:
                        ADD(inst);
                        break;

                    case 14:
                        ADI(inst);
                        break;

                    case 15:
                        SUB(inst);
                        break;

                    case 16:
                        MUL(inst);
                        break;

                    case 17:
                        DIV(inst);
                        break;

                    case 20:
                        CMP(inst);
                        break;

                    case 21:
                        if (inst[1] == 0) { running = false; break; }
                        TRP(inst);
                        break;

                    case 22:
                        STRadd(inst);
                        break;

                    case 23:
                        LDRadd(inst);
                        break;

                    case 24:
                        STBadd(inst);
                        break;

                    case 25:
                        LDBadd(inst);
                        break;

                    case 26:
                        RUN(inst);
                        continue;

                    case 27:
                        END(inst);
                        break;

                    case 28:
                        BLK(inst);
                        break;

                    case 29:
                        LCK(inst);
                        break;

                    case 30:
                        ULK(inst);
                        break;

                    case 31:
                        OR(inst);
                        break;

                    case 32:
                        AND(inst);
                        break;
                }
                threadSwitch(); // We thread switch here because we are doing Round Robin where we run 1 instruction each thread at a time
            }
        }

        int[] Fetch()
        {
            int opCode = BitConverter.ToInt32(mem, reg[8]);
            int op1 = BitConverter.ToInt32(mem, reg[8] + 4);
            int op2 = BitConverter.ToInt32(mem, reg[8] + 8);

            int[] inst = new int[] { opCode, op1, op2 };

            reg[8] += 12;

            return inst;
        }

        void threadSwitch()
        {
            int newThread = curThread;

            if (blk) checkBlk();

            for (int i = (curThread + 1) >= threads.Length ? 0 : curThread + 1; i < threads.Length; i++)
            {
                if (threads[i] == true)
                {
                    if (i == 0 && blk) continue;
                    newThread = i;
                    break;
                }

                if (i == threads.Length - 1)
                {
                    if (!blk)
                    {
                        newThread = 0;
                        break;
                    }
                    i = -1;
                }
            }

            if (newThread == curThread) return;

            //Store the current thread's register values into memory
            int threadLoc = memSize - (threadSize * curThread); // The memory location of the current thread.
            foreach (int regVal in reg) // Loop to go through registers and store them in memory
            {
                byte[] bytes = BitConverter.GetBytes(regVal);
                bytes.CopyTo(mem, threadLoc);
                threadLoc -= 4;
            }

            curThread = newThread;

            // Load the new current Thread with the register values stored in memory
            int newThreadLoc = memSize - (threadSize * curThread);
            for (int i = 0; i < reg.Length; i++)
            {
                int rval = BitConverter.ToInt32(mem, newThreadLoc);
                reg[i] = rval;
                newThreadLoc -= 4;
            }
        }

        // Method to check if created threads are done so main can start running agian
        void checkBlk()
        {
            for (int i = 1; i < threads.Length; i++)
            {
                if (threads[i] == true) return;
            }
            blk = false;
        }

        void JMP(int[] inst)
        {
            reg[8] = inst[1];
        }

        void JMR(int[] inst)
        {
            reg[8] = reg[inst[1]];
        }

        void BNZ(int[] inst)
        {
            if (reg[inst[1]] != 0) reg[8] = inst[2];
        }

        void BGT(int[] inst)
        {
            if (reg[inst[1]] > 0) reg[8] = inst[2];
        }

        void BLT(int[] inst)
        {
            if (reg[inst[1]] < 0) reg[8] = inst[2];
        }

        void BRZ(int[] inst)
        {
            if (reg[inst[1]] == 0) reg[8] = inst[2];
        }

        void MOV(int[] inst)
        {
            reg[inst[1]] = reg[inst[2]];
        }

        void LDA(int[] inst)
        {
            reg[inst[1]] = inst[2];
        }

        void LDB(int[] inst)
        {
            reg[inst[1]] = (char)mem[inst[2]];
        }

        void LDR(int[] inst)
        {
            reg[inst[1]] = BitConverter.ToInt32(mem, inst[2]);
        }

        void LDBadd(int[] inst)
        {
            reg[inst[1]] = (char)mem[reg[inst[2]]];
        }

        // This is the LDR function that adds the destination register with the value from the address in the source register
        void LDRadd(int[] inst)
        {
            reg[inst[1]] = BitConverter.ToInt32(mem, reg[inst[2]]);
        }

        void CMP(int[] inst)
        {
            reg[inst[1]] = reg[inst[1]] - reg[inst[2]];
        }

        void STBadd(int[] inst)
        {
            var bytes = (byte)reg[inst[1]];

            mem[reg[inst[2]]] = bytes;
        }

        void STB(int[] inst)
        {
            var bytes = (byte)reg[inst[1]];

            mem[inst[2]] = bytes;
        }

        void STR(int[] inst)
        {
            var bytes = BitConverter.GetBytes(reg[inst[1]]);

            int j = 0;
            for (int i = inst[2]; i < inst[2] + 4; i++)
            {
                mem[i] = bytes[j++];
            }

        }

        void STRadd(int[] inst)
        {
            var bytes = BitConverter.GetBytes(reg[inst[1]]);

            int j = 0;
            for (int i = reg[inst[2]]; i < reg[inst[2]] + 4; i++)
            {
                mem[i] = bytes[j++];
            }
        }

        void ADD(int[] inst)
        {
            reg[inst[1]] = reg[inst[1]] + reg[inst[2]];
        }

        void ADI(int[] inst)
        {
            reg[inst[1]] = reg[inst[1]] + inst[2];
        }

        void SUB(int[] inst)
        {
            reg[inst[1]] = reg[inst[1]] - reg[inst[2]];
        }

        void MUL(int[] inst)
        {
            reg[inst[1]] = reg[inst[1]] * reg[inst[2]];
        }

        void DIV(int[] inst)
        {
            reg[inst[1]] = reg[inst[1]] / reg[inst[2]];
        }

        void RUN(int[] inst)
        {
            int threadID = -1;

            for (int i = 1; i < threads.Length; i++)
            {
                if (threads[i] == false)
                {
                    threadID = i;
                    threads[i] = true;
                    break;
                }
            }

            if (threadID == -1) throw new Exception("Out of threads");

            int threadLoc = memSize - (threadSize * curThread); // The memory location of the current thread.
            foreach (int regVal in reg) // Loop to go through registers and store them in memory
            {
                byte[] bytes = BitConverter.GetBytes(regVal);
                bytes.CopyTo(mem, threadLoc);
                threadLoc -= 4;
            }

            int newThreadLoc = memSize - (threadSize * threadID);

            reg[8] = inst[2];
            reg[9] = memSize - (threadSize * threadID) - threadSize;
            reg[10] = newThreadLoc - 52;
            reg[11] = newThreadLoc - 52;
            reg[12] = newThreadLoc - 52;

            curThread = threadID;
        }

        void END(int[] inst)
        {
            if (curThread == 0) return;

            threads[curThread] = false;
        }

        void BLK(int[] inst)
        {
            blk = true;
        }

        void LCK(int[] inst)
        {
            int lockVal = BitConverter.ToInt32(mem, inst[1]);
            if (lockVal != curThread && lockVal != -1)
            {
                reg[8] -= 12; // Here we set the PC back to the last instruction because the mutex was locked
                return;
            }

            byte[] bytes = BitConverter.GetBytes(curThread);
            bytes.CopyTo(mem, inst[1]);
        }

        void ULK(int[] inst)
        {
            int lockVal = BitConverter.ToInt32(mem, inst[1]);
            if (lockVal != curThread) return;

            byte[] bytes = BitConverter.GetBytes(-1);
            bytes.CopyTo(mem, inst[1]);
        }

        void OR(int[] inst)
        {
            if (reg[inst[1]] == 1 || reg[inst[2]] == 1)
            {
                reg[inst[1]] = 1;
            }
            else reg[inst[1]] = 0;
        }

        void AND(int[] inst)
        {
            if (reg[inst[1]] == 1 && reg[inst[2]] == 1)
            {
                reg[inst[1]] = 1;
            }
            else reg[inst[1]] = 0;
        }

        void TRP(int[] inst)
        {
            if (inst[1] == 3)
            {
                char c = (char)reg[3];
                Console.Write(c);
            }
            else if (inst[1] == 2)
            {
                int input = int.Parse(Console.ReadLine());
                reg[3] = input;
            }
            else if (inst[1] == 1)
            {
                int i = reg[3];
                Console.Write(i);
            }
            else if (inst[1] == 4)
            {
                //char input = Console.ReadKey().KeyChar;
                char input = (char)Console.Read();
                if (input == '\r') input = '\n';
                reg[3] = input;
            }
            else if (inst[1] == 99)
            {
                Math.Abs(3); // This is purely here for a breakpoint
            }

        }
    }
}