using MediatR;
using Shared.Application;
using ProductCatalog.Shared.Domain.Products;

namespace ExportProductsToCsv.Application;

/// <summary>
/// 商品一覧CSV エクスポートクエリ
///
/// 【パターン: CSVエクスポート - Query】
///
/// 使用シナリオ:
/// - 商品一覧をCSV形式でダウンロード
/// - 検索条件に一致する商品のみをエクスポート
/// - 大量データのストリーム処理
///
/// 実装ガイド:
/// - 検索条件は SearchProductsQuery と同じパラメータを受け取る
/// - 戻り値は byte[] (CSVファイルのバイナリデータ)
/// - ページングは不要（全件エクスポート、ただし上限を設ける）
///
/// AI実装時の注意:
/// - CsvHelper ライブラリを使用してCSV生成
/// - MemoryStream を使用してインメモリでCSV生成
/// - エンコーディングは UTF-8 with BOM（Excel対応）
/// - ヘッダー行は日本語で出力
/// - エクスポート上限（例: 10,000件）を設定してメモリ枯渇を防ぐ
/// </summary>
public record ExportProductsToCsvQuery(
    string? NameFilter,
    decimal? MinPrice,
    decimal? MaxPrice,
    ProductStatus? Status
) : IRequest<Result<byte[]>>;
