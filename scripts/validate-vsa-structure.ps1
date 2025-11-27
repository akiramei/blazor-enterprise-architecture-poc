# VSA Structure Validation Script (PowerShell)
# Purpose: Detect Clean Architecture / Layered Architecture patterns
# Expected: Vertical Slice Architecture only

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "VSA Structure Validation" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

$Errors = 0
$Warnings = 0

# Check 1: src/ direct children should NOT have layer names
Write-Host "[Check 1] Checking for layer-based projects in src/..." -ForegroundColor White
$LayerProjects = Get-ChildItem -Path "src" -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match "\.(Application|Domain|Infrastructure|Web|UI|Core|Api)$" }

if ($LayerProjects) {
    Write-Host "❌ FAIL: Layer-based projects found in src/" -ForegroundColor Red
    Write-Host ""
    Write-Host "Found:"
    $LayerProjects | ForEach-Object { Write-Host "  - $($_.Name)" }
    Write-Host ""
    Write-Host "This is Clean Architecture / Layered Architecture, NOT VSA." -ForegroundColor Red
    Write-Host ""
    Write-Host "Expected structure:"
    Write-Host "  src/"
    Write-Host "  └── {BoundedContext}/"
    Write-Host "      └── Features/"
    Write-Host ""
    $Errors++
} else {
    Write-Host "✅ PASS: No layer-based projects in src/" -ForegroundColor Green
}
Write-Host ""

# Check 2: BC (Bounded Context) folder should exist
Write-Host "[Check 2] Checking for Bounded Context folder..." -ForegroundColor White
$BcFolders = Get-ChildItem -Path "src" -Directory -ErrorAction SilentlyContinue

if ($BcFolders.Count -eq 0) {
    Write-Host "❌ FAIL: No Bounded Context folder found in src/" -ForegroundColor Red
    Write-Host ""
    Write-Host "Expected at least one folder like:"
    Write-Host "  src/ProductCatalog/"
    Write-Host "  src/OrderManagement/"
    Write-Host ""
    $Errors++
} else {
    Write-Host "✅ PASS: Bounded Context folder(s) found" -ForegroundColor Green
    $BcFolders | ForEach-Object { Write-Host "  - $($_.FullName)" }
}
Write-Host ""

# Check 3: Features/ folder should exist under BC
Write-Host "[Check 3] Checking for Features/ folder under BC..." -ForegroundColor White
$FeaturesFolders = Get-ChildItem -Path "src\*\Features" -Directory -ErrorAction SilentlyContinue

if (-not $FeaturesFolders) {
    Write-Host "❌ FAIL: No Features/ folder found under Bounded Context" -ForegroundColor Red
    Write-Host ""
    Write-Host "Expected structure:"
    Write-Host "  src/"
    Write-Host "  └── ProductCatalog/"
    Write-Host "      └── Features/  ← This folder is missing"
    Write-Host ""
    $Errors++
} else {
    Write-Host "✅ PASS: Features/ folder found" -ForegroundColor Green
    $FeaturesFolders | ForEach-Object { Write-Host "  - $($_.FullName)" }
}
Write-Host ""

# Check 4: Feature folders should contain layer subfolders
Write-Host "[Check 4] Checking feature slice structure..." -ForegroundColor White
$FeatureSlices = Get-ChildItem -Path "src\*\Features\*" -Directory -ErrorAction SilentlyContinue

if (-not $FeatureSlices) {
    Write-Host "⚠️  WARNING: No feature slices found yet" -ForegroundColor Yellow
    $Warnings++
} else {
    Write-Host "Found feature slices:"
    $FeatureSlices | ForEach-Object { Write-Host "  - $($_.Name)" }
    Write-Host ""

    # Check each feature slice for layer folders
    foreach ($Slice in $FeatureSlices) {
        $SliceName = $Slice.Name
        $LayerFolders = Get-ChildItem -Path $Slice.FullName -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match "^(Application|Domain|Infrastructure|UI)$" }

        if (-not $LayerFolders) {
            Write-Host "⚠️  WARNING: $SliceName has no layer folders" -ForegroundColor Yellow
            Write-Host "    Expected: Application/, Domain/, Infrastructure/, UI/"
            $Warnings++
        } else {
            Write-Host "✅ $SliceName has layer folders" -ForegroundColor Green
        }
    }
}
Write-Host ""

# Check 5: Verify no cross-feature dependencies (future implementation)
Write-Host "[Check 5] Cross-feature dependency check..." -ForegroundColor White
Write-Host "ℹ️  TODO: Implement cross-feature dependency detection" -ForegroundColor Yellow
Write-Host ""

# Check 6: UI Placement Validation
Write-Host "[Check 6] UI Placement Validation..." -ForegroundColor White

# 6a. Features/配下の.razorが他のFeatureを参照していないかチェック
$FeatureRazors = Get-ChildItem -Path "src\*\Features" -Recurse -Filter "*.razor" -ErrorAction SilentlyContinue

