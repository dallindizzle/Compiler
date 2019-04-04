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

            SyntaxAnalyser syntaxAnalyser = new SyntaxAnalyser(scanner);

            syntaxAnalyser.go();

            SemanticAnalyser semanticsAnalyser = new SemanticAnalyser(new LexicalAnalyser(file), syntaxAnalyser.symTable);

            semanticsAnalyser.go();

            semanticsAnalyser.printTable();

            semanticsAnalyser.PrintICode();

            TargetCode target = new TargetCode(semanticsAnalyser.quads, semanticsAnalyser.symTable);
            target.go();

            Console.ReadKey();
        }
    }
}
