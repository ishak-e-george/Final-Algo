$baseUrl = "http://localhost:5000"
$createUrl = "$baseUrl/RescueCases/Create"
$vehiclesUrl = "$baseUrl/Vehicles"
$vehiclesUpdateUrl = "$baseUrl/Vehicles/UpdateStatus"

# 1. Start Session and Get Request Verification Token
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$resp = Invoke-WebRequest -Uri $createUrl -WebSession $session
$tokenRegex = 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"'
if ($resp.Content -match $tokenRegex) {
    $token = $Matches[1]
    Write-Host "Success: Retrieved __RequestVerificationToken = $token" -ForegroundColor Green
} else {
    Write-Error "Could not find RequestVerificationToken in page."
    exit 1
}

# Find RESCUE-01 ID dynamically
$vehiclesResp = Invoke-WebRequest -Uri $vehiclesUrl -WebSession $session
$rescue01Id = $null
if ($vehiclesResp.Content -match 'value="(\d+)"[^>]*>[^<]*RESCUE-01') {
    $rescue01Id = $Matches[1]
} else {
    $pattern = '(?s)<form[^>]*action="/Vehicles/UpdateStatus/(\d+)"[^>]*>.*?RESCUE-01'
    if ($vehiclesResp.Content -match '(?s)<form[^>]*action="/Vehicles/UpdateStatus\?id=(\d+)"[^>]*>.*?RESCUE-01') {
        $rescue01Id = $Matches[1]
    } elseif ($vehiclesResp.Content -match '(?s)<input[^>]*value="(\d+)"[^>]*name="id".*?RESCUE-01') {
        $rescue01Id = $Matches[1]
    } else {
        $lines = $vehiclesResp.Content -split "`n"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -like "*RESCUE-01*") {
                for ($j = [Math]::Max(0, $i-10); $j -lt [Math]::Min($lines.Count, $i+10); $j++) {
                    if ($lines[$j] -match 'name="id"\s+value="(\d+)"') {
                        $rescue01Id = $Matches[1]
                        break
                    }
                    if ($lines[$j] -match 'value="(\d+)"\s+name="id"') {
                        $rescue01Id = $Matches[1]
                        break
                    }
                }
            }
            if ($rescue01Id) { break }
        }
    }
}

if (-not $rescue01Id) {
    $rescue01Id = 4
    Write-Host "Could not parse RESCUE-01 ID dynamically. Using seeded fallback ID: $rescue01Id" -ForegroundColor Yellow
} else {
    Write-Host "Found RESCUE-01 ID dynamically: $rescue01Id" -ForegroundColor Green
}

# Function to submit a case
function Submit-Case($caseName, $payload) {
    Write-Host "--------------------------------------------------" -ForegroundColor Cyan
    Write-Host "Submitting $caseName..." -ForegroundColor Cyan
    
    $postParams = New-Object System.Collections.Specialized.OrderedDictionary
    $postParams.Add("__RequestVerificationToken", $token)
    foreach ($key in $payload.Keys) {
        $postParams.Add($key, $payload[$key])
    }

    $postResp = Invoke-WebRequest -Uri $createUrl -Method Post -Body $postParams -WebSession $session -MaximumRedirection 0 -ErrorAction SilentlyContinue
    $redirectUrl = $postResp.Headers.Location
    Write-Host "Post Redirect Location: $redirectUrl"
    
    if ($redirectUrl) {
        $detailsUrl = "$baseUrl$redirectUrl"
        $detailsResp = Invoke-WebRequest -Uri $detailsUrl -WebSession $session
        
        $status = "Unknown"
        if ($detailsResp.Content -match '(?i)Status:.*?(AutoAssigned|Pending|BlockedNoSafePlan|Escalated|Closed|Cancelled)') {
            $status = $Matches[1]
        } elseif ($detailsResp.Content -match '(?s)badge.*?>(AutoAssigned|Pending|BlockedNoSafePlan|Escalated|Closed|Cancelled)<') {
            $status = $Matches[1]
        } else {
            $matches = [regex]::Matches($detailsResp.Content, 'badge[^>]*>([^<]+)<')
            foreach ($m in $matches) {
                $val = $m.Groups[1].Value.Trim()
                if ($val -match 'Auto-Assigned|Blocked|Escalated|Pending') {
                    $status = $val
                    break
                }
            }
        }
        
        $severity = "Unknown"
        if ($detailsResp.Content -match 'Severity Score:.*?(\d)') {
            $severity = $Matches[1]
        }
        
        Write-Host "RESULT -> Status: $status | Severity: $severity" -ForegroundColor Green
        return [PSCustomObject]@{
            CaseName = $caseName
            Status = $status
            Severity = $severity
            Redirect = $redirectUrl
        }
    } else {
        Write-Warning "Failed to redirect. Validation Errors may have occurred!"
        return $null
    }
}

