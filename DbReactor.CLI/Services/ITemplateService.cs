namespace DbReactor.CLI.Services;

public interface ITemplateService
{
    Task<string> LoadTemplateAsync(string templateName, CancellationToken cancellationToken = default);
    string RenderTemplate(string template, Dictionary<string, string> variables);
}