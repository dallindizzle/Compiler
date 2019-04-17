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
            if (args.Length < 1) { Console.WriteLine("No arguments"); return; }

            //string file = "semanticsTest.kxi";

            string file = args[0];

            LexicalAnalyser scanner = new LexicalAnalyser(file);

            SyntaxAnalyser syntaxAnalyser = new SyntaxAnalyser(scanner);

            syntaxAnalyser.go();
            //syntaxAnalyser.printTable();

            SemanticAnalyser semanticsAnalyser = new SemanticAnalyser(new LexicalAnalyser(file), syntaxAnalyser.symTable);

            semanticsAnalyser.go();

            //semanticsAnalyser.printTable();

            //semanticsAnalyser.PrintICode();

            TargetCode target = new TargetCode(semanticsAnalyser.quads, semanticsAnalyser.symTable);
            target.go();
            target.PrintTCode();

            VM.go("output.asm");

            //Console.ReadKey();
        }
    }
}
