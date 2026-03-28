using ChatArchiveViewer.Core.Services;

namespace ChatArchiveViewer.Core.Tests.Services;

/// <summary>
/// FolderArchiveSource のブラックボックステスト (TP-060)
/// </summary>
[TestFixture]
public sealed class FolderArchiveSourceTests
{
    private string tempRoot = string.Empty;

    [SetUp]
    public void SetUp()
    {
        // テスト用一時ディレクトリを作成
        tempRoot = Path.Combine(Path.GetTempPath(), $"folder-src-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        Directory.CreateDirectory(Path.Combine(tempRoot, "general"));
        File.WriteAllText(Path.Combine(tempRoot, "channels.json"), "[]");
        File.WriteAllText(Path.Combine(tempRoot, "users.json"), "[]");
        File.WriteAllText(Path.Combine(tempRoot, "general", "2026-01-01.json"), "[]");
        File.WriteAllText(Path.Combine(tempRoot, "general", "2026-01-02.json"), "[]");
    }

    [TearDown]
    public void TearDown()
    {
        // テスト後にクリーンアップ
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    // TP-060a: 有効なフォルダで GetFilesAsync がファイル一覧を返す
    [Test]
    public async Task UT_IT_060a__GetFilesAsync_ReturnsFilesInDirectory()
    {
        await using var source = new FolderArchiveSource(tempRoot);

        var files = await source.GetFilesAsync("general", "*.json", CancellationToken.None);

        Assert.That(files, Has.Count.EqualTo(2));
        Assert.That(files.All(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    // TP-060b: 有効なフォルダで OpenFileAsync がストリームを返す
    [Test]
    public async Task UT_IT_060b__OpenFileAsync_ReturnsReadableStream()
    {
        await using var source = new FolderArchiveSource(tempRoot);

        await using var stream = await source.OpenFileAsync("channels.json", CancellationToken.None);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        Assert.That(content, Is.EqualTo("[]"));
    }

    // TP-060c: FileExistsAsync は存在するファイルに true を返す
    [Test]
    public async Task UT_IT_060c__FileExistsAsync_ReturnsTrueForExistingFile()
    {
        await using var source = new FolderArchiveSource(tempRoot);

        var exists = await source.FileExistsAsync("channels.json", CancellationToken.None);

        Assert.That(exists, Is.True);
    }

    // TP-060d: DirectoryExistsAsync は存在するディレクトリに true を返す
    [Test]
    public async Task UT_IT_060d__DirectoryExistsAsync_ReturnsTrueForExistingDirectory()
    {
        await using var source = new FolderArchiveSource(tempRoot);

        var exists = await source.DirectoryExistsAsync("general", CancellationToken.None);

        Assert.That(exists, Is.True);
    }

    // TP-060e: GetDirectoriesAsync がディレクトリ一覧を返す
    [Test]
    public async Task UT_IT_060e__GetDirectoriesAsync_ReturnsDirectories()
    {
        await using var source = new FolderArchiveSource(tempRoot);

        var dirs = await source.GetDirectoriesAsync(string.Empty, CancellationToken.None);

        Assert.That(dirs, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(dirs.Any(d => d.Equals("general", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    // TP-060f: FileExistsAsync は存在しないパスに false を返す
    [Test]
    public async Task UT_IT_060f__FileExistsAsync_ReturnsFalseForMissingFile()
    {
        await using var source = new FolderArchiveSource(tempRoot);

        var exists = await source.FileExistsAsync("does-not-exist.json", CancellationToken.None);

        Assert.That(exists, Is.False);
    }

    // TP-060g: DisplayPath はフォルダパスに基づく表示パスが設定されている
    [Test]
    public async Task UT_IT_060g__DisplayPath_ContainsFolderPath()
    {
        await using var source = new FolderArchiveSource(tempRoot);

        // DisplayPath はフォルダパスと同等（正規化済み）
        Assert.That(source.DisplayPath, Is.Not.Null.And.Not.Empty);
        Assert.That(
            Path.GetFullPath(source.DisplayPath),
            Is.EqualTo(Path.GetFullPath(tempRoot)).IgnoreCase);
    }

    // TP-060h: GetFilesAsync のパターン指定でフィルタが効く
    [Test]
    public async Task UT_IT_060h__GetFilesAsync_WithPattern_ReturnsOnlyMatchingFiles()
    {
        // テキストファイルを追加
        File.WriteAllText(Path.Combine(tempRoot, "readme.txt"), "hello");
        await using var source = new FolderArchiveSource(tempRoot);

        var jsonFiles = await source.GetFilesAsync(string.Empty, "*.json", CancellationToken.None);
        var txtFiles = await source.GetFilesAsync(string.Empty, "*.txt", CancellationToken.None);

        Assert.That(jsonFiles.All(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(txtFiles.All(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(txtFiles, Has.Count.EqualTo(1));
    }
}
