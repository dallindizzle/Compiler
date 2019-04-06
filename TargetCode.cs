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
        int labelCount = 0;
        Stack<string> labels = new Stack<string>();

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
                // vars for placing labels
                bool addLabel = false;
                int labelLoc = -1;

                var quad = iQuads[i];
                string instruction;
                if (quad[1] == "FUNC") instruction = "FUNC";
                else instruction = quad[0];

                Restart: switch (instruction)
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
                    case "SUB":
                    case "MUL":
                    case "DIV":
                        MathCase(quad);
                        break;

                    case "GT":
                        GreaterThanCase(quad);
                        break;

                    case "BF":
                        BranchFalseCase(quad);
                        break;

                    case "WRITE 1":
                        Write1Case(quad);
                        break;

                    case "FUNC":
                        FuncCase(quad);
                        break;

                    default:
                        labels.Push(quad[0]); // This means that there is a label
                        labelLoc = tQuads.Count;
                        addLabel = true;
                        quad.RemoveAt(0);
                        instruction = quad[0];
                        goto Restart;
                }

                if (addLabel)
                {
                    tQuads[labelLoc].Insert(0, labels.Pop());
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
            // Add 1 and 0 to literals
            tQuads.Add(new List<string>() { "ZERO", ".INT", "0" });
            tQuads.Add(new List<string>() { "ONE", ".INT", "1" });

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

        // Creates instructions to fetch a variable from memory and load it into a register. The register string is returned
        string FetchAndLoadValue(string symKey)
        {
            // TODO: Add case for loading a BYT

            string register = "R" + getRegister(symKey);
            Tuple<MemoryLocations, int> location = getLocation(symKey);

            if (location.Item1 == MemoryLocations.stack)
            {
                tQuads.Add(new List<string>() { "MOV", register, "FP" });
                tQuads.Add(new List<string>() { "ADI", register, $"{location.Item2}" });
            }
            else if (location.Item1 == MemoryLocations.memory)
            {
                tQuads.Add(new List<string>() { "LDR", register, symKey });
                return register;
            }

            string registerValue = "R" + getRegister("R1Val");
            tQuads.Add(new List<string>() { "LDR", registerValue, register });

            return registerValue;
        }

        string FetchAndLoadAddress(string symKey)
        {
            string register = "R" + getRegister(symKey);
            Tuple<MemoryLocations, int> location = getLocation(symKey);

            if (location.Item1 == MemoryLocations.stack)
            {
                tQuads.Add(new List<string>() { "MOV", $"{register}", "FP" });
                tQuads.Add(new List<string>() { "ADI", $"{register}", $"{location.Item2}" });
            }

            return register;
        }

        void MathCase(List<string> quad)
        {
            string register1Value = FetchAndLoadValue(quad[1]);

            string register2Value = FetchAndLoadValue(quad[2]);

            tQuads.Add(new List<string>() { quad[0], register1Value, register2Value });

            string register3 = FetchAndLoadAddress(quad[3]);

            tQuads.Add(new List<string>() { "STR", register1Value, register3 });
        }

        string genLabel(string l)
        {
            labelCount++;
            return $"{l}{labelCount}";
        }

        void GreaterThanCase(List<string> quad)
        {
            string register1 = FetchAndLoadValue(quad[1]);
            string register2 = FetchAndLoadValue(quad[2]);
            tQuads.Add(new List<string>() { "CMP", register1, register2 });

            string label = genLabel("L");

            tQuads.Add(new List<string>() { "BGT", register1, label });

            // Set FALSE
            string label2 = genLabel("L");
            string tempRegister = "R" + getRegister("temp");
            tQuads.Add(new List<string>() { "MOV", tempRegister, "ZERO"});
            string register3 = FetchAndLoadAddress(quad[3]);
            tQuads.Add(new List<string>() { "STR", tempRegister, register3 });
            tQuads.Add(new List<string>() { "JMP", label2 });
            labels.Push(label2); // Add to labels Stack so that in next BF we will use that label

            // Set TRUE
            tQuads.Add(new List<string>() { label, "MOV", tempRegister, "ONE" });
            tQuads.Add(new List<string>() { "STR", tempRegister, register3 });
           
        }

        void BranchFalseCase(List<string> quad)
        {
            string register1 = FetchAndLoadValue(quad[1]);
            tQuads[tQuads.Count - 3].Insert(0, labels.Pop());
            tQuads.Add(new List<string>() { "BRZ", register1, quad[2] });
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
