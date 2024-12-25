using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class ReturnNode : Node
    {
        public Node Expr;

        public ReturnNode(Node expr) {
            this.Expr = expr;
        }
    }
}
