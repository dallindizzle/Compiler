using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    class Symbol
    {
        public string Scope;
        public string Symid;
        public string Value;
        public string Kind;
        public Dictionary<string, dynamic> Data;

        public Symbol(string sc, string sy, string v, string k, Dictionary<string, dynamic> d = null)
        {
            Scope = sc;
            Symid = sy;
            Value = v;
            Kind = k;

            if (d == null) Data = new Dictionary<string, dynamic>();
            else Data = d;
        }
    }

    class SyntaxAnalyser
    {
        LexicalAnalyser scanner;
        string[] z = new string[] { "=", "&", "|", "!", "<", ">", "+", "-", "*", "/" };
        string[] state = new string[] { "{", "if", "while", "return", "cout", "cin", "switch", "break", "(", "true", "false", "null", "this" };
        string[] exp = new string[] { "(", "true", "false", "null", "this" };

        string scope;

        public Dictionary<string, Symbol> symTable;
        int symId;
        string lastId;

        public SyntaxAnalyser(LexicalAnalyser s)
        {
            scanner = s;
            scope = "g";

            symTable = new Dictionary<string, Symbol>();
            symId = 0;
        }

        public void go()
        {
            compilation_unit();
        }

        void compilation_unit()
        {
            while(scanner.getToken().lexeme == "class")
            {
                class_declaration();
            }

            if (scanner.getToken().lexeme != "void") syntaxError("void");
            scanner.nextToken();
            if (scanner.getToken().lexeme != "kxi2019") syntaxError("kxi2019");
            scanner.nextToken();
            if (scanner.getToken().lexeme != "main") syntaxError("main");

            string id = genId("F");
            symTable.Add(id, new Symbol(scope, id, scanner.getToken().lexeme, "main", new Dictionary<string, dynamic>() { { "returnType", "void" } }));

            scanner.nextToken();
            if (scanner.getToken().lexeme != "(") syntaxError("(");

            push("main");

            scanner.nextToken();
            if (scanner.getToken().lexeme != ")") syntaxError(")");
            scanner.nextToken();
            method_body();

            pop();

            if (scanner.getToken().lexeme != "eof") syntaxError("eof");
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
                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != ")") syntaxError(")");
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
                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != ")") syntaxError(")");
                scanner.nextToken();
                statement();
            }
            else if (scanner.getToken().lexeme == "return")
            {
                scanner.nextToken();
                if (isAexpression()) expression();
                if (scanner.getToken().lexeme != ";") syntaxError(";");
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
                scanner.nextToken();
            }
            else
            {
                expression();
                if (scanner.getToken().lexeme != ";") syntaxError(";");
                scanner.nextToken();
            }

        }

        void variable_declaration()
        {
            string typ = scanner.getToken().lexeme; // This is for symbol table

            type();
            if (scanner.getToken().type != "Identifier") syntaxError("Identifier");

            // This block is for symbol table
            string id = genId("L");
            symTable.Add(id, new Symbol(scope, id, scanner.getToken().lexeme, "lvar", new Dictionary<string, dynamic>() { {"type", typ } }));

            scanner.nextToken();
            if (scanner.getToken().lexeme == "[")
            {
                symTable[id].Data["type"] = $"@:{typ}";

                scanner.nextToken();
                if (scanner.getToken().lexeme != "]") syntaxError("]");
                scanner.nextToken();
            }
            if (scanner.getToken().lexeme == "=")
            {
                scanner.nextToken();
                assignment_expression();
            }
            if (scanner.getToken().lexeme != ";") syntaxError(";");
            scanner.nextToken();
        }

        void expression()
        {
            if (scanner.getToken().lexeme == "(")
            {
                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme == ")")
                {
                    scanner.nextToken();
                    if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
                }
                else syntaxError(")");
            }
            else if (scanner.getToken().lexeme == "true" || scanner.getToken().lexeme == "false" || scanner.getToken().lexeme == "null")
            {
                scanner.nextToken();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().lexeme == "this")
            {
                scanner.nextToken();
                if (isAmember_refZ(scanner.getToken().lexeme)) member_refZ();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().type == "Number" || scanner.getToken().lexeme == "+" || scanner.getToken().lexeme == "-")
            {
                string id = genId("N");
                symTable.Add(id, new Symbol(scope, id, scanner.getToken().lexeme, "ilit", new Dictionary<string, dynamic>() { { "type", "int" } }));

                numeric_literal();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().type == "Character")
            {
                string id = genId("H");
                symTable.Add(id, new Symbol(scope, id, scanner.getToken().lexeme, "clit", new Dictionary<string, dynamic>() { { "type", "char" } }));

                scanner.nextToken();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().type == "Identifier")
            {
                scanner.nextToken();
                if (isAfn_arr_member(scanner.getToken().lexeme)) fn_arr_member();
                if (isAmember_refZ(scanner.getToken().lexeme)) member_refZ();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else syntaxError("Expression");
        }

        void expressionZ()
        {
            if (scanner.getToken().lexeme == "=")
            {
                scanner.nextToken();
                if (scanner.getToken().lexeme == "=")
                {
                    scanner.nextToken();
                    expression();
                }
                else assignment_expression();
            }
            else if (scanner.getToken().lexeme == "&" && scanner.peekToken().lexeme == "&")
            {
                scanner.nextToken();
                scanner.nextToken();
                expression();
            }
            else if (scanner.getToken().lexeme == "|" && scanner.peekToken().lexeme == "|")
            {
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
                scanner.nextToken();
                if (isAargument_list(scanner.getToken().lexeme)) argument_list();
                if (scanner.getToken().lexeme != ")") syntaxError(")");
                scanner.nextToken();
            }
            else if (scanner.getToken().lexeme == "[")
            {
                scanner.nextToken();

                //TODO: change symbol to array
                //symTable[lastId].Data["type"] = $"@:{symTable[lastId].Data["type"]}";

                expression();
                if (scanner.getToken().lexeme != "]") syntaxError("]");
                scanner.nextToken();
            }
            else syntaxError("( or [");
        }

        void class_declaration()
        {
            if (scanner.getToken().lexeme != "class") syntaxError("class");
            scanner.nextToken();

            // This code is for the symbol table
            string id = genId("C");
            symTable.Add(id, new Symbol(scope, id, scanner.getToken().lexeme, "Class"));
            push(scanner.getToken().lexeme); // Here we push the class name to the scope, which we assume is here

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
                string modifier = scanner.getToken().lexeme; // This is for the symbol table

                scanner.nextToken();

                string typ = scanner.getToken().lexeme; // This is for the symbol table

                type();
                if (scanner.getToken().type != "Identifier") syntaxError("Identifier");

                // This whole block is for the symbol table
                string id;
                if (scanner.peekToken().lexeme == "(")
                {
                    id = genId("M");
                    symTable.Add(id, new Symbol(scope, id, scanner.getToken().lexeme, "method", new Dictionary<string, dynamic>() { { "returnType", typ }, { "accessMod", modifier } }));
                }
                else
                {
                    id = genId("V");
                    symTable.Add(id, new Symbol(scope, id, scanner.getToken().lexeme, "ivar", new Dictionary<string, dynamic>() { { "type", typ },{ "accessMod", modifier } }));
                }

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
                    symTable[lastId].Data["type"] = $"@:{symTable[lastId].Data["type"]}"; // For symbol table

                    scanner.nextToken();
                    if (scanner.getToken().lexeme != "]") syntaxError("]");
                    scanner.nextToken();
                }

                if (scanner.getToken().lexeme == "=")
                {
                    scanner.nextToken();
                    assignment_expression();
                }

                if (scanner.getToken().lexeme != ";") syntaxError(";");
                scanner.nextToken();
            }
        }

        void constructor_declaration()
        {
            string id = genId("X");
            symTable.Add(id, new Symbol(scope, id, scanner.getToken().lexeme, "Constructor", new Dictionary<string, dynamic>() { { "returnType", scanner.getToken().lexeme } }));

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
            // This block is for the symbol table
            string mId = lastId;
            List<string> pars = new List<string>();
            string id = genId("P");
            pars.Add(id);
            symTable.Add(id, new Symbol(scope, id, scanner.peekToken().lexeme, "param", new Dictionary<string, dynamic>() { { "type", scanner.getToken().lexeme }}));

            parameter();
            while (scanner.getToken().lexeme == ",")
            {
                scanner.nextToken();

                id = genId("P");
                pars.Add(id);
                symTable.Add(id, new Symbol(scope, id, scanner.peekToken().lexeme, "param", new Dictionary<string, dynamic>() { { "type", scanner.getToken().lexeme } }));

                parameter();
            }

            symTable[mId].Data.Add("Param", pars);
        }

        void parameter()
        {
            type();
            if (scanner.getToken().type != "Identifier") syntaxError("Identifier");
            scanner.nextToken();
            if (scanner.getToken().lexeme == "[")
            {
                symTable[lastId].Data["type"] = $"@:{symTable[lastId].Data["type"]}"; // For symbol table

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
                scanner.nextToken();
            }
            else class_name();
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
            scanner.nextToken();
            if (isAfn_arr_member(scanner.getToken().lexeme)) fn_arr_member();
            if (isAmember_refZ(scanner.getToken().lexeme)) member_refZ();
        }

        void fn_arr_member()
        {
            if (scanner.getToken().lexeme == "(")
            {
                scanner.nextToken();
                if (isAargument_list(scanner.getToken().lexeme)) argument_list();
                if (scanner.getToken().lexeme != ")") syntaxError(")");
                scanner.nextToken();
            }
            else if (scanner.getToken().lexeme == "[")
            {
                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != "]") syntaxError("[");
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
                symTable[lastId].Value += scanner.getToken().lexeme;
                scanner.nextToken();
            }
            else
            {
                if (scanner.getToken().type != "Number") syntaxError("Number");
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
            foreach(var symbol in symTable)
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
    }
}
