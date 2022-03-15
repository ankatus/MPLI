namespace MPLI;

public class Evaluator
{
    private readonly ProgramState _programState = new();
    private readonly AstProgram _program;

    public Evaluator(AstProgram program)
    {
        _program = program;
    }

    public void Evaluate()
    {
        RunProgram();
    }

    private void RunProgram()
    {
        foreach (var statement in _program.Statements)
        {
            ExecuteStatement(statement);
        }
    }

    #region Statements

    private void ExecuteStatement(AstStatement statement)
    {
        switch (statement)
        {
            case AstDeclarationAndAssignment node:
                ExecuteDeclarationAndAssignment(node);
                break;
            case AstDeclaration node:
                ExecuteDeclaration(node);
                break;
            case AstAssignment node:
                ExecuteAssignment(node);
                break;
            case AstFor node:
                ExecuteFor(node);
                break;
            case AstRead node:
                ExecuteRead(node);
                break;
            case AstPrint node:
                ExecutePrint(node);
                break;
            case AstAssert node:
                ExecuteAssert(node);
                break;
            default:
                throw new ArgumentException("Unknown Statement type in parse tree.", nameof(statement));
        }
    }

    private void ExecuteDeclaration(AstDeclaration node)
    {
        if (_programState.Variables.ContainsKey(node.VarName))
            throw new EvaluationException($"At {node.StartToken.Line}: Re-declaration of variable \"{node.VarName}\"");

        _programState.Variables[node.VarName] = node.Type switch
        {
            AstType.STRING => "",
            AstType.INT => 0,
            AstType.BOOL => false,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private void ExecuteDeclarationAndAssignment(AstDeclarationAndAssignment node)
    {
        ExecuteDeclaration(node);

        var value = EvaluateExpression(node.Expression);

        AssignVariable(node.VarName, value);
    }

    private void ExecuteAssignment(AstAssignment node)
    {
        if (!VariableExists(node.VarIdent.Name))
            throw new InvalidOperationException();

        var value = EvaluateExpression(node.Expression);

        AssignVariable(node.VarIdent.Name, value);
    }

    private void ExecuteFor(AstFor node)
    {
        var identifier = node.VarIdent.Name;

        if (!VariableExists(identifier))
            throw new InvalidOperationException();

        var startObject = EvaluateExpression(node.StartExpression);
        if (startObject is not int startValue)
            throw new InvalidOperationException();

        var endObject = EvaluateExpression(node.EndExpression);
        if (endObject is not int endValue)
            throw new InvalidOperationException();

        var i = startValue;
        AssignVariable(identifier, i);

        if (startValue <= endValue)
        {
            while (i <= endValue)
            {
                node.Statements.ForEach(ExecuteStatement);
                i++;
                AssignVariable(identifier, i);
            }    
        }
        else
        {            
            while (i >= endValue)
            {
                node.Statements.ForEach(ExecuteStatement);
                i--;
                AssignVariable(identifier, i);
            }
        }
        
    }

    private void ExecuteRead(AstRead node)
    {
        var varIdent = node.VarIdent;
        var varName = node.VarIdent.Name;

        if (!VariableExists(varName))
            throw new InvalidOperationException();

        var input = Console.ReadLine() ?? throw new InvalidOperationException();

        switch (varIdent.Type)
        {
            case AstType.INT:
                if (!int.TryParse(input, out var asInt))
                    throw new EvaluationException(
                        $"At line {node.StartToken.Line}: could not parse read input to type \"int\"");
                AssignVariable(varName, asInt);
                break;
            case AstType.STRING:
                AssignVariable(varName, input);
                break;
            case AstType.BOOL:
                if (!bool.TryParse(input, out var asBool))
                    throw new EvaluationException(
                        $"At line {node.StartToken.Line}: could not parse input to type \"bool\"");
                AssignVariable(varName, asBool);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ExecutePrint(AstPrint node)
    {
        var value = EvaluateExpression(node.Expression);
        
        if (value is string stringValue)
        {
            Console.Write(stringValue.Replace(@"\n", Environment.NewLine));
        }
        else
        {
            Console.Write(value);
        }
    }
        

    private void ExecuteAssert(AstAssert node)
    {
        var exprValue = EvaluateExpression(node.Expression);
        if (exprValue is not bool boolValue)
            throw new InvalidOperationException();

        if (!boolValue)
        {
            var message = $"Assert on line {node.StartToken.Line} failed.";
            throw new AssertionFailException(message);
        }
    }

    #endregion

    #region Expressions

    private object EvaluateExpression(AstExpression expression)
    {
        return expression switch
        {
            AstBinaryExpression node => EvaluateBinaryExpression(node),
            AstUnaryExpression node => EvaluateUnaryExpression(node),
            AstInt node => EvaluateIntExpression(node),
            AstString node => EvaluateStringExpression(node),
            AstVarIdent node => EvaluateVarIdentExpression(node),
            AstBool node => EvaluateBoolExpression(node),
            _ => throw new InvalidOperationException(),
        };
    }

    private static object EvaluateIntExpression(AstInt node)
    {
        return node.Value;
    }

    private static object EvaluateStringExpression(AstString node)
    {
        return node.Value;
    }

    private object EvaluateVarIdentExpression(AstVarIdent node)
    {
        return _programState.Variables[node.Name];
    }

    private static object EvaluateBoolExpression(AstBool node)
    {
        return node.Value;
    }

    private object EvaluateBinaryExpression(AstBinaryExpression node)
    {
        var result = EvaluateExpression(node.Operands[0]);
        for (var i = 1; i < node.Operands.Count; i++)
        {
            var left = result;
            var right = EvaluateExpression(node.Operands[i]);
            
            result = node switch
            {
                {Operator: AstOperator.ADD} => left switch
                {
                    string leftString => leftString + (string) right,
                    int leftInt => leftInt + (int) right,
                    _ => throw new InvalidOperationException(),
                },
                {Operator: AstOperator.SUB} => left switch
                {
                    int leftInt => leftInt - (int) right,
                    _ => throw new InvalidOperationException(),
                },
                {Operator: AstOperator.MUL} => left switch
                {
                    int leftInt => leftInt * (int) right,
                    _ => throw new InvalidOperationException(),
                },
                {Operator: AstOperator.DIV} => left switch
                {
                    int leftInt => leftInt / (int) right,
                    _ => throw new InvalidOperationException(),
                },
                {Operator: AstOperator.LESS} => left switch
                {
                    int leftInt => leftInt < (int) right,
                    _ => throw new InvalidOperationException(),
                },
                {Operator: AstOperator.EQUAL} => left switch
                {
                    string leftString => leftString == (string) right,
                    int leftInt => leftInt == (int) right,
                    bool leftBool => leftBool == (bool) right,
                    _ => throw new InvalidOperationException(),
                },
                {Operator: AstOperator.AND} => left switch
                {
                    bool leftBool => leftBool && (bool) right,
                    _ => throw new InvalidOperationException(),
                },
                _ => throw new InvalidOperationException(),
            };   
        }

        return result;
    }

    private object EvaluateUnaryExpression(AstUnaryExpression node)
    {
        var operand = EvaluateExpression(node.Operand);

        if (operand is not bool boolOperand)
            throw new InvalidOperationException();
        
        return node.Operator switch
        {
            AstOperator.NOT => !boolOperand,
            _ => throw new InvalidOperationException(),
        };
    }
    
    #endregion

    private bool VariableExists(string identifier)
    {
        return _programState.Variables.ContainsKey(identifier);
    }

    private void AssignVariable(string identifier, object value)
    {
        if (!VariableExists(identifier))
            throw new InvalidOperationException();

        if (_programState.Variables[identifier].GetType() != value.GetType())
            throw new InvalidOperationException();

        _programState.Variables[identifier] = value;
    }
}

public class ProgramState
{
    public Dictionary<string, object> Variables { get; } = new();
}

public class EvaluationException : Exception
{
    public EvaluationException(string message) : base(message)
    {
        
    }
}

public class AssertionFailException : Exception
{
    public AssertionFailException(string message) : base(message)
    {
        
    }
}