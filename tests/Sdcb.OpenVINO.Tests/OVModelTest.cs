﻿using Sdcb.OpenVINO.Natives;
using Sdcb.OpenVINO.Tests.Natives;
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
        Assert.NotEmpty(m.Outputs);
        Assert.NotNull(m.Outputs.Primary);
        Assert.NotNull(m.FriendlyName);
    }

    [Fact]
    public void CannotReadMemory_202301()
    {
        if (OpenVINOLibraryLoader.Is202302OrGreater())
        {
            _console.WriteLine($"Case {nameof(CannotReadMemory_202301)} invalid in version {OpenVINOLibraryLoader.VersionAbbr}");
            return;
        }

        Assert.Throws<OpenVINOException>(() =>
        {
            using OVCore c = new();
            (byte[] modelData, byte[] tensorData) = OVCoreNativeTest.PrepareModelData();
            using Model m = c.ReadModel(modelData, tensorData);
        });
    }

    [Fact(Skip = "Seems read from paddle is still not yet supported.")]
    public void CannotReadMemory_202302()
    {
        if (!OpenVINOLibraryLoader.Is202302OrGreater())
        {
            _console.WriteLine($"Case {nameof(CannotReadMemory_202302)} invalid in version {OpenVINOLibraryLoader.VersionAbbr}");
            return;
        }

        using OVCore c = new();
        (byte[] modelData, byte[] tensorData) = OVCoreNativeTest.PrepareModelData();
        using Model m = c.ReadModel(modelData, tensorData);
    }

    [Fact]
    public void CanReadChinese()
    {
        using OVCore c = new();
        using Model m = c.ReadModel(OVCoreNativeTest.PrepareModel("支持中文"));
        Assert.NotNull(m);
        Assert.NotEmpty(m.Inputs);
        Assert.NotNull(m.Inputs.Primary);
        Assert.NotEmpty(m.Outputs);
        Assert.NotNull(m.Outputs.Primary);
        Assert.NotNull(m.FriendlyName);
    }

    [Fact]
    public void CannotReadNonExists()
    {
        using OVCore c = new();
        Assert.Throws<FileNotFoundException>(() => c.ReadModel($@"Z:\a\model\that\not\exists.xml"));
    }

    [Fact]
    public void ReshapeByTensorNames()
    {
        using OVCore c = new();
        using Model m = c.ReadModel(_modelFile);
        using IOPort inputPort = m.Inputs.Primary;
        Assert.Equal("x", inputPort.Name);
        Assert.Equal("{?,3,?,?}", inputPort.PartialShape.ToString());

        m.ReshapeByTensorNames(("x", new PartialShape(1, 3, 256, 320)));
        Assert.Equal("{1,3,256,320}", inputPort.PartialShape.ToString());
    }

    [Fact]
    public void ReshapeByTensorName()
    {
        using OVCore c = new();
        using Model m = c.ReadModel(_modelFile);
        using IOPort inputPort = m.Inputs.Primary;
        Assert.Equal("x", inputPort.Name);
        Assert.Equal("{?,3,?,?}", inputPort.PartialShape.ToString());

        m.ReshapeByTensorName("x", new PartialShape(1, 3, new Dimension(32, 320), 320));
        Assert.Equal("{1,3,32..320,320}", inputPort.PartialShape.ToString());
    }

    [Fact]
    public void ReshapeByPorts()
    {
        using OVCore c = new();
        using Model m = c.ReadModel(_modelFile);
        using IOPort inputPort = m.Inputs.Primary;
        Assert.Equal("x", inputPort.Name);
        Assert.Equal("{?,3,?,?}", inputPort.PartialShape.ToString());

        m.ReshapeByPorts((inputPort, new PartialShape(new Dimension(1, 10), 3, new (32, 320), 320)));
        Assert.Equal("{1..10,3,32..320,320}", inputPort.PartialShape.ToString());
    }

    [Fact]
    public void ReshapeByPortIndexes()
    {
        using OVCore c = new();
        using Model m = c.ReadModel(_modelFile);
        using IOPort inputPort = m.Inputs.Primary;
        Assert.Equal("x", inputPort.Name);
        Assert.Equal("{?,3,?,?}", inputPort.PartialShape.ToString());

        m.ReshapeByPortIndexes(new()
        {
            [0] = new PartialShape(new Dimension(1, 10), 3, new(32, 320), 320),
        });
        Assert.Equal("{1..10,3,32..320,320}", inputPort.PartialShape.ToString());
    }

    [Fact]
    public void ReshapePrimaryInput()
    {
        using OVCore c = new();
        using Model m = c.ReadModel(_modelFile);
        using IOPort inputPort = m.Inputs.Primary;
        Assert.Equal("x", inputPort.Name);
        Assert.Equal("{?,3,?,?}", inputPort.PartialShape.ToString());

        m.ReshapePrimaryInput(new PartialShape(new Dimension(1, 10), 3, new(32, 320), 320));
        Assert.Equal("{1..10,3,32..320,320}", inputPort.PartialShape.ToString());
    }
}
