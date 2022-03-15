namespace MPLI;

public abstract class AstExpression
{
    protected AstExpression(AstType type, Token startToken)
    {
        Type = type;
        StartToken = startToken;
    }

    public Token StartToken { get; }
    public AstType Type { get; }
}

public class AstBinaryExpression : AstExpression
{
    public AstBinaryExpression(List<AstExpression> operands, AstOperator @operator, AstType type, Token startToken) : base(type, startToken)
    {
        Operands = operands;
        Operator = @operator;
    }

    public List<AstExpression> Operands { get; }
    public AstOperator Operator { get; }
}

public class AstUnaryExpression : AstExpression
{
    public AstUnaryExpression(AstExpression operand, AstOperator @operator, AstType type, Token startToken) : base(type, startToken)
    {
        Operand = operand;
        Operator = @operator;
    }

    public AstExpression Operand { get; }
    public AstOperator Operator { get; }
}

public class AstInt : AstExpression
{
    public AstInt(int value, Token startToken) : base(AstType.INT, startToken)
    {
        Value = value;
    }

    public int Value { get; }
}

public class AstString : AstExpression
{
    public AstString(string value, Token startToken) : base(AstType.STRING, startToken)
    {
        Value = value;
    }

    public string Value { get; }
}

public class AstVarIdent : AstExpression
{
    public AstVarIdent(string name, AstType type, Token startToken) : base(type, startToken)
    {
        Name = name;
    }

    public string Name { get; }
}

public class AstBool : AstExpression
{
    public AstBool(bool value, Token startToken) : base(AstType.BOOL, startToken)
    {
        Value = value;
    }

    public bool Value { get; }
}

public enum AstType
{
    INT,
    STRING,
    BOOL,
}

public enum AstOperator
{
    ADD,
    SUB,
    MUL,
    DIV,
    LESS,
    EQUAL,
    AND,
    NOT,
}

public class AstProgram
{
    public AstProgram(List<AstStatement> statements)
    {
        Statements = statements;
    }

    public List<AstStatement> Statements { get; }
}

public abstract class AstStatement
{
}

public class AstDeclaration : AstStatement
{
    public AstDeclaration(string varName, AstType type, Token startToken)
    {
        StartToken = startToken;
        VarName = varName;
        Type = type;
    }

    public Token StartToken { get; }
    public string VarName { get; }
    public AstType Type { get; }
}

public class AstDeclarationAndAssignment : AstDeclaration
{
    public AstDeclarationAndAssignment(string varName, AstType type, AstExpression expression, Token startToken) : base(varName,
        type, startToken)
    {
        Expression = expression;
    }

    public AstExpression Expression { get; }
}

public class AstAssignment : AstStatement
{
    public AstAssignment(AstVarIdent varIdent, AstExpression expression)
    {
        VarIdent = varIdent;
        Expression = expression;
    }

    public AstVarIdent VarIdent { get; }
    public AstExpression Expression { get; }
}

public class AstFor : AstStatement
{
    public AstFor(AstVarIdent varIdent, AstExpression startExpression, AstExpression endExpression,
        List<AstStatement> statements)
    {
        VarIdent = varIdent;
        StartExpression = startExpression;
        EndExpression = endExpression;
        Statements = statements;
    }

    public AstVarIdent VarIdent { get; }
    public AstExpression StartExpression { get; }
    public AstExpression EndExpression { get; }
    public List<AstStatement> Statements { get; }
}

public class AstRead : AstStatement
{
    public AstRead(AstVarIdent varIdent, Token startToken)
    {
        StartToken = startToken;
        VarIdent = varIdent;
    }

    public Token StartToken { get; }
    public AstVarIdent VarIdent { get; }
}

public class AstPrint : AstStatement
{
    public AstPrint(AstExpression expression)
    {
        Expression = expression;
    }

    public AstExpression Expression { get; }
}

public class AstAssert : AstStatement
{
    public AstAssert(AstExpression expression, Token startToken)
    {
        StartToken = startToken;
        Expression = expression;
    }

    public Token StartToken { get; }
    public AstExpression Expression { get; }
}
