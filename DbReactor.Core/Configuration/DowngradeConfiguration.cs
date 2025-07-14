using System;

namespace DbReactor.Core.Configuration
{
    public enum DowngradeMatchingMode
    {
        Suffix,
        Prefix,
        SameName
    }

    public class DowngradeMatchingOptions
    {
        public DowngradeMatchingMode Mode { get; set; } = DowngradeMatchingMode.SameName;
        public string Pattern { get; set; } = "_downgrade";
        public string UpgradeSuffix { get; set; } = ".sql";
        public string DowngradeSuffix { get; set; } = ".sql";
    }

    public class DowngradeConfiguration
    {
        public bool Enabled { get; set; } = false;
        public DowngradeMatchingOptions Matching { get; set; } = new DowngradeMatchingOptions();
    }
}