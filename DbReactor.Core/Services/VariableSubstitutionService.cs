using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DbReactor.Core.Services
{
    /// <summary>
    /// Service for performing variable substitution in SQL scripts
    /// </summary>
    public class VariableSubstitutionService
    {
        private static readonly Regex VariablePattern = new Regex(@"\$\{([^}]+)\}", RegexOptions.Compiled);

        /// <summary>
        /// Substitutes variables in the given script content
        /// </summary>
        /// <param name="scriptContent">Script content with variables in ${variableName} format</param>
        /// <param name="variables">Dictionary of variable values</param>
        /// <returns>Script content with variables substituted</returns>
        public string SubstituteVariables(string scriptContent, IReadOnlyDictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(scriptContent) || variables == null || variables.Count == 0)
            {
                return scriptContent;
            }

            return VariablePattern.Replace(scriptContent, match =>
            {
                string variableName = match.Groups[1].Value;
                
                if (variables.TryGetValue(variableName, out string variableValue))
                {
                    return variableValue ?? string.Empty;
                }

                // If variable is not found, leave the placeholder as-is
                return match.Value;
            });
        }

        /// <summary>
        /// Validates that all variables in the script content can be resolved
        /// </summary>
        /// <param name="scriptContent">Script content to validate</param>
        /// <param name="variables">Available variables</param>
        /// <returns>List of unresolved variable names</returns>
        public List<string> GetUnresolvedVariables(string scriptContent, IReadOnlyDictionary<string, string> variables)
        {
            var unresolvedVariables = new List<string>();
            
            if (string.IsNullOrEmpty(scriptContent))
            {
                return unresolvedVariables;
            }

            var matches = VariablePattern.Matches(scriptContent);
            
            foreach (Match match in matches)
            {
                string variableName = match.Groups[1].Value;
                
                if (!variables.ContainsKey(variableName))
                {
                    if (!unresolvedVariables.Contains(variableName))
                    {
                        unresolvedVariables.Add(variableName);
                    }
                }
            }

            return unresolvedVariables;
        }

        /// <summary>
        /// Gets all variable names found in the script content
        /// </summary>
        /// <param name="scriptContent">Script content to analyze</param>
        /// <returns>List of variable names found in the script</returns>
        public List<string> GetVariableNames(string scriptContent)
        {
            var variableNames = new List<string>();
            
            if (string.IsNullOrEmpty(scriptContent))
            {
                return variableNames;
            }

            var matches = VariablePattern.Matches(scriptContent);
            
            foreach (Match match in matches)
            {
                string variableName = match.Groups[1].Value;
                
                if (!variableNames.Contains(variableName))
                {
                    variableNames.Add(variableName);
                }
            }

            return variableNames;
        }
    }
}