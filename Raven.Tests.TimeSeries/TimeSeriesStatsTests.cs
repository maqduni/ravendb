﻿using System;
using System.Threading.Tasks;
using Xunit;

namespace Raven.Tests.TimeSeries
{
    public class TimeSeriesStatsTests : RavenBaseTimeSeriesTest
	{
	    [Fact]
	    public async Task Fetching_time_series_stats_should_work()
	    {
		    using (var store = NewRemoteTimeSeriesStore())
		    {
				await store.AppendAsync("Time", DateTime.Now, 3d);
				await store.AppendAsync("Time", DateTime.Now.AddHours(1), 4d);
				await store.AppendAsync("Is", DateTime.Now, 5d);
				await store.AppendAsync("Money", DateTime.Now, 6d);
				await store.AppendAsync("Money", DateTime.Now, 7d);
				await store.AppendAsync("Money", DateTime.Now, 8d);
				
				var stats = await store.GetTimeSeriesStatsAsync();
			    Assert.Equal(3, stats.KeysCount);
			    Assert.Equal(7, stats.ValuesCount);
			}
	    }
	}
}