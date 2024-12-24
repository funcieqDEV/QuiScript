using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class BinaryExpressionNode : Node
    {
        public Node Left { get; }
        public Token Operator { get; }
        public Node Right { get; }

        public BinaryExpressionNode(Node left, Token operatorToken, Node right)
        {
            Left = left;
            Operator = operatorToken;
            Right = right;
        }
    }
}
