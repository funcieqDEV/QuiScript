using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public  class BlockNode : Node
    {
        public List<Node> Statements = new List<Node>();
        public BlockNode(List<Node> stmts) {
            this.Statements = stmts;
        }
    }
}
