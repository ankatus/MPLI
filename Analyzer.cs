namespace MPLI;

public class Analyzer
{
    private class DeclaredVariable
    {
        public DeclaredVariable(string identifier, AstType type)
        {
            Identifier = identifier;
            Type = type;
        }

        public string Identifier { get; }
        public AstType Type { get; }
        public bool Initialized { get; set; }
    }

    private readonly Dictionary<string, DeclaredVariable> _variables = new();
    private readonly ParseTreeNode _parseTree;

    public Analyzer(ParseTreeNode parseTree)
    {
        _parseTree = parseTree;
    }

    public AstProgram Analyze()
    {
        return Program(_parseTree);
    }

    private AstProgram Program(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        // Get all statement-children (disregarding semicolons).
        var statements =
            branch.Children.OfType<ParseTreeBranch>()
                .Where(child => child.Type is NonTerminal.STATEMENT)
                .Select(Statement)
                .ToList();

        return new AstProgram(statements);
    }

    private AstStatement Statement(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        if (branch.Children[0] is not ParseTreeLeaf leaf)
            throw new InvalidOperationException();

        return leaf.Token switch
        {
            {Type: TokenType.KEYWORD, Lexeme: "var"} => Declaration(node),
            {Type: TokenType.KEYWORD, Lexeme: "for"} => For(node),
            {Type: TokenType.KEYWORD, Lexeme: "read"} => Read(node),
            {Type: TokenType.KEYWORD, Lexeme: "print"} => Print(node),
            {Type: TokenType.KEYWORD, Lexeme: "assert"} => Assert(node),
            {Type: TokenType.IDENTIFIER} => Assignment(node),
            _ => throw new InvalidOperationException(),
        };
    }

    private AstDeclaration Declaration(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        // Use the amount of children this node has to determine
        // whether it is a declaration or a declaration and an assignment.
        // Maybe I should have encoded such things in the parse tree?
        switch (branch.Children.Count)
        {
            case 6:
                return DeclarationAndAssignment(node);
            case 4:
            {
                if (branch.Children[1] is not ParseTreeLeaf identifierLeaf)
                    throw new InvalidOperationException();

                var varName = identifierLeaf.Token.Lexeme;
                var type = Type(branch.Children[3]);

                if (_variables.ContainsKey(varName))
                    throw new SemanticException(
                        $"At {identifierLeaf.Token.PosString}: Variable name \"{varName}\" already in use.");

                _variables.Add(varName,
                    new DeclaredVariable(varName, type));

                var startToken = ((ParseTreeLeaf) branch.Children[0]).Token;
                
                return new AstDeclaration(varName, type, startToken);
            }
            default:
                throw new InvalidOperationException();
        }
    }

    private AstDeclarationAndAssignment DeclarationAndAssignment(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        if (branch.Children[1] is not ParseTreeLeaf identifierLeaf)
            throw new InvalidOperationException();

        var varName = identifierLeaf.Token.Lexeme;
        var variableType = Type(branch.Children[3]);
        var expression = Expression(branch.Children[5]);

        if (variableType != expression.Type)
            throw new SemanticException(
                $"At {identifierLeaf.Token.PosString}: Can't assign value of type \"{GetAstTypeString(expression.Type)}\" to variable of type \"{GetAstTypeString(variableType)}\".");

        if (_variables.ContainsKey(varName))
            throw new SemanticException(
                $"At {identifierLeaf.Token.PosString}: Variable name \"{varName}\" already in use.");

        var variable = new DeclaredVariable(varName, variableType)
        {
            Initialized = true,
        };

        _variables.Add(variable.Identifier, variable);

        var startToken = ((ParseTreeLeaf) branch.Children[0]).Token;
        
        return new AstDeclarationAndAssignment(varName, variableType, expression, startToken);
    }

    private AstAssignment Assignment(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        var varIdent = VarIdent(branch.Children[0]);

        var expression = Expression(branch.Children[2]);

        if (expression.Type != varIdent.Type)
            throw new SemanticException(
                $"At {expression.StartToken.PosString}: Can't assign value of type \"{GetAstTypeString(expression.Type)}\" to variable of type \"{GetAstTypeString(varIdent.Type)}\".");
        
        _variables[varIdent.Name].Initialized = true;

        return new AstAssignment(varIdent, expression);
    }

