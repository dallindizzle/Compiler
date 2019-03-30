using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Compiler
{
    class SemanticAnalyser
    {
        LexicalAnalyser scanner;
        string[] z = new string[] { "=", "&", "|", "!", "<", ">", "+", "-", "*", "/" };
        string[] state = new string[] { "{", "if", "while", "return", "cout", "cin", "switch", "break", "(", "true", "false", "null", "this" };
        string[] exp = new string[] { "(", "true", "false", "null", "this" };

        string scope;

        public Dictionary<string, Symbol> symTable;
        int symId;
        string lastId;

        public SemanticAnalyser(LexicalAnalyser s, Dictionary<string, Symbol> symT)
        {
            scanner = s;
            scope = "g";

            symTable = symT;
            symId = 0;

            SAS = new Stack<SAR>();
            OS = new Stack<SAR>();

            // iCode
            quads = new List<List<string>>();
        }

        #region iCode definition

        List<List<string>> quads;

        #endregion

        #region SynataxAnalyser definition

        public void go()
        {
            compilation_unit();
        }

        void compilation_unit()
        {
            while (scanner.getToken().lexeme == "class")
            {
                class_declaration();
            }

            if (scanner.getToken().lexeme != "void") syntaxError("void");
            scanner.nextToken();
            if (scanner.getToken().lexeme != "kxi2019") syntaxError("kxi2019");
            scanner.nextToken();
            if (scanner.getToken().lexeme != "main") syntaxError("main");


            scanner.nextToken();
            if (scanner.getToken().lexeme != "(") syntaxError("(");

            push("main");

            scanner.nextToken();
            if (scanner.getToken().lexeme != ")") syntaxError(")");
            scanner.nextToken();
            method_body();

            pop();
        }

        void method_body()
        {
            if (scanner.getToken().lexeme != "{") syntaxError("{");
            scanner.nextToken();

            while (isaVariable_Declaration(scanner.getToken().lexeme))
            {
                variable_declaration();
            }

            while (isAstatement())
            {
                statement();
            }

            if (scanner.getToken().lexeme != "}") syntaxError("}");
            scanner.nextToken();
        }

        void statement()
        {
            if (scanner.getToken().lexeme == "{")
            {
                scanner.nextToken();
                while (isAstatement())
                {
                    statement();
                }
                if (scanner.getToken().lexeme != "}") syntaxError("}");
                scanner.nextToken();
            }
            else if (scanner.getToken().lexeme == "if")
            {
                scanner.nextToken();
                if (scanner.getToken().lexeme != "(") syntaxError("(");

                // Semantics code
                oPush(scanner.getToken().lexeme);

                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != ")") syntaxError(")");

                // Semantics code
                ClosingParen();
                ifCase();

                scanner.nextToken();
                statement();
                if (scanner.getToken().lexeme == "else")
                {
                    scanner.nextToken();
                    statement();
                }
            }
            else if (scanner.getToken().lexeme == "while")
            {
                scanner.nextToken();
                if (scanner.getToken().lexeme != "(") syntaxError("(");

                //Semantics code
                oPush(scanner.getToken().lexeme);

                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != ")") syntaxError(")");

                // Semantics code
                ClosingParen();
                whileCase();

                scanner.nextToken();
                statement();
            }
            else if (scanner.getToken().lexeme == "return")
            {
                scanner.nextToken();
                if (isAexpression()) expression();
                if (scanner.getToken().lexeme != ";") syntaxError(";");

                // Semantics code
                returnCase();

                scanner.nextToken();
            }
            else if (scanner.getToken().lexeme == "cout")
            {
                scanner.nextToken();
                if (scanner.getToken().lexeme != "<") syntaxError("<");
                scanner.nextToken();
                if (scanner.getToken().lexeme != "<") syntaxError("<");
                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != ";") syntaxError(";");

                // Semantics code
                coutCase();

                scanner.nextToken();
            }
            else if (scanner.getToken().lexeme == "cin")
            {
                scanner.nextToken();
                if (scanner.getToken().lexeme != ">") syntaxError("<");
                scanner.nextToken();
                if (scanner.getToken().lexeme != ">") syntaxError("<");
                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != ";") syntaxError(";");

                // Semantics code
                cinCase();

                scanner.nextToken();
            }
            else
            {
                expression();
                if (scanner.getToken().lexeme != ";") syntaxError(";");

                // Semantic code
                EOE(true);

                scanner.nextToken();
            }

        }

        void variable_declaration()
        {
            //string typ = scanner.getToken().lexeme; // This is for symbol table

            type();

            // Semnatic code
            tExist();

            if (scanner.getToken().type != "Identifier") syntaxError("Identifier");

            // Semantics code
            dup(scanner.getToken().lexeme);
            vPush(scanner.getToken().lexeme);

            scanner.nextToken();
            if (scanner.getToken().lexeme == "[")
            {
                // Semantics code
                //SAR tempSar = SAS.Pop();
                //tempSar.val += "[]";
                //SAS.Push(tempSar);

                scanner.nextToken();
                if (scanner.getToken().lexeme != "]") syntaxError("]");
                scanner.nextToken();
            }

            if (scanner.getToken().lexeme == "=")
            {

                // Semantics code
                oPush(scanner.getToken().lexeme);

                scanner.nextToken();
                assignment_expression();
            }
            if (scanner.getToken().lexeme != ";") syntaxError(";");

            // Semantics code
            EOE(true);

            scanner.nextToken();
        }

        void expression()
        {
            if (scanner.getToken().lexeme == "(")
            {
                // Semantics code
                oPush(scanner.getToken().lexeme);

                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme == ")")
                {
                    scanner.nextToken();

                    // Semantics code
                    ClosingParen();

                    if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
                }
                else syntaxError(")");
            }
            else if (scanner.getToken().lexeme == "true" || scanner.getToken().lexeme == "false" || scanner.getToken().lexeme == "null")
            {
                // Semantics code
                lPush(scanner.getToken().lexeme);

                scanner.nextToken();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().lexeme == "this")
            {
                // Semantics code
                iPush(scanner.getToken().lexeme);
                iExist();


                scanner.nextToken();
                if (isAmember_refZ(scanner.getToken().lexeme)) member_refZ();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().type == "Number" || scanner.getToken().lexeme == "+" || scanner.getToken().lexeme == "-")
            {
                numeric_literal();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().type == "Character")
            {
                // Semantics code
                lPush(scanner.getToken().lexeme);
                //iExist();

                scanner.nextToken();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().type == "Identifier")
            {
                // Semantic code
                if (!symTable.Where(tempsym => tempsym.Value.Scope == scope).Any(sym => sym.Value.Value == scanner.getToken().lexeme)) semanticError(scanner.getToken().lineNum, "identifier", scanner.getToken().lexeme, $"Variable {scanner.getToken().lexeme} not defined");
                string symKey = symTable.Where(tempsym => tempsym.Value.Scope == scope).Where(sym2 => sym2.Value.Value == scanner.getToken().lexeme).First().Key;
                iPush(scanner.getToken().lexeme, symKey);

                scanner.nextToken();
                if (isAfn_arr_member(scanner.getToken().lexeme)) fn_arr_member();

                // Semantic code
                iExist();

                if (isAmember_refZ(scanner.getToken().lexeme)) member_refZ();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else syntaxError("Expression");
        }

        void expressionZ()
        {
            if (scanner.getToken().lexeme == "=")
            {
                if (scanner.peekToken().lexeme == "=")
                {
                    // Semantics code
                    oPush("==");

                    scanner.nextToken();
                    scanner.nextToken();
                    expression();
                }
                else
                {
                    // Semantic code
                    oPush(scanner.getToken().lexeme);

                    scanner.nextToken();
                    assignment_expression();
                }
            }
            else if (scanner.getToken().lexeme == "&" && scanner.peekToken().lexeme == "&")
            {
                // Semantics code
                oPush("&&");

                scanner.nextToken();
                scanner.nextToken();
                expression();
            }
            else if (scanner.getToken().lexeme == "|" && scanner.peekToken().lexeme == "|")
            {
                // Semantics code
                oPush("||");

                scanner.nextToken();
                scanner.nextToken();
                expression();
            }
            else if (scanner.getToken().lexeme == "!" && scanner.peekToken().lexeme == "=")
            {
                scanner.nextToken();
                scanner.nextToken();
                expression();
            }
            else if (scanner.getToken().lexeme == "<" && scanner.peekToken().lexeme == "=")
            {
                scanner.nextToken();
                scanner.nextToken();
                expression();
            }
            else if (scanner.getToken().lexeme == ">" && scanner.peekToken().lexeme == "=")
            {
                scanner.nextToken();
                scanner.nextToken();
                expression();
            }
            else if (scanner.getToken().type == "Math" || scanner.getToken().type == "Boolean")
            {
                // Semantic code
                oPush(scanner.getToken().lexeme);

                scanner.nextToken();
                expression();
            }
            else syntaxError("expressionz");
        }

        void assignment_expression()
        {
            if (scanner.getToken().lexeme == "new")
            {
                scanner.nextToken();
                type();
                new_delcaration();
            }
            else if (scanner.getToken().lexeme == "atoi" || scanner.getToken().lexeme == "itoa")
            {
                scanner.nextToken();
                if (scanner.getToken().lexeme != "(") syntaxError("(");
                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != ")") syntaxError(")");
                scanner.nextToken();
            }
            else expression();
        }

        void new_delcaration()
        {
            if (scanner.getToken().lexeme == "(")
            {

                // Semantics code
                oPush(scanner.getToken().lexeme);
                BAL();

                scanner.nextToken();
                if (isAargument_list(scanner.getToken().lexeme)) argument_list();
                if (scanner.getToken().lexeme != ")") syntaxError(")");

                // Semantics code
                ClosingParen();
                EAL();
                newObj();

                scanner.nextToken();
            }
            else if (scanner.getToken().lexeme == "[")
            {

                // Semantics code
                oPush(scanner.getToken().lexeme);

                scanner.nextToken();

                expression();
                if (scanner.getToken().lexeme != "]") syntaxError("]");

                // Semantics code
                ClosingBracket();
                newArray();

                scanner.nextToken();
            }
            else syntaxError("( or [");
        }

        void class_declaration()
        {
            if (scanner.getToken().lexeme != "class") syntaxError("class");
            scanner.nextToken();

            push(scanner.getToken().lexeme); // Here we push the class name to the scope, which we assume is here

            // Semantics code
            dup(scanner.getToken().lexeme, true);

            class_name();

            if (scanner.getToken().lexeme != "{") syntaxError("{");
            scanner.nextToken();
            while (scanner.getToken().lexeme == "public" || scanner.getToken().lexeme == "private" || scanner.getToken().type == "Identifier")
            {
                class_member_declaration();
            }
            if (scanner.getToken().lexeme != "}") syntaxError("}");

            pop(); // Here we pop the class scope

            scanner.nextToken();
        }

        void class_member_declaration()
        {
            if (scanner.getToken().lexeme == "public" || scanner.getToken().lexeme == "private")
            {
                //string modifier = scanner.getToken().lexeme; // This is for the symbol table

                scanner.nextToken();

                //string typ = scanner.getToken().lexeme; // This is for the symbol table

                type();

                // Semantics code
                tExist();

                if (scanner.getToken().type != "Identifier") syntaxError("Identifier");

                // Semantics code
                dup(scanner.getToken().lexeme);
                vPush(scanner.getToken().lexeme);

                push(scanner.getToken().lexeme); // pushing scope of class method, if class field then it should still be okay, I think

                scanner.nextToken();
                field_declaration();


                pop();
            }
            else constructor_declaration();
        }

        void field_declaration()
        {
            if (scanner.getToken().lexeme == "(")
            {
                // Semantics code
                SAS.Pop(); // Pop first record because it is a function, not a variable

                scanner.nextToken();
                if (isAtype(scanner.getToken().lexeme)) parameter_list();
                if (scanner.getToken().lexeme != ")") syntaxError(")");
                scanner.nextToken();
                method_body();
            }
            else
            {
                if (scanner.getToken().lexeme == "[")
                {
                    scanner.nextToken();
                    if (scanner.getToken().lexeme != "]") syntaxError("]");
                    scanner.nextToken();
                }


                if (scanner.getToken().lexeme == "=")
                {
                    // Semantics code
                    oPush(scanner.getToken().lexeme);

                    scanner.nextToken();
                    assignment_expression();
                }

                if (scanner.getToken().lexeme != ";") syntaxError(";");
                scanner.nextToken();

                EOE(true);
            }
        }

        void constructor_declaration()
        {
            // Semantics code
            dup(scanner.getToken().lexeme);
            CD(scanner.getToken().lexeme);

            class_name();
            if (scanner.getToken().lexeme != "(") syntaxError("(");

            push("constructor"); // Pushing constructor to scope. I think this is the right name

            scanner.nextToken();
            if (isAtype(scanner.getToken().lexeme)) parameter_list();
            if (scanner.getToken().lexeme != ")") syntaxError(")");
            scanner.nextToken();
            method_body();

            pop(); // Popping contructor scope
        }

        void parameter_list()
        {
            parameter();
            while (scanner.getToken().lexeme == ",")
            {
                scanner.nextToken();

                parameter();
            }
        }

        void parameter()
        {
            type();

            // Semantics code
            tExist();

            if (scanner.getToken().type != "Identifier") syntaxError("Identifier");

            // Semantics code
            dup(scanner.getToken().lexeme);

            scanner.nextToken();
            if (scanner.getToken().lexeme == "[")
            {
                scanner.nextToken();
                if (scanner.getToken().lexeme != "]") syntaxError("]");
                scanner.nextToken();
            }
        }

        void argument_list()
        {
            expression();
            //scanner.nextToken();
            while (scanner.getToken().lexeme == ",")
            {
                // Semnatics code
                Argument();

                scanner.nextToken();
                expression();
            }
        }

        void type()
        {
            if (scanner.getToken().lexeme == "int"
                || scanner.getToken().lexeme == "char"
                || scanner.getToken().lexeme == "bool"
                || scanner.getToken().lexeme == "void"
                || scanner.getToken().lexeme == "sym")
            {
                // Semantics code
                tPush(scanner.getToken().lexeme);

                scanner.nextToken();
            }
            else
            {
                // Semantics code
                tPush(scanner.getToken().lexeme);

                class_name();
            }
        }

        void class_name()
        {
            if (scanner.getToken().type != "Identifier") syntaxError("Identifier");

            scanner.nextToken();
        }

        void member_refZ()
        {
            if (scanner.getToken().lexeme != ".") syntaxError(".");
            scanner.nextToken();
            if (scanner.getToken().type != "Identifier") syntaxError("Identifier");

            // Semantics code
            iPush(scanner.getToken().lexeme);

            scanner.nextToken();
            if (isAfn_arr_member(scanner.getToken().lexeme)) fn_arr_member();

            // Semantics code
            rExist();

            if (isAmember_refZ(scanner.getToken().lexeme)) member_refZ();
        }

        void fn_arr_member()
        {
            if (scanner.getToken().lexeme == "(")
            {
                // Semantics code
                oPush(scanner.getToken().lexeme);

                scanner.nextToken();

                // Semantics code
                BAL();

                if (isAargument_list(scanner.getToken().lexeme)) argument_list();
                if (scanner.getToken().lexeme != ")") syntaxError(")");

                // Semantics code
                ClosingParen();
                EAL();
                func();

                scanner.nextToken();
            }
            else if (scanner.getToken().lexeme == "[")
            {
                // Semantics code
                oPush(scanner.getToken().lexeme);

                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != "]") syntaxError("]");

                // Semantics code
                ClosingBracket();
                arr();

                scanner.nextToken();
            }
            else syntaxError("( or [");

        }

        void numeric_literal()
        {
            if (scanner.getToken().lexeme == "+" || scanner.getToken().lexeme == "-")
            {
                scanner.nextToken();
                if (scanner.getToken().type != "Number") syntaxError("Number");
                scanner.nextToken();
            }
            else
            {
                if (scanner.getToken().type != "Number") syntaxError("Number");

                // Semantics code
                lPush(scanner.getToken().lexeme);

                scanner.nextToken();
            }
        }

        bool isAstatement()
        {
            if (state.Contains(scanner.getToken().lexeme) || scanner.getToken().type == "Number" || scanner.getToken().type == "Character" || scanner.getToken().type == "Identifier")
                return true;
            return false;
        }

        bool isAexpression()
        {
            if (exp.Contains(scanner.getToken().lexeme) || scanner.getToken().type == "Number" || scanner.getToken().type == "Character" || scanner.getToken().type == "Identifier")
                return true;
            return false;
        }

        bool isAtype(string lexeme)
        {
            return (lexeme == "int" || lexeme == "char" || lexeme == "bool" || lexeme == "void" || lexeme == "sym" || scanner.getToken().type == "Identifier");
        }

        bool isaVariable_Declaration(string lexeme)
        {
            return (isAtype(lexeme) && scanner.peekToken().type == "Identifier");
        }

        bool isAargument_list(string lexeme)
        {
            return isAexpression();
        }

        bool isAexpressionZ(string lexeme)
        {
            return (z.Contains(lexeme));
        }

        bool isAmember_refZ(string lexeme)
        {
            return lexeme == ".";
        }

        bool isAfn_arr_member(string lexeme)
        {
            return lexeme == "(" || lexeme == "[";
        }

        void syntaxError(string expected)
        {
            Console.WriteLine($"<Line {scanner.getToken().lineNum}>:Found {scanner.getToken().lexeme} expecting {expected}");
            Console.ReadKey();
            Environment.Exit(0);
        }

        // Scope methods

        void push(string s)
        {
            scope += '.' + s;
        }

        void pop()
        {
            scope = scope.Substring(0, scope.LastIndexOf('.'));
        }

        // Symbol Table methods

        string genId(string s)
        {
            lastId = s + (symId++).ToString();
            return lastId;
        }


        // Print 
        public void printTable()
        {
            foreach (var symbol in symTable)
            {
                Console.Write($"{symbol.Key} ->");
                Console.Write($"\tScope:\t {symbol.Value.Scope}\n");
                Console.WriteLine($"\tSymid:\t {symbol.Value.Symid}");
                Console.WriteLine($"\tValue:\t {symbol.Value.Value}");
                Console.WriteLine($"\tKind:\t {symbol.Value.Kind}");

                Console.Write($"\tData:\t ");
                foreach (var data in symbol.Value.Data)
                {
                    if (data.Value is List<string>)
                    {
                        Console.Write($"{data.Key}: [");
                        foreach (string param in data.Value)
                        {
                            Console.Write($"{param}, ");
                        }
                        Console.Write("]\n");
                    }

                    else Console.Write($"{data.Key}: {data.Value}\n\t\t ");
                }

                if (symbol.Value.Data.Count == 0) Console.WriteLine();

                Console.WriteLine();
            }
        }

        #endregion


        #region SemanticAnalyser defintion

        struct SAR
        {
            public enum pushes
            {
                iExist,
                rExist,
                tExist,
                iPush,
                oPush,
                tPush,
                vPush,
                lPush,
                newArray,
                newObj,
                EOE,
                mul,
                BAL,
                EAL,
                func,
                arr,
                none
            };

            public enum types
            {
                id_sar,
                ref_sar,
                bal_sar,
                al_sar,
                arr_sar,
                func_sar,
                type_sar,
                var_sar,
                new_sar,
                lit_sar,
                none
            };

            public string val;
            public types type;
            public pushes pushType;
            public string symKey;

            public List<SAR> arguments; // This is used for al_sar (argument list sar)

            public SAR(string v, types t, pushes p = pushes.none, string key = "")
            {
                val = v;
                type = t;
                pushType = p;
                symKey = key;

                arguments = new List<SAR>();
            }
        }

        Stack<SAR> SAS;
        Stack<SAR> OS;

        void iPush(string val, string symKey = "")
        {
            SAR sar = new SAR(val, SAR.types.none ,SAR.pushes.iPush, symKey);

            SAS.Push(sar);
        }

        void vPush(string val)
        {
            string symKey = symTable.Where(sym => sym.Value.Scope == scope).ToList().Where(sym => sym.Value.Value == val).First().Key;

            SAS.Push(new SAR(val, SAR.types.var_sar, SAR.pushes.vPush, symKey));
        }

        void lPush(string lit)
        {
            string symKey;

            if (lit == "true" || lit == "false" || lit == "null")
            {
                symKey = symTable.Where(sym => sym.Value.Scope == "g").Where(sym => sym.Value.Kind == "clit" || sym.Value.Kind == "ilit").Where(sym => sym.Value.Value == lit).First().Key;
            }
            else symKey = symTable.Where(sym => sym.Value.Scope == "g").Where(sym => sym.Value.Kind == "clit" || sym.Value.Kind == "ilit").Where(sym => sym.Value.Value == lit).First().Key;

            SAS.Push(new SAR(lit, SAR.types.lit_sar, SAR.pushes.lPush, symKey));
        }

        void oPush(string op)
        {
            SAR sar = new SAR(op, SAR.types.none, SAR.pushes.oPush);

            if (op == "(" || op == "[") 
            {
                OS.Push(sar);
                return;
            }

            if (op == "=" && OS.Count > 0) semanticError(scanner.getToken().lineNum, "Assignment", string.Join("", scanner.buffer.Select(token => token.lexeme)), "Wrong assignment");

            if (OS.Count == 0) OS.Push(sar);
            else if (op == "*"  || op == "/")
            {
                if (OS.First().val == "*" || OS.First().val == "/")
                {
                    while (OS.First().val == "*" || OS.First().val == "/")
                    {
                        EOE();
                    }
                    OS.Push(sar);
                }
                else OS.Push(sar);
            }
            else if (op == "+" || op == "-")
            {
                if (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-" )
                {
                    while (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-")
                    {
                        EOE();
                    }
                    OS.Push(sar);
                }
                else OS.Push(sar);
            }
            else if (op == "<" || op == "<=" || op == ">" || op == ">=")
            {
                if (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-" || OS.First().val == "<" || OS.First().val == "<=" || OS.First().val == ">" || OS.First().val == ">=")
                {
                    while (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-" || OS.First().val == "<" || OS.First().val == "<=" || OS.First().val == ">" || OS.First().val == ">=")
                    {
                        EOE();
                    }
                    OS.Push(sar);
                }
                else OS.Push(sar);
            }
            else if (op == "==" || op == "!=")
            {
                if (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-" || OS.First().val == "<" || OS.First().val == "<=" || OS.First().val == ">" || OS.First().val == ">=" || OS.First().val == "==" || OS.First().val == "!=")
                {
                    while (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-" || OS.First().val == "<" || OS.First().val == "<=" || OS.First().val == ">" || OS.First().val == ">=" || OS.First().val == "==" || OS.First().val == "!=")
                    {
                        EOE();
                    }
                    OS.Push(sar);
                }
                else OS.Push(sar);         
            }
            else if (op == "&&")
            {
                if (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-" || OS.First().val == "<" || OS.First().val == "<=" || OS.First().val == ">" || OS.First().val == ">=" || OS.First().val == "==" || OS.First().val == "!=" || OS.First().val == "&&")
                {
                    while (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-" || OS.First().val == "<" || OS.First().val == "<=" || OS.First().val == ">" || OS.First().val == ">=" || OS.First().val == "==" || OS.First().val == "!=" || OS.First().val == "&&")
                    {
                        EOE();
                    }
                    OS.Push(sar);
                }
                else OS.Push(sar);
            }
            else if (op == "||")
            {
                if (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-" || OS.First().val == "<" || OS.First().val == "<=" || OS.First().val == ">" || OS.First().val == ">=" || OS.First().val == "==" || OS.First().val == "!=" || OS.First().val == "&&" || OS.First().val == "||")
                {
                    while (OS.First().val == "*" || OS.First().val == "/" || OS.First().val == "+" || OS.First().val == "-" || OS.First().val == "<" || OS.First().val == "<=" || OS.First().val == ">" || OS.First().val == ">=" || OS.First().val == "==" || OS.First().val == "!=" || OS.First().val == "&&" || OS.First().val == "||")
                    {
                        EOE();
                    }
                    OS.Push(sar);
                }
                else OS.Push(sar);
            }


        }

        void newObj()
        {
            SAR argumentsSar = SAS.Pop();
            SAR typeSar = SAS.Pop();

            // Get constructor
            var constructor = symTable.Where(sym => sym.Value.Scope == $"g.{typeSar.val}").Where(sym => sym.Value.Kind == "Constructor").First();

            if (argumentsSar.arguments.Count() > 0)
            {
                if (!constructor.Value.Data.ContainsKey("Param")) semanticError(scanner.getToken().lineNum, "Constructor", typeSar.val, "Invalid arguments");

                List<string> constructorArgs = constructor.Value.Data["Param"];

                if (argumentsSar.arguments.Count() != constructorArgs.Count()) semanticError(scanner.getToken().lineNum, "Constructor", typeSar.val, "Invalid arguments");

                for (int i = 0; i < constructorArgs.Count(); i++)
                {
                    if (i > argumentsSar.arguments.Count - 1) semanticError(scanner.getToken().lineNum, "Constructor", typeSar.val, "Invalid arguments");

                    // Get constructor type
                    string ct = symTable[constructorArgs[i]].Data["type"];

                    // Get passed in type
                    string pt = symTable[argumentsSar.arguments[i].symKey].Data["type"];

                    if (pt != ct) semanticError(scanner.getToken().lineNum, "Constructor", typeSar.val, "Invalid arguments");
                }
            }

            else if (constructor.Value.Data.ContainsKey("Param")) semanticError(scanner.getToken().lineNum, "Constructor", typeSar.val, "Invalid arguments");

            SAR new_sar = new SAR(typeSar.val, SAR.types.new_sar, SAR.pushes.newObj, constructor.Key);
            new_sar.arguments = argumentsSar.arguments;
            SAS.Push(new_sar);
        }

        void newArray()
        {
            SAR expression = SAS.Pop();

            // Test that expression is an int
            if (symTable[expression.symKey].Data["type"] != "int") semanticError(scanner.getToken().lineNum, "Array init", expression.val, $"Array requires int index got {symTable[expression.symKey].Data["type"]}");

            tExist();

            SAR new_sar = new SAR("new_sar", SAR.types.new_sar, SAR.pushes.newArray);
            new_sar.arguments.Add(expression);

            // Semantics code
            SAR tempSar = SAS.Pop();
            tempSar.val += "[]";
            SAS.Push(tempSar);

            SAS.Push(new_sar);
        }

        void iExist()
        {
            SAR sar = SAS.Pop();

            if (sar.val == "this")
            {
                Regex pat = new Regex(@"g\.\w*\.\w+");
                if (!pat.IsMatch(scope)) semanticError(scanner.getToken().lineNum, "iExist", "this", "Wrong use of \"this\"");
                sar.pushType = SAR.pushes.iExist;
                sar.type = SAR.types.id_sar;
                SAS.Push(sar);
                return;
            }

            if (sar.type == SAR.types.lit_sar) return; // It's a literal and literals always exist so we're good

            var potSym = symTable.Where(sym => sym.Value.Scope == scope).ToList();

            if (potSym.Count == 0) semanticError(scanner.getToken().lineNum, "Variable", scanner.getToken().lexeme, "not defined");

            if (!potSym.Any(sym => sym.Value.Value == sar.val)) semanticError(scanner.getToken().lineNum, "Variable", scanner.getToken().lexeme, "not defined");

            //string key = potSym.Where(sym => sym.Value.Value == sar.val).First().Key;

            sar.pushType = SAR.pushes.iExist;
            sar.type = SAR.types.id_sar;
            //sar.symKey = key;
            SAS.Push(sar);
        }

        void rExist()
        {
            SAR ivarSar = SAS.Pop();
            SAR classSar = SAS.Pop();

            string className = "";

            if (classSar.val == "this")
            {
                Regex pat = new Regex(@"(\w+)");

                className = pat.Matches(scope)[1].ToString();
            }
            else className = symTable[classSar.symKey].Data["type"];

            var classSym = symTable.Where(sym => sym.Value.Scope == $"g.{className}").ToList(); // Get list of symbols for the class

            if (!classSym.Any(sym => sym.Value.Value == ivarSar.val)) semanticError(scanner.getToken().lineNum, "Variable", ivarSar.val, $"Not defined/not public in {scope}");

            // TODO: mark sym to indicate indirect addressing
            var symId = genId("t");

            var ivarSymId = classSym.Where(sym => sym.Value.Value == ivarSar.val).First().Key; // Get the symid for the instance variable

            if (classSar.val != "this")
            {
                if (symTable[ivarSymId].Data["accessMod"] == "private") semanticError(scanner.getToken().lineNum, "Variable", ivarSar.val, $"not public in {className}");
            }

            if (ivarSar.type == SAR.types.func_sar) symTable.Add(symId, new Symbol(scope, symId, "temp_ref", symTable[ivarSymId].Kind, new Dictionary<string, dynamic>() { { "type", symTable[ivarSymId].Data["returnType"] } }));
            else symTable.Add(symId, new Symbol(scope, symId,"temp_ref", symTable[ivarSymId].Kind, new Dictionary<string, dynamic>() { { "type", symTable[ivarSymId].Data["type"] } }));

            SAR newSar = new SAR($"{classSar.val}.{ivarSar.val}", SAR.types.ref_sar, SAR.pushes.rExist, symId);
            newSar.arguments = ivarSar.arguments;

            SAS.Push(newSar);

            // iCode
            quads.Add(new List<string>() { "REF", classSar.symKey, ivarSymId, symId });

        }

        void tPush(string type)
        {
            SAS.Push(new SAR(type, SAR.types.type_sar, SAR.pushes.tPush));
        }

        void tExist()
        {
            SAR sar = SAS.Pop();

            if (sar.val != "int" && sar.val != "char" && sar.val != "bool" && sar.val != "void")
            {
                if (!symTable.Where(sym => sym.Value.Kind == "Class").ToList().Any(sym => sym.Value.Value == sar.val)) semanticError(scanner.getToken().lineNum, "Type", sar.val, "not defined");
            }
        }

        void BAL()
        {
            SAR sar = new SAR("BAL", SAR.types.bal_sar, SAR.pushes.BAL);
            SAS.Push(sar);
        }

        void EAL()
        {
            SAR sar = new SAR("al_sar", SAR.types.al_sar, SAR.pushes.EAL);
            while(SAS.First().pushType != SAR.pushes.BAL)
            {
                sar.arguments.Insert(0,SAS.Pop());
            }

            SAS.Pop(); // Pop bal_sar

            SAS.Push(sar);
        }

        void func()
        {
            SAR arguments = SAS.Pop();
            SAR fSar = SAS.Pop();

            SAR functionSar = new SAR(fSar.val, SAR.types.func_sar, SAR.pushes.func);
            functionSar.arguments = arguments.arguments;

            SAS.Push(functionSar);
        }

        void CD(string val)
        {
            string className = scope.Substring(2);

            if (val != className) semanticError(scanner.getToken().lineNum, "Constructor", val, $"must match class name {className}");
        }

        void EOE(bool eof = false)
        {
            if (OS.Count == 0)
            {
                SAS.Clear();
                return;
            }

            if (eof)
            {
                while(OS.First().val != "=")
                {
                    var y = SAS.Pop();
                    var x = SAS.Pop();
                    var operTemp = OS.Pop();

                    if (operTemp.val == "*" || operTemp.val == "/" || operTemp.val == "-" || operTemp.val == "+")
                    {
                        if (symTable[x.symKey].Data["type"] != "int" && symTable[y.symKey].Data["type"] != "int") semanticError(scanner.getToken().lineNum, "Math operation", x.val, "Wrong types for math op");
                        string sym = genId("t");
                        symTable.Add(sym, new Symbol(scope, sym, "temp", "tempVal", new Dictionary<string, dynamic>() { { "type", "int" } }));
                        SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.EOE, sym);
                        SAS.Push(sar);
                    }
                    else if (operTemp.val == "<" || operTemp.val == "<=" || operTemp.val == ">" || operTemp.val == ">=")
                    {
                        if (symTable[x.symKey].Data["type"] != "int" && symTable[y.symKey].Data["type"] != "int") semanticError(scanner.getToken().lineNum, "Math operation", x.val, "Wrong types for math op");
                        string sym = genId("t");
                        symTable.Add(sym, new Symbol(scope, sym, "temp", "tempVal", new Dictionary<string, dynamic>() { { "type", "bool" } }));
                        SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.EOE, sym);
                        SAS.Push(sar);
                    }
                    else if (operTemp.val == "==" || operTemp.val == "!=")
                    {
                        if (symTable[x.symKey].Data["type"] != symTable[y.symKey].Data["type"]) semanticError(scanner.getToken().lineNum, "Logical operation", x.val, "Wrong types for logical op");
                        string sym = genId("t");
                        symTable.Add(sym, new Symbol(scope, sym, "temp", "tempVal", new Dictionary<string, dynamic>() { { "type", "bool" } }));
                        SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.EOE, sym);
                        SAS.Push(sar);
                    }
                    else if (operTemp.val == "&&" || operTemp.val == "||")
                    {
                        if (symTable[x.symKey].Data["type"] != "bool" || symTable[y.symKey].Data["type"] != "bool") semanticError(scanner.getToken().lineNum, "Logical operation", x.val, "Wrong types for logical op");
                        string sym = genId("t");
                        symTable.Add(sym, new Symbol(scope, sym, "temp", "tempVal", new Dictionary<string, dynamic>() { { "type", "bool" } }));
                        SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.EOE, sym);
                        SAS.Push(sar);
                    }
                }
                SAR op2 = SAS.Pop();
                SAR op1 = SAS.Pop();
                SAR oper = OS.Pop();

                if (oper.val != "=" || OS.Count > 0) semanticError(scanner.getToken().lineNum, "Assignment", string.Join("", scanner.buffer.Select(token => token.lexeme)), "Wrong assignment");
                if (op1.val == "this") semanticError(scanner.getToken().lineNum, "Assignment", string.Join("", scanner.buffer.Select(token => token.lexeme)), "Wrong assignment");

                if (symTable[op1.symKey].Kind != "lvar" && symTable[op1.symKey].Kind != "ivar") semanticError(scanner.getToken().lineNum, "Type", op1.val, "not lvalue");

                if (op2.pushType == SAR.pushes.newObj)
                {
                    if (symTable[op2.symKey].Data["returnType"] != symTable[op1.symKey].Data["type"]) semanticError(scanner.getToken().lineNum, "Type", op2.val, "not valid type");
                }
                else if (op2.pushType == SAR.pushes.newArray)
                {
                    if (op1.val.Substring(op1.val.Length - 2) != "[]") semanticError(scanner.getToken().lineNum, "Array init", op1.val, "Not array type");
                }
                else if (symTable[op1.symKey].Data["type"][0] == '@')
                {
                    string type = symTable[op1.symKey].Data["type"].Substring(2);

                    if (op1.arguments.Count != 1 && symTable[op2.symKey].Data["type"] == type) semanticError(scanner.getToken().lineNum, "Type", op2.val, "not valid type");

                    if (symTable[op2.symKey].Data["type"] == $"@:{type}")
                    {
                        if (op2.arguments.Count != 1) semanticError(scanner.getToken().lineNum, "Type", op2.val, "not valid type");
                        return;
                    }
                    else if (symTable[op2.symKey].Data["type"] != type) semanticError(scanner.getToken().lineNum, "Type", op2.val, "not valid type");
                }
                else if (symTable[op2.symKey].Data["type"] != symTable[op1.symKey].Data["type"]) semanticError(scanner.getToken().lineNum, "Type", op2.val, "not valid type");

                // iCode
                quads.Add(new List<string>() {"MOV", op2.symKey, op1.symKey});
            }
            else
            {
                var y = SAS.Pop();
                var x = SAS.Pop();
                var op = OS.Pop();

                if (op.val == "*" || op.val == "/" || op.val == "-" || op.val == "+")
                {
                    if (symTable[x.symKey].Data["type"] != "int" && symTable[y.symKey].Data["type"] != "int") semanticError(scanner.getToken().lineNum, "Math operation", x.val, "Wrong types for math op");
                    string sym = genId("t");
                    symTable.Add(sym, new Symbol(scope, sym, "temp", "tempVal", new Dictionary<string, dynamic>() { { "type", "int" } }));
                    SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.EOE, sym);
                    SAS.Push(sar);
                }
                else if (op.val == "<" || op.val == "<=" || op.val == ">" || op.val == ">=")
                {
                    if (symTable[x.symKey].Data["type"] != "int" && symTable[y.symKey].Data["type"] != "int") semanticError(scanner.getToken().lineNum, "Math operation", x.val, "Wrong types for math op");
                    string sym = genId("t");
                    symTable.Add(sym, new Symbol(scope, sym, "temp", "tempVal", new Dictionary<string, dynamic>() { { "type", "bool" } }));
                    SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.EOE, sym);
                    SAS.Push(sar);
                }
                else if (op.val == "==" || op.val == "!=")
                {
                    if (symTable[x.symKey].Data["type"] != symTable[y.symKey].Data["type"]) semanticError(scanner.getToken().lineNum, "Logical operation", x.val, "Wrong types for logical op");
                    string sym = genId("t");
                    symTable.Add(sym, new Symbol(scope, sym, "temp", "tempVal", new Dictionary<string, dynamic>() { { "type", "bool" } }));
                    SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.EOE, sym);
                    SAS.Push(sar);
                }
                else if (op.val == "&&" || op.val == "||")
                {
                    if (symTable[x.symKey].Data["type"] != "bool" || symTable[y.symKey].Data["type"] != "bool") semanticError(scanner.getToken().lineNum, "Logical operation", x.val, "Wrong types for logical op");
                    string sym = genId("t");
                    symTable.Add(sym, new Symbol(scope, sym, "temp", "tempVal", new Dictionary<string, dynamic>() { { "type", "bool" } }));
                    SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.EOE, sym);
                    SAS.Push(sar);
                }

                else if (op.val == "=")
                {
                    if (symTable[x.symKey].Kind != "lvar") semanticError(scanner.getToken().lineNum, "Type", x.val, "not lvalue");
                    if (symTable[y.symKey].Data["type"] != symTable[x.symKey].Data["type"]) semanticError(scanner.getToken().lineNum, "Type", y.val, "not valid type");
                }
            }
        }

        void ClosingParen()
        {
            while (OS.First().val != "(")
            {
                EOE();
            }
            OS.Pop();
        }

        void ClosingBracket()
        {
            while (OS.First().val != "[")
            {
                EOE();
            }
            OS.Pop();
        }

        void Argument()
        {
            while (OS.First().val != "(")
            {
                EOE();
            }
        }

        void arr()
        {
            SAR argument = SAS.Pop();
            SAR array = SAS.Pop();

            if (symTable[argument.symKey].Data["type"] != "int") semanticError(scanner.getToken().lineNum, "Array init", array.val, $"Array requires int idnex got {symTable[argument.symKey].Data["type"]}");

            SAR arr_sar = new SAR(array.val, SAR.types.arr_sar, SAR.pushes.arr, array.symKey);
            arr_sar.arguments.Add(argument);

            SAS.Push(arr_sar);
        }

        void dup(string val, bool clss = false)
        {
            if (clss)
            {
                var clssPotentials = symTable.Where(sym => sym.Value.Scope == "g").ToList().Where(sym => sym.Value.Value == val).ToList();
                if (clssPotentials.Count > 1) semanticError(scanner.getToken().lineNum, "Duplicate name", val, "Duplicate name");
                return;
            }

            var potentials = symTable.Where(sym => sym.Value.Scope == scope).ToList().Where(sym => sym.Value.Value == val).ToList();

            if (potentials.Count > 1) semanticError(scanner.getToken().lineNum, "Duplicate name", val, "Duplicate name");
        }

        void ifCase()
        {
            SAR sar = SAS.Pop();
            string type = symTable[sar.symKey].Data["type"];

            if (type != "bool") semanticError(scanner.getToken().lineNum, "if", type, $"if requires bool got {type}");
        }

        void whileCase()
        {
            SAR sar = SAS.Pop();
            string type = symTable[sar.symKey].Data["type"];

            if (type != "bool") semanticError(scanner.getToken().lineNum, "while", type, $"while requires bool got {type}");
        }

        void returnCase()
        {
            while(OS.Count > 0)
            {
                EOE();
            }

            SAR sar = SAS.Pop();
            string varType = symTable[sar.symKey].Data["type"];

            string functionName = scope.Split('.').Last();
            string classScope = scope.Substring(0, scope.LastIndexOf('.'));

            var functionSym = symTable.Where(sym => sym.Value.Scope == classScope).Where(sym => sym.Value.Value == functionName).First();
            string functionReturnType = functionSym.Value.Data["returnType"];

            if (functionReturnType != varType) semanticError(scanner.getToken().lineNum, "Return", varType, $"Function requires {functionReturnType} returned {varType}");

        }

        void coutCase()
        {
            while(OS.Count > 0)
            {
                EOE();
            }

            SAR sar = SAS.Pop();

            string varType = symTable[sar.symKey].Data["type"];

            if (varType != "int" && varType != "char") semanticError(scanner.getToken().lineNum, "cout", sar.val, $"cout not defined for {varType}");
        }

        void cinCase()
        {
            while (OS.Count > 0)
            {
                EOE();
            }

            SAR sar = SAS.Pop();

            string varType = symTable[sar.symKey].Data["type"];

            if (varType != "int" && varType != "char") semanticError(scanner.getToken().lineNum, "cin", sar.val, $"cin not defined for {varType}");
        }



        void semanticError(int line, string type, string lexeme, string prob)
        {
            Console.WriteLine($"{line}:{type} {lexeme} {prob}");
            Console.ReadKey();
            Environment.Exit(0);
        }

        #endregion
    }
}
