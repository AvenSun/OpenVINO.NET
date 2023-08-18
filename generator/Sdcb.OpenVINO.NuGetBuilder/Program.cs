﻿using Microsoft.Extensions.DependencyInjection;
using Sdcb.OpenVINO.NuGetBuilder;
using Sdcb.OpenVINO.NuGetBuilder.ArtifactSources;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Sdcb.OpenVINO.NuGetBuilder.Tests")]
class Program
{
    static void Main()
    {
        IServiceProvider sp = ConfigureServices();
        Debug(sp);
    }

    private static void Debug(IServiceProvider sp)
    {
        StorageNodeRoot repo = sp.GetRequiredService<StorageNodeRoot>();
    }

    static IServiceProvider ConfigureServices()
    {
        return new ServiceCollection()
            .AddSingleton(_ => new CachedHttpGetService("cache"))
            .AddSingleton(sp => StorageNodeRoot.LoadRootFromHttp(sp).GetAwaiter().GetResult())
            .BuildServiceProvider();
    }
}