# Define payloads
$payload1 = @{
    ReporterName = "John Smith"
    ReporterPhone = "555-0101"
    LocationDescription = "Central Park Zoo Entrance"
    Latitude = "40.7300"
    Longitude = "-74.0010"
    IncidentCategory = "0"
    InjuryType = "1"
    NumberOfPatients = "1"
    Description = "Scraped knee and slight ankle pain after falling near zoo gate."
}

$payload2 = @{
    ReporterName = "Alice Cooper"
    ReporterPhone = "555-0202"
    LocationDescription = "Corporate Office, 5th Floor"
    Latitude = "40.7350"
    Longitude = "-74.0150"
    IncidentCategory = "0"
    InjuryType = "11"
    NumberOfPatients = "1"
    HasBreathingProblem = "true"
    IsUnconscious = "true"
    Description = "Elderly male experiencing chest pain, gasping for breath, unconscious."
}

$payload3 = @{
    ReporterName = "Officer Davis"
    ReporterPhone = "555-0303"
    LocationDescription = "Interstate 95, Exit 4"
    Latitude = "40.7420"
    Longitude = "-74.0180"
    IncidentCategory = "1"
    InjuryType = "16"
    NumberOfPatients = "2"
    HasSevereBleeding = "true"
    HasTrappedVictim = "true"
    Description = "Sedan crash. Victim trapped with heavy bleeding."
}

$payload4 = @{
    ReporterName = "Supervisor Jenkins"
    ReporterPhone = "555-0404"
    LocationDescription = "Apex Chem Warehouse B"
    Latitude = "40.7550"
    Longitude = "-73.9850"
    IncidentCategory = "4"
    InjuryType = "14"
    NumberOfPatients = "3"
    HasChemicalRisk = "true"
    HasBreathingProblem = "true"
    Description = "Ammonia vapor cloud leak from valve."
}

# Run Case 1-4
$res1 = Submit-Case "Case 1: Minor Injury" $payload1
$res2 = Submit-Case "Case 2: Heart Attack (ICU)" $payload2
$res3 = Submit-Case "Case 3: Trapped Victim (Multi-Vehicle)" $payload3
$res4 = Submit-Case "Case 4: Chemical Hazard (HazMat)" $payload4

# Close Case 3 to free up vehicles for Case 5 test
if ($res3 -and $res3.Redirect) {
    Write-Host "Closing Case 3 to release heavy rescue and fire engine resources..." -ForegroundColor Cyan
    $caseId = $res3.Redirect -split "/" | Select-Object -Last 1
    $closeUrl = "$baseUrl/RescueCases/Close/$caseId"
    $closeParams = @{ "__RequestVerificationToken" = $token }
    $closeResp = Invoke-WebRequest -Uri $closeUrl -Method Post -Body $closeParams -WebSession $session -MaximumRedirection 0 -ErrorAction SilentlyContinue
    Write-Host "Close Request Status: $($closeResp.StatusCode)"
}

# Set RESCUE-01 to Maintenance
Write-Host "--------------------------------------------------" -ForegroundColor Cyan
Write-Host "Setting RESCUE-01 to Maintenance status..." -ForegroundColor Cyan
$updateParams = @{
    "__RequestVerificationToken" = $token
    "id" = $rescue01Id
    "status" = "Maintenance"
}
$updateResp = Invoke-WebRequest -Uri $vehiclesUpdateUrl -Method Post -Body $updateParams -WebSession $session -MaximumRedirection 0 -ErrorAction SilentlyContinue
Write-Host "Update Status Response Code: $($updateResp.StatusCode)"