if ($FeatureRazors) {
    Write-Host "Found .razor files in Features/:" -ForegroundColor Gray
    foreach ($Razor in $FeatureRazors) {
        $FeatureName = $Razor.Directory.Name
        $Content = Get-Content $Razor.FullName -Raw -ErrorAction SilentlyContinue

        if ($Content) {
            # 他のFeatureを参照しているかチェック
            $OtherFeatures = Get-ChildItem -Path "src\*\Features" -Directory -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -ne $FeatureName }

            foreach ($Other in $OtherFeatures) {
                if ($Content -match "Features\.$($Other.Name)") {
                    Write-Host "⚠️  WARNING: $($Razor.Name) in $FeatureName references $($Other.Name)" -ForegroundColor Yellow
                    Write-Host "    Consider moving to Components/Pages/ (multi-feature page)"
                    $Warnings++
                }
            }
        }
        Write-Host "  ✅ $($Razor.Directory.Name)/$($Razor.Name)" -ForegroundColor Green
    }
} else {
    Write-Host "ℹ️  No .razor files found in Features/ (this may be expected)" -ForegroundColor Gray
}
Write-Host ""

# 6b. Components/Pages/に機能固有ページがないかチェック
Write-Host "[Check 6b] Checking Components/Pages/ for feature-specific pages..." -ForegroundColor White

$CommonPages = @("Home.razor", "Error.razor", "Dashboard.razor", "_Host.razor", "App.razor", "Routes.razor", "NotFound.razor", "Login.razor", "Weather.razor", "Counter.razor")
$PageRazors = Get-ChildItem -Path "src\*\Components\Pages" -Filter "*.razor" -ErrorAction SilentlyContinue

if ($PageRazors) {
    foreach ($Page in $PageRazors) {
        if ($Page.Name -notin $CommonPages) {
            $Content = Get-Content $Page.FullName -Raw -ErrorAction SilentlyContinue

            if ($Content) {
                # 参照しているFeatureをカウント
                $ReferencedFeatures = [regex]::Matches($Content, 'Features\.(\w+)') |
                    ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique

                if ($ReferencedFeatures.Count -eq 1) {
                    Write-Host "⚠️  WARNING: $($Page.Name) references only '$($ReferencedFeatures[0])'" -ForegroundColor Yellow
                    Write-Host "    Should be in Features/$($ReferencedFeatures[0])/ (single-feature page)"
                    $Warnings++
                } elseif ($ReferencedFeatures.Count -eq 0) {
                    # Featureを参照していない場合は問題ない（ナビゲーションページなど）
                    Write-Host "  ✅ $($Page.Name) (no Feature references)" -ForegroundColor Green
                } else {
                    Write-Host "  ✅ $($Page.Name) (multi-feature: $($ReferencedFeatures -join ', '))" -ForegroundColor Green
                }
            }
        } else {
            Write-Host "  ✅ $($Page.Name) (common page)" -ForegroundColor Green
        }
    }
} else {
    Write-Host "ℹ️  No Pages found in Components/Pages/" -ForegroundColor Gray
}
Write-Host ""

# Check 7: UI-Boundary Symmetry Validation
Write-Host "[Check 7] UI-Boundary Symmetry Validation..." -ForegroundColor White
Write-Host ""

$Infos = 0

# 7a. UIがあるのにBoundaryServiceが1つもない場合を検出
$AllRazorInFeatures = Get-ChildItem -Path "src\*\Features" -Recurse -Filter "*.razor" -ErrorAction SilentlyContinue
$AllBoundaryServices = Get-ChildItem -Path "src\*\*\Boundaries" -Recurse -Filter "*BoundaryService.cs" -ErrorAction SilentlyContinue

if ($AllRazorInFeatures -and -not $AllBoundaryServices) {
    Write-Host "[!] WARNING: UI components exist but no BoundaryService found" -ForegroundColor Yellow
    Write-Host "    Found .razor files in Features/ but no *BoundaryService.cs in Boundaries/"
    Write-Host ""
    Write-Host "    When UI exists, Boundary modeling is recommended."
    Write-Host ""
    Write-Host "    References:" -ForegroundColor Gray
    Write-Host "      - Pattern:  catalog/patterns/boundary-pattern.yaml"
    Write-Host "      - Mistakes: catalog/COMMON_MISTAKES.md (Boundary Modeling section)"
    Write-Host "      - Template: catalog/scaffolds/spec-template.yaml"
    Write-Host ""
    $Warnings++
}

