using Sdcb.OpenVINO.NuGetBuilder.ArtifactSources;
using System.Text.Json;
using Xunit.Abstractions;

namespace Sdcb.OpenVINO.NuGetBuilder.Tests;

public class OpenVINOFileTreeTest
{
    private readonly ITestOutputHelper _console;
    private readonly OpenVINOFileTreeRoot _root;

    public OpenVINOFileTreeTest(ITestOutputHelper console)
    {
        _console = console;
        string fileTreeJsonPath = @"asset/filetree.json";
        _root = JsonSerializer.Deserialize<OpenVINOFileTreeRoot>(File.ReadAllText(fileTreeJsonPath)) ?? throw new Exception($"Failed to load {fileTreeJsonPath}.");
    }

    [Fact]
    public void ListVersions()
    {
        _console.WriteLine(string.Join(Environment.NewLine, _root.VersionFolders.OrderByDescending(x => x.Version)));
    }

    [Fact]
    public void PrintLatestVersion()
    {
        _console.WriteLine(_root.LatestStableVersion.ToString());
    }

    [Fact]
    public void ListPaths()
    {
        _console.WriteLine(string.Join(Environment.NewLine, _root.VersionFolders.OrderByDescending(x => x.Version).Select(x => x.Path)));
    }

    [Fact]
    public void ListContainsInLatestStableVersion()
    {
        VersionFolder vf = _root.LatestStableVersion;
        _console.WriteLine(string.Join(Environment.NewLine, vf.Folder.EnumerateFiles("").Select(x => x.Name)));
    }
}