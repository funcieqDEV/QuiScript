using QuiScript.backend.codegen;
using QuiScript.frontend;
using System.IO;

public class Program
{
    public static string src_path;
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("I can't compile air!");
        }
        else
        {


            src_path = args[0];
            Lexer l = new Lexer(File.ReadAllText(src_path));
            var toks = l.Tokenize();
            Parser p = new Parser(toks);
            var r = p.Parse();

            foreach (var s in r.Statements)
            {
                Console.WriteLine(s.ToString());
            }

            CodeGen compiler = new(r);
            compiler.Compile(args[0].Replace(".qui",".exe"));
        }
    }
}