using ChatArchiveViewer.CloudFetch.Services;

namespace ChatArchiveViewer.CloudFetch.Tests.Services;

[TestFixture]
public sealed class Sha256VerifierTests
{
    // TP-C007a: ハッシュ一致は true
    [Test]
    public async Task UT_IT_TP_C007a__VerifyAsync_HashMatches_ReturnsTrue()
    {
        var filePath = CreateTempFile("hello");
        try
        {
            var sut = new Sha256Verifier();

            var result = await sut.VerifyAsync(filePath, "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824", CancellationToken.None);

            Assert.That(result.Matched, Is.True);
            Assert.That(result.ActualSha256, Is.EqualTo("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824"));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    // TP-C007b: ハッシュ不一致は false
    [Test]
    public async Task UT_IT_TP_C007b__VerifyAsync_HashMismatch_ReturnsFalse()
    {
        var filePath = CreateTempFile("hello");
        try
        {
            var sut = new Sha256Verifier();

            var result = await sut.VerifyAsync(filePath, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", CancellationToken.None);

            Assert.That(result.Matched, Is.False);
            Assert.That(result.ActualSha256, Is.EqualTo("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824"));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    // TP-C007c: 対象ファイルが存在しない場合は FileNotFoundException
    [Test]
    public void UT_IT_TP_C007c__VerifyAsync_MissingFile_ThrowsFileNotFoundException()
    {
        var sut = new Sha256Verifier();
        var filePath = Path.Combine(Path.GetTempPath(), $"sha256-missing-{Guid.NewGuid():N}.txt");

        Assert.ThrowsAsync<FileNotFoundException>(
            () => sut.VerifyAsync(filePath, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", CancellationToken.None));
    }

    // TP-C007d: 0 バイトファイルのハッシュも正しく判定される
    [Test]
    public async Task UT_IT_TP_C007d__VerifyAsync_EmptyFileHash_ReturnsTrue()
    {
        var filePath = CreateTempFile(string.Empty);
        try
        {
            var sut = new Sha256Verifier();

            var result = await sut.VerifyAsync(filePath, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", CancellationToken.None);

            Assert.That(result.Matched, Is.True);
            Assert.That(result.ActualSha256, Is.EqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sha256-tests-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, content);
        return path;
    }
}
