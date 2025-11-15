# ドメイン独立性リファクタリング - 移行スクリプト（PowerShell版）
# 目的: src/Domain/ を中心とした正しいアーキテクチャ構造に移行する

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "ドメイン独立性リファクタリング - 移行開始" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# フェーズ1は完了済み
Write-Host "`n✅ フェーズ1: Domain の分離（完了済み）" -ForegroundColor Green

# フェーズ2: Application層の整理
Write-Host "`n⏳ フェーズ2: Application層の整理" -ForegroundColor Yellow
Write-Host "-----------------------------------" -ForegroundColor Yellow

Write-Host "⚠️  Visual Studio を閉じてください" -ForegroundColor Red
Write-Host "⚠️  ビルド成果物を削除します..." -ForegroundColor Yellow

# bin/obj を削除
Get-ChildItem -Path src -Recurse -Directory -Filter "bin" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path src -Recurse -Directory -Filter "obj" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Application/ ディレクトリを作成..." -ForegroundColor White
New-Item -ItemType Directory -Path "src\Application\PurchaseManagement" -Force | Out-Null
New-Item -ItemType Directory -Path "src\Application\ProductCatalog" -Force | Out-Null

Write-Host "Features を移動..." -ForegroundColor White
if (Test-Path "src\PurchaseManagement\Features") {
    try {
        git mv "src/PurchaseManagement/Features" "src/Application/PurchaseManagement/"
    } catch {
        Copy-Item -Path "src\PurchaseManagement\Features" -Destination "src\Application\PurchaseManagement\" -Recurse -Force
    }
}

if (Test-Path "src\ProductCatalog\Features") {
    try {
        git mv "src/ProductCatalog/Features" "src/Application/ProductCatalog/"
    } catch {
        Copy-Item -Path "src\ProductCatalog\Features" -Destination "src\Application\ProductCatalog\" -Recurse -Force
    }
}

Write-Host "Shared/Application を移動..." -ForegroundColor White
if (Test-Path "src\PurchaseManagement\Shared\Application") {
    try {
        git mv "src/PurchaseManagement/Shared/Application" "src/Application/PurchaseManagement/Shared"
    } catch {
        New-Item -ItemType Directory -Path "src\Application\PurchaseManagement\Shared" -Force | Out-Null
        Copy-Item -Path "src\PurchaseManagement\Shared\Application" -Destination "src\Application\PurchaseManagement\Shared\" -Recurse -Force
    }
}

Write-Host "Shared/UI を移動..." -ForegroundColor White
if (Test-Path "src\PurchaseManagement\Shared\UI") {
    try {
        git mv "src/PurchaseManagement/Shared/UI" "src/Application/PurchaseManagement/Shared/UI"
    } catch {
        Copy-Item -Path "src\PurchaseManagement\Shared\UI" -Destination "src\Application\PurchaseManagement\Shared\UI" -Recurse -Force
    }
}

Write-Host "Application の名前空間を更新..." -ForegroundColor White
Get-ChildItem -Path "src\Application\PurchaseManagement" -Filter "*.cs" -Recurse | ForEach-Object {
    (Get-Content $_.FullName) -replace 'namespace PurchaseManagement\.Features', 'namespace Application.PurchaseManagement.Features' | Set-Content $_.FullName
    (Get-Content $_.FullName) -replace 'namespace PurchaseManagement\.Shared\.Application', 'namespace Application.PurchaseManagement.Shared' | Set-Content $_.FullName
    (Get-Content $_.FullName) -replace 'namespace PurchaseManagement\.Shared\.UI', 'namespace Application.PurchaseManagement.Shared.UI' | Set-Content $_.FullName
}

Write-Host "Application の using ステートメントを更新..." -ForegroundColor White
Get-ChildItem -Path "src\Application\PurchaseManagement" -Filter "*.cs" -Recurse | ForEach-Object {
    (Get-Content $_.FullName) -replace 'using PurchaseManagement\.Shared\.Domain', 'using Domain.PurchaseManagement' | Set-Content $_.FullName
    (Get-Content $_.FullName) -replace 'using PurchaseManagement\.Shared\.Application', 'using Application.PurchaseManagement.Shared' | Set-Content $_.FullName
}

Write-Host "✅ フェーズ2完了: Application層の整理" -ForegroundColor Green

# フェーズ3: Infrastructure層の整理
Write-Host "`n⏳ フェーズ3: Infrastructure層の整理" -ForegroundColor Yellow
Write-Host "-----------------------------------" -ForegroundColor Yellow

