$scriptPath = Split-Path $MyInvocation.MyCommand.Path -Parent

Write-Output "Loading environment variables..."

. $scriptPath\SetPsEnv.ps1
Set-PsEnv


$deploy = [System.Environment]::GetEnvironmentVariable("artifactPath") -replace '"', ""
$db = Join-Path $deploy db

Write-Output "Tearing down database container..."
cd $db
docker-compose down

Write-Output "Deleting DB contents..."
Remove-Item -Recurse -Force "$db\Database"

Write-Output "Starting database..."
docker-compose up -d

Write-Output "Applying Migrations..."
cd "$scriptPath\Api"

Start-Sleep -Seconds 15

dotnet ef database update
cd "$scriptPath"