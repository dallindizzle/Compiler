using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

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

                // Semantic code
                EOE(true);

                scanner.nextToken();
            }

        }

        void variable_declaration()
        {
            string typ = scanner.getToken().lexeme; // This is for symbol table

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
                numeric_literal();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().type == "Character")
            {
                scanner.nextToken();
                if (isAexpressionZ(scanner.getToken().lexeme)) expressionZ();
            }
            else if (scanner.getToken().type == "Identifier")
            {
                // Semantic code
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
                EAL();
                newObj();

                scanner.nextToken();
            }
            else if (scanner.getToken().lexeme == "[")
            {
                scanner.nextToken();

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

                // Semantics code
                tExist();

                if (scanner.getToken().type != "Identifier") syntaxError("Identifier");

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
            if (scanner.getToken().type != "Identifier") syntaxError("Identifier");
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
                // Semnatics code
                oPush(scanner.getToken().lexeme);

                scanner.nextToken();

                // Semantics code
                BAL();

                if (isAargument_list(scanner.getToken().lexeme)) argument_list();
                if (scanner.getToken().lexeme != ")") syntaxError(")");

                // Semantics code
                OS.Pop(); // This should pop the open paranthesis
                EAL();
                func();

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
                EOE,
                mul,
                BAL,
                EAL,
                func,
                none
            };

            public enum types
            {
                id_sar,
                ref_sar,
                bal_sar,
                al_sar,
                func_sar,
                type_sar,
                var_sar,
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

        void oPush(string op)
        {
            SAR sar = new SAR(op, SAR.types.none, SAR.pushes.oPush);

            if (op == "(")
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


        }

        void iExist()
        {
            SAR sar = SAS.Pop();

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

            string className = symTable[classSar.symKey].Data["type"];

            var classSym = symTable.Where(sym => sym.Value.Scope == $"g.{className}").ToList(); // Get list of symbols for the class

            if (!classSym.Any(sym => sym.Value.Value == ivarSar.val)) semanticError(scanner.getToken().lineNum, "Variable", ivarSar.val, $"Not defined/not public in {scope}");

            var symId = genId("t");

            var ivarSymId = classSym.Where(sym => sym.Value.Value == ivarSar.val).First().Key; // Get the symid for the instance variable

            if (symTable[ivarSymId].Data["accessMod"] == "private") semanticError(scanner.getToken().lineNum, "Variable", ivarSar.val, $"not public in {className}");

            if (ivarSar.type == SAR.types.func_sar) symTable.Add(symId, new Symbol(scope, symId, "temp_ref", symTable[ivarSymId].Kind, new Dictionary<string, dynamic>() { { "type", symTable[ivarSymId].Data["returnType"] } }));
            else symTable.Add(symId, new Symbol(scope, symId,"temp_ref", symTable[ivarSymId].Kind, new Dictionary<string, dynamic>() { { "type", symTable[ivarSymId].Data["type"] } }));

            SAS.Push(new SAR($"{classSar.val}.{ivarSar.val}", SAR.types.ref_sar, SAR.pushes.rExist, symId));

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
                sar.arguments.Add(SAS.Pop());
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

        void EOE(bool eof = false)
        {
            if (eof)
            {
                while(OS.First().val != "=")
                {
                    var op2temp = SAS.Pop();
                    var op1temp = SAS.Pop();
                    var operTemp = OS.Pop();

                    if (operTemp.val == "*" || operTemp.val == "/" || operTemp.val == "-" || operTemp.val == "+")
                    {
                        if (symTable[op1temp.symKey].Data["type"] != "int" && symTable[op2temp.symKey].Data["type"] != "int") semanticError(scanner.getToken().lineNum, "Math operation", op1temp.val, "Wrong types for math op");
                        string sym = genId("t");
                        symTable.Add(sym, new Symbol(scope, sym, "temp", "tempVal", new Dictionary<string, dynamic>() { { "type", "int" } }));
                        SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.EOE, sym);
                        SAS.Push(sar);
                    }
                }
                SAR op2 = SAS.Pop();
                SAR op1 = SAS.Pop();
                SAR oper = OS.Pop();

                if (oper.val != "=" || OS.Count > 0) semanticError(scanner.getToken().lineNum, "Assignment", string.Join("", scanner.buffer.Select(token => token.lexeme)), "Wrong assignment");

                if (symTable[op1.symKey].Kind != "lvar" && symTable[op1.symKey].Kind != "ivar") semanticError(scanner.getToken().lineNum, "Type", op1.val, "not lvalue");
                if (symTable[op2.symKey].Data["type"] != symTable[op1.symKey].Data["type"]) semanticError(scanner.getToken().lineNum, "Type", op2.val, "not valid type");
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
                    SAR sar = new SAR(sym, SAR.types.none, SAR.pushes.mul, sym);
                    SAS.Push(sar);
                }

                else if (op.val == "=")
                {
                    if (symTable[x.symKey].Kind != "lvar") semanticError(scanner.getToken().lineNum, "Type", x.val, "not lvalue");
                    if (symTable[y.symKey].Data["type"] != symTable[x.symKey].Data["type"]) semanticError(scanner.getToken().lineNum, "Type", y.val, "not valid type");
                }
            }
        }

        void dup(string val)
        {
            if (symTable.Where(sym => sym.Value.Scope == scope).ToList().Where(sym => sym.Value.Value == val).ToList().Count > 1) semanticError(scanner.getToken().lineNum, "Duplicate name", val, "Duplicate name");
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
