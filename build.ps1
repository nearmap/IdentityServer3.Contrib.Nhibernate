Param(
	[string]$buildNumber = "0",
	[string]$preRelease = "beta"
)

gci .\src -Recurse "packages.config" |% {
	"Restoring " + $_.FullName
	.\src\.nuget\nuget.exe install $_.FullName -o .\src\packages
}

Import-Module .\src\packages\psake.4.9.0\tools\psake\psake.psm1

# Thanks to Gian Maria https://www.codewrecks.com/post/general/find-msbuild-location-in-powershell/
# -------------------------------------------------------------------------------------------------
function Get-LatestMsbuildLocation
{
  Param 
  (
    [bool] $allowPreviewVersions = $false
  )
    if ($allowPreviewVersions) {
      $latestVsInstallationInfo = Get-VSSetupInstance -All -Prerelease | Sort-Object -Property InstallationVersion -Descending | Select-Object -First 1
    } else {
      $latestVsInstallationInfo = Get-VSSetupInstance -All | Sort-Object -Property InstallationVersion -Descending | Select-Object -First 1
    }
    Write-Host "Latest version installed is $($latestVsInstallationInfo.InstallationVersion)"
    if ($latestVsInstallationInfo.InstallationVersion -like "15.*") {
      $msbuildLocation = "$($latestVsInstallationInfo.InstallationPath)\MSBuild\15.0\Bin\msbuild.exe"
    
      Write-Host "Located msbuild for Visual Studio 2017 in $msbuildLocation"
    } else {
      $msbuildLocation = "$($latestVsInstallationInfo.InstallationPath)\MSBuild\Current\Bin\msbuild.exe"
      Write-Host "Located msbuild in $msbuildLocation"
    }

    return $msbuildLocation
}
# -------------------------------------------------------------------------------------------------

$msBuildLocation = Get-LatestMsbuildLocation

if(Test-Path Env:\APPVEYOR_BUILD_NUMBER){
	$buildNumber = [int]$Env:APPVEYOR_BUILD_NUMBER
	Write-Host "Using APPVEYOR_BUILD_NUMBER"

	$task = "appVeyor"
}

if(Test-Path env:BuildRunner) {
        $buildRunner = Get-Content env:BuildRunner

		if($buildRunner -eq "myget") {
			$buildNumber = [int]$Env:BuildCounter
			Write-Host "Using MYGET_BUILD_NUMBER"

			$task = "myGet"
		}
}

"Build number $buildNumber"

Invoke-Psake .\default.ps1 $task -properties @{ buildNumber=$buildNumber; preRelease=$preRelease; msbuild=$msBuildLocation }

Remove-Module psake