Write-Host "Infrastructure/ ディレクトリを作成..." -ForegroundColor White
New-Item -ItemType Directory -Path "src\Infrastructure\PurchaseManagement" -Force | Out-Null
New-Item -ItemType Directory -Path "src\Infrastructure\ProductCatalog" -Force | Out-Null

Write-Host "Infrastructure を移動..." -ForegroundColor White
if (Test-Path "src\PurchaseManagement\Infrastructure") {
    try {
        git mv "src/PurchaseManagement/Infrastructure" "src/Infrastructure/PurchaseManagement/"
    } catch {
        Copy-Item -Path "src\PurchaseManagement\Infrastructure" -Destination "src\Infrastructure\PurchaseManagement\" -Recurse -Force
    }
}

if (Test-Path "src\ProductCatalog\Infrastructure") {
    try {
        git mv "src/ProductCatalog/Infrastructure" "src/Infrastructure/ProductCatalog/"
    } catch {
        Copy-Item -Path "src\ProductCatalog\Infrastructure" -Destination "src\Infrastructure\ProductCatalog\" -Recurse -Force
    }
}

Write-Host "Shared/Infrastructure を移動..." -ForegroundColor White
if (Test-Path "src\PurchaseManagement\Shared\Infrastructure") {
    try {
        git mv "src/PurchaseManagement/Shared/Infrastructure" "src/Infrastructure/PurchaseManagement/Shared"
    } catch {
        New-Item -ItemType Directory -Path "src\Infrastructure\PurchaseManagement\Shared" -Force | Out-Null
        Copy-Item -Path "src\PurchaseManagement\Shared\Infrastructure" -Destination "src\Infrastructure\PurchaseManagement\Shared\" -Recurse -Force
    }
}

Write-Host "Infrastructure の名前空間を更新..." -ForegroundColor White
Get-ChildItem -Path "src\Infrastructure\PurchaseManagement" -Filter "*.cs" -Recurse | ForEach-Object {
    (Get-Content $_.FullName) -replace 'namespace PurchaseManagement\.Infrastructure', 'namespace Infrastructure.PurchaseManagement' | Set-Content $_.FullName
    (Get-Content $_.FullName) -replace 'namespace PurchaseManagement\.Shared\.Infrastructure', 'namespace Infrastructure.PurchaseManagement.Shared' | Set-Content $_.FullName
}

Write-Host "Infrastructure の using ステートメントを更新..." -ForegroundColor White
Get-ChildItem -Path "src\Infrastructure\PurchaseManagement" -Filter "*.cs" -Recurse | ForEach-Object {
    (Get-Content $_.FullName) -replace 'using PurchaseManagement\.Shared\.Domain', 'using Domain.PurchaseManagement' | Set-Content $_.FullName
    (Get-Content $_.FullName) -replace 'using PurchaseManagement\.Shared\.Application', 'using Application.PurchaseManagement.Shared' | Set-Content $_.FullName
    (Get-Content $_.FullName) -replace 'using PurchaseManagement\.Infrastructure', 'using Infrastructure.PurchaseManagement' | Set-Content $_.FullName
}

Write-Host "✅ フェーズ3完了: Infrastructure層の整理" -ForegroundColor Green

# フェーズ4: テストプロジェクトの更新
Write-Host "`n⏳ フェーズ4: テストプロジェクトの更新" -ForegroundColor Yellow
Write-Host "-----------------------------------" -ForegroundColor Yellow

Write-Host "テストの名前空間を更新..." -ForegroundColor White
Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse | ForEach-Object {
    (Get-Content $_.FullName) -replace 'using PurchaseManagement\.Shared\.Domain', 'using Domain.PurchaseManagement' | Set-Content $_.FullName
    (Get-Content $_.FullName) -replace 'using ProductCatalog\.Shared\.Domain', 'using Domain.ProductCatalog' | Set-Content $_.FullName
}

Write-Host "✅ フェーズ4完了: テストプロジェクトの更新" -ForegroundColor Green

# 完了メッセージ
Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "✅ 移行スクリプト完了" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "`n次のステップ:" -ForegroundColor Yellow
Write-Host "1. Visual Studio を開く" -ForegroundColor White
Write-Host "2. プロジェクト参照を手動で更新" -ForegroundColor White
Write-Host "3. ソリューションファイルを更新" -ForegroundColor White
Write-Host "4. ビルドを実行: dotnet build" -ForegroundColor White
Write-Host "5. テストを実行: dotnet test" -ForegroundColor White
Write-Host "`n詳細は docs\REFACTORING-NEXT-STEPS.md を参照してください" -ForegroundColor Cyan
