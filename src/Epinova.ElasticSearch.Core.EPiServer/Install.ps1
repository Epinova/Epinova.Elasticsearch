param($installPath, $toolsPath, $package, $project)

#save the project file first - this commits the changes made by nuget before this script runs.
$project.Save()


$xml = [XML] (gc $project.FullName)

$nsmgr = New-Object System.Xml.XmlNamespaceManager -ArgumentList $xml.NameTable
$nsmgr.AddNamespace('a',$xml.Project.GetAttribute("xmlns"))

$node = $xml.SelectSingleNode("//a:ProjectTypeGuids", $nsmgr)
$isWebProject =  $false
if($node -ne $null) {
    $guid = [String]$node.InnerText.ToUpper();
    $isWebProject = $guid.Contains("{349C5851-65DF-11DA-9384-00065B846F21}")
}

if($isWebProject -eq $false) {
    Write-Host $project.FullName " is not a web project"

    $modules = $project.ProjectItems.Item("Modules");

    Write-Host "Deleting _protected folder"
    $modules.ProjectItems.Item("_protected").Delete()

    if($modules.ProjectItems.Count -eq 0) {
        Write-Host "Deleting modules folder because it is empty"
        $modules.Delete()
    }

    $project.Save()
}