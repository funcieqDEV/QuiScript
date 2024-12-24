using QuiScript.frontend;
using QuiScript.frontend.parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace QuiScript.backend.codegen
{
    public class CodeGen
    {
        private Root r;
        private string Path;
        private List<string> imports = new List<string>();
        private FunctionNode currentFunction;

        public CodeGen(Root r)
        {
            this.r = r;
        }

        public void Compile(string outputPath)
        {
            string ilPath = outputPath.Replace(".exe", ".il");
            this.Path = ilPath;

            string start = @"
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)        
  .ver 4:0:0:0
}

.assembly quiscript
{
  .ver 1:0:0:0
}

.module main.exe

.imagebase           0x00400000
.file alignment      0x00000200
.stackreserve        0x00100000
.subsystem           0x0003       
.corflags            0x00020003
";
            File.Delete(ilPath);
            File.AppendAllText(ilPath, start);

            TraverseStatements(r.Statements);

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "\"C:\\Users\\HP\\Downloads\\QuiScript\\QuiScript\\ilasm.exe\"";
                process.StartInfo.Arguments = $"/exe \"{ilPath}\" /output=\"{outputPath}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                Console.WriteLine("Compilation successful.");
                Console.WriteLine(output);

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("Compilation errors:");
                    Console.WriteLine(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during compilation: {ex.Message}");
            }
        }

        private void TraverseStatements(List<Node> statements)
        {
            foreach (var stmt in statements)
            {
                if (stmt is ImportNode importNode)
                {
                    CompileImport(importNode);
                }
                else if (stmt is ClassNode classNode)
                {
                    CompileClass(classNode);
                }
            }
        }
        private void CompileImport(ImportNode importNode)
        {
            if (importNode.Name == "IO")
            {
                imports.Add("IO");

                string ioClass = @"
.class public auto ansi abstract sealed beforefieldinit IO
       extends [mscorlib]System.Object
{
  .method public hidebysig static void Print(string text) cil managed
  {
    .maxstack 8
    ldarg.0
    call void [mscorlib]System.Console::WriteLine(string)
    ret
  }
}";
                File.AppendAllText(Path, ioClass + "\n");
            }
            else if (importNode.Name == "Conv")
            {
                imports.Add("Conv");

                string convClass = @"
.class public auto ansi abstract sealed beforefieldinit Conv
       extends [mscorlib]System.Object
{
  .method public hidebysig static string ToString(int32 value) cil managed
  {
    .maxstack 8
    ldarg.0
    call string [mscorlib]System.Convert::ToString(int32)
    ret
  }
}";
                File.AppendAllText(Path, convClass + "\n");
            }
        }



        private void CompileClass(ClassNode stmt)
        {
            if (stmt.Name == "Program")
            {
                File.AppendAllText(Path, ".class public auto ansi beforefieldinit Program\r\n       extends [mscorlib]System.Object\r\n{");

                foreach (var method in stmt.Methods)
                {
                    if (method is FunctionNode funcNode)
                    {
                        CompileMethodSignature(funcNode);
                        currentFunction = funcNode;

                        if (funcNode.Name == "Main")
                        {
                            CompileMethodBody(funcNode, true);
                        }
                        else
                        {
                            CompileMethodBody(funcNode);
                        }
                    }
                }

                File.AppendAllText(Path, "\n}");
            }
        }

        private void CompileMethodSignature(FunctionNode funcNode)
        {
            string methodSignature = $"\n.method public hidebysig {funcNode.Type} {funcNode.Name}(";

            if (funcNode.Args != null && funcNode.Args.args.Count > 0)
            {
                foreach (var (argType, argName) in funcNode.Args.args)
                {
                    methodSignature += $"{argName} {argType}, ";
                }
                methodSignature = methodSignature.TrimEnd(',', ' ');
            }

            if (funcNode.Name == "Main")
            {
                methodSignature = methodSignature.Replace("public", "public static");
            }

            File.AppendAllText(Path, methodSignature + ")\r\n");
        }

        private void CompileMethodBody(FunctionNode funcNode, bool isMain = false)
        {
            List<VariableDeclarationNode> vars = new();
            int localIndex = 0;

            File.AppendAllText(Path, "{\r\n");
            if (isMain)
            {
                File.AppendAllText(Path, "\n\r.entrypoint\n");
            }

            foreach (var stmt in funcNode.Block.Statements)
            {
                if (stmt is VariableDeclarationNode dec)
                {
                    vars.Add(dec);
                }
            }

            CompileVariableDeclaration(vars, ref localIndex);

            foreach (var statement in funcNode.Block.Statements)
            {
                if (statement is InstanceMethodCall instanceMethodCall)
                {
                    CompileInstanceMethodCall(instanceMethodCall);
                }
            }

            File.AppendAllText(Path, "ret\r\n");
            File.AppendAllText(Path, "}\r\n");
        }

        private void CompileBinaryExpression(BinaryExpressionNode binaryExpression)
        {
           
            CompileExpression(binaryExpression.Left);

            
            if (binaryExpression.Operator.Value == "+")
            {
                
                if (binaryExpression.Left is StringLiteralNode || binaryExpression.Left is IdentifiterNode id && IsStringVariable(id.Name))
                {
                    
                    if (binaryExpression.Right is StringLiteralNode || binaryExpression.Right is IdentifiterNode id2 && IsStringVariable(id2.Name))
                    {
                        
                        CompileExpression(binaryExpression.Right);

                        
                        File.AppendAllText(Path, "call string [mscorlib]System.String::Concat(string, string)\r\n");
                    }
                    else
                    {
                        
                        CompileExpression(binaryExpression.Right);
                    }
                }
                else
                {
                   
                    CompileExpression(binaryExpression.Right);
                    File.AppendAllText(Path, "add\r\n");
                }
            }
            else
            {
                
                CompileExpression(binaryExpression.Right);
                switch (binaryExpression.Operator.Value)
                {
                    case "-":
                        File.AppendAllText(Path, "sub\r\n");
                        break;
                    case "*":
                        File.AppendAllText(Path, "mul\r\n");
                        break;
                    case "/":
                        File.AppendAllText(Path, "div\r\n");
                        break;
                   
                    default:
                        throw new Exception($"Unsupported operator in binary expression: {binaryExpression.Operator.Value}");
                }
            }
        }




        private bool IsStringExpression(Node expr)
        {
            return expr is StringLiteralNode ||
                   (expr is IdentifiterNode id && currentFunction?.Locals?.Exists(v => v.Name == id.Name && v.Type == "string") == true);
        }

        private void CompileInstanceMethodCall(InstanceMethodCall instanceMethodCall)
        {
            if (instanceMethodCall.Parent == "IO" && imports.Contains("IO"))
                if (instanceMethodCall.Parent == "IO" && imports.Contains("IO"))
                {
                    if (instanceMethodCall.Method == "Print" && instanceMethodCall.args.Count == 1)
                    {
                        var arg = instanceMethodCall.args[0];

                        
                        if (!(arg is StringLiteralNode))
                        {
                          
                            CompileExpression(arg);

                         
                            if (arg is IntLiteral || arg is IdentifiterNode)
                            {
                                File.AppendAllText(Path, "call string [mscorlib]System.Convert::ToString(int32)\r\n");
                            }
                        }
                        else
                        {
                            
                            CompileExpression(arg);
                        }

                      
                        File.AppendAllText(Path, "call void IO::Print(string)\r\n");
                    }
                }

                else if (instanceMethodCall.Parent == "Conv" && imports.Contains("Conv"))
            {
                if (instanceMethodCall.Method == "ToString" && instanceMethodCall.args.Count == 1)
                {
                    CompileExpression(instanceMethodCall.args[0]);
                    File.AppendAllText(Path, "call string [mscorlib]System.Convert::ToString(int32)\\r\\n\"");
                }
            }
        }


        private void CompileVariableDeclaration(List<VariableDeclarationNode> dec, ref int index)
        {
            File.AppendAllText(Path, "\n.locals init (\n");

            for (int i = 0; i < dec.Count; i++)
            {
                var v = dec[i];
                string separator = (i == dec.Count - 1) ? "" : ",";
                File.AppendAllText(Path, $"[{index}] {v.Type} {v.Name}{separator}\n");
                index++;

            
                if (currentFunction.Locals == null)
                {
                    currentFunction.Locals = new List<VariableDeclarationNode>();
                }
                currentFunction.Locals.Add(v);
            }


            File.AppendAllText(Path, ")\n");

            foreach (var v in dec)
            {
                if (v.Expr != null)
                {
                    CompileExpression(v.Expr, v.Name); 
                }
            }
        }


        private void CompileExpression(Node expr, string varName = null)
        {
            if (expr is StringLiteralNode stringLiteral)
            {
            
                File.AppendAllText(Path, $"ldstr \"{stringLiteral.tok}\"\r\n");
            }
            else if (expr is IntLiteral intLiteral)
            {
             
                File.AppendAllText(Path, $"ldc.i4 {intLiteral.tok.Value}\r\n");
            }
            else if (expr is IdentifiterNode identifier)
            {
                int localIndex = GetLocalIndex(identifier.Name);
                File.AppendAllText(Path, $"ldloc.{localIndex}\r\n");
            }
            else if (expr is BinaryExpressionNode binaryExpression)
            {
                CompileBinaryExpression(binaryExpression);
            }

         
            if (varName != null)
            {
                int index = GetLocalIndex(varName);
                File.AppendAllText(Path, $"stloc.{index}\r\n");
            }
        }


        private bool IsStringVariable(string varName)
        {
            if (currentFunction == null || currentFunction.Locals == null)
            {
                return false;
            }

       
            var variable = currentFunction.Locals.FirstOrDefault(v => v.Name == varName);
            return variable != null && variable.Type == "string";
        }



        private int GetLocalIndex(string name)
        {
            if (currentFunction == null)
            {
                throw new Exception("No current function context found.");
            }

            for (int i = 0; i < currentFunction.Locals.Count; i++)
            {
                if (currentFunction.Locals[i].Name == name)
                {
                    return i;
                }
            }
            throw new Exception($"Variable {name} not found in current function's local variables.");
        }
    }
}
