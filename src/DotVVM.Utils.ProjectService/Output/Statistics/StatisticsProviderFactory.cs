﻿namespace DotVVM.Utils.ProjectService.Output.Statistics
{
    public class StatisticsProviderFactory
    {
        public IStatisticsProvider GetProvider(DotvvmProjectSertviceConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.StatisticsFolder))
            {
                return new DummyStatisticsProvider();
            }
            return new StatisticsProvider(configuration.StatisticsFolder);
        }
    }
}