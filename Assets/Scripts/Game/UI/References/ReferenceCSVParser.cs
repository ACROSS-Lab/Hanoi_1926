using System.Collections.Generic;
using System.Text;

public static class ReferenceCSVParser
{
    /// <summary>
    /// Parses a CSV string that may contain multi-line quoted fields, escaped quotes, and commas.
    /// </summary>
    public static List<List<string>> Parse(string csvText)
    {
        var result = new List<List<string>>();
        if (string.IsNullOrEmpty(csvText)) return result;

        // Strip UTF-8 BOM if present
        if (csvText.Length > 0 && csvText[0] == '\uFEFF')
        {
            csvText = csvText.Substring(1);
        }

        var currentRow = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped double quotes: ""
                    if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // Skip the next quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentRow.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else if (c == '\r')
                {
                    // Handle \r\n
                    if (i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    {
                        i++;
                    }
                    currentRow.Add(currentField.ToString().Trim());
                    currentField.Clear();
                    if (currentRow.Count > 0 && !IsEmptyRow(currentRow))
                    {
                        result.Add(currentRow);
                    }
                    currentRow = new List<string>();
                }
                else if (c == '\n')
                {
                    currentRow.Add(currentField.ToString().Trim());
                    currentField.Clear();
                    if (currentRow.Count > 0 && !IsEmptyRow(currentRow))
                    {
                        result.Add(currentRow);
                    }
                    currentRow = new List<string>();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        // Add the last pending field / row if any
        if (currentField.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentField.ToString().Trim());
            if (currentRow.Count > 0 && !IsEmptyRow(currentRow))
            {
                result.Add(currentRow);
            }
        }

        return result;
    }

    private static bool IsEmptyRow(List<string> row)
    {
        foreach (var item in row)
        {
            if (!string.IsNullOrWhiteSpace(item)) return false;
        }
        return true;
    }
}
