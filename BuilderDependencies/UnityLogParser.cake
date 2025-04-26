
public string ParseUnityLogError(string logPath)
{
  var lineWithErorrs = System.IO.File.ReadAllLines(logPath)
    .Where(IsValuableLine);
    
  if(lineWithErorrs.Count() > 0)
    return lineWithErorrs
        .Distinct()
        .Select(line => $"⛔ {line}\n\n")
        .Aggregate(AddLineToMessage);

      return string.Empty;
}

private bool IsValuableLine(string line) => line.Contains("error CS");
private string AddLineToMessage(string message, string line) => message += line;

