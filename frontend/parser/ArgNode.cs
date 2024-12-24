using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class ArgNode : Node
    {
       public List<(string, string)> args;
       
        public ArgNode(List<(string, string)> args)
        {
            this.args = args;
        }
    }
}
