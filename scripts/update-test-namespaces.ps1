# Update test file namespaces
$ErrorActionPreference = "Stop"

Write-Host "Updating test file namespaces..." -ForegroundColor Yellow

Get-ChildItem -Path tests -Filter *.cs -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Encoding UTF8 -Raw
    $updated = $content `
        -replace 'using PurchaseManagement\.Shared\.Domain', 'using Domain.PurchaseManagement' `
        -replace 'using ProductCatalog\.Shared\.Domain', 'using Domain.ProductCatalog'

    Set-Content $_.FullName -Value $updated -Encoding UTF8 -NoNewline
}

Write-Host "Test namespaces updated." -ForegroundColor Green
