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
            string file = "semanticsTest.kxi";
            LexicalAnalyser scanner = new LexicalAnalyser(file);

            SemanticAnalyser syntaxAnalyser = new SemanticAnalyser(scanner);

            syntaxAnalyser.go();

            syntaxAnalyser.printTable();
            Console.ReadKey();
        }
    }
}
