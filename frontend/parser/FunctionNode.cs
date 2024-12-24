using QuiScript.frontend.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class FunctionNode : Node
    {
       public string Name;
        public BlockNode Block;
        public string Type;
       public ArgNode Args;
        public List<VariableDeclarationNode> Locals { get; set; } = new List<VariableDeclarationNode>();

        public FunctionNode(string name, BlockNode block, string type, ArgNode args)
        {
            this.Name = name;
            this.Block = block;
            this.Type = type;
            Args = args;
        }
    }
}
