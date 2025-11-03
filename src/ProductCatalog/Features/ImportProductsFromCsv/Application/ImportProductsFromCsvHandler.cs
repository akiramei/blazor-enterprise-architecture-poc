using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductCatalog.Application.Common;
using ProductCatalog.Domain.Products;

namespace ProductCatalog.Application.Features.Products.ImportProductsFromCsv;

/// <summary>
/// 商品CSV インポートハンドラ
///
/// 【パターン: CSVインポート - Handler】
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
public class ImportProductsFromCsvHandler : IRequestHandler<ImportProductsFromCsvCommand, Result<BulkOperationResult>>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ImportProductsFromCsvHandler> _logger;
    private const int MaxImportCount = 1000;

    public ImportProductsFromCsvHandler(
        IProductRepository repository,
        ILogger<ImportProductsFromCsvHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<BulkOperationResult>> Handle(ImportProductsFromCsvCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("商品CSVインポート開始");

            var products = new List<Product>();
            var errors = new List<string>();
            var lineNumber = 1; // ヘッダー行

            using var reader = new StreamReader(request.CsvStream, Encoding.UTF8);
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
                    _logger.LogWarning(ex, "行{LineNumber}の処理中にエラーが発生しました", lineNumber);
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
                    await _repository.SaveAsync(product, cancellationToken);
                    succeededCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "行{LineNumber}の保存中にエラーが発生しました", productIndex);
                    errors.Add($"行{productIndex}: {ex.Message}");
                }
            }

            _logger.LogInformation("商品CSVインポート完了: 成功{SuccessCount}件、失敗{FailCount}件",
                succeededCount, errors.Count);

            return Result.Success(new BulkOperationResult(
                succeededCount: succeededCount,
                failedCount: errors.Count,
                errors: errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品CSVインポート中にエラーが発生しました");
            return Result.Fail<BulkOperationResult>("CSV インポート中にエラーが発生しました");
        }
    }
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
