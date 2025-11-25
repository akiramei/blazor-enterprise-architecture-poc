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

# Summary
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Validation Summary" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
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
    Write-Host "⚠️  PASSED with $Warnings warning(s)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Review warnings above and ensure VSA structure is correct."
    Write-Host ""
    exit 0
} else {
    Write-Host "✅ ALL CHECKS PASSED" -ForegroundColor Green
    Write-Host ""
    Write-Host "Project structure conforms to Vertical Slice Architecture."
    Write-Host ""
    exit 0
}
