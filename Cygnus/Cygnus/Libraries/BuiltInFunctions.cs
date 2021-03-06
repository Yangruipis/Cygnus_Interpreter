﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cygnus.SyntaxTree;
using Cygnus.Extensions;
using Cygnus.AssemblyImporter;
using Cygnus.LexicalAnalyzer;
using Cygnus.SyntaxAnalyzer;
using Cygnus.Errors;
namespace Cygnus.Libraries
{
    public static class BuiltInFunctions
    {
        public static Expression Print(Expression[] args, Scope scope)
        {
            var obj = args.Single().GetObjectValue(scope);
            if (obj is Expression[])
                PrintList(((Expression[])obj).Select(j => j.GetObjectValue(scope)));
            else if (obj is List<Expression>)
                PrintList(((List<Expression>)obj).Select(j => j.GetObjectValue(scope)));
            else if (obj is Dictionary<ConstantExpression, Expression>)
                PrintList(((Dictionary<ConstantExpression, Expression>)obj)
                    .Select(j => (object)new KeyValuePair<object, object>(j.Key.GetObjectValue(scope), j.Value.GetObjectValue(scope))));
            else if (obj != null)
                Console.WriteLine(obj);
            return Expression.Void();
        }
        private static void PrintList(IEnumerable<object> objs)
        {
            Console.Write("{ ");
            using (var obj = objs.GetEnumerator())
                if (obj.MoveNext())
                    while (true)
                    {
                        Console.Write(obj.Current);
                        if (obj.MoveNext())
                            Console.Write(", ");
                        else break;
                    }
            Console.WriteLine(" }");
        }
        public static Expression InitArray(Expression[] args, Scope scope)
        {
            int n = (int)args.Single().GetValue<ConstantExpression>(ExpressionType.Constant, scope).Value;
            var arr = new Expression[n];
            for (int i = 0; i < n; i++)
                arr[i] = Expression.Null();
            return new ArrayExpression(arr);
        }
        public static Expression InitList(Expression[] args, Scope scope)
        {
            if (args.Length == 1 && args.Single().IsVoid(scope))
                return Expression.List(new List<Expression>());
            else
                return Expression.List(args.Select(i => i.GetValue(scope)).ToList());
        }
        public static Expression InitDictionary(Expression[] args, Scope scope)
        {
            if (args.Length == 1 && args.Single().IsVoid(scope))
                return new DictionaryExpression(new Dictionary<ConstantExpression, Expression>());
            else
                return new DictionaryExpression(args.Map(i =>
                {
                    var kvparr = i.AsArray(scope);
                    if (kvparr.Length != 2)
                        throw new ArgumentException("The length of key-value pair must be 2");
                    else
                        return new KeyValuePair<ConstantExpression, Expression>
                        (kvparr[0].AsConstant(scope), kvparr[1].GetValue(scope));
                }
                ));
        }
        public static Expression InitTable(Expression[] args, Scope scope)
        {
            return new TableExpression(args.Cast<ParameterExpression>()
                .Select(i => new KeyValuePair<string, Expression>(i.Name, Expression.Null())).ToArray());
        }
        public static Expression Length(Expression[] args, Scope scope)
        {
            return (args.Single().GetValue(scope) as IIndexable).Length;
        }
        public static Expression Import(Expression[] args, Scope scope)
        {
            new CSharpAssembly(args[0].AsString(scope), args[1].AsString(scope)).Import();
            return Expression.Void();
        }
        public static Expression SetParent(Expression[] args, Scope scope)
        {
            var table = args[0].GetValue<TableExpression>(ExpressionType.Table, scope);
            var parent_table = args[1].GetValue<TableExpression>(ExpressionType.Table, scope);
            table.Parent = parent_table;
            return Expression.Void();
        }
        public static Expression Range(Expression[] args, Scope scope)
        {
            if (args.Length == 1)
            {
                return
                    Expression.IEnumerable(
                    Enumerable.Range(0, args[0].As<int>(scope))
                    .Select(i => Expression.Constant(i, ConstantType.Integer)));
            }
            else if (args.Length == 2 || args.Length == 3)
            {
                int start = args[0].As<int>(scope);
                int end = args[1].As<int>(scope);
                if (args.Length == 2)
                    return
                         Expression.IEnumerable(
                             Enumerable.Range(start, end - start)
                             .Select(i => Expression.Constant(i, ConstantType.Integer)));
                else if (args.Length == 3)
                {
                    int step = args[2].As<int>(scope);
                    return
                        Expression.IEnumerable(
                            GetRange(start, end, step)
                            .Select(i => Expression.Constant(i, ConstantType.Integer)));
                }
                else throw new ArgumentException();
            }
            else throw new ArgumentException();
        }
        private static IEnumerable<int> GetRange(int start, int end, int step)
        {
            if (step == 0) throw new ArgumentException();
            if (step > 0)
                for (int i = start; i < end; i += step)
                    yield return i;
            else
                for (int i = start; i > end; i += step)
                    yield return i;
        }
        public static Expression ExecuteFile(Expression[] args, Scope scope)
        {
            var FilePath = args[0].AsString(scope);
            var encoding = Encoding.Default;
            using (var lex = new Lexical(FilePath, encoding, TokenDefinition.tokenDefinitions))
            {
                lex.Tokenize();
                var lex_array = Lexeme.Generate(lex.tokenList);
                var ast = new AST();
                BlockExpression Root = ast.Parse(lex_array, scope);
                //    ast.Display(Root);
                Expression Result = Root.Eval(scope).GetValue(scope);
                return Result;
            }
        }
        public static Expression Scan(Expression[] args, Scope scope)
        {
            if (args.Length != 0 && args.Length != 1) throw new ArgumentException();
            if (args.Length == 1)
                Console.Write(args.Single().GetValue<ConstantExpression>(ExpressionType.Constant, scope).Value);
            return Console.ReadLine();
        }
        public static Expression Throw(Expression[] args, Scope scope)
        {
            throw new Exception(args.Single().AsString(scope));
        }
        public static Expression Delete(Expression[] args, Scope scope)
        {
            foreach (var item in args.Cast<ParameterExpression>().Select(i => i.Name))
            {
                if (!scope.Delete(item))
                {
                    if (Scope.functionTable.ContainsKey(item))
                        Scope.functionTable.Remove(item);
                    else throw new NotDefinedException(item);
                }
            }
            return new ConstantExpression(null, ConstantType.Void);
        }
        public static Expression Exit(Expression[] args, Scope scope)
        {
            Environment.Exit(0);
            return Expression.Void();
        }
    }
}
