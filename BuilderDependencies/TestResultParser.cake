using System.Xml.Linq;

public string ParseTestResult(string resultPath)
{
    var testResult = XDocument.Load(resultPath);
    var tests = testResult.Descendants("test-case");

    var failledTests = tests
                .Where(IsFailedResult)
                .Select(FailedTestSummury);

    var skippedTests = tests
                .Where(IsSkippedResult)
                .Select(SkippedTestSummury);
                
    var inconclusiveTests = tests
                .Where(IsInconclusiveResult)
                .Select(InconclusiveTestSummury);                                

    int passedTestCount = tests.Count(IsPassedResult);
    int failledTestsCount = failledTests.Count();
    int skippedTestCount = skippedTests.Count();
    int inconclusiveTestsCount = inconclusiveTests.Count();

    var message = $"✅ {passedTestCount}\n";
    message += $"❌ {failledTestsCount}\n";
    message += $"💨 {skippedTestCount}\n";
    message += $"♾ {inconclusiveTestsCount}\n\n";

    foreach (string testCaseResult in failledTests)
        message += $"{testCaseResult}\n";

    foreach (string testCaseResult in skippedTests)
        message += $"{testCaseResult}\n";

    foreach (string testCaseResult in inconclusiveTests)
        message += $"{testCaseResult}\n";

    return message;
}

string FailedTestSummury(XElement x) => $"❌ {GetTestName(x)} Failed: {GetTestErrorMessage(x)}";

string SkippedTestSummury(XElement x) => $"💨 {GetTestName(x)} Skipped: {GetTestErrorMessage(x)}";

string InconclusiveTestSummury(XElement x) => $"♾ {GetTestName(x)} Inconclusive: {GetTestErrorMessage(x)}";

string GetTestName(XElement x) => x.Attribute("name").Value;

string GetTestErrorMessage(XElement x) => x.Element("failure").Element("message").Value;


bool IsFailedResult(XElement x) => x.Attribute("result").Value == "Failed";
bool IsPassedResult(XElement x) => x.Attribute("result").Value == "Passed";
bool IsInconclusiveResult(XElement x) => x.Attribute("result").Value == "Inconclusive";
bool IsSkippedResult(XElement x) => x.Attribute("result").Value == "Skipped";