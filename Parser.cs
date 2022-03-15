namespace MPLI;

public class Parser
{
    private readonly List<Token> _tokens;
    private int _nextTokenIndex;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public ParseTreeNode Parse()
    {
        return ParseProgram();
    }

    private ParseTreeNode ParseProgram()
    {
        var children = new List<ParseTreeNode>();

        do
        {
            children.Add(ParseStatement());
            children.Add(new ParseTreeLeaf(Consume(TokenType.SEMICOLON)));
        } while (NextToken.Type != TokenType.EOF);

        return new ParseTreeBranch(NonTerminal.PROGRAM, children);
    }

    private ParseTreeNode ParseStatement()
    {
        return NextToken switch
        {
            {Type: TokenType.IDENTIFIER} => ParseAssignment(),
            {Type: TokenType.KEYWORD, Lexeme: "var"} => ParseDeclaration(),
            {Type: TokenType.KEYWORD, Lexeme: "for"} => ParseFor(),
            {Type: TokenType.KEYWORD, Lexeme: "read"} => ParseRead(),
            {Type: TokenType.KEYWORD, Lexeme: "print"} => ParsePrint(),
            {Type: TokenType.KEYWORD, Lexeme: "assert"} => ParseAssert(),
            _ => throw new ParseException($"At {NextToken.PosString}: Expected start of statement."),
        };
    }

