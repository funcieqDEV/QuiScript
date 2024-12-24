using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class InstanceMethodCall : Node
    {
        public string Parent;
        public string Method;
        public List<Node> args;

        public InstanceMethodCall(string parent, string method, List<Node> args)
        {
            this.Parent = parent;
            this.Method = method;
            this.args = args;
        }
    }
}
