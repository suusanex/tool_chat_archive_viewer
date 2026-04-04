namespace ChatArchiveViewer.CloudFetch.Models;

/// <summary>
/// SHA-256 検証の結果を保持します。
/// </summary>
/// <param name="Matched">期待値と一致した場合は true。</param>
/// <param name="ActualSha256">ファイルから計算した実ハッシュ値（小文字16進数）。</param>
public record HashVerifyResult(bool Matched, string ActualSha256);
