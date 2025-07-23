namespace DbReactor.CLI.Models;

public class VariableInfo
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public bool IsSensitive { get; set; }
}

public class VariableCollection
{
    public Dictionary<string, VariableInfo> Variables { get; set; } = new();
    
    // For backward compatibility with existing CliOptions.Variables
    public Dictionary<string, string> ToDictionary()
    {
        return Variables.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
    }
    
    public static VariableCollection FromDictionary(Dictionary<string, string> variables, Func<string, bool>? sensitivityChecker = null)
    {
        var collection = new VariableCollection();
        
        foreach (var kvp in variables)
        {
            collection.Variables[kvp.Key] = new VariableInfo
            {
                Key = kvp.Key,
                Value = kvp.Value,
                IsSensitive = sensitivityChecker?.Invoke(kvp.Key) ?? false
            };
        }
        
        return collection;
    }
    
    public void AddVariable(string key, string value, bool isSensitive)
    {
        Variables[key] = new VariableInfo
        {
            Key = key,
            Value = value,
            IsSensitive = isSensitive
        };
    }
    
    public bool RemoveVariable(string key)
    {
        return Variables.Remove(key);
    }
    
    public VariableInfo? GetVariable(string key)
    {
        return Variables.TryGetValue(key, out var variable) ? variable : null;
    }
    
    public bool IsVariableSensitive(string key)
    {
        var variable = GetVariable(key);
        return variable?.IsSensitive ?? false;
    }
}