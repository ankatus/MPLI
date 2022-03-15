namespace MPLI;

public enum NonTerminal
{
    PROGRAM,
    STATEMENT,
    EXPRESSION,
    EXPRESSION_6,
    EXPRESSION_5,
    EXPRESSION_4,
    EXPRESSION_3,
    EXPRESSION_2,
    EXPRESSION_1,
    EXPRESSION_0,
    TYPE,
}

public abstract class ParseTreeNode
{
}

public class ParseTreeBranch : ParseTreeNode
{
    public ParseTreeBranch(NonTerminal type, List<ParseTreeNode> children)
    {
        Type = type;
        Children = children;
    }

    public NonTerminal Type { get; }
    public List<ParseTreeNode> Children { get; }
}

public class ParseTreeLeaf : ParseTreeNode
{
    public ParseTreeLeaf(Token token)
    {
        Token = token;
    }

    public Token Token { get; }
}