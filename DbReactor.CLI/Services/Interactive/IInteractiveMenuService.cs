namespace DbReactor.CLI.Services.Interactive;

public interface IInteractiveMenuService
{
    void ShowWelcomeBanner();
    string ShowCommandMenu();
}