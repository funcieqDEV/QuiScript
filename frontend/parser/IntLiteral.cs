using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class IntLiteral : Node
    {
        public Token tok;

        public IntLiteral(Token tok)
        {
            this.tok = tok;
        }
    }
}
