param ($name)

$pluginPath = "$PSScriptRoot/$name"
$assemblyName = $name.Replace("-", "_")
$productName = [cultureinfo]::GetCultureInfo("en-US").TextInfo.ToTitleCase($name.Replace("-", " "))
$pluginName = $productName.Replace(" ", "")

New-Item -ItemType Directory -Path $pluginPath

Set-Content -Path "$pluginPath/.gitignore" -Value @"
/*

!.gitignore
!$name.csproj
!Plugin.cs
"@

Set-Content -Path "$pluginPath/$name.csproj" -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>$assemblyName</AssemblyName>
    <Product>$productName</Product>
    <Version>0.0.0</Version>
  </PropertyGroup>
  <Import Project="../utils-lib/utils-lib.targets"/>
</Project>
"@

Set-Content -Path "$pluginPath/Plugin.cs" -Value @"
using BepInEx;

[BepInPlugin("$name", ThisPluginInfo.Name, ThisPluginInfo.Version)]
public class $pluginName : BaseUnityPlugin {

}
"@
