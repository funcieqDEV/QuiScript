using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class ClassNode : Node
    {
        public string Name;
        public FunctionNode Constructor;
        public List<FunctionNode> Methods = [];

        public ClassNode(string name, FunctionNode c, List<FunctionNode> m)
        {
            this.Name = name;
            this.Constructor = c;
            this.Methods = m;
        }
    }
}
