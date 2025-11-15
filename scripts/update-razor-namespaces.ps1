# Update Razor file namespaces
$ErrorActionPreference = "Stop"

Write-Host "Updating Razor file namespaces..." -ForegroundColor Yellow

# Update PurchaseManagement Razor files
Get-ChildItem -Path src\PurchaseManagement\Features -Filter *.razor -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Encoding UTF8 -Raw
    $updated = $content `
        -replace '@using PurchaseManagement\.Shared\.Domain', '@using Domain.PurchaseManagement'

    if ($content -ne $updated) {
        Set-Content $_.FullName -Value $updated -Encoding UTF8 -NoNewline
        Write-Host "Updated: $($_.FullName)" -ForegroundColor Green
    }
}

# Update ProductCatalog Razor files
Get-ChildItem -Path src\ProductCatalog\Features -Filter *.razor -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Encoding UTF8 -Raw
    $updated = $content `
        -replace '@using ProductCatalog\.Shared\.Domain', '@using Domain.ProductCatalog'

    if ($content -ne $updated) {
        Set-Content $_.FullName -Value $updated -Encoding UTF8 -NoNewline
        Write-Host "Updated: $($_.FullName)" -ForegroundColor Green
    }
}

Write-Host "Razor file namespaces updated." -ForegroundColor Cyan
