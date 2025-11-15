using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Application;
using Shared.Application.Interfaces;
using ProductCatalog.Shared.Application;
using Domain.ProductCatalog.Products;

namespace ExportProductsToCsv.Application;

/// <summary>
/// 商品CSV エクスポートハンドラ
///
/// 【パターン: CSVエクスポート - Handler】
///
/// 使用シナリオ:
/// - 検索条件に一致する商品をCSV形式で出力
/// - Excel で開くことを想定した UTF-8 with BOM エンコーディング
/// - メモリ枯渇を防ぐため上限件数を設定
///
/// 実装ガイド:
/// - IProductReadRepository で商品データを取得
/// - CsvHelper でCSVを生成
/// - MemoryStream を使用してインメモリでCSV生成
/// - UTF-8 with BOM でエンコード（Excel対応）
///
/// AI実装時の注意:
/// - エクスポート上限（10,000件）を超える場合はエラー
/// - ステータスは ProductStatus enum から日本語文字列に変換
/// - CSV設定: ヘッダーあり、カルチャは日本語
/// - MemoryStream は using で確実に解放
/// </summary>
public class ExportProductsToCsvHandler : IRequestHandler<ExportProductsToCsvQuery, Result<byte[]>>
{
    private readonly IProductReadRepository _readRepository;
    private readonly ILogger<ExportProductsToCsvHandler> _logger;
    private const int MaxExportCount = 10000;

    public ExportProductsToCsvHandler(
        IProductReadRepository readRepository,
        ILogger<ExportProductsToCsvHandler> logger)
    {
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<Result<byte[]>> Handle(ExportProductsToCsvQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("商品CSVエクスポート開始: NameFilter={NameFilter}, MinPrice={MinPrice}, MaxPrice={MaxPrice}, Status={Status}",
                request.NameFilter, request.MinPrice, request.MaxPrice, request.Status);

            // 全件取得（ページングなし、上限あり）
            // Note: 実際のプロダクションでは、ページングを使用してストリーム処理することを推奨
            var pagedResult = await _readRepository.SearchAsync(
                request.NameFilter,
                request.MinPrice,
                request.MaxPrice,
                request.Status,
                page: 1,
                pageSize: MaxExportCount + 1, // 上限+1件取得して、上限超過を検出
                orderBy: "Name",
                isDescending: false,
                cancellationToken);

            // 上限超過チェック
            if (pagedResult.TotalCount > MaxExportCount)
            {
                _logger.LogWarning("エクスポート件数が上限を超えています: {TotalCount} > {MaxExportCount}",
                    pagedResult.TotalCount, MaxExportCount);
                return Result.Fail<byte[]>($"エクスポート件数が上限({MaxExportCount}件)を超えています。検索条件を絞り込んでください。");
            }

            // DTO変換
            var csvData = pagedResult.Items.Select(p => new ProductCsvDto
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Currency = p.Currency,
                Stock = p.Stock,
                Status = MapStatusToJapanese(p.Status),
                Version = p.Version
            }).ToList();

            // CSV生成
            var csvBytes = GenerateCsv(csvData);

            _logger.LogInformation("商品CSVエクスポート完了: {Count}件", csvData.Count);

            return Result.Success(csvBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "商品CSVエクスポート中にエラーが発生しました");
            return Result.Fail<byte[]>("CSV エクスポート中にエラーが発生しました");
        }
    }

    /// <summary>
    /// CSV生成
    /// </summary>
    private byte[] GenerateCsv(List<ProductCsvDto> data)
    {
        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream, new UTF8Encoding(true)); // UTF-8 with BOM
        using var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.GetCultureInfo("ja-JP"))
        {
            HasHeaderRecord = true, // ヘッダー行を出力
        });

        csvWriter.WriteRecords(data);
        streamWriter.Flush();

        return memoryStream.ToArray();
    }

    /// <summary>
    /// ステータスを日本語に変換
    /// </summary>
    private string MapStatusToJapanese(string status)
    {
        return status switch
        {
            "Draft" => "下書き",
            "Published" => "公開中",
            "Archived" => "アーカイブ済み",
            _ => status
        };
    }
}
