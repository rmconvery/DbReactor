using System.Reflection;
using Microsoft.Extensions.Logging;

namespace DbReactor.CLI.Services;

public class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(ILogger<TemplateService> logger)
    {
        _logger = logger;
    }

    public async Task<string> LoadTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        try
        {
            var templatePath = Path.Combine("Templates", templateName);
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"DbReactor.CLI.{templatePath.Replace(Path.DirectorySeparatorChar, '.')}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Template not found: {templateName}");
            }

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            
            _logger.LogDebug("Loaded template: {TemplateName}", templateName);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load template: {TemplateName}", templateName);
            throw;
        }
    }

    public string RenderTemplate(string template, Dictionary<string, string> variables)
    {
        var rendered = template;
        
        foreach (var variable in variables)
        {
            rendered = rendered.Replace($"{{{variable.Key}}}", variable.Value);
        }

        return rendered;
    }
}