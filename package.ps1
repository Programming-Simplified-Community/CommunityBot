param(
    [Parameter(ParameterSetName="CreateEnvFile",
               HelpMessage="If .env file doesn't exist, one will be created for you with default values",
               Mandatory=$False)]
    [string]$createEnvFile=$False,
    [Parameter(ParameterSetName="DeployArtifactPath",
               HelpMessage="Location to use when packaging artifacts",
               Mandatory=$False)]
    [string]$artifactPath
)

$scriptPath = Split-Path $MyInvocation.MyCommand.Path -Parent

. $scriptPath\SetPsEnv.ps1

# If env file doesn't exist AND we want to initialize file...
if(-not(Test-Path -Path "$scriptPath\.env") -and $createEnvFile) {
    Write-Output "An Environment file does not exist on your machine. We'll create one!"
    
    if([string]::IsNullOrEmpty($artifactPath))
    {
        Write-Output "You need to give us an artifact location to use! Please provide the`DeployArtifactPath` parameter"
        exit 1
    }
    
    $path = "$scriptPath\.env"
    New-Item $path
    
    $defaultContents = "
artifactPath=$artifactPath
overwrite=true
    "
    
    Set-Content $path $defaultContents
    Write-Output "Environment file created!"
    Write-Output $defaultContents
}

Set-PsEnv

$deploy = [System.Environment]::GetEnvironmentVariable("artifactPath")
$overwrite = [System.Environment]::GetEnvironmentVariable("overwrite")

$apiPath = "$deploy\api"
$dbPath = "$deploy\db"

if(-not(Test-Path -Path $dbPath))
{
    Write-Output "Creating Db Deployment Directory..."
    New-Item -Type Directory -Path $dbPath
}

if(Test-Path -Path $dbPath\docker-compose.yml)
{
   if($overwrite)
   {
       Write-Output "Copying newer version of docker-compose-db.yml"
       Remove-Item $dbPath\docker-compose.yml
       Copy-Item $scriptPath\docker-compose-db.yml -Destination $dbPath\docker-compose.yml
       
       Write-Output "Ensuring Db run script is up-to-date..."
       Remove-Item $dbPath\run.ps1
       New-Item $dbPath\run.ps1
       Set-Content $dbPath\run.ps1 "docker-compose up -d"
   }
}
else
{
    Write-Output "Copying `docker-compose-db.yml` as `docker-compose.yml` to $dbPath"
    Copy-Item $scriptPath\docker-compose-db.yml -Destination $dbPath\docker-compose.yml
    
    Write-Output "Creating Db run script..."
    New-Item $dbPath\run.ps1
    Set-Content $dbPath\run.ps1 "docker-compose up -d"
}



if(-not(Test-Path -Path $apiPath))
{
    Write-Output "Creating API Deployment Directory..."
    New-Item -Type Directory -Path $apiPath
}

if(Test-Path -Path $apiPath\docker-compose.yml)
{
    if($overwrite) {
        Write-Output "Copying new version of `docker-compose-api.yml` to `docker-compose.yml` at $apiPath"
        Remove-Item $apiPath\docker-compose.yml
        Copy-Item $scriptPath\docker-compose-api.yml -Destination $apiPath\docker-compose.yml
    }
}
else
{
    Write-Output "Copying `docker-compose-api.yml` to `docker-compose.yml` at $apiPath"
    Copy-Item $scriptPath\docker-compose-api.yml -Destination $apiPath\docker-compose.yml
}

if(Test-Path -Path $apiPath\appsettings.json)
{
    if($overwrite)
    {
        Write-Output "Copying new version of `appsettings.json` at $apiPath"
        Remove-Item $apiPath\appsettings.json
        Copy-Item $scriptPath\Api\appsettings.json -Destination $apiPath\appsettings.json
    }
}
else
{
    Write-Output "Copying `appsettings.json` at $apiPath"
    Copy-Item $scriptPath\Api\appsettings.json
}

Write-Output "Building Api Docker Image..."
docker build -f $scriptPath\Api\Dockerfile -t social-coder-api .

Write-Output "Packaging Complete..."