    private AstFor For(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        var varIdent = VarIdent(branch.Children[1]);

        if (varIdent.Type is not AstType.INT)
            throw new SemanticException(
                $"At {varIdent.StartToken.PosString}: expected a variable of type \"int\", not \"{GetAstTypeString(varIdent.Type)}\".");

        // The variable incremented during a for-loop will be initialized, obviously
        _variables[varIdent.Name].Initialized = true;

        var startExpr = Expression(branch.Children[3]);
        if (startExpr.Type is not AstType.INT)
            throw new SemanticException(
                $"Expression starting at {startExpr.StartToken.PosString}: Expression type should be \"int\" for start-expressions in for-loops.");

        var endExpr = Expression(branch.Children[5]);
        if (endExpr.Type is not AstType.INT)
            throw new SemanticException(
                $"Expression starting at {endExpr.StartToken.PosString}: Expression type should be \"int\" for end-expressions in for-loops.");

        // Build statements
        var statements = branch.Children
            .OfType<ParseTreeBranch>()
            .Where(child => child.Type is NonTerminal.STATEMENT)
            .Select(Statement)
            .ToList();

        return new AstFor(varIdent, startExpr, endExpr, statements);
    }

    private AstRead Read(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        var varIdent = VarIdent(branch.Children[1]);
        _variables[varIdent.Name].Initialized = true;
        var startToken = ((ParseTreeLeaf) branch.Children[0]).Token;

        return new AstRead(varIdent, startToken);
    }

    private AstPrint Print(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        return new AstPrint(Expression(branch.Children[1]));
    }

    private AstAssert Assert(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        var startToken = ((ParseTreeLeaf) branch.Children[0]).Token;

        var expression = Expression(branch.Children[2]);

        // Can only assert a boolean expression
        if (expression.Type is not AstType.BOOL)
            throw new SemanticException($"Expected expression of type \"bool\" for assert on line {startToken.Line}.");

        return new AstAssert(expression, startToken);
    }

    private AstExpression Expression(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        // Recursively build expression tree
        switch (branch)
        {
            case {Type: NonTerminal.EXPRESSION_0}:
                return Expression0(branch);
            case {Type: NonTerminal.EXPRESSION_1}:
                return branch.Children.Count > 1 ? UnaryExpression(branch) : Expression(branch.Children[0]);
            case {Type: NonTerminal.EXPRESSION_2}:
            case {Type: NonTerminal.EXPRESSION_3}:
            case {Type: NonTerminal.EXPRESSION_4}:
            case {Type: NonTerminal.EXPRESSION_5}:
            case {Type: NonTerminal.EXPRESSION_6}:
                return branch.Children.Count > 1 ? BinaryExpression(branch) : Expression(branch.Children[0]);
            case {Type: NonTerminal.EXPRESSION}:
                return Expression(branch.Children[0]);
            default:
                throw new InvalidOperationException();
        }
    }

    private AstExpression Expression0(ParseTreeBranch branch)
    {
        if (branch.Children[0] is not ParseTreeLeaf leaf)
            throw new InvalidOperationException();

        switch (leaf.Token)
        {
            case {Type: TokenType.NUMBER}:
                if (!int.TryParse(leaf.Token.Lexeme, out var parsedInt))
                    throw new InvalidOperationException();
                return new AstInt(parsedInt, leaf.Token);

            case {Type: TokenType.STRING}:
                return new AstString(leaf.Token.Lexeme, leaf.Token);

            case {Type: TokenType.IDENTIFIER}:
                var variable = VarIdent(leaf);
                // If a VarIdent is used as an expression, it needs to have a value.
                if (!_variables[variable.Name].Initialized)
                    throw new SemanticException(
                        $"At {leaf.Token.PosString}: Variable \"{variable.Name}\" read before initialized.");
                return VarIdent(leaf);

            case {Type: TokenType.BOOL}:
                if (!bool.TryParse(leaf.Token.Lexeme, out var parsedBool))
                    throw new InvalidOperationException();
                return new AstBool(parsedBool, leaf.Token);

            case {Type: TokenType.OPEN_PARENS}:
                return Expression(branch.Children[1]);

            default:
                throw new InvalidOperationException();
        }
    }

