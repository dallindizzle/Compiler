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
        Queue<string> labels = new Queue<string>();

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

            bool addOwnLabel = false;
            int ownLabelLoc = 0;
            for (int i = 0; i < iQuads.Count; i++)
            {
                // vars for placing labels
                bool addLabel = false;
                int labelLoc = -1;
                Stack<string> leadingLabel = new Stack<string>();

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

                    case "RTN":
                        RtnCase(quad);
                        break;

                    case "RETURN":
                        ReturnCase(quad);
                        break;

                    case "ADD":
                    case "SUB":
                    case "MUL":
                    case "DIV":
                    case "OR":
                    case "AND":
                        MathLogicCase(quad);
                        break;

                    case "GT":
                    case "LT":
                    case "EQ":
                    case "NE":
                    case "GE":
                    case "LE":
                        ConditionalCase(quad);
                        break;

                    case "BF":
                        BranchFalseCase(quad);
                        break;

                    case "JMP":
                        tQuads.Add(new List<string>() { quad[0], quad[1] });
                        break;

                    case "WRITE 1":
                        Write1Case(quad);
                        break;

                    case "WRITE 2":
                        Write2Case(quad);
                        break;

                    case "FUNC":
                        FuncCase(quad);
                        break;

                    case "NEWI":
                        NewInstanceCase(quad);
                        break;

                    case "PEEK":
                        PeekCase(quad);
                        break;

                    case "PUSH":
                        PushCase(quad);
                        break;

                    case "REF":
                        RefCase(quad);
                        break;

                    case "TRP":
                        tQuads.Add(quad);
                        break;

                    default:
                        //labels.Push(quad[0]); // This means that there is a label
                        leadingLabel.Push(quad[0]);
                        labelLoc = tQuads.Count;
                        addLabel = true;
                        quad.RemoveAt(0);
                        instruction = quad[0];
                        goto Restart;
                }

                if (addLabel)
                {
                    tQuads[labelLoc].Insert(0, leadingLabel.Pop());
                }
                if (addOwnLabel)
                {
                    tQuads[ownLabelLoc].Insert(0, labels.Dequeue());
                    addOwnLabel = false;
                }
                if (labels.Count > 0)
                {
                    addOwnLabel = true;
                    ownLabelLoc = tQuads.Count;
                }

                ResetRegisters();
            }

            //tQuads.Add(new List<string>() { "TRP", "0" });
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
            string varScope = symTable[symKey].Scope;

            if (symTable[symKey].Kind == "ivar")
            {
                var classSyms = symTable.Where(sym => sym.Value.Scope == varScope && sym.Value.Kind == "ivar").ToList();
                int heapOffset = 0;
                foreach (var sym in classSyms)
                {
                    if (sym.Key == symKey) break;
                    heapOffset += 4;
                }

                return new Tuple<MemoryLocations, int>(MemoryLocations.heap, heapOffset);
            }

            if (symTable[symKey].Data.ContainsKey("location")) return symTable[symKey].Data["location"];

            if (varScope == "g")
            {
                return new Tuple<MemoryLocations, int>(MemoryLocations.memory, -1);
            }

            if (symTable[symKey].Data.ContainsKey("heapLocation") && symTable[symKey].Kind != "lvar")
            {
                return new Tuple<MemoryLocations, int>(MemoryLocations.heap, symTable[symKey].Data["heapLocation"]);
            }

            //if (symTable[symKey].Kind == "ivar")
            //{
            //    var classSyms = symTable.Where(sym => sym.Value.Scope == varScope && sym.Value.Kind == "ivar").ToList();
            //    int heapOffset = 0;
            //    foreach (var sym in classSyms)
            //    {
            //        if (sym.Key == symKey) break;
            //        heapOffset += 4;
            //    }

            //    return new Tuple<MemoryLocations, int>(MemoryLocations.heap, heapOffset);
            //}

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

            string register2 = "";

            if (quad[2] == "this")
            {
                register2 = "R" + getRegister("this");
                tQuads.Add(new List<string>() { "MOV", register2, "FP" });
                tQuads.Add(new List<string>() { "ADI", register2, "-8" });
                tQuads.Add(new List<string>() { "LDR", register2, register2 });
            }
            else register2 = FetchAndLoadValue(quad[2]);

            if (quad[2][0] == 'r') {
                //tQuads.Add(new List<string>() { "TRP", "99" });
                tQuads.Add(new List<string>() { "LDR", register2, register2 });
            }

            // FRAME
            string register = $"R{getRegister(quad[1])}";
            tQuads.Add(new List<string>() { "MOV", register, "FP" });
            tQuads.Add(new List<string>() { "MOV", "FP", "SP" });
            tQuads.Add(new List<string>() { "ADI", "SP", "-4" });
            tQuads.Add(new List<string>() { "STR", register, "SP" });
            tQuads.Add(new List<string>() { "ADI", "SP", "-4" });

            tQuads.Add(new List<string>() { "STR", register2, "SP" });
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
            string register1;

            if (quad[2][0] == 'r') register1 = FetchAndLoadValue(quad[2]);
            else register1 = FetchAndLoadAddress(quad[2]);

            //int register = getRegister(quad[2]);
            //Tuple<MemoryLocations, int> location = getLocation(quad[2]);

            //if (location.Item1 == MemoryLocations.stack)
            //{
            //    tQuads.Add(new List<string>() { "MOV", $"R{register}", "FP" });
            //    tQuads.Add(new List<string>() { "ADI", $"R{register}", $"{location.Item2}" });
            //}

            string register2 = FetchAndLoadValue(quad[1]);

            if (quad[1][0] == 'r')
            {
                if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register2, register2 });
                else tQuads.Add(new List<string>() { "LDR", register2, register2 });
            }

            // Load into a register the second operand
            //int register2 = getRegister(quad[1]);
            //Tuple<MemoryLocations, int> location2 = getLocation(quad[1]);

            //if (location2.Item1 == MemoryLocations.memory)
            //{
            //    if (symTable[quad[1]].Data["type"] == "int")
            //    {
            //        tQuads.Add(new List<string>() { "LDR", $"R{register2}", quad[1] });
            //    }
            //}

            if (symTable[quad[2]].Data["type"] != "char")
            {
                tQuads.Add(new List<string>() { "STR", register2, register1 });
            }
            else tQuads.Add(new List<string>() { "STB", register2, register1 });
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
                if (symTable[symKey].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register, symKey });
                else tQuads.Add(new List<string>() { "LDR", register, symKey });
                return register;
            }
            else if (location.Item1 == MemoryLocations.heap)
            {
                string thisRegister = FetchAndLoadThis();
                tQuads.Add(new List<string>() { "ADI", thisRegister, location.Item2.ToString() });
                if (symTable[symKey].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register, thisRegister });
                else tQuads.Add(new List<string>() { "LDR", register, thisRegister });
                return register;
            }

            string registerValue = "R" + getRegister("R1Val");


            if (symKey[0] == 'r')
            {
                tQuads.Add(new List<string>() { "LDR", registerValue, register });
                return registerValue;
            }

            if (symTable[symKey].Data.ContainsKey("returnType")) tQuads.Add(new List<string>() { "LDR", registerValue, register });
            else if (symTable[symKey].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", registerValue, register });
            else tQuads.Add(new List<string>() { "LDR", registerValue, register });

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
            else if (location.Item1 == MemoryLocations.heap)
            {
                string register2 = FetchAndLoadThis();
                //tQuads.Add(new List<string>() { "LDR", register, register2 });
                tQuads.Add(new List<string>() { "ADI", register2, location.Item2.ToString() });
                return register2;
                //tQuads.Add(new List<string>() { "MOV", register, "SL" });
                //tQuads.Add(new List<string>() { "ADI", register, location.Item2.ToString() });
            }
            //else if (location.Item1 == MemoryLocations.memory)
            //{
            //    if (symTable[symKey].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register, symKey });
            //    else tQuads.Add(new List<string>() { "LDR", register, symKey });
            //    return register;
            //}

            return register;
        }

        void RtnCase(List<string> quad)
        {
            // Check underflow

            string register1 = "R" + getRegister("RTN");
            tQuads.Add(new List<string>() { "LDR", register1, "FP" });
            string register2 = "R" + getRegister("RTN");
            tQuads.Add(new List<string>() { "MOV", register2, "FP" });
            tQuads.Add(new List<string>() { "ADI", register2, "-4" });
            tQuads.Add(new List<string>() { "LDR", "FP", register2 });
            tQuads.Add(new List<string>() { "JMR", register1 });
        }

        void ReturnCase(List<string> quad)
        {
            // VM Debug
            //tQuads.Add(new List<string>() { "TRP", "99" });

            // Check underflow

            string valRegister;

            if (quad[1] == "this")
            {
                valRegister = FetchAndLoadThis();
            }
            else valRegister = FetchAndLoadValue(quad[1]);

            if (quad[1] != "this" && getLocation(quad[1]).Item1 == MemoryLocations.heap && symTable[quad[1]].Kind != "ivar")
            {
                if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", valRegister, valRegister });
                else tQuads.Add(new List<string>() { "LDR", valRegister, valRegister });
            }

            //if (getLocation(quad[1]).Item1 == MemoryLocations.heap)
            //{
            //    if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", valRegister, valRegister });
            //    else tQuads.Add(new List<string>() { "LDR", valRegister, valRegister });
            //}

            string register1 = "R" + getRegister("RTN");
            tQuads.Add(new List<string>() { "LDR", register1, "FP" });
            string register2 = "R" + getRegister("RTN");
            tQuads.Add(new List<string>() { "MOV", register2, "FP" });
            tQuads.Add(new List<string>() { "ADI", register2, "-4" });
            tQuads.Add(new List<string>() { "LDR", "FP", register2 });

            if (quad[1] == "this") tQuads.Add(new List<string>() { "STR", valRegister, "SP" });
            else if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "STB", valRegister, "SP" });
            else tQuads.Add(new List<string>() { "STR", valRegister, "SP" });

            tQuads.Add(new List<string>() { "JMR", register1 });
        }

        void MathLogicCase(List<string> quad)
        {
            string register1Value = FetchAndLoadValue(quad[1]);

            if (quad[1][0] == 'r')
            {
                if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register1Value, register1Value });
                else tQuads.Add(new List<string>() { "LDR", register1Value, register1Value });
            }

            string register2Value = FetchAndLoadValue(quad[2]);

            if (quad[2][0] == 'r')
            {
                if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register2Value, register2Value });
                else tQuads.Add(new List<string>() { "LDR", register2Value, register2Value });
            }

            tQuads.Add(new List<string>() { quad[0], register1Value, register2Value });

            string register3 = FetchAndLoadAddress(quad[3]);

            tQuads.Add(new List<string>() { "STR", register1Value, register3 });
        }

        string genLabel(string l)
        {
            labelCount++;
            return $"{l}{labelCount}";
        }

        void ConditionalCase(List<string> quad)
        {
            string register1 = FetchAndLoadValue(quad[1]);

            if (quad[1][0] == 'r')
            {
                if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register1, register1 });
                else tQuads.Add(new List<string>() { "LDR", register1, register1 });
            }

            string register2 = FetchAndLoadValue(quad[2]);

            if (quad[2][0] == 'r')
            {
                if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register2, register2 });
                else tQuads.Add(new List<string>() { "LDR", register2, register2 });
            }

            tQuads.Add(new List<string>() { "CMP", register1, register2 });

            string label = genLabel("L");

            if (quad[0] == "LT") tQuads.Add(new List<string>() { "BLT", register1, label }); // less than
            else if (quad[0] == "GT") tQuads.Add(new List<string>() { "BGT", register1, label }); // greater than
            else if (quad[0] == "EQ") tQuads.Add(new List<string>() { "BRZ", register1, label }); // equal
            else if (quad[0] == "NE") tQuads.Add(new List<string>() { "BNZ", register1, label });
            else if (quad[0] == "GE")
            {
                tQuads.Add(new List<string>() { "BRZ", register1, label });
                tQuads.Add(new List<string>() { "BGT", register1, label });
            }
            else if (quad[0] == "LE")
            {
                tQuads.Add(new List<string>() { "BRZ", register1, label });
                tQuads.Add(new List<string>() { "BLT", register1, label });
            }

            // Set FALSE
            string label2 = genLabel("L");
            string tempRegister = "R" + getRegister("temp");
            tQuads.Add(new List<string>() { "LDR", tempRegister, "ZERO" });
            string register3 = FetchAndLoadAddress(quad[3]);
            tQuads.Add(new List<string>() { "STR", tempRegister, register3 });
            tQuads.Add(new List<string>() { "JMP", label2 });
            labels.Enqueue(label2); // Add to labels Stack so that in next BF we will use that label

            // Set TRUE
            tQuads.Add(new List<string>() { label, "LDR", tempRegister, "ONE" });
            register3 = FetchAndLoadAddress(quad[3]);
            tQuads.Add(new List<string>() { "STR", tempRegister, register3 });

        }

        void EqualGreaterLesserCase(List<string> quad)
        {
            string register1 = FetchAndLoadValue(quad[1]);

            if (quad[1][0] == 'r')
            {
                if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register1, register1 });
                else tQuads.Add(new List<string>() { "LDR", register1, register1 });
            }

            string register2 = FetchAndLoadValue(quad[2]);

            if (quad[2][0] == 'r')
            {
                if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register2, register2 });
                else tQuads.Add(new List<string>() { "LDR", register2, register2 });
            }
            tQuads.Add(new List<string>() { "CMP", register1, register2 });

            string label = genLabel("L");

            //tQuads.Add(new List<string>() { "BLT", register1, label });

            if (quad[0] == "GE") tQuads.Add(new List<string>() { "BLT", register1, label });
            else if (quad[0] == "LE") tQuads.Add(new List<string>() { "BGT", register1, label });

            // Set FALSE
            string label2 = genLabel("L");
            string tempRegister = "R" + getRegister("temp");
            tQuads.Add(new List<string>() { "MOV", tempRegister, "ZERO" });
            string register3 = FetchAndLoadAddress(quad[3]);
            tQuads.Add(new List<string>() { "STR", tempRegister, register3 });
            tQuads.Add(new List<string>() { "JMP", label2 });
            labels.Enqueue(label2); // Add to labels Stack so that in next BF we will use that label

            // Set TRUE
            tQuads.Add(new List<string>() { label, "MOV", tempRegister, "ONE" });
            tQuads.Add(new List<string>() { "STR", tempRegister, register3 });

        }

        void NewInstanceCase(List<string> quad)
        {
            string register1 = "R" + getRegister("SL");
            tQuads.Add(new List<string>() { "MOV", register1, "SL" });
            tQuads.Add(new List<string>() { "ADI", "SL", quad[1] });
            string register2 = FetchAndLoadAddress(quad[2]);
            tQuads.Add(new List<string>() { "STR", register1, register2 });

        }

        void PeekCase(List<string> quad)
        {
            // Debug VM
            //tQuads.Add(new List<string>() { "TRP", "99" });

            string register1 = "R" + getRegister("peek");
            //tQuads.Add(new List<string>() { "MOV", register1, "SP" });
            //tQuads.Add(new List<string>() { "ADI", register1, "4" });
            string tempRegister = "R" + getRegister("temp");
            string register2 = FetchAndLoadAddress(quad[1]);

            if (symTable[quad[1]].Data.ContainsKey("returnType"))
            {
                if (symTable[quad[1]].Data["returnType"] == "char")
                {
                    tQuads.Add(new List<string>() { "LDB", tempRegister, "SP" });
                    tQuads.Add(new List<string>() { "STB", tempRegister, register2 });
                }
                else
                {
                    tQuads.Add(new List<string>() { "LDR", tempRegister, "SP" });
                    tQuads.Add(new List<string>() { "STR", tempRegister, register2 });
                }
            }
            else
            {
                if (symTable[quad[1]].Data["type"] == "char")
                {
                    tQuads.Add(new List<string>() { "LDB", tempRegister, "SP" });
                    tQuads.Add(new List<string>() { "STB", tempRegister, register2 });
                }
                else
                {
                    tQuads.Add(new List<string>() { "LDR", tempRegister, "SP" });
                    tQuads.Add(new List<string>() { "STR", tempRegister, register2 });
                }
            }
        }

        void PushCase(List<string> quad)
        {
            // Debug VM
            //tQuads.Add(new List<string>() { "TRP", "99" });

            var loc = getLocation(quad[1]);

            string valRegister = "";

            if (loc.Item1 == MemoryLocations.stack) // Get old frame
            {
                string register1 = "R" + getRegister("pfp");
                tQuads.Add(new List<string>() { "MOV", register1, "FP" });
                tQuads.Add(new List<string>() { "ADI", register1, "-4" });
                string oldFrameRegister = "R" + getRegister("ofr");
                tQuads.Add(new List<string>() { "LDR", oldFrameRegister, register1 });
                tQuads.Add(new List<string>() { "ADI", oldFrameRegister, loc.Item2.ToString() });

                valRegister = "R" + getRegister("val");

                if (quad[1][0] == 'r')
                {
                    tQuads.Add(new List<string>() { "LDR", oldFrameRegister, oldFrameRegister });
                }

                if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", valRegister, oldFrameRegister });
                else tQuads.Add(new List<string>() { "LDR", valRegister, oldFrameRegister });
            }
            else
            {
                valRegister = FetchAndLoadValue(quad[1]);
            }

            if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "STB", valRegister, "SP" });
            else tQuads.Add(new List<string>() { "STR", valRegister, "SP" });

            tQuads.Add(new List<string>() { "ADI", "SP", "-4" });
        }

        void RefCase(List<string> quad)
        {
            //Debug VM
            //tQuads.Add(new List<string>() { "TRP", "99" });

            string register1 = FetchAndLoadValue(quad[1]);

            if (quad[1][0] == 'r') tQuads.Add(new List<string>() { "LDR", register1, register1 });


            // Get offset
            int offset = 0;
            var fellowIvars = symTable.Where(sym => sym.Value.Scope == "g." + symTable[quad[1]].Data["type"] && sym.Value.Kind == "ivar").ToList();
            foreach (var ivar in fellowIvars)
            {
                if (ivar.Key == quad[2]) break;
                if (ivar.Value.Data["type"] == "char") offset += 4;
                else offset += 4;
            }

            tQuads.Add(new List<string>() { "ADI", register1, offset.ToString() });

            //if (symTable[quad[1]].Data["type"] == "char") tQuads.Add(new List<string>() { "LDB", register1, register1 });
            //else tQuads.Add(new List<string>() { "LDR", register1, register1 });

            string register2 = FetchAndLoadAddress(quad[3]);

            tQuads.Add(new List<string>() { "STR", register1, register2 });
        }

        string FetchAndLoadThis()
        {
            string register1 = "R" + getRegister("this");
            tQuads.Add(new List<string>() { "MOV", register1, "FP" });
            tQuads.Add(new List<string>() { "ADI", register1, "-8" });
            string register2 = "R" + getRegister("thisValue");
            tQuads.Add(new List<string>() { "LDR", register2, register1 });

            return register2;
        }

        void BranchFalseCase(List<string> quad)
        {
            string register1 = FetchAndLoadValue(quad[1]);
            tQuads.Add(new List<string>() { "BRZ", register1, quad[2] });
        }

        void Write1Case(List<string> quad)
        {
            // Debug VM
            //tQuads.Add(new List<string>() { "TRP", "99" });

            string register = FetchAndLoadValue(quad[1]);

            if (quad[1][0] == 'r') tQuads.Add(new List<string>() { "LDR", register, register });

            tQuads.Add(new List<string>() { "MOV", "R3", register });

            tQuads.Add(new List<string>() { "TRP", "1" });
        }

        void Write2Case(List<string> quad)
        {
            // Debug VM
            //tQuads.Add(new List<string>() { "TRP", "99" });

            string register;

            //if (quad[1][0] == 'r')
            //{
            //    string tempRegister = FetchAndLoadAddress(quad[1]);
            //    tQuads.Add(new List<string>() { "LDR", tempRegister, tempRegister });
            //    register = "R" + getRegister("cool");
            //    tQuads.Add(new List<string>() { "LDB", register, tempRegister });
            //}
            //else register = FetchAndLoadValue(quad[1]);

            register = FetchAndLoadValue(quad[1]);

            if (quad[1][0] == 'r') tQuads.Add(new List<string>() { "LDB", register, register });


            tQuads.Add(new List<string>() { "MOV", "R3", register });

            tQuads.Add(new List<string>() { "TRP", "3" });
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
