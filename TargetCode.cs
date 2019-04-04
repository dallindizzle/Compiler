using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public TargetCode( List<List<string>> ic, Dictionary<string, Symbol> st)
        {
            symTable = st;
            iQuads = ic;
        }

        public void go()
        {
            foreach (var quad in iQuads)
            {
                switch(quad[0])
                {
                    case "MOV":
                        MovCase(quad);
                        break;

                    default:
                        FuncCase(quad);
                        break;
                }
            }
        }

        private int getRegister(string symKey)
        {
            int reg = -1;

            for (int i = 0; i < 8; i++)
            {
                if (registers[i] == "" || registers[i] == null) reg = i;
            }

            registers[reg] = symKey;

            return reg;         }

        private Dictionary<MemoryLocations, int> getLocation(string symKey)
        {
            Dictionary<MemoryLocations, int> locations = new Dictionary<MemoryLocations, int>();
            // First look in registers
            for (int i = 0; i < 8; i++)
            {
                if (registers[i] == symKey)
                {
                    locations.Add(MemoryLocations.register, i);
                }
            }

            string varScope = symTable[symKey].Scope;
            var fellowSyms = symTable.Where(sym => sym.Value.Scope == varScope).ToList();

            if (varScope == "g")
            {
                locations.Add(MemoryLocations.memory, -1);
                return locations;
            }

            int offset = 0;
            foreach(var sym in fellowSyms)
            {
                if (sym.Key == symKey) break;
                offset += -4;
            }

            if (varScope.Split('.')[1] == "main")
            {
                locations.Add(MemoryLocations.stack, offset - 12);
            }


            return locations;
        }

        private int SizeOfFunc(string funcKey)
        {
            string funScope = "g." + symTable[funcKey].Value;

            int size = symTable.Where(sym => sym.Value.Scope == funScope).Count();

            return size * 4;
        }

        void FuncCase(List<string> quad)
        {
            int funcSize = SizeOfFunc(quad[0]);
            tQuads.Add(new List<string>() { quad[0], "ADI", "SP", $"-{funcSize}"  } );
            
            // Test overflow here


        }

        void MovCase(List<string> quad)
        {
            int register = getRegister(quad[2]);
            tQuads.Add(new List<string>() { "MOV", $"R{register}", "FP" });
            var locations = getLocation(quad[2]);

            foreach(var loc in locations)
            {
                if (loc.Key == MemoryLocations.stack)
                {
                    tQuads.Add(new List<string>() { "ADI", $"R{register}", $"{loc.Value}" });
                }
            }

            // Load into a register the second operand
        }
    }
}
