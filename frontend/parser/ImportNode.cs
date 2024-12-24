using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class ImportNode : Node
    {
        public string Name;
        public ImportNode(string name)
        {
            Name = name;
        }
    }
}
