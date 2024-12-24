using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend.parser
{
    public class VariableDeclarationNode : Node
    {
        public string Type;
        public string Name;
        public Node Expr;
        public VariableDeclarationNode(string t,string n, Node ex) {
            this.Type = t;
            this.Name = n;
            this.Expr = ex;
        }
    }
}
