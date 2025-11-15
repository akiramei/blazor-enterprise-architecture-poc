#!/bin/bash

# ドメイン独立性リファクタリング - 移行スクリプト
# 目的: src/Domain/ を中心とした正しいアーキテクチャ構造に移行する

set -e  # エラーで停止

echo "========================================="
echo "ドメイン独立性リファクタリング - 移行開始"
echo "========================================="

# フェーズ1は完了済み
echo "✅ フェーズ1: Domain の分離（完了済み）"

# フェーズ2: Application層の整理
echo ""
echo "⏳ フェーズ2: Application層の整理"
echo "-----------------------------------"

# Visual Studioを閉じるよう警告
echo "⚠️  Visual Studio を閉じてください"
echo "⚠️  ビルド成果物を削除します..."
find src -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find src -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

echo "Application/ ディレクトリを作成..."
mkdir -p src/Application/PurchaseManagement
mkdir -p src/Application/ProductCatalog

echo "Features を移動..."
if [ -d "src/PurchaseManagement/Features" ]; then
  git mv src/PurchaseManagement/Features src/Application/PurchaseManagement/ || \
  cp -r src/PurchaseManagement/Features src/Application/PurchaseManagement/
fi

if [ -d "src/ProductCatalog/Features" ]; then
  git mv src/ProductCatalog/Features src/Application/ProductCatalog/ || \
  cp -r src/ProductCatalog/Features src/Application/ProductCatalog/
fi

echo "Shared/Application を移動..."
if [ -d "src/PurchaseManagement/Shared/Application" ]; then
  git mv src/PurchaseManagement/Shared/Application src/Application/PurchaseManagement/Shared || \
  cp -r src/PurchaseManagement/Shared/Application src/Application/PurchaseManagement/Shared
fi

echo "Shared/UI を移動..."
if [ -d "src/PurchaseManagement/Shared/UI" ]; then
  git mv src/PurchaseManagement/Shared/UI src/Application/PurchaseManagement/Shared/UI || \
  cp -r src/PurchaseManagement/Shared/UI src/Application/PurchaseManagement/Shared/UI
fi

echo "Application の名前空間を更新..."
find src/Application/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/namespace PurchaseManagement\.Features/namespace Application.PurchaseManagement.Features/g' {} \;

find src/Application/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/namespace PurchaseManagement\.Shared\.Application/namespace Application.PurchaseManagement.Shared/g' {} \;

find src/Application/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/namespace PurchaseManagement\.Shared\.UI/namespace Application.PurchaseManagement.Shared.UI/g' {} \;

echo "Application の using ステートメントを更新..."
find src/Application/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/using PurchaseManagement\.Shared\.Domain/using Domain.PurchaseManagement/g' {} \;

find src/Application/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/using PurchaseManagement\.Shared\.Application/using Application.PurchaseManagement.Shared/g' {} \;

echo "✅ フェーズ2完了: Application層の整理"

# フェーズ3: Infrastructure層の整理
echo ""
echo "⏳ フェーズ3: Infrastructure層の整理"
echo "-----------------------------------"

echo "Infrastructure/ ディレクトリを作成..."
mkdir -p src/Infrastructure/PurchaseManagement
mkdir -p src/Infrastructure/ProductCatalog

echo "Infrastructure を移動..."
if [ -d "src/PurchaseManagement/Infrastructure" ]; then
  git mv src/PurchaseManagement/Infrastructure src/Infrastructure/PurchaseManagement/ || \
  cp -r src/PurchaseManagement/Infrastructure src/Infrastructure/PurchaseManagement/
fi

if [ -d "src/ProductCatalog/Infrastructure" ]; then
  git mv src/ProductCatalog/Infrastructure src/Infrastructure/ProductCatalog/ || \
  cp -r src/ProductCatalog/Infrastructure src/Infrastructure/ProductCatalog/
fi

echo "Shared/Infrastructure を移動..."
if [ -d "src/PurchaseManagement/Shared/Infrastructure" ]; then
  git mv src/PurchaseManagement/Shared/Infrastructure src/Infrastructure/PurchaseManagement/Shared || \
  cp -r src/PurchaseManagement/Shared/Infrastructure src/Infrastructure/PurchaseManagement/Shared
fi

echo "Infrastructure の名前空間を更新..."
find src/Infrastructure/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/namespace PurchaseManagement\.Infrastructure/namespace Infrastructure.PurchaseManagement/g' {} \;

find src/Infrastructure/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/namespace PurchaseManagement\.Shared\.Infrastructure/namespace Infrastructure.PurchaseManagement.Shared/g' {} \;

echo "Infrastructure の using ステートメントを更新..."
find src/Infrastructure/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/using PurchaseManagement\.Shared\.Domain/using Domain.PurchaseManagement/g' {} \;

find src/Infrastructure/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/using PurchaseManagement\.Shared\.Application/using Application.PurchaseManagement.Shared/g' {} \;

find src/Infrastructure/PurchaseManagement -name "*.cs" -type f -exec sed -i \
  's/using PurchaseManagement\.Infrastructure/using Infrastructure.PurchaseManagement/g' {} \;

echo "✅ フェーズ3完了: Infrastructure層の整理"

# フェーズ4: テストプロジェクトの更新
echo ""
echo "⏳ フェーズ4: テストプロジェクトの更新"
echo "-----------------------------------"

echo "テストの名前空間を更新..."
find tests -name "*.cs" -type f -exec sed -i \
  's/using PurchaseManagement\.Shared\.Domain/using Domain.PurchaseManagement/g' {} \;

find tests -name "*.cs" -type f -exec sed -i \
  's/using ProductCatalog\.Shared\.Domain/using Domain.ProductCatalog/g' {} \;

echo "✅ フェーズ4完了: テストプロジェクトの更新"

# 完了メッセージ
echo ""
echo "========================================="
echo "✅ 移行スクリプト完了"
echo "========================================="
echo ""
echo "次のステップ:"
echo "1. Visual Studio を開く"
echo "2. プロジェクト参照を手動で更新"
echo "3. ソリューションファイルを更新"
echo "4. ビルドを実行: dotnet build"
echo "5. テストを実行: dotnet test"
echo ""
echo "詳細は docs/REFACTORING-NEXT-STEPS.md を参照してください"
