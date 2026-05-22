# RescueFlow - Validation Summary

This document summarizes the validation steps and test executions performed to verify the SC-RCMDA algorithm and the RescueFlow web application.

## 1. Automated Test Suites (xUnit)

A total of 29 modular tests were run and passed successfully. The tests are distributed across the following test suites in the `RescueFlow.Tests` project:

### SeverityAnalyzerTests.cs (4 Tests)
- Verifies base severity assignments based on primary injury classifications.
- Validates escalation triggers to severity `4` due to life-threat flags (unconsciousness, breathing difficulty, trapped victims, etc.).
- Verifies severity `5` assignment for explosion risks and mass casualty events (>= 5 patients).
- Assures mathematical clamping limits severity to the `[1, 5]` range.

### RequirementEngineTests.cs (4 Tests)
- Verifies that minor injuries require `1 Ambulance` and **no hospital** transport (`MinorInjury_DoesNotRequireHospital`).
- Verifies that heart attacks request an ambulance and a hospital with `ICU` and `Cardiac` capabilities.
- Verifies that trapped victims request an ambulance, a fire engine, and a heavy rescue vehicle equipped with extraction tools.
- Verifies that chemical hazards request an ambulance, a fire engine, and a HazMat vehicle with gas detectors and chemical suits.

### ResourceMatcherTests.cs (2 Tests)
- Validates proximity-based selection by routing ETA.
- Verifies that scarcity penalties are applied to preserve local station capacities during tie-breakers.

### HospitalMatcherTests.cs (2 Tests)
- Verifies capability-based matching and distance sorting.
- Confirms that hospitals exceeding capability load capacities are skipped during matching.

### SafetyValidatorTests.cs (6 Tests)
- Assures safety blocks are triggered if crew sizes, vehicle states, required equipment, hospital capabilities, or hospital capacities violate safety rules.

### AutoAssignmentServiceTests.cs (4 Tests)
- Verifies atomic, transactional database updates.
- Tests optimistic concurrency checks and locks.
- **Rollback Verification**: Proves that if hospital reservation fails after vehicle reservation succeeds, the database transaction rolls back, releasing the vehicle status and deleting active assignments.
- Verifies that closing a case correctly decrements loads and prevents hospital capacities from underflowing below zero.

### EndToEndDispatchTests.cs (7 Tests)
- Verifies the full algorithm pipeline execution from the initial `RescueCase` submission to the final committed and verified `ResponsePlan`.

---

## 2. Manual/Smoke Verification results

The manual smoke test script `manual-smoke-test.ps1` executes six distinct scenarios against the running API on port 5000 and asserts their final outcomes.

| Case | Scenario Description | Expected Status | Validation Result |
| :--- | :--- | :--- | :--- |
| **Case 1** | Minor Injury | `AutoAssigned` | **Passed** |
| **Case 2** | Heart Attack | `AutoAssigned` | **Passed** |
| **Case 3** | Trapped Victim | `AutoAssigned` -> `Closed` | **Passed** (Resources released) |
| **Case 4** | Chemical Hazard | `AutoAssigned` | **Passed** |
| **Case 5** | Missing Rescue Vehicle | `BlockedNoSafePlan` | **Passed** (Heavy Rescue in Maintenance) |
| **Case 6** | Invalid Input (0 patients) | `Escalated` | **Passed** (Bypasses matcher) |

---

## 3. Final Acceptance Checklist

- [x] **dotnet build passes**: Compiles with 0 warnings and 0 errors.
- [x] **dotnet test passes**: All 29 unit and integration tests execute successfully.
- [x] **dotnet ef database update succeeds**: LocalDB schema is generated and seeded.
- [x] **App starts on http://localhost:5000**: Runs on the designated port.
- [x] **Manual smoke test exits with code 0**: Script validates all cases and exits cleanly.
- [x] **Case 1 Minor Injury -> AutoAssigned**: Assigned ambulance, no hospital.
- [x] **Case 2 Heart Attack -> AutoAssigned**: ICU/Cardiac hospital matched.
- [x] **Case 3 Trapped Victim -> AutoAssigned then Closed**: Correct vehicles dispatched, successfully released.
- [x] **Case 4 Chemical Hazard -> AutoAssigned**: HazMat vehicle and Toxicology hospital matched.
- [x] **Case 5 Missing Rescue Vehicle -> BlockedNoSafePlan**: Handled safely without assigning partial plans.
- [x] **Case 6 Invalid Input -> Escalated**: Input boundaries validated, case flagged for human overview.
- [x] **No vehicle is double-booked**: Unique active assignment index prevents duplicate assignments.
- [x] **Hospital capacity never goes below zero**: Closure update query clamps load reduction to 0.
- [x] **Every blocked/escalated case has a visible reason**: Logged into database decision and safety violation tables.