# Submit Case 5: Trapped Victim (Requires RESCUE-01) -> Should fail because RESCUE-01 is in Maintenance!
$payload5 = @{
    ReporterName = "Wayne Gretzky"
    ReporterPhone = "555-0505"
    LocationDescription = "Industrial Site construction shaft"
    Latitude = "40.7480"
    Longitude = "-74.0120"
    IncidentCategory = "3"
    InjuryType = "8"
    NumberOfPatients = "1"
    HasTrappedVictim = "true"
    Description = "Pinned worker. Heavy Rescue required."
}
$res5 = Submit-Case "Case 5: Blocked Scenario (No Rescue Vehicle)" $payload5

# Submit Case 6: Escalated Incident (Invalid patient count)
$payload6 = @{
    ReporterName = "Anxious Caller"
    ReporterPhone = "555-9999"
    LocationDescription = "Empty Lot"
    Latitude = "40.7100"
    Longitude = "-74.0200"
    IncidentCategory = "4"
    InjuryType = "0"
    NumberOfPatients = "0"
    HasChemicalRisk = "true"
    Description = ""
}
$res6 = Submit-Case "Case 6: Escalated Incident (Invalid input)" $payload6

# Set RESCUE-01 back to Available
Write-Host "--------------------------------------------------" -ForegroundColor Cyan
Write-Host "Restoring RESCUE-01 status to Available..." -ForegroundColor Cyan
$updateParams2 = @{
    "__RequestVerificationToken" = $token
    "id" = $rescue01Id
    "status" = "Available"
}
$updateResp2 = Invoke-WebRequest -Uri $vehiclesUpdateUrl -Method Post -Body $updateParams2 -WebSession $session -MaximumRedirection 0 -ErrorAction SilentlyContinue
Write-Host "Update Status Response Code: $($updateResp2.StatusCode)"

Write-Host "==================================================" -ForegroundColor Yellow
Write-Host "Summary of Test Scenarios" -ForegroundColor Yellow
Write-Host "==================================================" -ForegroundColor Yellow
Write-Host "$($res1.CaseName) -> Status: $($res1.Status) | Severity: $($res1.Severity)"
Write-Host "$($res2.CaseName) -> Status: $($res2.Status) | Severity: $($res2.Severity)"
Write-Host "$($res3.CaseName) -> Status: $($res3.Status) (Closed) | Severity: $($res3.Severity)"
Write-Host "$($res4.CaseName) -> Status: $($res4.Status) | Severity: $($res4.Severity)"
Write-Host "$($res5.CaseName) -> Status: $($res5.Status) | Severity: $($res5.Severity)"
Write-Host "$($res6.CaseName) -> Status: $($res6.Status) | Severity: $($res6.Severity)"

# Assertion Checks
$failures = @()

function Assert-Status($result, $expected) {
    if (-not $result) {
        $script:failures += "Result was null, expected $expected"
        return
    }

    if ($result.Status -ne $expected -and $result.Status -notlike "*$expected*") {
        $script:failures += "$($result.CaseName): expected $expected but got $($result.Status)"
    }
}

Assert-Status $res1 "AutoAssigned"
Assert-Status $res2 "AutoAssigned"
Assert-Status $res3 "AutoAssigned"
Assert-Status $res4 "AutoAssigned"
Assert-Status $res5 "BlockedNoSafePlan"

if ($res6 -and $res6.Status -ne "Escalated") {
    $failures += "$($res6.CaseName): expected Escalated or validation rejection but got $($res6.Status)"
}

if ($failures.Count -gt 0) {
    Write-Host "FAILED SMOKE TESTS:" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" -ForegroundColor Red }
    exit 1
}

Write-Host "ALL SMOKE TESTS PASSED" -ForegroundColor Green
exit 0
