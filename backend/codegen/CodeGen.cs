using QuiScript.frontend;
using QuiScript.frontend.parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace QuiScript.backend.codegen
{
    public class CodeGen
    {
        private Root r;
        private string Path;
        private List<string> imports = new List<string>();
        private FunctionNode currentFunction;

   
        private Dictionary<string, ClassInfo> availableClasses = new Dictionary<string, ClassInfo>();

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
            if (importNode.Name == "IO" && !imports.Contains("IO"))
            {
                imports.Add("IO");
                availableClasses["IO"] = new ClassInfo("IO", new Dictionary<string, MethodInfo>
                {
                    { "Print", new MethodInfo("Print", "void", new List<string> { "string" }) },
                    { "ReadLine", new MethodInfo("ReadLine", "string", new List<string>()) }
                });
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
  .method public hidebysig static string ReadLine() cil managed
  {
    .maxstack 8
    call string [mscorlib]System.Console::ReadLine()
    ret
  }
}
";
                File.AppendAllText(Path, ioClass + "\n");
            }
            else if (importNode.Name == "Conv" && !imports.Contains("Conv"))
            {
                imports.Add("Conv");

          
                availableClasses["Conv"] = new ClassInfo("Conv", new Dictionary<string, MethodInfo>
                {
                    { "ToString", new MethodInfo("ToString", "string", new List<string> { "int32" }) }
                });

                string convClass = @"
.class public auto ansi abstract sealed beforefieldinit Conv
       extends [mscorlib]System.Object
{
  .method public hidebysig static string ToString(int32 number) cil managed
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
       
            Console.WriteLine($"Registering class: {stmt.Name}"); 
            availableClasses[stmt.Name] = new ClassInfo(stmt.Name, new Dictionary<string, MethodInfo>());

        
            if (stmt.Methods != null)
            {
                foreach (var method in stmt.Methods)
                {
                    if (method is FunctionNode funcNode)
                    {
                        // Rejestrujemy każdą metodę w słowniku
                        Console.WriteLine($"Registering method: {funcNode.Name} in class {stmt.Name}"); // Logowanie rejestracji metody
                        availableClasses[stmt.Name].Methods[funcNode.Name] = new MethodInfo(funcNode.Name, funcNode.Type,
                            funcNode.Args?.args.Select(arg => arg.Item1).ToList() ?? new List<string>());
                    }
                }
            }

         
            if (stmt.Name == "Program")
            {
                Console.WriteLine("Compiling 'Program' class...");
                File.AppendAllText(Path, ".class public auto ansi beforefieldinit Program\r\n       extends [mscorlib]System.Object\r\n{");

                foreach (var method in stmt.Methods)
                {
                    if (method is FunctionNode funcNode)
                    {
                        Console.WriteLine($"Compiling method: {funcNode.Name}"); 
                        CompileMethodSignature(funcNode);
                        currentFunction = funcNode;

                   
                        if (funcNode.Name == "Main")
                        {
                            Console.WriteLine("Main method found. Marking as entry point.");
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
            string methodSignature = $"\n.method public static hidebysig {funcNode.Type} {funcNode.Name}(";

            HashSet<string> declaredArguments = new HashSet<string>();

            if (funcNode.Args != null && funcNode.Args.args.Count > 0)
            {
                foreach (var (argType, argName) in funcNode.Args.args)
                {
                    if (!declaredArguments.Contains(argName))
                    {
                        methodSignature += $"{argName} {argType}, ";
                        declaredArguments.Add(argName);
                    }
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

            foreach (var stmt in funcNode.Block.Statements)
            {
                if (stmt is InstanceMethodCall instanceMethodCall)
                {
                    CompileMethodCall(instanceMethodCall);
                }
                else if (stmt is AssigmentNode asign)
                {
                    CompileExpression(asign.Expr, asign.Name);
                }
                else if(stmt is ReturnNode r)
                {
                    CompileExpression(r.Expr);
                    File.AppendAllText(Path,"ret\r\n");
                }
            }

            File.AppendAllText(Path, "ret\r\n");
            File.AppendAllText(Path, "}\r\n");
        }

        private void CompileVariableDeclaration(List<VariableDeclarationNode> dec, ref int index)
        {
            List<string> declaredVariables = new List<string>();

            File.AppendAllText(Path, "\n.locals init (\n");

            for (int i = 0; i < dec.Count; i++)
            {
                var v = dec[i];

                if (declaredVariables.Contains(v.Name))
                {
                    continue;
                }

                string separator = (i == dec.Count - 1) ? "" : ",";
                File.AppendAllText(Path, $"[{index}] {v.Type} {v.Name}{separator}\n");
                declaredVariables.Add(v.Name);
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
            else if (expr is InstanceMethodCall instanceMethodCall)
            {
                CompileMethodCall(instanceMethodCall);
            }
            else
            {
                throw new Exception($"Unsupported expression type: {expr.GetType().Name}");
            }

            if (varName != null)
            {
                int index = GetLocalIndex(varName);
                File.AppendAllText(Path, $"stloc.{index}\r\n");
            }
        }

        private void CompileMethodCall(InstanceMethodCall instanceMethodCall)
        {
            string className = instanceMethodCall.Parent;
            string methodName = instanceMethodCall.Method;

            if (availableClasses.ContainsKey(className))
            {
                var classInfo = availableClasses[className];

                if (classInfo.Methods.ContainsKey(methodName))
                {
                    var methodInfo = classInfo.Methods[methodName];

                    if (methodInfo.Arguments.Count == instanceMethodCall.args.Count)
                    {
                        foreach (var arg in instanceMethodCall.args)
                        {
                            CompileExpression(arg);
                        }
                        File.AppendAllText(Path, $"call {methodInfo.ReturnType} {className}::{methodName}({string.Join(",", methodInfo.Arguments)})\r\n");
                    }
                    else
                    {
                        throw new Exception($"Argument count does not match for method {methodName} in class {className}");
                    }
                }
                else
                {
                    Console.WriteLine("avaible methods: ");
                    foreach(var met in classInfo.Methods)
                    {
                        Console.WriteLine(met);
                    }
                    throw new Exception($"Method {methodName} not found in class {className}");
                }
            }
            else
            {
                throw new Exception($"Class {className} not found");
            }
        }

        private void CompileBinaryExpression(BinaryExpressionNode binaryExpression)
        {
            CompileExpression(binaryExpression.Left);
            CompileExpression(binaryExpression.Right);

            if (binaryExpression.Operator.Value == "+" && IsStringType(binaryExpression.Left) && IsStringType(binaryExpression.Right))
            {
                File.AppendAllText(Path, $"call string [mscorlib]System.String::Concat(string, string)\r\n");
            }
            else if (binaryExpression.Operator.Value == "*")
            {
                File.AppendAllText(Path, "mul\r\n");
            }
            else if (binaryExpression.Operator.Value == "/")
            {
                File.AppendAllText(Path, "div\r\n");
            }
            else if (binaryExpression.Operator.Value == "-")
            {
                File.AppendAllText(Path, "sub\r\n");
            }
            else
            {
                File.AppendAllText(Path, "add\r\n");
            }
        }

        private bool IsStringType(Node node)
        {
            if (node is IdentifiterNode identifier)
            {
                var variable = currentFunction.Locals?.FirstOrDefault(v => v.Name == identifier.Name);
                return variable != null && variable.Type == "string";
            }
            else if (node is StringLiteralNode)
            {
                return true;
            }
            else if (node is InstanceMethodCall instanceMethodCall)
            {
                if (availableClasses.ContainsKey(instanceMethodCall.Parent))
                {
                    var classInfo = availableClasses[instanceMethodCall.Parent];
                    var methodInfo = classInfo.Methods[instanceMethodCall.Method];
                    return methodInfo.ReturnType == "string";
                }
            }
            return false;
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

        public class ClassInfo
        {
            public string Name { get; set; }
            public Dictionary<string, MethodInfo> Methods { get; set; }

            public ClassInfo(string name, Dictionary<string, MethodInfo> methods)
            {
                Name = name;
                Methods = methods;
            }
        }

        public class MethodInfo
        {
            public string Name { get; set; }
            public string ReturnType { get; set; }
            public List<string> Arguments { get; set; }

            public MethodInfo(string name, string returnType, List<string> arguments)
            {
                Name = name;
                ReturnType = returnType;
                Arguments = arguments;
            }
        }
    }
}
