# Update UI project references to Domain
$ErrorActionPreference = "Stop"

Write-Host "Updating UI project references..." -ForegroundColor Yellow

Get-ChildItem -Path src\ProductCatalog\Features -Filter "*.UI.csproj" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Encoding UTF8 -Raw
    $updated = $content `
        -replace '\.\.\\\.\.\\\.\.\\\Shared\\Domain\\Products\\ProductCatalog\.Shared\.Domain\.Products\.csproj', '..\..\..\..\..\Domain\ProductCatalog\Domain.ProductCatalog.csproj'

    if ($content -ne $updated) {
        Set-Content $_.FullName -Value $updated -Encoding UTF8 -NoNewline
        Write-Host "Updated: $($_.FullName)" -ForegroundColor Green
    }
}

Write-Host "UI project references updated." -ForegroundColor Cyan
