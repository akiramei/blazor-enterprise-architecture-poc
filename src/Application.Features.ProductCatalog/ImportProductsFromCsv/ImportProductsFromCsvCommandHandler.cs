using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Shared.Application;
using Shared.Application.Common;
using Application.Core.Commands;
using Shared.Kernel;
using Domain.ProductCatalog.Products;

namespace Application.Features.ProductCatalog.ImportProductsFromCsv;

/// <summary>
/// 商品CSV インポートハンドラ
///
/// 【パターン: CSVインポート - Handler + CommandPipeline】
///
/// 使用シナリオ:
/// - CSVファイルから商品データを一括登録
/// - バリデーションエラーを行単位で報告
/// - 部分成功/失敗を追跡してユーザーにフィードバック
///
/// 実装ガイド:
/// - CsvHelper でCSVをパース
/// - 行ごとにProduct.Createで作成
/// - ドメインエラーはキャッチして記録
/// - 成功した商品のみをリポジトリに保存
///
/// AI実装時の注意:
/// - ヘッダー行の検証（必須カラムチェック）
/// - 各行のバリデーション（必須項目、型変換エラー）
/// - エラーメッセージには行番号を含める
/// - 最大インポート件数（1,000件）を超えたらエラー
/// - using でStreamを確実にクローズ
/// </summary>
public class ImportProductsFromCsvCommandHandler
    : CommandPipeline<ImportProductsFromCsvCommand, BulkOperationResult>
{
    private readonly IProductRepository _repository;
    private const int MaxImportCount = 1000;

    public ImportProductsFromCsvCommandHandler(
        IProductRepository repository)
    {
        _repository = repository;
    }

    protected override async Task<Result<BulkOperationResult>> ExecuteAsync(
        ImportProductsFromCsvCommand command,
        CancellationToken ct)
    {

        var products = new List<Product>();
        var errors = new List<string>();
        var lineNumber = 1; // ヘッダー行

        using var reader = new StreamReader(command.CsvStream, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.GetCultureInfo("ja-JP"))
        {
            HasHeaderRecord = true,
            MissingFieldFound = null, // 欠損フィールドを許容
        });

        // CSVレコードを読み込み
        var records = csv.GetRecords<ProductCsvImportDto>().ToList();

        if (records.Count == 0)
        {
            return Result.Fail<BulkOperationResult>("CSVファイルにデータがありません");
        }

        if (records.Count > MaxImportCount)
        {
            return Result.Fail<BulkOperationResult>($"インポート件数が上限({MaxImportCount}件)を超えています");
        }

        // 各行を処理
        foreach (var record in records)
        {
            lineNumber++;

            try
            {
                // バリデーション
                if (string.IsNullOrWhiteSpace(record.Name))
                {
                    errors.Add($"行{lineNumber}: 商品名は必須です");
                    continue;
                }

                if (record.Price <= 0)
                {
                    errors.Add($"行{lineNumber}: 価格は0より大きい値を指定してください");
                    continue;
                }

                if (record.Stock < 0)
                {
                    errors.Add($"行{lineNumber}: 在庫数は0以上の値を指定してください");
                    continue;
                }

                var currency = string.IsNullOrWhiteSpace(record.Currency) ? "JPY" : record.Currency;

                // Productエンティティを作成
                var product = Product.Create(
                    record.Name,
                    record.Description ?? string.Empty,
                    new Money(record.Price, currency),
                    record.Stock);

                products.Add(product);
            }
            catch (Exception ex)
            {
                errors.Add($"行{lineNumber}: {ex.Message}");
            }
        }

        // 成功した商品を保存
        var succeededCount = 0;
        var productIndex = lineNumber - products.Count; // 保存失敗時の行番号計算用

        foreach (var product in products)
        {
            productIndex++;
            try
            {
                await _repository.SaveAsync(product, ct);
                succeededCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"行{productIndex}: {ex.Message}");
            }
        }

        return Result.Success(BulkOperationResult.PartiallySucceeded(
            succeededCount,
            errors.Count,
            errors));
    }
}
