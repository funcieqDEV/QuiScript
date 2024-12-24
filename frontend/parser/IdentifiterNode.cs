using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend.parser
{
    public class IdentifiterNode : Node
    {
        public string Name;

        public IdentifiterNode(string name)
        {
            Name = name;
        }
    }
}
