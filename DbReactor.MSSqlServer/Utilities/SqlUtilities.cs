using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DbReactor.MSSqlServer.Utilities
{
    public static class SqlUtilities
    {
        public static bool IsEfTransactionScript(string scriptContent)
        {
            return Regex.IsMatch(
                scriptContent,
                @"BEGIN\s+TRAN(SACTION)?\b.*?^\s*GO\s*$.*?(COMMIT|ROLLBACK)\b.*?^\s*GO\s*$",
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
        }

        public static string RemoveGoStatements(string scriptContent)
        {
            return Regex.Replace(
                scriptContent,
                @"^\s*GO(?:\s+\d+)?\s*(?:--.*)?$",
                string.Empty,
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        public static List<string> ParseScriptIntoBatches(string scriptContent)
        {
            try
            {
                TSql150Parser parser = new TSql150Parser(true);
                using StringReader reader = new StringReader(scriptContent);
                TSqlFragment fragment = parser.Parse(reader, out IList<ParseError> errors);

                if (errors.Count == 0 && fragment is TSqlScript script)
                {
                    List<string> batches = new List<string>();
                    foreach (TSqlBatch batch in script.Batches)
                    {
                        string batchSql = GetSqlFromFragment(batch, scriptContent);
                        if (!string.IsNullOrWhiteSpace(batchSql))
                            batches.Add(batchSql);
                    }
                    if (batches.Count > 0)
                        return batches;
                }
            }
            catch
            {
                // Fallback to regex splitting
            }
            return SplitOnGoStatements(scriptContent);
        }

        public static List<string> SplitOnGoStatements(string scriptContent)
        {
            Regex regex = new Regex(
                @"^\s*GO(?:\s+(\d+))?\s*(?:--.*)?$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            List<string> batches = new List<string>();
            int lastPos = 0;
            MatchCollection matches = regex.Matches(scriptContent);

            foreach (Match match in matches)
            {
                int len = match.Index - lastPos;
                if (len > 0)
                {
                    string batch = scriptContent.Substring(lastPos, len).Trim();
                    if (!string.IsNullOrWhiteSpace(batch))
                    {
                        int repeat = 1;
                        if (match.Groups[1].Success && int.TryParse(match.Groups[1].Value, out int n) && n > 1)
                            repeat = n;
                        for (int i = 0; i < repeat; i++)
                            batches.Add(batch);
                    }
                }
                lastPos = match.Index + match.Length;
            }
            if (lastPos < scriptContent.Length)
            {
                string batch = scriptContent.Substring(lastPos).Trim();
                if (!string.IsNullOrWhiteSpace(batch))
                    batches.Add(batch);
            }
            return batches;
        }

        public static string GetSqlFromFragment(TSqlFragment fragment, string originalScript)
        {
            if (fragment == null) return string.Empty;
            int startOffset = fragment.StartOffset;
            int length = fragment.FragmentLength;
            if (startOffset >= 0 && length > 0 && startOffset + length <= originalScript.Length)
                return originalScript.Substring(startOffset, length);
            return string.Empty;
        }

        public static bool ContainsTransactionStatements(string scriptContent)
        {
            string upperScript = scriptContent.ToUpperInvariant();
            return upperScript.Contains("BEGIN TRANSACTION") ||
                   upperScript.Contains("BEGIN TRAN") ||
                   upperScript.Contains("COMMIT TRANSACTION") ||
                   upperScript.Contains("COMMIT TRAN") ||
                   upperScript.Contains("ROLLBACK TRANSACTION") ||
                   upperScript.Contains("ROLLBACK TRAN") ||
                   upperScript.Contains("COMMIT;") ||
                   upperScript.Contains("ROLLBACK;");
        }
    }
}