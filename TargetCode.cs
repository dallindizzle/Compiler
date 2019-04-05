using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Compiler
{
    class TargetCode
    {
        Dictionary<string, Symbol> symTable;
        List<List<string>> iQuads;
        List<List<string>> tQuads = new List<List<string>>();
        string[] registers = new string[12];

        enum MemoryLocations
        {
            register,
            stack,
            heap,
            memory
        };

        public TargetCode(List<List<string>> ic, Dictionary<string, Symbol> st)
        {
            symTable = st;
            iQuads = ic;
        }

        public void go()
        {
            // First generate data segment
            SetConstants();

            for (int i = 0; i < iQuads.Count; i++)
            {
                var quad = iQuads[i];

                switch (quad[0])
                {
                    case "MOV":
                        MovCase(quad);
                        break;

                    case "FRAME":
                        FrameCase(quad);
                        break;

                    case "CALL":
                        CallCase(quad, iQuads[i + 1]);
                        break;

                    case "ADD":
                        AddCase(quad);
                        break;

                    case "WRITE 1":
                        Write1Case(quad);
                        break;

                    default:
                        FuncCase(quad);
                        break;
                }

                ResetRegisters();
            }

            tQuads.Add(new List<string>() { "TRP", "0" });
        }

        void ResetRegisters()
        {
            for (int i = 0; i < registers.Length; i++)
            {
                registers[i] = "";
            }
        }

        private void SetConstants()
        {
            var literalKeys = symTable.Where(sym => sym.Value.Scope == "g" && sym.Value.Kind == "ilit" || sym.Value.Kind == "clit").ToList();

            int offset = 0;
            foreach (var lit in literalKeys)
            {
                if (lit.Value.Kind == "ilit")
                {
                    if (lit.Value.Value == "true") lit.Value.Value = "1";
                    else if (lit.Value.Value == "false") lit.Value.Value = "0";
                    else if (lit.Value.Value == "null") lit.Value.Value = "-99";

                    tQuads.Add(new List<string>() { lit.Key, ".INT", lit.Value.Value });
                    //symTable[lit.Key].Data["location"] = new Tuple<MemoryLocations, int>(MemoryLocations.memory, offset);
                    offset += 4;
                }
                else if (lit.Value.Kind == "clit")
                {
                    tQuads.Add(new List<string>() { lit.Key, ".BYT", lit.Value.Value });
                    //symTable[lit.Key].Data["location"] = new Tuple<MemoryLocations, int>(MemoryLocations.memory, offset);
                    offset += 1;
                }
            }
        }

        private int getRegister(string symKey)
        {
            int reg = -1;

            for (int i = 0; i < 8; i++)
            {
                if (registers[i] == "" || registers[i] == null)
                {
                    reg = i;
                    break;
                }
            }

            registers[reg] = symKey;

            return reg;
        }


        private Tuple<MemoryLocations, int> getLocation(string symKey)
        {
            if (symTable[symKey].Data.ContainsKey("location")) return symTable[symKey].Data["location"];

            string varScope = symTable[symKey].Scope;

            if (varScope == "g")
            {
                return new Tuple<MemoryLocations, int>(MemoryLocations.memory, -1);
            }

            var fellowSyms = symTable.Where(sym => sym.Value.Scope == varScope).ToList();

            int offset = 0;
            foreach (var sym in fellowSyms)
            {
                if (sym.Key == symKey) break;
                offset += -4;
            }

            if (varScope.Split('.')[1] == "main")
            {
                Tuple<MemoryLocations, int> location = new Tuple<MemoryLocations, int>(MemoryLocations.stack, offset - 12);
                symTable[symKey].Data["location"] = location;
                return location;
            }

            return new Tuple<MemoryLocations, int>(MemoryLocations.stack, offset - 12);
        }

        private int SizeOfFunc(string funcKey)
        {
            string funScope = "g." + symTable[funcKey].Value;

            int size = symTable.Where(sym => sym.Value.Scope == funScope).Count();

            return size * 4;
        }

        void FrameCase(List<string> quad)
        {
            // test for overflow

            // FRAME
            string register = $"R{getRegister(quad[1])}";
            tQuads.Add(new List<string>() { "MOV", register, "FP" });
            tQuads.Add(new List<string>() { "MOV", "FP", "SP" });
            tQuads.Add(new List<string>() { "ADI", "SP", "-4" });
            tQuads.Add(new List<string>() { "STR", register, "SP" });
            tQuads.Add(new List<string>() { "ADI", "SP", "-4" });
            tQuads.Add(new List<string>() { "STR", "R7", "SP" });
            tQuads.Add(new List<string>() { "ADI", "SP", "-4" }); // Setting "this" onto the stack (probably change later)

        }

        void CallCase(List<string> quad, List<string> quad2)
        {
            string register = $"R{getRegister("CALL")}";

            int returnAddress = 0;
            if (quad2[0] == "PEAK") returnAddress = 7 * 12;
            else returnAddress = 3 * 12;

            tQuads.Add(new List<string>() { "MOV", register, "PC" });
            tQuads.Add(new List<string>() { "ADI", register, returnAddress.ToString() });
            tQuads.Add(new List<string>() { "STR", register, "FP" });
            tQuads.Add(new List<string>() { "JMP", quad[1] });
        }

        void FuncCase(List<string> quad)
        {
            int funcSize = SizeOfFunc(quad[0]);
            tQuads.Add(new List<string>() { quad[0], "ADI", "SP", $"-{funcSize}" });

            // Test overflow here


        }

        void MovCase(List<string> quad)
        {
            int register = getRegister(quad[2]);
            Tuple<MemoryLocations, int> location = getLocation(quad[2]);

            if (location.Item1 == MemoryLocations.stack)
            {
                tQuads.Add(new List<string>() { "MOV", $"R{register}", "FP" });
                tQuads.Add(new List<string>() { "ADI", $"R{register}", $"{location.Item2}" });
            }

            // Load into a register the second operand
            int register2 = getRegister(quad[1]);
            Tuple<MemoryLocations, int> location2 = getLocation(quad[1]);

            if (location2.Item1 == MemoryLocations.memory)
            {
                if (symTable[quad[1]].Data["type"] == "int")
                {
                    tQuads.Add(new List<string>() { "LDR", $"R{register2}", quad[1] });
                }
            }

            if (symTable[quad[2]].Data["type"] == "int")
            {
                tQuads.Add(new List<string>() { "STR", $"R{register2}", $"R{register}" });
            }
        }

        void AddCase(List<string> quad)
        {
            int register = getRegister(quad[1]);
            Tuple<MemoryLocations, int> location = getLocation(quad[1]);
            if (location.Item1 == MemoryLocations.stack)
            {
                tQuads.Add(new List<string>() { "MOV", $"R{register}", "FP" });
                tQuads.Add(new List<string>() { "ADI", $"R{register}", $"{location.Item2}" });
            }
            string register1Value = "R" + getRegister("R1Val");
            tQuads.Add(new List<string>() { "LDR", register1Value, "R" + register });

            string register2 = "R" + getRegister(quad[2]);
            Tuple<MemoryLocations, int> location2 = getLocation(quad[2]);
            if (location.Item1 == MemoryLocations.stack)
            {
                tQuads.Add(new List<string>() { "MOV", register2, "FP" });
                tQuads.Add(new List<string>() { "ADI", register2, $"{location2.Item2}" });
            }
            string register2Value = "R" + getRegister("R2Val");
            tQuads.Add(new List<string>() { "LDR", register2Value, register2 });

            tQuads.Add(new List<string>() { "ADD", register1Value, register2Value });

            string register3 = "R" + getRegister(quad[3]);
            Tuple<MemoryLocations, int> location3 = getLocation(quad[3]);
            if (location.Item1 == MemoryLocations.stack)
            {
                tQuads.Add(new List<string>() { "MOV", register3, "FP" });
                tQuads.Add(new List<string>() { "ADI", register3, $"{location3.Item2}" });
            }

            tQuads.Add(new List<string>() { "STR", register1Value, register3 });
        }

        void Write1Case(List<string> quad)
        {
            int register = getRegister(quad[1]);
            Tuple<MemoryLocations, int> location = getLocation(quad[1]);

            if (location.Item1 == MemoryLocations.stack)
            {
                tQuads.Add(new List<string>() { "MOV", $"R{register}", "FP" });
                tQuads.Add(new List<string>() { "ADI", $"R{register}", $"{location.Item2}" });
            }

            tQuads.Add(new List<string>() { "LDR", "R3", "R" + register });

            tQuads.Add(new List<string>() { "TRP", "1" });
        }

        public void PrintTCode()
        {
            using (StreamWriter sw = new StreamWriter("output.asm"))
            {
                 foreach (var quad in tQuads)
                {
                    sw.WriteLine(string.Join(" ", quad));
                }
            }

            foreach (var quad in tQuads)
            {
                Console.WriteLine(string.Join(" ", quad));
            }
        }
    }
}
