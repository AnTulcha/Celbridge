﻿using Celbridge.Project.Models;
using Celbridge.Project.Services;
using CommunityToolkit.Diagnostics;

namespace Celbridge.Tests;

[TestFixture]
public class ResourceRegistryTests
{
    private string? _resourceFolder;

    [SetUp]
    public void Setup()
    {
        _resourceFolder = Path.Combine(Path.GetTempPath(), $"Celbridge/{nameof(ResourceRegistryTests)}");
        if (Directory.Exists(_resourceFolder))
        {
            Directory.Delete(_resourceFolder, true);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_resourceFolder))
        {
            Directory.Delete(_resourceFolder!, true);
        }
    }

    [Test]
    public void ICanScanFileAndFolderResources()
    {
        Guard.IsNotNull(_resourceFolder);

        const string FolderNameA = "FolderA";
        const string FileNameA = "FileA.txt";
        const string FileNameB = "FileB.txt";

        const string FileContents = "Lorem Ipsum";

        //
        // Create some files and folders on disk
        //

        Directory.CreateDirectory(_resourceFolder);
        Directory.Exists(_resourceFolder).Should().BeTrue();

        var resourceFileA = Path.Combine(_resourceFolder, FileNameA);
        File.WriteAllText(resourceFileA, FileContents);
        File.Exists(resourceFileA).Should().BeTrue();

        var resourceFolder = Path.Combine(_resourceFolder, FolderNameA);
        Directory.CreateDirectory(resourceFolder);
        Directory.Exists(resourceFolder).Should().BeTrue();

        var resourceFileB = Path.Combine(_resourceFolder, FolderNameA, FileNameB);
        File.WriteAllText(resourceFileB, FileContents);
        File.Exists(resourceFileB).Should().BeTrue();

        //
        // Scan the files and folders
        //

        var resourceRegistry = new ResourceRegistry(_resourceFolder);
        var scanResult = resourceRegistry.UpdateRegistry();
        scanResult.IsSuccess.Should().BeTrue();

        //
        // Check the scanned resources match the files we wrote earlier
        //

        var resources = resourceRegistry.Resources;
        resources.Count.Should().Be(2);

        (resources[0] is FolderResource).Should().BeTrue();
        resources[0].Name.Should().Be(FolderNameA);

        (resources[1] is FileResource).Should().BeTrue();
        resources[1].Name.Should().Be(FileNameA);

        var subFolder = resources[0] as FolderResource;
        Guard.IsNotNull(subFolder);

        subFolder.Children.Count.Should().Be(1);
        subFolder.Children[0].Name.Should().Be(FileNameB);

        //
        // Expand a folder and retrieve it from the registry
        //
        var expandedFoldersIn = new List<string>() { FolderNameA };
        resourceRegistry.SetExpandedFolders(expandedFoldersIn);

        var folder = (resources[0] as FolderResource)!;
        folder.Expanded.Should().BeTrue();

        var expandedFoldersOut = resourceRegistry.GetExpandedFolders();
        expandedFoldersOut.Count.Should().Be(1);
        expandedFoldersOut[0].Should().Be(FolderNameA);

        var folderPath = resourceRegistry.GetResourcePath(folder);
        resourceRegistry.IsFolderExpanded(folderPath).Should().BeTrue();
    }
}