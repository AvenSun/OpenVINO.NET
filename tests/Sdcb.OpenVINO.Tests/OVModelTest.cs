﻿using Sdcb.OpenVINO.Tests.Natives;
using Xunit.Abstractions;

namespace Sdcb.OpenVINO.Tests;

public class OVModelTest
{
    private readonly ITestOutputHelper _console;
    private readonly string _modelFile;

    public OVModelTest(ITestOutputHelper console)
    {
        _console = console;
        _modelFile = OVCoreNativeTest.PrepareModel();
    }

    [Fact]
    public void CanRead()
    {
        using OVCore c = new();
        using Model m = c.ReadModel(_modelFile);
        Assert.NotNull(m);
        Assert.NotEmpty(m.Inputs);
        Assert.NotNull(m.Inputs.Primary);
        Assert.NotEmpty(m.Output);
        Assert.NotNull(m.Output.Primary);
    }
}
