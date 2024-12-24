using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend.parser
{
    public class StringLiteralNode : Node
    {
        public string tok;
        public StringLiteralNode(string t) {
            this.tok = t;
        }
    }
}
