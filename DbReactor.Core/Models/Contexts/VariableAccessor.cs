using System;
using System.Collections.Generic;

namespace DbReactor.Core.Models.Contexts
{
    /// <summary>
    /// Provides a fluent API for accessing and converting variables from CodeScriptContext
    /// </summary>
    public class VariableAccessor
    {
        private readonly IReadOnlyDictionary<string, string> _variables;

        public VariableAccessor(IReadOnlyDictionary<string, string> variables)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
        }

        /// <summary>
        /// Gets a string variable with an optional default value
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <param name="defaultValue">Default value if variable is not found</param>
        /// <returns>Variable value or default</returns>
        public string GetString(string key, string defaultValue = null)
        {
            return _variables.TryGetValue(key, out string value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets a required string variable, throwing an exception if not found
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <returns>Variable value</returns>
        /// <exception cref="ArgumentException">Thrown when variable is not found or empty</exception>
        public string GetRequiredString(string key)
        {
            if (!_variables.TryGetValue(key, out string value) || string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"Required variable '{key}' is missing or empty");
            }
            return value;
        }

        /// <summary>
        /// Gets an integer variable with an optional default value
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <param name="defaultValue">Default value if variable is not found or invalid</param>
        /// <returns>Variable value or default</returns>
        public int GetInt(string key, int defaultValue = 0)
        {
            if (_variables.TryGetValue(key, out string value) && int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets a required integer variable, throwing an exception if not found or invalid
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <returns>Variable value</returns>
        /// <exception cref="ArgumentException">Thrown when variable is not found or invalid</exception>
        public int GetRequiredInt(string key)
        {
            if (!_variables.TryGetValue(key, out string value) || !int.TryParse(value, out int result))
            {
                throw new ArgumentException($"Required integer variable '{key}' is missing or invalid");
            }
            return result;
        }

        /// <summary>
        /// Gets a boolean variable with an optional default value
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <param name="defaultValue">Default value if variable is not found or invalid</param>
        /// <returns>Variable value or default</returns>
        public bool GetBool(string key, bool defaultValue = false)
        {
            if (_variables.TryGetValue(key, out string value) && bool.TryParse(value, out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets a required boolean variable, throwing an exception if not found or invalid
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <returns>Variable value</returns>
        /// <exception cref="ArgumentException">Thrown when variable is not found or invalid</exception>
        public bool GetRequiredBool(string key)
        {
            if (!_variables.TryGetValue(key, out string value) || !bool.TryParse(value, out bool result))
            {
                throw new ArgumentException($"Required boolean variable '{key}' is missing or invalid");
            }
            return result;
        }

        /// <summary>
        /// Checks if a variable exists
        /// </summary>
        /// <param name="key">Variable name</param>
        /// <returns>True if variable exists, false otherwise</returns>
        public bool HasVariable(string key)
        {
            return _variables.ContainsKey(key);
        }

        /// <summary>
        /// Gets all variable names
        /// </summary>
        /// <returns>Collection of variable names</returns>
        public IEnumerable<string> GetVariableNames()
        {
            return _variables.Keys;
        }
    }
}