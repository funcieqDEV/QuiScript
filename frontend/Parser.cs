using QuiScript.frontend.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QuiScript.frontend
{
    public class Parser
    {
        public List<Token> tokens;
        private int _position = 0;
        public Parser(List<Token> t) {
            this.tokens = t;
        }

        public Root Parse()
        {
            Root root = new Root();
           
            while (Peek().Type != TokenType.EndOfInput)
            {
                
                root.Statements.Add(ParseStatement());
            }

            return root;
           
        }

        private Node ParseStatement()
        {
            Token current = Peek();

            return current.Type switch
            {
                TokenType.KeywordClass => ParseClassNode(),
                TokenType.KeywordFun => ParseFunctionNode(),
                TokenType.KeywordImport => ParseImportNode(),
                TokenType.Identifier when IsInstanceMethodCall() => ParseInstanceMethodCall(),
                TokenType.Identifier when IsAssigment() => ParseAssigment(),
                TokenType.Identifier => ParseFunctionCallNode(),
                TokenType.KeywordLet => ParseVariableDeclaration(),
                TokenType.KeywordReturn => ParseReturn(),
                _ => throw new Exception("Not implemented yet"),
            };
        }

        private ReturnNode ParseReturn()
        {
            Consume(TokenType.KeywordReturn, "");
            var expr = ParseExpression();
            Consume(TokenType.Semi, "Expected ';'");
            return new ReturnNode(expr);
        }

        private AssigmentNode ParseAssigment()
        {
            Node expr;
            var id = Consume(TokenType.Identifier, "Expected variable name.");
            Consume(TokenType.Assignment, "Expected '='");
            expr = ParseExpression();
            Consume(TokenType.Semi, "Expected ';'");
            return new AssigmentNode(id.Value,expr);
        }
        private VariableDeclarationNode ParseVariableDeclaration()
        {
            Node Expr;
            Consume(TokenType.KeywordLet, "Expected 'let' keyword.");
            var id = Consume(TokenType.Identifier, "Expected variable name.");
            Consume(TokenType.Colon, "Expected ':' ");
            var Type = Consume(TokenType.Identifier, "Expected variable type.");

            
            if (tokens[_position].Type == TokenType.Assignment)
            {
                Consume(TokenType.Assignment, "Expected '='");
                Expr = ParseExpression(); 
            }
            else
            {
                Expr = null;
            }

            Consume(TokenType.Semi, "Expected ';'");
            VariableDeclarationNode dec = new(Type.Value, id.Value, Expr);
            return dec;
        }

        private bool IsInstanceMethodCall()
        {
            
            if (_position + 1 < tokens.Count && tokens[_position + 1].Type == TokenType.Dot)
            {
                return true;
            }
            return false;
        }

        private bool IsAssigment()
        {
            if(_position +1<tokens.Count && tokens[_position+1].Type == TokenType.Assignment)
            {
                return true;
            }
            return false;
        }




        public FunctionCallNode ParseFunctionCallNode() {
            var name = Consume(TokenType.Identifier, "Expected function name.");
            Consume(TokenType.LeftParen, "Expected '(' after function name.");
            var args = ParseFunctionArgs();
            Consume(TokenType.RightParen, "Expected ')' after arguments.");
            Consume(TokenType.Semi, "Expected ';' after ')'");
            return new FunctionCallNode(name.Value,args);
        }

        private Node ParseExpression()
        {
            Node left = ParseTerm(); // Start with terms

            // After a term, we can have instance method calls or other operators.
            while (Peek().Type == TokenType.Operator && (Peek().Value == "+" || Peek().Value == "-"))
            {
                Token operatorToken = NextToken();
                Node right = ParseTerm();
                left = new BinaryExpressionNode(left, operatorToken, right);
            }

            return left;
        }

        private Node ParseTerm()
        {
            Node left = ParseFactor(); // Start with factors

            // Handle multiplication and division operators
            while (Peek().Type == TokenType.Operator && (Peek().Value == "*" || Peek().Value == "/"))
            {
                Token operatorToken = NextToken();
                Node right = ParseFactor();
                left = new BinaryExpressionNode(left, operatorToken, right);
            }

            return left;
        }

        private Node ParseFactor()
        {
            Node node = ParsePrimary(); // Parse primary expressions, which can include function calls or instance method calls

            while (Peek().Type == TokenType.Operator && (Peek().Value == "*" || Peek().Value == "/"))
            {
                Token operatorToken = NextToken();
                Node right = ParsePrimary();
                node = new BinaryExpressionNode(node, operatorToken, right);
            }

            return node;
        }

        private Node ParsePrimary()
        {
            Token current = Peek();

            // Sprawdzamy, czy mamy do czynienia z wywołaniem metody na obiekcie (np. Conv.ToString)
            if (current.Type == TokenType.Identifier && Peek(1).Type == TokenType.Dot)
            {
                return ParseInstanceMethodCall(true); // Obsługuje takie wywołania jak Conv.ToString
            }

            switch (current.Type)
            {
                case TokenType.IntegerLiteral:
                    NextToken();
                    return new IntLiteral(current);
                case TokenType.FloatLiteral:
                case TokenType.StringLiteral:
                    NextToken();
                    return new StringLiteralNode(current.Value);
                case TokenType.Identifier:
                    NextToken();
                    return new IdentifiterNode(current.Value);
                default:
                    throw new Exception($"Unexpected token: {current}");
            }
        }




        public InstanceMethodCall ParseInstanceMethodCall(bool inExpr = false)
        {
            var parent = Consume(TokenType.Identifier, "Expected class name.");
            Consume(TokenType.Dot, "Expected '.' after class name.");
            var child = Consume(TokenType.Identifier, "Expected method name.");
            Consume(TokenType.LeftParen, "Expected '(' after method name.");
            var args = ParseFunctionArgs(); 
            Consume(TokenType.RightParen, "Expected ')' after arguments.");
            if (!inExpr)
            {
                Consume(TokenType.Semi, "Expected ';' after statement");
            }
            

            
            return new InstanceMethodCall(parent.Value, child.Value, args);
        }


        private List<Node> ParseFunctionArgs()
        {
            List<Node> args = new List<Node>();

            if (Peek().Type != TokenType.RightParen)
            {
                
                args.Add(ParseExpression());
            }

            while (Peek().Type != TokenType.RightParen)
            {
                Consume(TokenType.Comma, "Expected ',' between arguments.");
                
                args.Add(ParseExpression());
            }

            return args;
        }

        public ImportNode ParseImportNode()
        {
            Consume(TokenType.KeywordImport, "Expected 'import' keyword.");
            var name = Consume(TokenType.Identifier, "Expected name of imported library.");
            Consume(TokenType.Semi, "Expected ';'");
            return new ImportNode(name.Value);
        }

        public Node ParseFunctionNode()
        {
            Consume(TokenType.KeywordFun, "Expected 'fun' keyword.");
            var id = Consume(TokenType.Identifier, "Expected function name.");
            Consume(TokenType.LeftParen, "Expected '(' after function name.");
            var args = ParseArgNode();
            Consume(TokenType.RightParen, "Expected ')' after arguments.");
            Consume(TokenType.Colon, "Expected ':'");
            var type = Consume(TokenType.Identifier, "Expected return type.");
            var body = ParseBlockNode();
            FunctionNode f = new(id.Value, body, type.Value, args);
            return f;
        }

        public ArgNode ParseArgNode()
        {
            List<(string, string)> args = new List<(string, string)>();
            (string, string) arg;

            
            if (Peek().Type != TokenType.RightParen)
            {
                var Name = Consume(TokenType.Identifier, "Expected argument name.");
                Consume(TokenType.Colon, "Expected ':' ");
                var t = Consume(TokenType.Identifier, "Expected argument type.");
                arg = (Name.Value, t.Value);
                args.Add(arg);
            }

            
            while (Peek().Type != TokenType.RightParen)
            {
                Consume(TokenType.Comma, "Expected ',' between arguments.");

                var Name = Consume(TokenType.Identifier, "Expected argument name.");
                Consume(TokenType.Colon, "Expected ':' ");
                var t = Consume(TokenType.Identifier, "Expected argument type.");
                arg = (Name.Value, t.Value);
                args.Add(arg);
            }

            return new ArgNode(args);
        }

        public Node ParseClassNode()
        {
            Consume(TokenType.KeywordClass, "Expected 'class' keyword.");
            var id = Consume(TokenType.Identifier, "Expected class name.");
            var body = ParseBlockNode();
            List<FunctionNode> methods = new List<FunctionNode>();
            foreach (var method in body.Statements)
            {
                if(method is FunctionNode)
                {
                    methods.Add((FunctionNode)method);
                }
            }
            ClassNode c = new(id.Value, null,methods);
            return c;
        }


        private BlockNode ParseBlockNode()
        {
            List<Node> statements = [];

            Consume(TokenType.LeftBrace, "Expected '{'.");

            while (!IsAtEnd() && Peek().Type != TokenType.RightBrace)
            {
                statements.Add(ParseStatement());
            }

            Consume(TokenType.RightBrace, "Expected '}'.");

            return new BlockNode(statements);
        }

        private Token Consume(TokenType type, string errorMessage)
        {
            if (Peek().Type == type)
            {
                return NextToken();
            }

            throw new Exception(errorMessage + " got " + Peek().Type);
        }

        private Token NextToken()
        {
            if (!IsAtEnd()) _position++;
            return tokens[_position - 1];
        }

        private bool IsAtEnd()
        {
            return _position >= tokens.Count;
        }

        private Token Peek(int offset = 0)
        {
            if (IsAtEnd()) return new Token(TokenType.EndOfInput, "");
            return tokens[_position + offset];
        }

    }
}