    private AstBinaryExpression BinaryExpression(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        var op = Operator(branch.Children[1]);

        // Get all expression-type non-terminals and build AstExpressions out of them
        var operands = branch.Children
            .OfType<ParseTreeBranch>()
            .Where(child => child.Type is
                NonTerminal.EXPRESSION_5
                or NonTerminal.EXPRESSION_4
                or NonTerminal.EXPRESSION_3
                or NonTerminal.EXPRESSION_2
                or NonTerminal.EXPRESSION_1)
            .Select(Expression)
            .ToList();

        var operandType = operands.First().Type;

        // Get a list of allowed operand types based on operator
        var allowedTypes = op switch
        {
            AstOperator.ADD => new List<AstType> {AstType.INT, AstType.STRING},
            AstOperator.SUB
                or AstOperator.MUL
                or AstOperator.DIV
                or AstOperator.LESS => new List<AstType> {AstType.INT},
            AstOperator.EQUAL => new List<AstType> {AstType.INT, AstType.STRING, AstType.BOOL},
            AstOperator.AND => new List<AstType> {AstType.BOOL},
            _ => throw new InvalidOperationException(),
        };

        // Check if operands are not all of the same type
        var diffTypes = operands.DistinctBy(x => x.Type).ToList();
        if (diffTypes.Count > 1)
        {
            var errorMessage = $"Multiple operand types used in one expression:{Environment.NewLine}";
            errorMessage = diffTypes.Aggregate(errorMessage,
                (current, expr) =>
                    current +
                    $"\tExpression operand starting at {expr.StartToken.PosString} is of type \"{GetAstTypeString(expr.Type)}\".{Environment.NewLine}");
            throw new SemanticException(errorMessage);
        }

        // Check that operand type is allowed for operator
        if (!allowedTypes.Contains(operandType))
        {
            var expr = operands.First();
            throw new SemanticException(
                $"Expression type \"{GetAstTypeString(expr.Type)}\" at {expr.StartToken.PosString} is incorrect for operator \"{GetAstOperatorString(op)}\".");
        }

        // Determine type of expression based on operator
        var exprType = op switch
        {
            AstOperator.EQUAL or AstOperator.LESS => AstType.BOOL,
            _ => operandType,
        };

        return new AstBinaryExpression(operands, op, exprType, operands.First().StartToken);
    }

    private AstUnaryExpression UnaryExpression(ParseTreeBranch branch)
    {
        var op = Operator(branch.Children[0]);
        var operand = Expression(branch.Children[1]);
        var startToken = ((ParseTreeLeaf) branch.Children[0]).Token;
        return new AstUnaryExpression(operand, op, operand.Type, startToken);
    }

    private AstVarIdent VarIdent(ParseTreeNode node)
    {
        if (node is not ParseTreeLeaf leaf)
            throw new InvalidOperationException();

        // Each VarIdent in the AST is either a write or a read of a variable
        // so need to make sure that such a variable exists.
        if (!_variables.TryGetValue(leaf.Token.Lexeme, out var variable))
            throw new SemanticException($"At {leaf.Token.PosString}: Reference to undeclared variable.");

        return new AstVarIdent(variable.Identifier, variable.Type, leaf.Token);
    }

    private static AstType Type(ParseTreeNode node)
    {
        if (node is not ParseTreeBranch branch)
            throw new InvalidOperationException();

        if (branch.Children[0] is not ParseTreeLeaf leaf)
            throw new InvalidOperationException();

        return leaf.Token switch
        {
            {Type: TokenType.KEYWORD, Lexeme: "int"} => AstType.INT,
            {Type: TokenType.KEYWORD, Lexeme: "string"} => AstType.STRING,
            {Type: TokenType.KEYWORD, Lexeme: "bool"} => AstType.BOOL,
            _ => throw new InvalidOperationException(),
        };
    }

    private static AstOperator Operator(ParseTreeNode node)
    {
        if (node is not ParseTreeLeaf leaf)
            throw new InvalidOperationException();

        return leaf.Token switch
        {
            {Type: TokenType.BINARY_OPERATOR, Lexeme: "+"} => AstOperator.ADD,
            {Type: TokenType.BINARY_OPERATOR, Lexeme: "-"} => AstOperator.SUB,
            {Type: TokenType.BINARY_OPERATOR, Lexeme: "*"} => AstOperator.MUL,
            {Type: TokenType.BINARY_OPERATOR, Lexeme: "/"} => AstOperator.DIV,
            {Type: TokenType.BINARY_OPERATOR, Lexeme: "<"} => AstOperator.LESS,
            {Type: TokenType.BINARY_OPERATOR, Lexeme: "="} => AstOperator.EQUAL,
            {Type: TokenType.BINARY_OPERATOR, Lexeme: "&"} => AstOperator.AND,
            {Type: TokenType.UNARY_OPERATOR, Lexeme: "!"} => AstOperator.NOT,
            _ => throw new InvalidOperationException(),
        };
    }

    private static string GetAstOperatorString(AstOperator op)
    {
        return op switch
        {
            AstOperator.ADD => "+",
            AstOperator.SUB => "-",
            AstOperator.MUL => "*",
            AstOperator.DIV => "/",
            AstOperator.LESS => "<",
            AstOperator.EQUAL => "=",
            AstOperator.AND => "&",
            AstOperator.NOT => "!",
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
        };
    }

    private static string GetAstTypeString(AstType type)
    {
        return type switch
        {
            AstType.INT => "int",
            AstType.STRING => "string",
            AstType.BOOL => "bool",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }
}

public class SemanticException : Exception
{
    public SemanticException(string message) : base(message)
    {
    }
}