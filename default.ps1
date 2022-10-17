
Framework '4.7.2'
properties {
	$base_directory = Resolve-Path . 
    $target_config = "Release"
	$src_directory = "$base_directory\src"
	
	$dist_directory = "$base_directory\distribution"
	$sln_file = "$src_directory\IdentityServer3.Contrib.Nhibernate.sln"
	
	$framework_version = "v4.7.2"
    $output_directory = "$src_directory\Core.Nhibernate\bin\$target_config\net472"
	$obj_directory = "$src_directory\Core.Nhibernate\obj\$target_config"
    $xunit_path = "$src_directory\packages\xunit.runner.console.2.4.1\tools\net472\xunit.console.exe"
    $nuget_path = "$src_directory\.nuget\nuget.exe"
	$ilmerge_path = "$src_directory\packages\ILMerge.3.0.41\tools\net452\ILMerge.exe"
	
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

task ILMerge -depends Compile {
	$input_dlls = "$output_directory\Core.Nhibernate.dll"

	New-Item $dist_directory\lib\net472 -Type Directory
	
  if ($preRelease){
	  Invoke-Expression "$ilmerge_path /targetplatform:v4 /internalize /allowDup /target:library /out:$dist_directory\lib\net472\IdentityServer3.Contrib.Nhibernate.dll $input_dlls"
  }else{
	  Invoke-Expression "$ilmerge_path /ndebug /targetplatform:v4 /internalize /allowDup /target:library /out:$dist_directory\lib\net472\IdentityServer3.Contrib.Nhibernate.dll $input_dlls"
  }
}

task CreateNuGetPackage -depends ILMerge {
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
  
  copy-item $src_directory\IdentityServer3.Contrib.Nhibernate.nuspec $dist_directory
  
	exec { . $nuget_path pack $dist_directory\IdentityServer3.Contrib.Nhibernate.nuspec -BasePath $dist_directory -OutputDirectory $dist_directory -version $packageVersion }
}