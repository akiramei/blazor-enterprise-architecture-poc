# Script to update namespaces from Application.Host to Application

$rootPath = "C:\Users\akira\source\repos\VSASample\src\Application"

# Get all C# files
$files = Get-ChildItem -Path $rootPath -Filter "*.cs" -Recurse

$totalFiles = $files.Count
$updatedFiles = 0

Write-Host "Found $totalFiles C# files to process..."

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # Replace namespace declarations
    $content = $content -replace 'namespace Application\.Host\.', 'namespace Application.'
    $content = $content -replace 'namespace ProductCatalog\.Host\.', 'namespace Application.'

    # Replace using statements
    $content = $content -replace 'using Application\.Host\.', 'using Application.'
    $content = $content -replace 'using ProductCatalog\.Host\.', 'using Application.'

    # Only write if content changed
    if ($content -ne $originalContent) {
        [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($false))
        $updatedFiles++
        Write-Host "Updated: $($file.FullName)"
    }
}

Write-Host "`nUpdated $updatedFiles out of $totalFiles files."
