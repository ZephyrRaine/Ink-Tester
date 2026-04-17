using InkTester;

var options = new Tester.Options();
var csvOptions = new CSVHandler.Options();
string listKnotsPath = "";
bool testAllKnots = false;

// ----- Simple Args -----
foreach (var arg in args)
{
    if (arg.StartsWith("--folder="))
        options.folder = arg.Substring(9);
    else if (arg.StartsWith("--storyFile="))
        options.storyFile = arg.Substring(12);
    else if (arg.StartsWith("--testVar="))
        options.testVar = arg.Substring(10);
    else if (arg.StartsWith("--runs="))
        options.testRuns = int.Parse(arg.Substring(7));
    else if (arg.StartsWith("--csv="))
        csvOptions.outputFilePath = arg.Substring(6);
    else if (arg.StartsWith("--maxChoices="))
        options.maxChoices = int.Parse(arg.Substring(13));
    else if (arg.StartsWith("--listKnots="))
        listKnotsPath = arg.Substring(12);
    else if (arg.Equals("--testAllKnots"))
        testAllKnots = true;
    else if (arg.StartsWith("--startKnot="))
        options.startKnots.Add(arg.Substring(12));
    else if (arg.StartsWith("--startKnotsFile=")) {
        var path = arg.Substring(17);
        if (!File.Exists(path)) {
            Console.Error.WriteLine($"Error - can't find startKnotsFile: {path}");
            return -1;
        }
        options.startKnots.AddRange(File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)));
    }
    else if (arg.StartsWith("--ooc"))
        options.ooc = true;
    else if (arg.StartsWith("--maxSteps="))
    {
        options.maxSteps = int.Parse(arg.Substring(11));
        options.maxStepsErrors = false; // Changing the default stops this being reported as an error.
    }
    else if (arg.Equals("--help") || arg.Equals("-h"))
    {
        Console.WriteLine("Ink Tester");
        Console.WriteLine("Arguments:");
        Console.WriteLine("  --folder=<folder> - Root working folder for Ink files, relative to current working dir.");
        Console.WriteLine("                      e.g. --folder=inkFiles/");
        Console.WriteLine("                      Default is the current working dir.");
        Console.WriteLine("  --storyFile=<file> - Ink file to test.");
        Console.WriteLine("                       e.g. --storyFile=start.ink");
        Console.WriteLine("  --runs=<num> - How many times to run the randomized test.");
        Console.WriteLine("                 e.g. --runs=1000");
        Console.WriteLine("                 Default is 100.");
        Console.WriteLine("  --csv=<csvFile> - Path to a CSV file to export, relative to working dir.");
        Console.WriteLine("                    e.g. --csv=output/report.csv");
        Console.WriteLine("                    Default is empty, so no CSV file will be generated.");
        Console.WriteLine("  --testVar=<varName> - Set this variable to TRUE. Useful for setting test data in Ink.");
        Console.WriteLine("                        e.g. --testVar=Testing");
        Console.WriteLine("  --maxSteps=<num> - How many steps to allow your ink story to take before ending. This avoids infinite loops and deals with stories that don't have an explicit ->END.");
        Console.WriteLine("                    e.g. --maxSteps=1000");
        Console.WriteLine("                    Default is 10000, to avoid infinite loops - but when using default, an error will be reported and testing will cease. If you specify your own maxSteps, this won't error.");
        Console.WriteLine("  --maxChoices=<num> - Limits the number of choices to this number. Useful if your interface will only show the top N of choices.");
        Console.WriteLine("                      e.g. --maxChoices=3");
        Console.WriteLine("                      Default is -1, which means no limit.");
        Console.WriteLine("  --startKnot=<knot> - Start each run from this knot instead of the story beginning.");
        Console.WriteLine("  --startKnotsFile=<file> - Load knot names from a txt file (one per line) to use as start knots.");
        Console.WriteLine("                       Can be specified multiple times; runs are distributed across all knots.");
        Console.WriteLine("                       e.g. --startKnot=BUFFET_GAME --startKnot=INTRO");
        Console.WriteLine("  --listKnots=<file> - Write all knot names to a txt file (one per line) and exit.");
        Console.WriteLine("  --testAllKnots - Automatically find all knots and test each one. Shorthand for --listKnots + --startKnotsFile.");
        Console.WriteLine("                       e.g. --listKnots=knots.txt");
        Console.WriteLine("  --ooc - Run an out-of-content check, instead of the normal coverage check.");

        return 0;
    }
    else if (arg.Equals("--test"))
    { // Internal testing, for dev. Not to be confused with testVar.
        options.folder = "tests";
        options.storyFile = "test.ink";
        options.testRuns = 1000;
        //options.ooc = true;
        //options.maxSteps = 1000;
        //options.maxStepsErrors = false;
        //options.testVar = "Testing";
        csvOptions.outputFilePath = "tests/report.csv";
    }
}

// ----- List Knots -----
if (!string.IsNullOrEmpty(listKnotsPath)) {
    var knotLister = new Tester(options);
    if (!knotLister.WriteKnotList(listKnotsPath))
        return -1;
    return 0;
}

// ----- Test All Knots -----
if (testAllKnots) {
    var knots = new Tester(options).GetKnotNames();
    if (knots == null) return -1;
    Console.WriteLine($"Found {knots.Count} knots to test.");
    options.startKnots.AddRange(knots);
}

// ----- Test Ink -----
if (options.ooc) {
    Console.WriteLine("Starting out-of-content test.");
} else {
    Console.WriteLine("Starting coverage test.");
}

var tester = new Tester(options);
if (!tester.Run()) {
    Console.Error.WriteLine("Tests not completed.");
    return -1;
}
Console.WriteLine($"Tested.");

if (options.ooc) {
    if (tester.OOCLog.Count==0) {
        Console.WriteLine("No out-of-content errors found, all good! Report not written.");
        return 0;
    }
    Console.WriteLine($"{tester.OOCLog.Count} out-of-content errors found! See report.");
}

// ----- CSV Output -----
if (!String.IsNullOrEmpty(csvOptions.outputFilePath)) {
    var csvHandler = new CSVHandler(tester, csvOptions);

    if (options.ooc)
    {
        if (!csvHandler.WriteOOCReport()) {
            Console.Error.WriteLine("Report not written.");
            return -1;
        }
    }
    else {
         if (!csvHandler.WriteReport()) {
            Console.Error.WriteLine("Report not written.");
            return -1;
        }
    }
    Console.WriteLine($"CSV file written: {csvOptions.outputFilePath}");
    csvHandler.WriteErrorReport();
}
else {
    Console.Error.WriteLine("No CSV file path supplied - use --csv=<filename.csv>");
}

return 0;
