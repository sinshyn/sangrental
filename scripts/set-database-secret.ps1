# Run interactively from PowerShell. Never pass the password as a command argument.
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$apiProject = Join-Path $PSScriptRoot '../backend/SangRental.Api'
$sqlUser = Read-Host 'SQL user (Enter for sa)'
if ([string]::IsNullOrWhiteSpace($sqlUser)) { $sqlUser = 'sa' }
$sqlPassword = Read-Host 'SQL password (hidden)' -AsSecureString
$passwordPointer = [IntPtr]::Zero
$plainPassword = $null
$secretJson = $null
$connectionBuilder = $null
try {
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlPassword)
    $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    if ([string]::IsNullOrEmpty($plainPassword)) { throw 'SQL password must not be empty.' }
    # Builder escapes semicolons, quotes and other connection-string characters.
    $connectionBuilder = New-Object System.Data.Common.DbConnectionStringBuilder
    $connectionBuilder['Server'] = 'localhost,1436'
    $connectionBuilder['Database'] = 'SangRentalDb'
    $connectionBuilder['Encrypt'] = 'True'
    $connectionBuilder['TrustServerCertificate'] = 'True'
    $connectionBuilder['User ID'] = $sqlUser
    $connectionBuilder['Password'] = $plainPassword
    $secretJson = @{ 'ConnectionStrings:SangRental' = $connectionBuilder.ConnectionString } | ConvertTo-Json -Compress
    # stdin avoids placing the secret in process arguments, command history or repository files.
    $secretJson | & dotnet user-secrets set --project $apiProject
    if ($LASTEXITCODE -ne 0) { throw 'User Secrets could not be saved.' }
}
finally {
    if ($passwordPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer) }
    if ($null -ne $connectionBuilder) { $connectionBuilder.Clear() }
    $plainPassword = $null
    $secretJson = $null
    $sqlPassword.Dispose()
}
