using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public abstract class Node
    {
    }

    public class Root : Node
    {
        public List<Node> Statements { get; }
        public Root() => Statements = [];
    }
}
