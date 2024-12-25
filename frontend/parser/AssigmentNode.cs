using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class AssigmentNode : Node
    {
        public string Name;
        public Node Expr;

        public AssigmentNode(string name, Node expr)
        {
            Name = name;
            Expr = expr;
        }
    }
}
