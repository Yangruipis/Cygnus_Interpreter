﻿using System;
using Cygnus.SyntaxTree;
using Cygnus.LexicalAnalyzer;
using Cygnus.SymbolTable;
namespace Cygnus.SyntaxAnalyzer
{
    public class AST
    {
        public AST() { }
        public BlockExpression Parse(Lexeme[] array, Scope GlobalScope)
        {
            var ASTparser = new ASTParser(array, GlobalScope);
            ASTparser.Parse(0, array.Length - 1);
            return ASTparser.program;
        }
        public void Display(BlockExpression Root)
        {
            Console.WriteLine("Abstract syntax tree: ");
            Console.WriteLine();
            ASTViewer.PrintTree(Root);
            Console.WriteLine();
            Console.WriteLine("End of the tree");
            Console.WriteLine();
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("                    output                    ");
            Console.WriteLine("----------------------------------------------");
        }
    }
}
