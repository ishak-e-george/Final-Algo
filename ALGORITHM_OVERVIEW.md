# SC-RCMDA - Algorithm Overview

The **Safety-Critical Rescue Case Management and Dispatch Algorithm (SC-RCMDA)** guarantees that emergency responders and medical facilities are assigned efficiently and safely. 

Unlike standard dispatch algorithms that perform simple proximity matching, SC-RCMDA incorporates dual-stage safety checks (pre-validation and atomic post-reservation checks) and utilizes optimistic concurrency tokens to prevent double-booking or over-committing critical hospital infrastructure.

## Pipeline Architecture

```text
       Input Case Report
               │
               ▼
       CaseFactsBuilder
               │
               ▼
        CaseClassifier
               │
               ▼
       SeverityAnalyzer
               │
               ▼
       RequirementEngine
         ┌─────┴─────┐
         ▼           ▼
  ResourceMatcher  HospitalMatcher
         └─────┬─────┘ (via RoutingService)
               ▼
       ResponsePlanGenerator
               │
               ▼
        SafetyValidator (Pre-Reservation Check)
               │
         [Is Valid?] ──────No─────┐
               │                  │
              Yes                 │
               ▼                  │
      AutoAssignmentService       │
      (Atomic Transaction)        │
               │                  │
        SafetyValidator           │
     (Post-Reservation Check)     │
               │                  │
         [Is Valid?] ──────No─────┼─────┐
               │                  │     │
              Yes                 │     │
               ▼                  ▼     ▼
         AutoAssigned    BlockedNoSafePlan (or Escalated if invalid inputs)
```

## Step-by-Step Execution Stages

### 1. CaseFactsBuilder
Translates raw database entities or view model forms (reporter names, phone numbers, GPS coordinates, injury types, checkboxes for severe bleeding/trauma) into a structured, normalized `CaseFacts` object that is decoupled from DB schemas.

### 2. CaseClassifier
Deduces metadata tags (e.g., `"HazMat"`, `"Entrapment"`, `"FireActive"`) by analyzing injury types and incident description logs, flagging whether the case represents an immediate threat to life.

### 3. SeverityAnalyzer
Calculates a base severity score from `1` (Minor) to `5` (Catastrophic). 
- Minor Injury -> 1
- Fracture or bleeding -> 2
- Trauma, burn, heart attack, or stroke -> 3
- Life threat flags (severe bleeding, unconscious, breathing issues, entrapment, chemical risk) -> 4
- Explosion hazard or massive patient count (>= 5 patients) -> 5
- Clamped between `1` and `5`.

### 4. RequirementEngine
Converts the severity, facts, and classification tags into a specific manifest of requirements:
- **Vehicle Requirements**: Specifying `VehicleType`, `RequiredVehicleCount` (e.g. 3 ambulances if severity >= 5, otherwise 1; fire engines if fire tags are present), crew size, and required equipment list (e.g., Gas Detectors, Hydraulic Cutters, Trauma Bags).
- **Hospital Requirements**: Specifies required capabilities (ICU, Trauma, Toxicology, Burn, Cardiac) and capacity load size matching the patient count. Note that Minor Injuries (Severity 1) do not require a hospital.

### 5. RoutingService
Uses the Haversine formula to compute great-circle distances between vehicle GPS coordinates, the incident scene, and available hospitals to calculate travel times (ETAs).

### 6. ResourceMatcher
Searches for available vehicles matching the required type, crew counts, and equipment. Eligible vehicles are ranked by travel times (ETA). A **Scarcity Penalty** is applied as a tie-breaker, penalizing stations with low remaining vehicle inventories to preserve local regional backup capacity.

### 7. HospitalMatcher
Finds active hospitals matching the necessary capability set. It filters out hospitals with insufficient capacity in their corresponding specialty queues (ICU beds, trauma bays, burn units) and ranks the remaining candidates by shortest travel ETA from the incident location.

### 8. SafetyValidator
Executes hard-constraint validation rules:
- Verifies that the correct number of vehicles are assigned.
- Confirms all assigned vehicles are in `Available` status.
- Validates that crews meet personnel minimums.
- Verifies that all required equipment exists on-board.
- Confirms hospital capability matches and that the hospital has adequate beds remaining.

### 9. AutoAssignmentService
Wraps the resource allocation in a database transaction:
- Atomically marks vehicles as `Assigned` (using optimistic concurrency tokens, ensuring no vehicle status was modified since read).
- Atomically increments hospital load counters (using capacity-checked update clauses, preventing over-commitment).
- Performs a secondary post-reservation Safety Validator check using fresh database reads.
- Commits the transaction, or rolls back completely if any race condition or reservation failure occurs.