    private ParseTreeNode ParseAssert()
    {
        var children = new List<ParseTreeNode>();

        children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "assert")));
        children.Add(new ParseTreeLeaf(Consume(TokenType.OPEN_PARENS)));
        children.Add(ParseExpression());
        children.Add(new ParseTreeLeaf(Consume(TokenType.CLOSE_PARENS)));

        return new ParseTreeBranch(NonTerminal.STATEMENT, children);
    }

    private ParseTreeNode ParsePrint()
    {
        var children = new List<ParseTreeNode>();

        children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "print")));
        children.Add(ParseExpression());

        return new ParseTreeBranch(NonTerminal.STATEMENT, children);
    }

    private ParseTreeNode ParseRead()
    {
        var children = new List<ParseTreeNode>();

        children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "read")));
        children.Add(new ParseTreeLeaf(Consume(TokenType.IDENTIFIER)));

        return new ParseTreeBranch(NonTerminal.STATEMENT, children);
    }

    private ParseTreeNode ParseFor()
    {
        var children = new List<ParseTreeNode>();

        children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "for")));
        children.Add(new ParseTreeLeaf(Consume(TokenType.IDENTIFIER)));
        children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "in")));
        children.Add(ParseExpression());
        children.Add(new ParseTreeLeaf(Consume(TokenType.RANGE)));
        children.Add(ParseExpression());
        children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "do")));
        children.Add(ParseStatement());
        children.Add(new ParseTreeLeaf(Consume(TokenType.SEMICOLON)));
        while (!(NextToken.Type == TokenType.KEYWORD && NextToken.Lexeme == "end"))
        {
            children.Add(ParseStatement());
            children.Add(new ParseTreeLeaf(Consume(TokenType.SEMICOLON)));
        }

        children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "end")));
        children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "for")));

        return new ParseTreeBranch(NonTerminal.STATEMENT, children);
    }

    private ParseTreeNode ParseDeclaration()
    {
        var children = new List<ParseTreeNode>();

        children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "var")));
        children.Add(new ParseTreeLeaf(Consume(TokenType.IDENTIFIER)));
        children.Add(new ParseTreeLeaf(Consume(TokenType.COLON)));
        children.Add(ParseType());

        if (NextToken.Type == TokenType.ASSIGN)
        {
            children.Add(new ParseTreeLeaf(Consume(TokenType.ASSIGN)));
            children.Add(ParseExpression());
        }

        return new ParseTreeBranch(NonTerminal.STATEMENT, children);
    }

    private ParseTreeNode ParseAssignment()
    {
        var children = new List<ParseTreeNode>();

        children.Add(new ParseTreeLeaf(Consume(TokenType.IDENTIFIER)));
        children.Add(new ParseTreeLeaf(Consume(TokenType.ASSIGN)));
        children.Add(ParseExpression());

        return new ParseTreeBranch(NonTerminal.STATEMENT, children);
    }

    private ParseTreeNode ParseExpression()
    {
        var children = new List<ParseTreeNode>
        {
            ParseExpression6(),
        };
        return new ParseTreeBranch(NonTerminal.EXPRESSION, children);
    }

    private ParseTreeNode ParseExpression6()
    {
        var children = new List<ParseTreeNode>();
        children.Add(ParseExpression5());

        while (NextToken.Lexeme is "&")
        {
            children.Add(new ParseTreeLeaf(Consume()));
            children.Add(ParseExpression5());
        }

        return new ParseTreeBranch(NonTerminal.EXPRESSION_6, children);
    }

    private ParseTreeNode ParseExpression5()
    {
        var children = new List<ParseTreeNode>();
        children.Add(ParseExpression4());

        while (NextToken.Lexeme is "=")
        {
            children.Add(new ParseTreeLeaf(Consume()));
            children.Add(ParseExpression4());
        }

        return new ParseTreeBranch(NonTerminal.EXPRESSION_5, children);
    }

    private ParseTreeNode ParseExpression4()
    {
        var children = new List<ParseTreeNode>();
        children.Add(ParseExpression3());

        while (NextToken.Lexeme is "<")
        {
            children.Add(new ParseTreeLeaf(Consume()));
            children.Add(ParseExpression3());
        }

        return new ParseTreeBranch(NonTerminal.EXPRESSION_4, children);
    }

    private ParseTreeNode ParseExpression3()
    {
        var children = new List<ParseTreeNode>();
        children.Add(ParseExpression2());

        while (NextToken.Lexeme is "+" or "-")
        {
            children.Add(new ParseTreeLeaf(Consume()));
            children.Add(ParseExpression2());
        }

        return new ParseTreeBranch(NonTerminal.EXPRESSION_3, children);
    }

    private ParseTreeNode ParseExpression2()
    {
        var children = new List<ParseTreeNode>();
        children.Add(ParseExpression1());

        while (NextToken.Lexeme is "*" or "/")
        {
            children.Add(new ParseTreeLeaf(Consume()));
            children.Add(ParseExpression1());
        }

        return new ParseTreeBranch(NonTerminal.EXPRESSION_2, children);
    }

    private ParseTreeNode ParseExpression1()
    {
        var children = new List<ParseTreeNode>();

        switch (NextToken)
        {
            case {Lexeme: "!"}:
                children.Add(new ParseTreeLeaf(Consume()));
                children.Add(ParseExpression1());
                break;
            default:
                children.Add(ParseExpression0());
                break;
        }

        return new ParseTreeBranch(NonTerminal.EXPRESSION_1, children);
    }

    private ParseTreeNode ParseExpression0()
    {
        var children = new List<ParseTreeNode>();
        switch (NextToken)
        {
            case {Type: TokenType.NUMBER}:
            case {Type: TokenType.STRING}:
            case {Type: TokenType.IDENTIFIER}:
            case {Type: TokenType.BOOL}:
                children.Add(new ParseTreeLeaf(Consume()));
                break;
            case {Type: TokenType.OPEN_PARENS}:
                children.Add(new ParseTreeLeaf(Consume()));
                children.Add(ParseExpression());
                children.Add(new ParseTreeLeaf(Consume(TokenType.CLOSE_PARENS)));
                break;
            default:
                throw new ParseException($"At {NextToken.PosString}: Unexpected token \"{NextToken.Lexeme}\".");
        }

        return new ParseTreeBranch(NonTerminal.EXPRESSION_0, children);
    }

    private ParseTreeNode ParseType()
    {
        var children = new List<ParseTreeNode>();
        switch (NextToken)
        {
            case {Type: TokenType.KEYWORD, Lexeme: "int"}:
                children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "int")));
                break;
            case {Type: TokenType.KEYWORD, Lexeme: "string"}:
                children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "string")));
                break;
            case {Type: TokenType.KEYWORD, Lexeme: "bool"}:
                children.Add(new ParseTreeLeaf(Consume(TokenType.KEYWORD, "bool")));
                break;
            default:
                throw new ParseException($"At {NextToken.PosString}: Unknown type \"{NextToken.Lexeme}\".");
        }

        return new ParseTreeBranch(NonTerminal.TYPE, children);
    }

    private Token NextToken => _tokens[_nextTokenIndex];

    private Token Consume()
    {
        return _tokens[_nextTokenIndex++];
    }

    private Token Consume(params TokenType[] types)
    {
        if (!types.Contains(NextToken.Type))
            throw new ParseException($"At {NextToken.PosString}: Unexpected token \"{NextToken.Lexeme}\".");

        return _tokens[_nextTokenIndex++];
    }

    private Token Consume(string lexeme)
    {
        if (NextToken.Lexeme != lexeme)
            throw new ParseException($"At {NextToken.PosString}: Unexpected token \"{NextToken.Lexeme}\".");

        return _tokens[_nextTokenIndex++];
    }

    private Token Consume(TokenType type, string lexeme)
    {
        if (NextToken.Type != type)
            throw new ParseException($"At {NextToken.PosString}: Unexpected token \"{NextToken.Lexeme}\".");

        if (NextToken.Lexeme != lexeme)
            throw new ParseException($"At {NextToken.PosString}: Unexpected token \"{NextToken.Lexeme}\".");

        return _tokens[_nextTokenIndex++];
    }
}

public class ParseException : Exception
{
    public ParseException()
    {
        
    }

    public ParseException(string message) : base(message)
    {
        
    }

    public ParseException(string message, Exception innerException) : base(message, innerException)
    {
        
    }
}