$ErrorActionPreference = 'Stop';

ipmo "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1"
 
$packageName= 'dotnetcore-sdk'
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
#$url        = 'https://download.microsoft.com/download/D/7/8/D788D3CD-44C4-487D-829B-413E914FB1C3/dotnet-sdk-2.1.300-preview1-008174-win-x86.exe'
#$checksum   = 'bd8a9145f651026cfa1ca7c264c2e05b3740afc0b5f8ac5572409a95836d8f87e1a8c460eb985182501f679b721a97fd174b7690ab8cdc5e43c8155ee8af94b5'
$url64      = 'https://download.microsoft.com/download/B/1/9/B19A2F87-F00F-420C-B4B9-A0BA4403F754/dotnet-sdk-2.1.300-rc1-008673-win-x64.exe'
$checksum64 = '7256aca2c02827028213ce06ceb5414231b01bbc509d0d57d5258106760c0fa5621a9d5f629fca3f34d6c45523a133206561d7188a0cb4817d4d5cc6c172d6f0'
 
$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  fileType      = 'EXE'
  url           = $url
  url64bit      = $url64
 
  silentArgs    = "/install /quiet /norestart /log `"$env:TEMP\$($packageName)\$($packageName).MsiInstall.log`""
  validExitCodes= @(0, 3010, 1641)
 
  softwareName  = 'dotnet-core*'
  checksum      = $checksum
  checksumType  = 'SHA512'
  checksum64    = $checksum64
}
 
Install-ChocolateyPackage @packageArgs
