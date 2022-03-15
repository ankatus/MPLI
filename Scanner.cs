namespace MPLI;

public enum TokenType
{
    UNKNOWN,
    OPEN_PARENS,
    CLOSE_PARENS,
    ASSIGN,
    BINARY_OPERATOR,
    UNARY_OPERATOR,
    COLON,
    SEMICOLON,
    IDENTIFIER,
    NUMBER,
    KEYWORD,
    STRING,
    BOOL,
    RANGE,
    EOF,
}

public record Token(TokenType Type, string Lexeme, int Line, int Col)
{
    public static Token Eof => new(TokenType.EOF, "", 0, 0);
    public string PosString => $"line {Line}, col {Col}";
}

public class Scanner
{
    private readonly string[] _keywords =
    {
        "var", "for", "end", "in", "do", "read", "print", "int", "string", "bool", "assert",
    };

    private readonly string _input;
    private readonly List<Token> _tokens = new();
    private int _currentPos = -1;
    private char _currentSymbol = '\0';
    private int _currentLine = 1;
    private int _currentCol;

    public Scanner(string input)
    {
        _input = input;
    }

    public List<Token> Scan()
    {
        while (MoreInputLeft())
        {
            Advance();

            if (char.IsLetter(_currentSymbol))
            {
                BuildIdentifier();
            }
            else if (char.IsNumber(_currentSymbol))
            {
                BuildNumber();
            }
            else
            {
                switch (_currentSymbol)
                {
                    case '(':
                        _tokens.Add(new Token(TokenType.OPEN_PARENS, _currentSymbol.ToString(), _currentLine,
                            _currentCol));
                        break;
                    case ')':
                        _tokens.Add(new Token(TokenType.CLOSE_PARENS, _currentSymbol.ToString(), _currentLine,
                            _currentCol));
                        break;
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '<':
                    case '&':
                    case '=':
                        _tokens.Add(new Token(TokenType.BINARY_OPERATOR, _currentSymbol.ToString(), _currentLine,
                            _currentCol));
                        break;
                    case '!':
                        _tokens.Add(new Token(TokenType.UNARY_OPERATOR, _currentSymbol.ToString(), _currentLine,
                            _currentCol));
                        break;
                    case ':':
                        if (MoreInputLeft() && PeekNext() == '=')
                        {
                            Advance();
                            _tokens.Add(new Token(TokenType.ASSIGN, ":=", _currentLine, _currentCol - 1));
                        }
                        else
                        {
                            _tokens.Add(new Token(TokenType.COLON, _currentSymbol.ToString(), _currentLine,
                                _currentCol));
                        }

                        break;
                    case ';':
                        _tokens.Add(new Token(TokenType.SEMICOLON, _currentSymbol.ToString(), _currentLine,
                            _currentCol));
                        break;
                    case '"':
                        BuildString();
                        break;
                    case '.':
                        if (PeekNext() == '.')
                        {
                            _tokens.Add(new Token(TokenType.RANGE, "..", _currentLine, _currentCol));
                            Advance();
                        }
                        else
                        {
                            goto default;
                        }

                        break;
                    case '\n':
                        _currentLine++;
                        _currentCol = 0;
                        break;
                    case ' ':
                    case '\t':
                        break;
                    default:
                        _tokens.Add(new Token(TokenType.UNKNOWN, _currentSymbol.ToString(), _currentLine,
                            _currentCol));
                        break;
                }
            }
        }

        _tokens.Add(Token.Eof);

        var unknowns = _tokens.Where(token => token.Type == TokenType.UNKNOWN).ToList();

        if (unknowns.Count > 0)
        {
            var unknownsStr = "";
            foreach (var token in unknowns)
            {
                unknownsStr += $"Unknown token \"{token.Lexeme}\" at {token.PosString}.\n";
            }

            throw new ScanningException("Unknown tokens found:\n" + unknownsStr);
        }
        
        return _tokens;
    }

    private void BuildIdentifier()
    {
        var identifier = "";
        var startPos = _currentCol;

        if (!char.IsLetterOrDigit(_currentSymbol))
            throw new InvalidOperationException("BuildIdentifier() called with incorrect starting symbol");

        while (true)
        {
            identifier += _currentSymbol;

            if (!MoreInputLeft())
                break;

            if (char.IsLetterOrDigit(PeekNext()))
                Advance();
            else
                break;
        }

        if (identifier is "true" or "false")
        {
            _tokens.Add(new Token(TokenType.BOOL, identifier, _currentLine, startPos));
        }
        else
        {
            _tokens.Add(_keywords.Contains(identifier)
                ? new Token(TokenType.KEYWORD, identifier, _currentLine, startPos)
                : new Token(TokenType.IDENTIFIER, identifier, _currentLine, startPos));    
        }
    }

    private void BuildNumber()
    {
        var number = "";
        var startPos = _currentCol;

        if (!char.IsDigit(_currentSymbol))
            throw new InvalidOperationException("BuildNumber() called with incorrect starting symbol");

        while (true)
        {
            number += _currentSymbol;

            if (!MoreInputLeft())
                break;

            if (char.IsDigit(PeekNext()))
                Advance();
            else
                break;
        }

        _tokens.Add(new Token(TokenType.NUMBER, number, _currentLine, startPos));
    }

    private void BuildString()
    {
        var resultString = "";
        var startCol = _currentCol;
        if (_currentSymbol != '"')
            throw new InvalidOperationException("BuildString() called with incorrect starting symbol");

        if (!MoreInputLeft())
            throw new ScanningException(
                $"Ran out of characters while building string starting at line {_currentLine}, col {_currentCol}.");

        Advance();

        while (_currentSymbol != '"')
        {
            resultString += _currentSymbol;

            if (!MoreInputLeft())
                throw new ScanningException(
                    $"Ran out of characters while building string starting at line {_currentLine}, col {_currentCol}.");

            Advance();
        }

        _tokens.Add(new Token(TokenType.STRING, resultString, _currentLine, startCol));
    }

    private bool MoreInputLeft()
    {
        return _currentPos < _input.Length - 1;
    }

    private char PeekNext()
    {
        return _input[_currentPos + 1];
    }

    private void Advance()
    {
        _currentSymbol = _input[++_currentPos];
        _currentCol++;
    }
}

public class ScanningException : Exception
{
    public ScanningException(string message) : base(message)
    {
    }
}