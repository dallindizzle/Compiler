using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = "collin.kxi";
            LexicalAnalyser scanner = new LexicalAnalyser(file);

            SyntaxAnalyser syntaxAnalyser = new SyntaxAnalyser(scanner);

            syntaxAnalyser.go();

            syntaxAnalyser.printTable();
            Console.ReadKey();
        }
    }
}
