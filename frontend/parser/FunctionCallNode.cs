using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend.parser
{
    public class FunctionCallNode : Node
    {
        public string Name;
        public List<Node> args;
        public FunctionCallNode(string name, List<Node> args) {
            this.Name = name;
            this.args = args;
        }
    }
}
