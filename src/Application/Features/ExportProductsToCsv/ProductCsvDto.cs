using CsvHelper.Configuration.Attributes;

namespace Application.Features.ExportProductsToCsv;

/// <summary>
/// 商品CSV出力用DTO
///
/// 【パターン: CSV用DTO】
///
/// 使用シナリオ:
/// - CsvHelper でのCSV出力用データ構造
/// - Excel で開くことを想定した列名（日本語）
///
/// 実装ガイド:
/// - Name 属性でCSVヘッダー名を日本語で指定
/// - Index 属性で列順を制御
/// - プロパティ型は string, decimal, int など基本型
///
/// AI実装時の注意:
/// - CsvHelper の属性を活用してヘッダー名と列順を制御
/// - 日本語ヘッダーにすることでExcelでの可読性向上
/// - 価格はフォーマット済み文字列ではなく数値で出力（Excelで計算可能にする）
/// - ステータスは日本語に変換
/// </summary>
public class ProductCsvDto
{
    /// <summary>
    /// 商品ID
    /// </summary>
    [Index(0)]
    [Name("商品ID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 商品名
    /// </summary>
    [Index(1)]
    [Name("商品名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 説明
    /// </summary>
    [Index(2)]
    [Name("説明")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 価格
    /// </summary>
    [Index(3)]
    [Name("価格")]
    public decimal Price { get; set; }

    /// <summary>
    /// 通貨
    /// </summary>
    [Index(4)]
    [Name("通貨")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// 在庫数
    /// </summary>
    [Index(5)]
    [Name("在庫数")]
    public int Stock { get; set; }

    /// <summary>
    /// ステータス
    /// </summary>
    [Index(6)]
    [Name("ステータス")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// バージョン
    /// </summary>
    [Index(7)]
    [Name("バージョン")]
    public int Version { get; set; }
}
