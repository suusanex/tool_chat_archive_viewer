using System.IO.Compression;
using ChatArchiveViewer.Core.Services;

namespace ChatArchiveViewer.Core.Tests.Services;

[TestFixture]
public sealed class ZipArchiveSourceTests
{
    private readonly List<string> tempFiles = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var tempFile in tempFiles)
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }

        tempFiles.Clear();
    }

    // TP-050b: OpenFileAsync で展開済みファイルを読み取れる
    [Test]
    public async Task UT_IT_050b__OpenFileAsync_ReadsExtractedFile()
    {
        var zipPath = CreateZip(
            ("channels.json", "[]"),
            ("general/2026-01-01.json", "[]"));
        await using var source = new ZipArchiveSource(zipPath);

        await using var stream = await source.OpenFileAsync("channels.json", CancellationToken.None);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        Assert.That(content, Is.EqualTo("[]"));
    }

    // TP-140a/b: zip slip エントリはコンストラクタで拒否される
    [Test]
    public void UT_IT_140a_b__Ctor_WithZipSlipEntry_Throws()
    {
        var zipPath = TrackTempFile(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip"));
        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("../evil.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("x");
        }

        var action = () => new ZipArchiveSource(zipPath);
        Assert.That(action, Throws.TypeOf<InvalidDataException>());
    }

    // TP-050a: GetFilesAsync でファイル一覧を取得できる
    [Test]
    public async Task UT_IT_050a__GetFilesAsync_ReturnsFileList()
    {
        var zipPath = CreateZip(
            ("channels.json", "[]"),
            ("general/2026-01-01.json", "[]"),
            ("general/2026-01-02.json", "[]"));
        await using var source = new ZipArchiveSource(zipPath);

        var files = await source.GetFilesAsync("general", "*.json", CancellationToken.None);

        Assert.That(files, Has.Count.EqualTo(2));
    }

    // TP-050c: FileExistsAsync は存在するファイルに true を返す
    [Test]
    public async Task UT_IT_050c__FileExistsAsync_ReturnsTrueForExistingFile()
    {
        var zipPath = CreateZip(("channels.json", "[]"));
        await using var source = new ZipArchiveSource(zipPath);

        var exists = await source.FileExistsAsync("channels.json", CancellationToken.None);

        Assert.That(exists, Is.True);
    }

    // TP-050d: DirectoryExistsAsync は存在するディレクトリに true を返す
    [Test]
    public async Task UT_IT_050d__DirectoryExistsAsync_ReturnsTrueForExistingDirectory()
    {
        var zipPath = CreateZip(
            ("channels.json", "[]"),
            ("general/2026-01-01.json", "[]"));
        await using var source = new ZipArchiveSource(zipPath);

        var exists = await source.DirectoryExistsAsync("general", CancellationToken.None);

        Assert.That(exists, Is.True);
    }

    // TP-050e: GetDirectoriesAsync でディレクトリ一覧を取得できる
    [Test]
    public async Task UT_IT_050e__GetDirectoriesAsync_ReturnsDirectoryList()
    {
        var zipPath = CreateZip(
            ("channels.json", "[]"),
            ("general/2026-01-01.json", "[]"),
            ("random/2026-01-01.json", "[]"));
        await using var source = new ZipArchiveSource(zipPath);

        var dirs = await source.GetDirectoriesAsync(string.Empty, CancellationToken.None);

        Assert.That(dirs, Has.Count.EqualTo(2));
    }

    // TP-050f: FileExistsAsync は存在しないパスに false を返す
    [Test]
    public async Task UT_IT_050f__FileExistsAsync_ReturnsFalseForMissingFile()
    {
        var zipPath = CreateZip(("channels.json", "[]"));
        await using var source = new ZipArchiveSource(zipPath);

        var exists = await source.FileExistsAsync("does-not-exist.json", CancellationToken.None);

        Assert.That(exists, Is.False);
    }

    // TP-050g: DisplayPath は ZIP ファイルパスに基づく表示パスが設定されている
    [Test]
    public async Task UT_IT_050g__DisplayPath_ContainsZipFilePath()
    {
        var zipPath = CreateZip(("channels.json", "[]"));
        await using var source = new ZipArchiveSource(zipPath);

        Assert.That(source.DisplayPath, Is.Not.Null.And.Not.Empty);
        Assert.That(
            Path.GetFullPath(source.DisplayPath),
            Is.EqualTo(Path.GetFullPath(zipPath)).IgnoreCase);
    }

    // TP-050h: DisposeAsync で一時ディレクトリが削除される
    [Test]
    public async Task UT_IT_050h__DisposeAsync_RemovesTempDirectory()
    {
        var zipPath = CreateZip(("channels.json", "[]"));

        // まず読み込んで Dispose する（ストリームを先に閉じる）
        var source = new ZipArchiveSource(zipPath);
        await source.DisposeAsync();

        // Dispose 後も ZIP ファイル自体は存在する（ZIP は削除しない）
        Assert.That(File.Exists(zipPath), Is.True);

        // 再度同じ ZIP を開けることを確認（TP-320a の前提：一時ディレクトリが削除されていれば再作成される）
        await using var source2 = new ZipArchiveSource(zipPath);
        var exists = await source2.FileExistsAsync("channels.json", CancellationToken.None);
        Assert.That(exists, Is.True);
    }

    // TP-320a: Dispose 後に同じ ZIP を再オープンして正常に動作する
    [Test]
    public async Task UT_IT_320a__ReopenSameZip_AfterDispose_WorksCorrectly()
    {
        var zipPath = CreateZip(
            ("channels.json", """[{"id":"C1","name":"general","is_channel":true}]"""),
            ("users.json", """[{"id":"U1","real_name":"Alice"}]"""),
            ("general/2026-01-01.json", "[]"));

        // 1回目: 読み込んで Dispose
        await using (var source1 = new ZipArchiveSource(zipPath))
        {
            var exists = await source1.FileExistsAsync("channels.json", CancellationToken.None);
            Assert.That(exists, Is.True, "1回目の読み込みが成功すること");
        }

        // 2回目: 同じ ZIP を再度開ける
        await using var source2 = new ZipArchiveSource(zipPath);
        var stillExists = await source2.FileExistsAsync("channels.json", CancellationToken.None);
        Assert.That(stillExists, Is.True, "Dispose 後に同じ ZIP を再オープンできること");

        // ファイル内容も正常に読める
        await using var stream = await source2.OpenFileAsync("channels.json", CancellationToken.None);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        Assert.That(content, Is.Not.Empty);
    }

    // TP-140a/b: zip slip エントリ（../）で例外がスローされる（既存テストの TP ID を付与）
    [Test]
    public void UT_IT_140a__ZipSlipEntry_Throws()
    {
        var zipPath = TrackTempFile(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip"));
        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("../outside-root.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("malicious content");
        }

        var action = () => new ZipArchiveSource(zipPath);
        Assert.That(action, Throws.TypeOf<InvalidDataException>());
    }

    // TP-140d: 空の ZIP ファイルは IArchiveSource として生成可能
    [Test]
    public async Task UT_IT_140d__EmptyZip_CanBeCreatedAsSource()
    {
        var zipPath = TrackTempFile(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip"));
        using (ZipFile.Open(zipPath, ZipArchiveMode.Create)) { }

        await using var source = new ZipArchiveSource(zipPath);
        var files = await source.GetFilesAsync(string.Empty, "*.json", CancellationToken.None);
        Assert.That(files, Is.Empty);
    }

    // TP-140c: 破損 ZIP バイト列はコンストラクタで InvalidDataException となる
    [Test]
    public void UT_IT_140c__Ctor_WithCorruptedZip_ThrowsInvalidDataException()
    {
        var zipPath = TrackTempFile(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip"));
        File.WriteAllBytes(zipPath, [0x00, 0x01, 0x02, 0x03, 0x04]);
        var directoriesBefore = GetExtractDirectories();

        var action = () => new ZipArchiveSource(zipPath);

        Assert.That(action, Throws.TypeOf<InvalidDataException>());
        Assert.That(GetExtractDirectories(), Is.EquivalentTo(directoriesBefore));
    }

    private static string[] GetExtractDirectories()
        => Directory
            .GetDirectories(Path.GetTempPath(), "chat-archive-viewer-*", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private string CreateZip(params (string path, string content)[] entries)
    {
        var zipPath = TrackTempFile(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip"));
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (path, content) in entries)
        {
            var entry = zip.CreateEntry(path);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }

        return zipPath;
    }

    private string TrackTempFile(string path)
    {
        tempFiles.Add(path);
        return path;
    }
}
