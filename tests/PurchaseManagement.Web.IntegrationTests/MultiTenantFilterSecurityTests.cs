using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PurchaseManagement.Infrastructure.Persistence;
using PurchaseManagement.Shared.Domain.PurchaseRequests;
using Shared.Application.Interfaces;
using Shared.Kernel;

namespace PurchaseManagement.Web.IntegrationTests;

/// <summary>
/// マルチテナントフィルタのセキュリティ回帰テスト
///
/// 【SECURITY REGRESSION TEST】
///
/// TenantId が null のコンテキストで、グローバルクエリフィルタが
/// 全データをブロックすることを確認する。
///
/// 以前の脆弱性:
///   _appContext.TenantId == null || e.TenantId == _appContext.TenantId.Value
///   → null の場合に全テナントのデータが見える
///
/// 修正後:
///   _appContext.TenantId.HasValue && e.TenantId == _appContext.TenantId
///   → null の場合に全データをブロック
///
/// 【テスト手法】
/// このテストは Test 環境ではなく Production 環境として DbContext を構築し、
/// 実際の ApplyGlobalFilters() を実行してグローバルクエリフィルタを検証します。
///
/// 【テスト環境の制限】
/// - ✅ EF Core Global Query Filter: SQLite でも正しく動作し、実際のフィルタを検証可能
/// - ⚠️ Dapper Query Handler: PostgreSQL 固有の SQL 構文のため SQLite では実行不可
///
/// 詳細は tests/PurchaseManagement.Web.IntegrationTests/README.md を参照。
/// </summary>
public class MultiTenantFilterSecurityTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public MultiTenantFilterSecurityTests()
    {
        // SQLite In-Memory 接続を作成して開く
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task GlobalQueryFilter_TenantIdがNull_全データをブロックする()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // 1. データを挿入（フィルタなしで）
        await using (var setupContext = CreateDbContext(tenantA))
        {
            await setupContext.Database.EnsureCreatedAsync();

            var requestA = PurchaseRequest.Create(
                Guid.NewGuid(),
                "User A",
                "Request from Tenant A",
                "Description A",
                tenantA
            );

            var requestB = PurchaseRequest.Create(
                Guid.NewGuid(),
                "User B",
                "Request from Tenant B",
                "Description B",
                tenantB
            );

            setupContext.PurchaseRequests.Add(requestA);
            setupContext.PurchaseRequests.Add(requestB);
            await setupContext.SaveChangesAsync();
        }

        // 2. TenantId が null のコンテキストで実際のグローバルクエリフィルタを検証
        await using (var nullTenantContext = CreateDbContext(null))
        {
            // Act: 実際の DbContext の GlobalQueryFilter が適用された状態でクエリ
            var results = await nullTenantContext.PurchaseRequests.ToListAsync();

            // Assert: TenantId が null の場合、全データがブロックされる
            results.Should().BeEmpty(
                "TenantId が null の場合、GlobalQueryFilter により全テナントのデータをブロックすることで " +
                "未認証ユーザーや設定ミスでのデータ漏洩を防ぐ（修正後の挙動）"
            );
        }

        // 3. 修正前の脆弱なロジック（|| 演算子）を検証するための比較テスト
        // 注: IgnoreQueryFilters() で GlobalQueryFilter を無効化して手動でフィルタリング
        await using (var compareContext = CreateDbContext(null))
        {
            Guid? nullTenantId = null;

            // 修正前の脆弱なロジック: nullTenantId == null || e.TenantId == nullTenantId.Value
            var vulnerableResults = await compareContext.PurchaseRequests
                .IgnoreQueryFilters() // GlobalQueryFilter を無効化
                .Where(e => nullTenantId == null || e.TenantId == nullTenantId.Value)
                .ToListAsync();

            // 修正前は全データが見えてしまう（脆弱性の証明）
            vulnerableResults.Should().HaveCount(2,
                "修正前の脆弱なロジック（|| 演算子）では null の場合に全データが見える");
        }
    }

    [Fact]
    public async Task GlobalQueryFilter_正しいTenantId_そのテナントのデータのみ取得()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // データを挿入
        await using (var setupContext = CreateDbContext(tenantA))
        {
            await setupContext.Database.EnsureCreatedAsync();

            var requestA = PurchaseRequest.Create(
                Guid.NewGuid(),
                "User A",
                "Request from Tenant A",
                "Description A",
                tenantA
            );

            var requestB = PurchaseRequest.Create(
                Guid.NewGuid(),
                "User B",
                "Request from Tenant B",
                "Description B",
                tenantB
            );

            setupContext.PurchaseRequests.Add(requestA);
            setupContext.PurchaseRequests.Add(requestB);
            await setupContext.SaveChangesAsync();
        }

        // Act: TenantA のコンテキストで実際の GlobalQueryFilter を検証
        await using (var tenantAContext = CreateDbContext(tenantA))
        {
            var resultsA = await tenantAContext.PurchaseRequests.ToListAsync();

            // Assert: TenantA のデータのみ取得
            resultsA.Should().HaveCount(1);
            resultsA[0].TenantId.Should().Be(tenantA);
            resultsA[0].Title.Should().Be("Request from Tenant A");
        }
    }

    [Fact]
    public async Task GlobalQueryFilter_異なるTenant_互いのデータが見えない()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // データを挿入
        await using (var setupContext = CreateDbContext(tenantA))
        {
            await setupContext.Database.EnsureCreatedAsync();

            var requestA = PurchaseRequest.Create(
                Guid.NewGuid(),
                "User A",
                "Request from Tenant A",
                "Description A",
                tenantA
            );

            var requestB1 = PurchaseRequest.Create(
                Guid.NewGuid(),
                "User B1",
                "Request 1 from Tenant B",
                "Description B1",
                tenantB
            );

            var requestB2 = PurchaseRequest.Create(
                Guid.NewGuid(),
                "User B2",
                "Request 2 from Tenant B",
                "Description B2",
                tenantB
            );

            setupContext.PurchaseRequests.AddRange(requestA, requestB1, requestB2);
            await setupContext.SaveChangesAsync();
        }

        // Act: TenantB のコンテキストで実際の GlobalQueryFilter を検証
        await using (var tenantBContext = CreateDbContext(tenantB))
        {
            var resultsB = await tenantBContext.PurchaseRequests.ToListAsync();

            // Assert: TenantB のデータ（2件）のみ取得、TenantA のデータは見えない
            resultsB.Should().HaveCount(2);
            resultsB.Should().AllSatisfy(r => r.TenantId.Should().Be(tenantB));
            resultsB.Should().NotContain(r => r.Title == "Request from Tenant A");
        }
    }

    [Fact]
    public async Task GlobalQueryFilter_IgnoreQueryFilters_全データ取得可能()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // データを挿入
        await using (var setupContext = CreateDbContext(tenantA))
        {
            await setupContext.Database.EnsureCreatedAsync();

            var requestA = PurchaseRequest.Create(
                Guid.NewGuid(),
                "User A",
                "Request from Tenant A",
                "Description A",
                tenantA
            );

            var requestB = PurchaseRequest.Create(
                Guid.NewGuid(),
                "User B",
                "Request from Tenant B",
                "Description B",
                tenantB
            );

            setupContext.PurchaseRequests.Add(requestA);
            setupContext.PurchaseRequests.Add(requestB);
            await setupContext.SaveChangesAsync();
        }

        // Act: IgnoreQueryFilters() で GlobalQueryFilter を無効化
        await using (var tenantAContext = CreateDbContext(tenantA))
        {
            var allResults = await tenantAContext.PurchaseRequests
                .IgnoreQueryFilters()
                .ToListAsync();

            // Assert: 全テナントのデータが取得できる
            allResults.Should().HaveCount(2,
                "IgnoreQueryFilters() により GlobalQueryFilter を無効化すると全データが取得可能");
            allResults.Should().Contain(r => r.TenantId == tenantA);
            allResults.Should().Contain(r => r.TenantId == tenantB);
        }
    }

    /// <summary>
    /// 実際の GlobalQueryFilter が適用される Production モードの DbContext を作成
    /// </summary>
    private PurchaseManagementDbContext CreateDbContext(Guid? tenantId)
    {
        var options = new DbContextOptionsBuilder<PurchaseManagementDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Production 環境として DbContext を構築（GlobalQueryFilter が有効）
        var appContext = new TestAppContext(tenantId);
        var environment = new TestWebHostEnvironment { EnvironmentName = "Production" };

        return new PurchaseManagementDbContext(options, appContext, environment);
    }

    /// <summary>
    /// テスト用 IAppContext 実装
    /// </summary>
    private class TestAppContext : IAppContext
    {
        public TestAppContext(Guid? tenantId)
        {
            TenantId = tenantId;
        }

        public Guid UserId => Guid.NewGuid();
        public string UserName => "Test User";
        public Guid? TenantId { get; }
        public bool IsAuthenticated => TenantId.HasValue;
        public System.Security.Claims.ClaimsPrincipal User => new();
        public string CorrelationId => "test";
        public Guid RequestId => Guid.NewGuid();
        public DateTime RequestStartTimeUtc => DateTime.UtcNow;
        public string RequestPath => "/test";
        public string HttpMethod => "GET";
        public string? ClientIpAddress => null;
        public string? UserAgent => null;
        public string EnvironmentName => "Production";
        public string HostName => "test";
        public string ApplicationName => "test";
        public bool IsInRole(string role) => false;
        public bool IsInAnyRole(params string[] roles) => false;
    }

    /// <summary>
    /// テスト用 IWebHostEnvironment 実装
    /// </summary>
    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "Test";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Production";
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
