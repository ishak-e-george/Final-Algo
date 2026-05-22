# RescueFlow - Safety-Critical Rescue Case Management and Dispatch Algorithm (SC-RCMDA)

RescueFlow is an automated, web-based rescue case management and dispatch system running the **SC-RCMDA (Safety-Critical Rescue Case Management and Dispatch Algorithm)**. The system is designed to handle high-consequence, life-critical emergency responses with zero tolerance for resource double-booking, capacity over-commitment, or under-response.

## System Tech Stack
- **Backend**: ASP.NET Core MVC (C#, .NET 8)
- **Database**: SQL Server / LocalDB (via Entity Framework Core with optimistic concurrency)
- **Frontend**: Razor Views + Bootstrap (Premium glassmorphic theme)
- **Testing**: xUnit (29 automated test cases covering core modules, rollback safety, and concurrency)

---

## The Workflow: Sequential Pipeline with Conditional Branches & Revalidation

SC-RCMDA is **not a fully linear algorithm**. While it processes cases in a structured pipeline, it utilizes dynamic loops, conditional branches (e.g., bypassing hospital selection for minor injuries, activating multi-hospital disaster modes for mass casualties), and dual-phase safety validations (pre-reservation and atomic post-reservation checks).

### High-Level Workflow Diagram

```text
               User Submits RescueCase Form
                             │
                             ▼
                 [ 1. Schema & Rule Validation ] ─── Reject/Escalate if Invalid 
                             │
                             ▼
                 [ 2. Case Classification ]
                             │
                             ▼
                 [ 3. Severity Scoring & Overrides ]
                             │
                             ▼
                 [ 4. Incident Requirement Matrix ]
                             │
                             ▼
                 [ 5. Vehicle Constraint Filtering ]
                             │
                             ▼
                 [ 6. Personnel & Equipment Matching ]
                             │
                             ▼
                 [ 7. Traffic-Aware A* Routing / ETA ]
                             │
                             ▼
                 [ 8. Branch & Bound Vehicle Assignment ]
                             │
            ┌────────────────┴────────────────┐
            ▼                                 ▼
   [ Hospital Needed? ]             [ Disaster Level (Severity 5)? ]
      ├── No ──┐                       ├── Yes ── [ 10. Disaster Min-Cost Flow ]
      └── Yes ─┼─► [ 9. Hospital Match ] └── No  ─── Normal Allocation
               └─────────────┬───────────────┘
                             │
                             ▼
                 [ 11. Pre-Safety Validation ] ──── Fail ──► [ BlockedNoSafePlan ]
                             │
                             ▼
                 [ 12. Transactional Locking ] ──── Fail ──► [ Rollback & Retry ]
                             │
                             ▼
                Save Plan & Set Auto-Assigned
```

---

## System Inputs and Data Sources

### Primary Algorithm Input (Rescue Case Form)
* **Location Data**: Latitude and Longitude coordinates.
* **Metadata**: Incident category, injury type, description.
* **Victim Metrics**: Number of patients.
* **Risk Flags**: Fire risk, trapped victim risk, chemical risk, explosion risk.
* **Symptom Flags**: Severe bleeding, breathing problems, unconscious patients.

### System Data Sources (Read from Database)
* **Stations & Vehicles**: Locations, capabilities, types, and real-time status.
* **Personnel & Crews**: Active crew sizes and certifications per vehicle.
* **Equipment Inventory**: Specialized gear (Gas detectors, hydraulic cutters, trauma kits).
* **Hospitals**: Locations, ICU/trauma bed capacities, and specialized departments.
* **Network Graph**: Road nodes, edges, distance metrics, and real-time traffic profiles.

### Algorithm Output (The Response Plan)
The output is a complete, executable response plan containing:
* Selected dispatch vehicles and dispatching stations.
* Assigned crews and equipment manifests.
* Selected receiving hospital (if medical transport is required).
* Multi-segment route paths and traffic-aware ETAs.
* Deterministic Safety Validation signature.
* Final case status (`AutoAssigned`, `BlockedNoSafePlan`, or `Escalated`).

---

## Detailed SC-RCMDA Sub-Algorithms

### 1. Case Validation (Schema + Business Rules)
* **Description**: The system validates incoming forms to prevent garbage data from entering a safety-critical queue.
* **Inputs**: Raw Rescue Case Form data.
* **Outputs**: Validated Case Facts or immediate rejection with validation errors.
* **Why**: Prevents critical system crashes or impossible routing requests (e.g., negative patient counts, invalid coordinates).

### 2. Rule-Based Case Classification
* **Description**: Matches keyword patterns and checkbox attributes to infer secondary rescue demands.
* **Inputs**: Category, injury type, hazard flags, and description text.
* **Outputs**: Classifications (`HazMat`, `Entrapment`, `ActiveFire`) and resource flags (`RequiresEMS`, `RequiresHeavyRescue`, `RequiresFire`).
* **Why**: Human operators might misclassify incidents under stress. Business rules guarantee that a "Road Accident" description containing "trapped" automatically triggers heavy rescue demands.

### 3. Severity Scoring with Override Rules
* **Description**: Computes a base severity score ($1$ to $5$) and applies safety overrides. For example, a minor incident with an unconscious patient is forced to Severity 4.
* **Inputs**: Symptom indicators, patient counts, hazard flags.
* **Outputs**: Final Severity Level ($1$ to $5$).
* **Why**: Prevents under-response. Over-response is safer than under-response. If critical symptoms (unconsciousness, breathing difficulty) are selected, the severity is boosted regardless of the base incident type.

### 4. Requirement Matrix Lookup
* **Description**: Map of case category and severity levels to precise physical needs.
* **Inputs**: Case classification, severity level, patient count.
* **Outputs**: Needed vehicle types, minimum crew sizes, mandatory equipment, and hospital capabilities.
* **Why**: Eliminates guess-work. The system maps severity and hazards to exact minimum standards dynamically.

### 5. Vehicle Constraint Filtering
* **Description**: Scans the global fleet and eliminates ineligible vehicles before any scoring occurs.
* **Inputs**: Global vehicle table, station status, resource requirements.
* **Outputs**: List of candidate vehicles.
* **Why**: Optimization must only run on valid assets. Eliminates vehicles in maintenance, vehicles stationed at inactive depots, or vehicles already dispatched.

### 6. Personnel and Equipment Matching
* **Description**: Performs set-inclusion checks to ensure candidate vehicles are physically capable of performing the mission.
* **Inputs**: Candidate vehicles, required equipment list, minimum crew counts.
* **Outputs**: Fully eligible vehicles.
* **Why**: An available ambulance is unsafe to dispatch if it lacks a complete crew or missing trauma kits.

### 7. Traffic-Aware A* Routing
* **Description**: Computes the optimal path through the road network using real-time congestion weights.
* **Inputs**: Vehicle coordinates, scene coordinates, road edges, traffic multipliers.
* **Outputs**: Shortest path by duration, estimated travel time (ETA).
* **Why**: A straight-line distance (Haversine) or standard Dijkstra search can miscalculate travel times by neglecting live traffic jams or blocked roads.

### 8. Branch & Bound Vehicle Assignment
* **Description**: Performs combinatorial optimization to find the best set of vehicles to satisfy the case requirements.
* **Inputs**: Eligible vehicles list, ETAs, station coverage penalties.
* **Outputs**: Optimal vehicle combination.
* **Why**: Simple greedy selection (assigning the closest vehicle) can deplete critical stations and leave nearby sectors unprotected. Branch and Bound checks all valid combinations, prioritizing total minimal ETA and station safety coverage, while skipping paths mathematically proven to be sub-optimal.

### 9. Hospital Filtering and Ranking
* **Description**: Evaluates and ranks receiving facilities based on specialized medical capabilities and live capacity.
* **Inputs**: Hospital locations, ICU/trauma loads, required specialty (e.g., Burn unit, Cardiac department), incident route.
* **Outputs**: Best receiving hospital (or skipped if minor injury).
* **Why**: Hospital distance is secondary to capability. Sending a severe burn victim to a close hospital without a burn unit is a fatal routing error; the algorithm enforces capability matching first, then ranks by traffic-aware ETA.

### 10. Disaster Mode Allocation
* **Description**: Activated for mass casualty events (Severity 5). Solves a Min-Cost Flow network model to distribute load across multiple resources.
* **Inputs**: Total patient count, available vehicles, hospital capacities.
* **Outputs**: Disaster plan distributing patients proportionally across multiple hospitals.
* **Why**: In a disaster, routing all victims to the closest hospital will immediately overwhelm its ICU. A Min-Cost Flow algorithm balances patient transport times against hospital capacity constraints to maximize survival rates.

### 11. Safety Validator (Deterministic Safety Invariant Checker)
* **Description**: A hardcoded, read-only rule evaluator that verifies all constraints are met before committing.
* **Inputs**: Generated Response Plan, case facts, requirement lists.
* **Outputs**: `SafetyValidationPassed` (Boolean) and validation errors.
* **Why**: Functions as a software firewall. If the optimizer contains a bug that generates an invalid resource mapping, the validator catches the invariant violation and blocks assignment.

### 12. Transactional Locking & Concurrency Protection
* **Description**: Enforces atomicity at the database level during commit.
* **Inputs**: Chosen response plan, SQL Server connection context.
* **Process**:
  1. Open SQL transaction.
  2. Perform row-level locking with optimistic concurrency check (`RowVersion` tokens) on selected vehicles.
  3. Validate vehicle status is still `Available`.
  4. Perform capacity-checked update on hospital beds.
  5. Save Response Plan and change case status to `AutoAssigned`.
  6. Commit transaction.
* **Why**: Prevents double-booking. If two cases are processed simultaneously and select the same last available ambulance, the database aborts one transaction, forcing the algorithm to recalculate a plan with the remaining resources.

---

## How to Set Up and Run the Application

Please refer to the setup instructions in the [main project directory README](README.md) to clean the database, apply migrations, run the MVC server on port 5000, and execute the automated xUnit tests or manual smoke-test scripts.