# 7b. 各Featureに対して、対応するBoundaryを推測して検索
if ($AllRazorInFeatures) {
    Write-Host "Checking UI-Boundary correspondence..." -ForegroundColor Gray
    Write-Host ""

    # Featureフォルダ名のユニークリストを取得
    $FeatureNames = $AllRazorInFeatures | ForEach-Object { $_.Directory.Name } | Select-Object -Unique

    foreach ($FeatureName in $FeatureNames) {
        # Featureからエンティティ名を推測（Create/Update/Delete/Get/Searchを除去）
        $EntityName = $FeatureName -replace "^(Create|Update|Delete|Get|Search|List|Edit|Add|Remove)", ""

        if ([string]::IsNullOrWhiteSpace($EntityName)) {
            $EntityName = $FeatureName
        }

        # 対応するBoundaryServiceを検索（$AllBoundaryServicesがnullの場合も考慮）
        $MatchingBoundary = $null
        if ($AllBoundaryServices) {
            # パターン1: {Entity}BoundaryService.cs
            # パターン2: I{Entity}Boundary.cs
            $MatchingBoundary = $AllBoundaryServices | Where-Object {
                $_.Name -match "^$($EntityName)BoundaryService\.cs$" -or
                $_.Name -match "^I$($EntityName)Boundary\.cs$"
            }

            # 部分一致も検索（EntityNameを含むBoundaryService）
            if (-not $MatchingBoundary) {
                $MatchingBoundary = $AllBoundaryServices | Where-Object {
                    $_.BaseName -match $EntityName
                }
            }
        }

        if ($MatchingBoundary) {
            $BoundaryName = ($MatchingBoundary | Select-Object -First 1).Name
            Write-Host "  [OK] $FeatureName -> $BoundaryName" -ForegroundColor Green
        } else {
            # Boundaryが見つからない場合は INFO 扱い（警告ではない）
            Write-Host "  [INFO] $FeatureName - No matching BoundaryService found" -ForegroundColor Cyan
            Write-Host "         Inferred entity: $EntityName"
            Write-Host "         Expected: ${EntityName}BoundaryService.cs or I${EntityName}Boundary.cs"
            $Infos++
        }
    }

    if ($Infos -gt 0) {
        Write-Host ""
        Write-Host "  Note: INFO items are suggestions, not errors." -ForegroundColor Gray
        Write-Host "        See: catalog/patterns/boundary-pattern.yaml" -ForegroundColor Gray
    }

    Write-Host ""
}

# 7c. Boundaryがあるのに対応するUIがない場合（逆方向チェック）
if ($AllBoundaryServices -and -not $AllRazorInFeatures) {
    Write-Host "  [INFO] BoundaryService exists but no UI found in Features/" -ForegroundColor Cyan
    Write-Host "         This may be intentional (API-only or batch processing)"
    $Infos++
}

# 7d. Intent定義の存在チェック
$AllIntentFiles = Get-ChildItem -Path "src\*\*\Boundaries" -Recurse -Filter "*Intent.cs" -ErrorAction SilentlyContinue

if ($AllBoundaryServices -and -not $AllIntentFiles) {
    Write-Host "  [INFO] BoundaryService exists but no Intent enum found" -ForegroundColor Cyan
    Write-Host "         Consider defining EntityIntent.cs for explicit intent modeling"
    Write-Host "         See: catalog/patterns/boundary-pattern.yaml"
    $Infos++
}

Write-Host ""

# Summary
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Validation Summary" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "  Errors:   $Errors" -ForegroundColor $(if ($Errors -gt 0) { "Red" } else { "Green" })
Write-Host "  Warnings: $Warnings" -ForegroundColor $(if ($Warnings -gt 0) { "Yellow" } else { "Green" })
Write-Host "  Infos:    $Infos" -ForegroundColor Cyan
Write-Host ""

if ($Errors -gt 0) {
    Write-Host "❌ FAILED with $Errors error(s)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Action required:"
    Write-Host "1. Review the errors above"
    Write-Host "2. Refer to: docs/architecture/VSA-STRICT-RULES.md"
    Write-Host "3. Restructure the project to VSA"
    Write-Host ""
    exit 1
} elseif ($Warnings -gt 0) {
    Write-Host "[!] PASSED with $Warnings warning(s)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Review warnings above and ensure VSA structure is correct."
    if ($Infos -gt 0) {
        Write-Host ""
        Write-Host "[i] $Infos info message(s) about UI-Boundary symmetry." -ForegroundColor Cyan
        Write-Host "    Review if Boundary modeling is needed for your UI components."
        Write-Host ""
        Write-Host "    References:" -ForegroundColor Gray
        Write-Host "      - catalog/patterns/boundary-pattern.yaml"
        Write-Host "      - catalog/COMMON_MISTAKES.md"
        Write-Host "      - catalog/scaffolds/spec-template.yaml"
    }
    Write-Host ""
    exit 0
} else {
    Write-Host "[OK] ALL CHECKS PASSED" -ForegroundColor Green
    if ($Infos -gt 0) {
        Write-Host ""
        Write-Host "[i] $Infos info message(s) about UI-Boundary symmetry." -ForegroundColor Cyan
        Write-Host "    These are suggestions, not errors."
        Write-Host "    See: catalog/patterns/boundary-pattern.yaml" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "Project structure conforms to Vertical Slice Architecture."
    Write-Host ""
    exit 0
}
