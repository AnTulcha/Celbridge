# Get the directory of the currently running script
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Check if the venv folder exists in the current directory
$venvPath = Join-Path $scriptDirectory "venv"
$requirementsFile = Join-Path $scriptDirectory "requirements.txt"

if (!(Test-Path $venvPath))
{
    Write-Host "Virtual environment not found. Please run Install.ps1 to setup the virtual environment."
    exit 1
}

# Activate the virtual environment
$activateScript = Join-Path $venvPath "Scripts\Activate.ps1"
if (Test-Path $activateScript)
{
    # Write-Host "Activating the virtual environment..."
    & $activateScript
}
else
{
    Write-Host "Activate script not found. Please run Install.ps1 to recreate the virtual environment."
    exit 1
}

# Check if a Python script file is specified as the first argument
if ($args.Count -ge 1)
{
    $firstArg = $args[0]
    $fileExtension = [System.IO.Path]::GetExtension($firstArg)

    if ($fileExtension -eq ".py") 
    {
        $pythonScript = $args[0]
        $pythonArgs = $args[1..($args.Count - 1)]

        # Run the specified Python script with additional arguments
        # Capture the standard output and error output
        $pythonOutput = python $pythonScript $pythonArgs 2>&1

        Write-Host $pythonOutput
    } 
    else 
    {
        $pythonUtil = $args[0]
        $pythonArgs = $args[1..($args.Count - 1)]

        # Run the specified Python script with additional arguments
        # Capture the standard output and error output
        $pythonOutput = Invoke-Expression "$pythonUtil $pythonArgs" 2>&1
        if ($LastExitCode -ne 0)
        {
            exit 1
        }
        
        Write-Host $pythonOutput
    }    
}
else
{
    Write-Host "No Python script file specified."
    exit 1
}

# Deactivate the virtual environment
deactivate
