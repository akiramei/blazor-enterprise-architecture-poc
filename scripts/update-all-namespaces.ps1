# Update all namespace references
$ErrorActionPreference = "Stop"

Write-Host "Updating all namespace references..." -ForegroundColor Yellow

# Update all .cs files in ProductCatalog
Get-ChildItem -Path src\ProductCatalog -Filter *.cs -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Encoding UTF8 -Raw
    $updated = $content `
        -replace 'using ProductCatalog\.Shared\.Domain', 'using Domain.ProductCatalog'

    if ($content -ne $updated) {
        Set-Content $_.FullName -Value $updated -Encoding UTF8 -NoNewline
        Write-Host "Updated: $($_.FullName)" -ForegroundColor Green
    }
}

# Update all .cs files in PurchaseManagement
Get-ChildItem -Path src\PurchaseManagement -Filter *.cs -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Encoding UTF8 -Raw
    $updated = $content `
        -replace 'using PurchaseManagement\.Shared\.Domain', 'using Domain.PurchaseManagement'

    if ($content -ne $updated) {
        Set-Content $_.FullName -Value $updated -Encoding UTF8 -NoNewline
        Write-Host "Updated: $($_.FullName)" -ForegroundColor Green
    }
}

Write-Host "All namespaces updated." -ForegroundColor Cyan
