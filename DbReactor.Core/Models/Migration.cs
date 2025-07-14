using DbReactor.Core.Abstractions;
using DbReactor.Core.Models.Scripts;

namespace DbReactor.Core.Models
{
    public class Migration : IMigration
    {
        public string Name { get; }
        public IScript UpgradeScript { get; }
        public IScript DowngradeScript { get; }
        public bool HasDowngrade => DowngradeScript != null;

        public Migration(string name, IScript upgradeScript, IScript downgradeScript = null)
        {
            Name = name;
            UpgradeScript = upgradeScript;
            DowngradeScript = downgradeScript;
        }

        public Migration(string name, IScript upgradeScript, IScript downgradeScript, string downgradeScriptContent)
        {
            Name = name;
            UpgradeScript = upgradeScript;

            if (!string.IsNullOrWhiteSpace(downgradeScriptContent))
                DowngradeScript = new GenericScript(name, downgradeScriptContent);
            else
                DowngradeScript = downgradeScript;

        }
    }
}
