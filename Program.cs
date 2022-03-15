using System.Text;
using MPLI;
using InvalidOperationException = System.InvalidOperationException;

if (args.Length != 1)
{
    Console.WriteLine("Please give path to file as arg.");
    return 1;
}

var text = File.ReadAllText(args[0], Encoding.UTF8);

var scanner = new Scanner(text);

try
{
    var tokens = scanner.Scan();
    var parser = new Parser(tokens);
    var parseTree = parser.Parse();
    var program = new Analyzer(parseTree).Analyze();
    new Evaluator(program).Evaluate();
}
catch (Exception exception)
{
    switch (exception)
    {
        case ScanningException scanning:
            ReportScanningError(scanning);
            break;
        
        case ParseException parse:
            ReportParsingError(parse);
            break;
        
        case SemanticException semantic:
            ReportSemanticError(semantic);
            break;
        
        case EvaluationException evaluation:
            ReportEvaluationError(evaluation);
            break;
        
        case AssertionFailException assertion:
            Console.WriteLine(assertion.Message);
            break;

        case InvalidOperationException:
            Console.WriteLine("Something in this program causes the interpreter to break. Please try a different program.");
            return 1;
        
        default:
            throw;
    }
}


return 0;

static void ReportScanningError(ScanningException e)
{
    Console.WriteLine("There were errors found during scanning:");
    Console.WriteLine(e.Message);
}

static void ReportParsingError(ParseException e)
{
    Console.WriteLine("There were errors found during parsing:");
    Console.WriteLine(e.Message);
}

static void ReportSemanticError(SemanticException e)
{
    Console.WriteLine("There were errors found during semantic analysis:");
    Console.WriteLine(e.Message);
}

static void ReportEvaluationError(EvaluationException e)
{
    Console.WriteLine("There were errors found during execution:");
    Console.WriteLine(e.Message);
}