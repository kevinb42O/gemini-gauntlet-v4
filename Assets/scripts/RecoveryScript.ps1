# EMERGENCY SCRIPT RECOVERY - Fix nested folder issue
Write-Host "EMERGENCY RECOVERY: Moving scripts from nested location..." -ForegroundColor Red

# Move all scripts from the nested Assets/scripts/Assets/scripts/ back to correct locations
$nestedPath = "Assets\scripts"

# Check if nested structure exists
if (Test-Path $nestedPath) {
    Write-Host "Found nested structure, moving scripts back..." -ForegroundColor Yellow
    
    # Move all folders from nested location to root
    $foldersToMove = @("Player", "Combat", "Enemies", "Platforms", "UI", "Audio", "VFX", "Inventory", "Progression", "Cheats", "Utilities", "DEPRECATED")
    
    foreach ($folder in $foldersToMove) {
        $sourcePath = "$nestedPath\$folder"
        $destPath = ".\$folder"
        
        if (Test-Path $sourcePath) {
            Write-Host "Moving $folder folder..." -ForegroundColor Cyan
            
            # If destination exists, merge contents
            if (Test-Path $destPath) {
                Get-ChildItem $sourcePath | Move-Item -Destination $destPath -Force
                Remove-Item $sourcePath -Force -Recurse
            } else {
                Move-Item $sourcePath $destPath -Force
            }
            Write-Host "Moved $folder" -ForegroundColor Green
        }
    }
    
    # Clean up the nested Assets folder
    if (Test-Path "Assets") {
        Remove-Item "Assets" -Force -Recurse
        Write-Host "Cleaned up nested Assets folder" -ForegroundColor Green
    }
}

Write-Host "RECOVERY COMPLETE! All scripts should be back in correct folders." -ForegroundColor Green
Write-Host "Checking script locations..." -ForegroundColor Yellow

# Verify key scripts are in correct locations
$keyScripts = @{
    "Player\AAAMovementController.cs" = "Main movement script"
    "Player\CelestialDriftController.cs" = "Flight controller" 
    "Player\PlayerHealth.cs" = "Player health"
    "Cheats\AdminCheats.cs" = "Admin cheats"
    "Combat\HandFiringMechanics.cs" = "Hand firing mechanics"
}

foreach ($script in $keyScripts.Keys) {
    if (Test-Path $script) {
        Write-Host "Found: $script" -ForegroundColor Green
    } else {
        Write-Host "Missing: $script" -ForegroundColor Red
    }
}
