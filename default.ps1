
Framework '4.5'
properties {
	$base_directory = Resolve-Path . 
    $target_config = "Release"
	$src_directory = "$base_directory\src"
	
	$dist_directory = "$base_directory\distribution"
	$sln_file = "$src_directory\IdentityServer3.Contrib.Nhibernate.sln"
	
	$framework_version = "v4.7.2"
    $output_directory = "$src_directory\Core.Nhibernate\bin\$target_config"
	$obj_directory = "$src_directory\Core.Nhibernate\obj\$target_config"
    $xunit_path = "$src_directory\packages\xunit.runner.console.2.4.1\tools\net472\xunit.console.exe"
    $nuget_path = "$src_directory\.nuget\nuget.exe"
	
	$buildNumber = 0;
	$version = "2.0.0.0"
	$preRelease = $null
	$msbuild = null
}


task default -depends Clean, CreateNuGetPackage
task appVeyor -depends Clean, RunIntegraionTests, CreateNuGetPackage
task myGet -depends Clean, CreateNuGetPackage

task Clean {
	rmdir $output_directory -ea SilentlyContinue -recurse
	rmdir $obj_directory -ea SilentlyContinue -recurse
	rmdir $dist_directory -ea SilentlyContinue -recurse
	exec { & $msbuild /nologo /verbosity:quiet $sln_file /p:Configuration=$target_config /t:Clean }
}

task Compile -depends UpdateVersion {
    
	exec { & $msbuild /nologo /verbosity:q $sln_file /p:Configuration=$target_config /t:Restore }
	exec { & $msbuild /nologo /verbosity:q $sln_file /p:Configuration=$target_config /p:TargetFrameworkVersion=$framework_version }

	if ($LastExitCode -ne 0) {
        exit $LastExitCode
    }
}

task UpdateVersion {
	$vSplit = $version.Split('.')
	if($vSplit.Length -ne 4)
	{
		throw "Version number is invalid. Must be in the form of 0.0.0.0"
	}
	$major = $vSplit[0]
	$minor = $vSplit[1]
	$patch = $vSplit[2]
	$assemblyFileVersion =  "$major.$minor.$patch.$buildNumber"
	$assemblyVersion = "$major.$minor.0.0"
	$versionAssemblyInfoFile = "$src_directory/VersionInfo.cs"
	"using System.Reflection;" > $versionAssemblyInfoFile
	"" >> $versionAssemblyInfoFile
	"[assembly: AssemblyVersion(""$assemblyVersion"")]" >> $versionAssemblyInfoFile
	"[assembly: AssemblyFileVersion(""$assemblyFileVersion"")]" >> $versionAssemblyInfoFile
}

task CopyConfigFile {
	copy-item $src_directory\Core.Nhibernate.IntegrationTests\bin\$target_config\connectionstring.appveyor.config $src_directory\Core.Nhibernate.IntegrationTests\bin\$target_config\connectionstring.config -Force
}

task RunIntegraionTests -depends Compile, CopyConfigFile {
	$project = "Core.Nhibernate.IntegrationTests"
	mkdir $output_directory\xunit\$project -ea SilentlyContinue
	.$xunit_path "$src_directory\Core.Nhibernate.IntegrationTests\bin\$target_config\$project.dll"
}

task CreateNuGetPackage -depends Compile {
	$vSplit = $version.Split('.')
	if($vSplit.Length -ne 4)
	{
		throw "Version number is invalid. Must be in the form of 0.0.0.0"
	}
	$major = $vSplit[0]
	$minor = $vSplit[1]
	$patch = $vSplit[2]
	$packageVersion =  "$major.$minor.$patch"
	if($preRelease){
		$packageVersion = "$packageVersion-$preRelease" 
	}

	if ($buildNumber -ne 0){
		$packageVersion = $packageVersion + "-build" + $buildNumber.ToString().PadLeft(5,'0')
	}

  md $dist_directory
  md $dist_directory\lib
  md $dist_directory\lib\net472
  
  copy-item $src_directory\IdentityServer3.Contrib.Nhibernate.nuspec $dist_directory
  copy-item $output_directory\net472\IdentityServer3.Contrib.Nhibernate.dll $dist_directory\lib\net472
  copy-item $output_directory\net472\IdentityServer3.Contrib.Nhibernate.pdb $dist_directory\lib\net472
  
	exec { . $nuget_path pack $dist_directory\IdentityServer3.Contrib.Nhibernate.nuspec -BasePath $dist_directory -OutputDirectory $dist_directory -version $packageVersion }
}