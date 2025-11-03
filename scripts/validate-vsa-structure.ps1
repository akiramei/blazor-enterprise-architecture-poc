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
