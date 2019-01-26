using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Compiler
{
    class LexicalAnalyser
    {
        public struct Token
        {
            public string type;
            public int lineNum;
            public string lexeme;

            public Token(string t, int l, string lex)
            {
                type = t;
                lineNum = l;
                lexeme = lex;
            }

            public override string ToString()
            {
                return $"Type: {type}, Line Number: {lineNum}, Lexeme: {lexeme}";
            }
        }

        string[] keywordsLib = new string[] { "atoi", "and", "bool", "block", "break", "case", "class", "class_name", "char", "cin", "cout", "default", "else", "false", "if", "int", "itoa", "kxi2019", "lock", "main", "new", "null", "object", "or", "public", "private", "protected", "return", "release", "string", "spawn", "sym", "set", "switch", "this", "true", "thread", "unprotected", "unlock", "void", "while", "wait", "whatif" };
        string[] puncLib = new string[] { ".", ",", ":", ";" };
        string[] mathSym = new string[] { "+", "-", "/", "*" };
        string[] boolSym = new string[] { "<", ">" };
        string[] logSym = new string[] { "&", "|" };
        string[] symLib = new string[] { "+", "-", "/", "*", "=", "(", ")", "[", "]", "{", "}", "<", ">" }; // Catches all symbols not already caught

        StreamReader reader;

        int curToken;
        List<Token> buffer;
        int lineNum;

        string indentPat = @"[a-zA-Z0-9_]+";
        string pat = @"([*+/\-<>=)(,;.{}?!:\[\]]|[0-9]+|'\\n'|[""'][a-zA-Z._ ]+[""']|[a-zA-Z0-9_]+)";

        public LexicalAnalyser(string file)
        {
            reader = new StreamReader(file);
            buffer = new List<Token>();
            curToken = 0;
            lineNum = 1;
        }

        public Token getToken()
        {
            if (buffer.Count() == 0) nextToken();
            return buffer[curToken];
        }

        public Token peekToken()
        {
            if (curToken + 1 < buffer.Count()) return buffer[curToken + 1];
            return buffer[curToken];
        }

        public void nextToken()
        {
            if (curToken + 1 >= buffer.Count())
            {
                ReadLine();
                curToken = 0;
            }
            else curToken++;
        }

        void ReadLine()
        {
            buffer.Clear();

            if (reader.EndOfStream)
            {
                buffer.Add(new Token("eof", lineNum, "eof"));
                return;
            }

            string line = reader.ReadLine();

            while (line == "")
            {
                lineNum++;
                line = reader.ReadLine();
            }

            var matches = Regex.Matches(line, pat, RegexOptions.Singleline);
            foreach (var match in matches)
            {
                //Console.WriteLine(match);

                if (int.TryParse(match.ToString(), out int i)) buffer.Add(new Token("Number", lineNum, match.ToString()));
                else if (match.ToString()[0] == '\'') buffer.Add(new Token("Character", lineNum, match.ToString()));
                else if (keywordsLib.Contains(match.ToString())) buffer.Add(new Token("Keyword", lineNum, match.ToString()));
                else if (puncLib.Contains(match.ToString())) buffer.Add(new Token("Punctuation", lineNum, match.ToString()));
                else if (mathSym.Contains(match.ToString())) buffer.Add(new Token("Math", lineNum, match.ToString()));
                else if (boolSym.Contains(match.ToString())) buffer.Add(new Token("Boolean", lineNum, match.ToString()));
                else if (logSym.Contains(match.ToString())) buffer.Add(new Token("Logical", lineNum, match.ToString()));
                else if (symLib.Contains(match.ToString())) buffer.Add(new Token("Symbol", lineNum, match.ToString()));
                else if (Regex.IsMatch(match.ToString(), indentPat)) buffer.Add(new Token("Identifier", lineNum, match.ToString()));
                else buffer.Add(new Token("Unknown", lineNum, match.ToString()));
            }

            //foreach (var token in buffer) Console.WriteLine(token.ToString());
            //Console.WriteLine(buffer.Count);

            lineNum++;
        }

        //static void Main(string[] args)
        //{
        //    //string file = args[1];

        //    LexicalAnalyser analyser = new LexicalAnalyser("test3.txt");

        //    Token output = analyser.getToken();
        //    while (output.lexeme != "eof")
        //    {
        //        Console.WriteLine(output.ToString());
        //        analyser.nextToken();
        //        output = analyser.getToken();
        //    }

        //    Console.WriteLine(output.ToString());

        //    Console.ReadKey();
        //}
    }
}
