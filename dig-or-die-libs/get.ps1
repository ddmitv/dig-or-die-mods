
$src_path = $args[0]
$dest_path = $PSScriptRoot

Copy-Item "$src_path/DigOrDie_Data/Managed/Assembly-CSharp.dll" $dest_path
Copy-Item "$src_path/DigOrDie_Data/Managed/Assembly-CSharp-firstpass.dll" $dest_path
Copy-Item "$src_path/DigOrDie_Data/Managed/UnityEngine.Analytics.dll" $dest_path
Copy-Item "$src_path/DigOrDie_Data/Managed/UnityEngine.dll" $dest_path
Copy-Item "$src_path/DigOrDie_Data/Managed/UnityEngine.Networking.dll" $dest_path
Copy-Item "$src_path/DigOrDie_Data/Managed/UnityEngine.UI.dll" $dest_path
