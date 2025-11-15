using Shared.Application;
using Shared.Application.Common;
using Shared.Application.Interfaces;

namespace Application.Features.ImportProductsFromCsv;

/// <summary>
/// 商品CSV インポートコマンド
///
/// 【パターン: CSVインポート - Command】
///
/// 使用シナリオ:
/// - CSVファイルから商品データを一括登録
/// - エラー行を特定して報告
/// - 部分成功/失敗の追跡
///
/// 実装ガイド:
/// - CSVファイルはStreamで受け取る
/// - 行ごとにバリデーションを実施
/// - エラーが発生した行は記録するが、他の行は処理継続
/// - トランザクションは行わない（各行独立）
///
/// AI実装時の注意:
/// - ファイルサイズ上限をチェック（例: 10MB）
/// - CSVヘッダー行の検証
/// - 文字エンコーディングはUTF-8 with BOMを想定
/// - メモリ効率のためストリーム処理を使用
/// - 最大インポート件数制限（例: 1,000件）
/// </summary>
public record ImportProductsFromCsvCommand : ICommand<Result<BulkOperationResult>>
{
    public Stream CsvStream { get; init; } = Stream.Null;
}

/// <summary>
/// CSVインポート用DTO
/// </summary>
public class ProductCsvImportDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public int Stock { get; set; }
}
