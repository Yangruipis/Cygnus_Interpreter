﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cygnus.LexicalAnalyzer;
using Cygnus.SyntaxAnalyzer;
using Cygnus.SyntaxTree;
using Cygnus.SymbolTable;
namespace Cygnus.Executors
{
    public class ExecuteInConsole
    {
        public Scope GlobalScope;
        Stack<TokenType> stack;
        LinkedList<Token> currentList;
        public ExecuteInConsole()
        {
            GlobalScope = new Scope();
            stack = new Stack<TokenType>();
            currentList = new LinkedList<Token>();
        }
        public ExecuteInConsole(Scope GlobalScope)
        {
            this.GlobalScope = GlobalScope;
            stack = new Stack<TokenType>();
            currentList = new LinkedList<Token>();
        }
        public void Run()
        {
            while (true)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(">>> ");
                    string line = Console.ReadLine();
                Start:
                    using (var lex = new Lexical(line, TokenDefinition.tokenDefinitions))
                    {
                        lex.Tokenize();
                        foreach (var item in lex.tokenList)
                            currentList.AddLast(item);
                        if (!Check(lex.tokenList))
                        {
                            Console.Write("... ");
                            line = Console.ReadLine();
                            goto Start;
                        }
                        var lex_array = Lexeme.Generate(currentList);
                        currentList.Clear();

                        var ast = new AST();
                        BlockExpression Root = ast.Parse(lex_array, GlobalScope);
                       //   ast.Display(Root);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Expression Result = Root.Eval(GlobalScope).GetValue(GlobalScope);
                        //Console.WriteLine(Result);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.ToString());
                    currentList.Clear();
                    stack.Clear();
                }
            }
        }
        public bool Check(LinkedList<Token> list)
        {
            foreach (var item in list)
            {
                switch (item.tokenType)
                {
                    case TokenType.LeftBrace:
                    case TokenType.LeftParenthesis:
                    case TokenType.LeftBracket:
                    case TokenType.Function:
                    case TokenType.Do:
                    case TokenType.Begin:
                    case TokenType.Then:
                        stack.Push(item.tokenType); break;
                    case TokenType.RightBrace:
                        if (stack.Peek() != TokenType.LeftBrace) throw new ArgumentException();
                        else stack.Pop(); break;

                    case TokenType.RightBracket:
                        if (stack.Peek() != TokenType.LeftBracket) throw new ArgumentException();
                        else stack.Pop(); break;
                    case TokenType.RightParenthesis:
                        if (stack.Peek() != TokenType.LeftParenthesis
                            && stack.Peek() != TokenType.Function)
                            throw new ArgumentException();
                        else stack.Pop(); break;
                    case TokenType.End:
                        if (stack.Peek() != TokenType.Do
                            && stack.Peek() != TokenType.Begin
                            && stack.Peek() != TokenType.Then
                            && stack.Peek() != TokenType.Else)
                            throw new ArgumentException();
                        else stack.Pop(); break;
                }
            }
            return stack.Count == 0;
        }
    }
}
