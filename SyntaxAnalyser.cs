using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    class SyntaxAnalyser
    {
        LexicalAnalyser scanner;
        string[] z = new string[] { "=", "&", "|", "!", "<", ">", "+", "-", "*", "/" };
        string[] state = new string[] { "{", "if", "while", "return", "cout", "cin", "switch", "break", "(", "true", "false", "null", "this" };
        string[] exp = new string[] { "(", "true", "false", "null", "this" };

        public SyntaxAnalyser(LexicalAnalyser s) { scanner = s; }

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
            scanner.nextToken();
            if (scanner.getToken().lexeme != "(") syntaxError("(");
            scanner.nextToken();
            if (scanner.getToken().lexeme != ")") syntaxError(")");
            scanner.nextToken();
            method_body();
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
                statement();
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
            type();
            if (scanner.getToken().type != "Identifier") syntaxError("Identifier");
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
            else if (scanner.getToken().lexeme == "&" && scanner.peekToken().lexeme == "&") expression();
            else if (scanner.getToken().lexeme == "|" && scanner.peekToken().lexeme == "|") expression();
            else if (scanner.getToken().lexeme == "!" && scanner.peekToken().lexeme == "=") expression();
            else if (scanner.getToken().lexeme == "<" && scanner.peekToken().lexeme == "=") expression();
            else if (scanner.getToken().lexeme == ">" && scanner.peekToken().lexeme == "=") expression();
            else if (scanner.getToken().type == "Math" || scanner.getToken().type == "Boolean") expression();
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
            }
            else if (scanner.getToken().lexeme == "[")
            {
                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != "]") syntaxError("]");
            }
            else syntaxError("( or [");
        }

        void class_declaration()
        {
            if (scanner.getToken().lexeme != "class") syntaxError("class");
            scanner.nextToken();
            class_name();
            if (scanner.getToken().lexeme != "{") syntaxError("{");
            scanner.nextToken();
            while (scanner.getToken().lexeme == "public" || scanner.getToken().lexeme == "private" || scanner.getToken().type == "Identifier")
            {
                class_member_declaration();
            }
            if (scanner.getToken().lexeme != "}") syntaxError("}");
            scanner.nextToken();
        }

        void class_member_declaration()
        {
            if (scanner.getToken().lexeme == "public" || scanner.getToken().lexeme == "private")
            {
                scanner.nextToken();
                type();
                if (scanner.getToken().type != "Identifier") syntaxError("Identifier");
                scanner.nextToken();
                field_declaration();
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
            scanner.nextToken();
            if (isAtype(scanner.getToken().lexeme)) parameter_list();
            if (scanner.getToken().lexeme != ")") syntaxError(")");
            scanner.nextToken();
            method_body();
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
            scanner.nextToken();
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
            }
            else if (scanner.getToken().lexeme == "[")
            {
                scanner.nextToken();
                expression();
                if (scanner.getToken().lexeme != "]") syntaxError("[");
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
            return true;
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
    }
}
