namespace DbReactor.Core.Constants
{
    /// <summary>
    /// Defines execution priority for seed strategies.
    /// Lower values execute first.
    /// </summary>
    public static class SeedStrategyPriority
    {
        /// <summary>
        /// RunOnce seeds execute first (priority 1)
        /// </summary>
        public const int RunOnce = 1;

        /// <summary>
        /// RunIfChanged seeds execute second (priority 2)
        /// </summary>
        public const int RunIfChanged = 2;

        /// <summary>
        /// RunAlways seeds execute last (priority 3)
        /// </summary>
        public const int RunAlways = 3;

        /// <summary>
        /// Default priority for unknown strategies
        /// </summary>
        public const int Default = 99;

        /// <summary>
        /// Gets the execution priority for a given strategy name.
        /// Lower values execute first.
        /// </summary>
        /// <param name="strategyName">The strategy name from ISeedExecutionStrategy.Name</param>
        /// <returns>Priority integer for sorting</returns>
        public static int GetPriority(string strategyName)
        {
            if (strategyName == DbReactorConstants.SeedStrategies.RunOnce)
                return RunOnce;
            if (strategyName == DbReactorConstants.SeedStrategies.RunIfChanged)
                return RunIfChanged;
            if (strategyName == DbReactorConstants.SeedStrategies.RunAlways)
                return RunAlways;

            return Default;
        }
    }
}
