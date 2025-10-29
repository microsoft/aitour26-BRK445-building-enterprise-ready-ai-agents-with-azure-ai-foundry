# Navigation Scenarios

This document provides detailed step-by-step navigation instructions for various routes through the store. Each scenario returns a NavigationInstructions object that can be used to guide customers.

## Scenario Format

Each scenario includes:

- **Route**: The path through store nodes
- **NavigationInstructions JSON**: Complete object with StartLocation, Steps array, and EstimatedTime
- **Notes**: Any special considerations (restricted areas, congestion, etc.)

## NavigationInstructions Schema

All scenarios return a JSON object matching this C# class structure:

```csharp
public class NavigationInstructions
{
    public string StartLocation { get; set; } = string.Empty;
    public NavigationStep[] Steps { get; set; } = Array.Empty<NavigationStep>();
    public string EstimatedTime { get; set; } = string.Empty;
}

public class NavigationStep
{
    public string Direction { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NavigationLandmark? Landmark { get; set; }
}

public class NavigationLandmark
{
    public string? Description { get; set; }
    public Location? Location { get; set; }
}
```

---

## Scenario 1: Front Entrance to Power Tools (Zone B1)

**Route**: ENTRANCE_FRONT → AISLE_A1 → AISLE_A2 → AISLE_B2 → AISLE_B1

**NavigationInstructions JSON**:

```json
{
  "StartLocation": "Front Entrance - Zone A",
  "Steps": [
    {
      "Direction": "Enter through the front entrance",
      "Description": "Welcome! Enter through the main front entrance doors.",
      "Landmark": {
        "Description": "Front Entrance - Zone A",
        "Location": null
      }
    },
    {
      "Direction": "Walk straight ahead",
      "Description": "Proceed straight for 12 meters past the seasonal displays.",
      "Landmark": {
        "Description": "Aisle A1 - Seasonal Decor & Lighting",
        "Location": null
      }
    },
    {
      "Direction": "Continue to the next aisle",
      "Description": "Walk 8 meters to Aisle A2.",
      "Landmark": {
        "Description": "Aisle A2 - Paint & Brushes",
        "Location": null
      }
    },
    {
      "Direction": "Turn left",
      "Description": "Turn left and proceed 6 meters into the Tools zone.",
      "Landmark": {
        "Description": "Aisle B2 - Hand Tools & Fasteners",
        "Location": null
      }
    },
    {
      "Direction": "Arrive at destination",
      "Description": "Walk 5 meters forward to reach the Power Tools section.",
      "Landmark": {
        "Description": "Aisle B1 - Power Tools & Safety Gear",
        "Location": null
      }
    }
  ],
  "EstimatedTime": "1 minute (41 meters total distance)"
}
```

---

## Scenario 2: Side Entrance to Electrical Supplies (Zone C2) - Avoiding Restricted Area

**Route**: ENTRANCE_SIDE → AISLE_B1 → AISLE_B2 → AISLE_C2

**NavigationInstructions JSON**:

```json
{
  "StartLocation": "Side Entrance - Zone B",
  "Steps": [
    {
      "Direction": "Enter through side entrance",
      "Description": "Use the side entrance to enter the store.",
      "Landmark": {
        "Description": "Side Entrance - Zone B",
        "Location": null
      }
    },
    {
      "Direction": "Proceed to Tools section",
      "Description": "Walk 10 meters straight ahead to the Power Tools aisle.",
      "Landmark": {
        "Description": "Aisle B1 - Power Tools & Safety Gear",
        "Location": null
      }
    },
    {
      "Direction": "Turn right",
      "Description": "Turn right and walk 5 meters to Hand Tools.",
      "Landmark": {
        "Description": "Aisle B2 - Hand Tools & Fasteners",
        "Location": null
      }
    },
    {
      "Direction": "Arrive at destination",
      "Description": "Continue 7 meters ahead. Note: We're bypassing restricted Aisle C1 per safety policies.",
      "Landmark": {
        "Description": "Aisle C2 - Electrical & Lighting Controls",
        "Location": null
      }
    }
  ],
  "EstimatedTime": "30 seconds (22 meters total distance)"
}
```

**Note**: Route avoids restricted node AISLE_C1

---

## Scenario 3: Front Entrance to Checkout (Zone E1)

**Route**: ENTRANCE_FRONT → CHECKOUT_E1

**NavigationInstructions JSON**:

```json
{
  "StartLocation": "Front Entrance - Zone A",
  "Steps": [
    {
      "Direction": "Enter through front entrance",
      "Description": "Welcome! Enter through the main front entrance.",
      "Landmark": {
        "Description": "Front Entrance - Zone A",
        "Location": null
      }
    },
    {
      "Direction": "Turn right to checkout",
      "Description": "Turn right and walk 15 meters to the checkout area.",
      "Landmark": {
        "Description": "Checkout E1 - Customer Service",
        "Location": null
      }
    }
  ],
  "EstimatedTime": "15 seconds (15 meters total distance)"
}
```

---

## Scenario 4: Multi-Stop Shopping (Paint, Tools, Garden)

**Route**: ENTRANCE_FRONT → A1 → A2 → B2 → B1 → B2 → C2 → D1

**NavigationInstructions JSON**:

```json
{
  "StartLocation": "Front Entrance - Zone A",
  "Steps": [
    {
      "Direction": "Enter through front entrance",
      "Description": "Start your shopping trip at the main entrance.",
      "Landmark": {
        "Description": "Front Entrance - Zone A",
        "Location": null
      }
    },
    {
      "Direction": "Walk to seasonal section",
      "Description": "Walk 12 meters straight ahead.",
      "Landmark": {
        "Description": "Aisle A1 - Seasonal Decor & Lighting",
        "Location": null
      }
    },
    {
      "Direction": "Continue to paint section",
      "Description": "Move 8 meters ahead to find paint and brushes.",
      "Landmark": {
        "Description": "Aisle A2 - Paint & Brushes",
        "Location": null
      }
    },
    {
      "Direction": "Head to tools area",
      "Description": "Walk 6 meters to reach the tools section.",
      "Landmark": {
        "Description": "Aisle B2 - Hand Tools & Fasteners",
        "Location": null
      }
    },
    {
      "Direction": "Visit power tools",
      "Description": "Go 5 meters to see power tools and safety equipment.",
      "Landmark": {
        "Description": "Aisle B1 - Power Tools & Safety Gear",
        "Location": null
      }
    },
    {
      "Direction": "Return through tools",
      "Description": "Walk back 5 meters through the tools area.",
      "Landmark": {
        "Description": "Aisle B2 - Hand Tools & Fasteners",
        "Location": null
      }
    },
    {
      "Direction": "Navigate to electrical",
      "Description": "Continue 7 meters to electrical supplies. (Bypassing restricted Aisle C1 for safety)",
      "Landmark": {
        "Description": "Aisle C2 - Electrical & Lighting Controls",
        "Location": null
      }
    },
    {
      "Direction": "Proceed to garden section",
      "Description": "Walk 9 meters to reach the outdoor and garden area.",
      "Landmark": {
        "Description": "Aisle D1 - Outdoor Furniture & Grills",
        "Location": null
      }
    }
  ],
  "EstimatedTime": "1.2 minutes (52 meters total distance)"
}
```

---

## Scenario 5: Quick Path with Temperature-Sensitive Items

**Route**: ENTRANCE_SIDE → B1 → B2 → C2 → C1-R (Refrigerated Storage)

**NavigationInstructions JSON**:

```json
{
  "StartLocation": "Side Entrance - Zone B",
  "Steps": [
    {
      "Direction": "Enter through side entrance",
      "Description": "Use the side entrance for efficient access.",
      "Landmark": {
        "Description": "Side Entrance - Zone B",
        "Location": null
      }
    },
    {
      "Direction": "Walk to power tools",
      "Description": "Proceed 10 meters straight ahead.",
      "Landmark": {
        "Description": "Aisle B1 - Power Tools & Safety Gear",
        "Location": null
      }
    },
    {
      "Direction": "Continue to hand tools",
      "Description": "Walk 5 meters to the hand tools section.",
      "Landmark": {
        "Description": "Aisle B2 - Hand Tools & Fasteners",
        "Location": null
      }
    },
    {
      "Direction": "Head to electrical section",
      "Description": "Move 7 meters ahead to electrical supplies.",
      "Landmark": {
        "Description": "Aisle C2 - Electrical & Lighting Controls",
        "Location": null
      }
    },
    {
      "Direction": "Final stop: refrigerated items",
      "Description": "Walk to the rear of Zone C (6 meters) to pick up temperature-sensitive items. Save refrigerated products for last to maintain freshness.",
      "Landmark": {
        "Description": "C1-R Refrigerated Storage - Ice Melt & Sensitive Paints",
        "Location": null
      }
    }
  ],
  "EstimatedTime": "40 seconds (28 meters total distance)"
}
```

**Note**: Refrigerated items collected last per routing policy

---

## Scenario 6: Checkout with Congestion Awareness (Evening Rush)

**Route**: AISLE_D1 → CHECKOUT_E1 (Evening Hours 17:00-19:00)

**NavigationInstructions JSON**:

```json
{
  "StartLocation": "Aisle D1 - Outdoor Furniture & Grills",
  "Steps": [
    {
      "Direction": "Start from garden section",
      "Description": "Begin your journey from the outdoor and garden area.",
      "Landmark": {
        "Description": "Aisle D1 - Outdoor Furniture & Grills",
        "Location": null
      }
    },
    {
      "Direction": "Navigate to checkout",
      "Description": "Walk 11 meters to reach the checkout area. Please note: Evening rush hour (5-7 PM) - expect approximately 5 minutes queue time at checkout.",
      "Landmark": {
        "Description": "Checkout E1 - Customer Service",
        "Location": null
      }
    }
  ],
  "EstimatedTime": "10 seconds walking + 5 minutes queue time (11 meters total distance)"
}
```

**Note**: Congestion window active - extended wait times expected

---

## Scenario 7: Morning Route with Restocking Avoidance

**Route**: ENTRANCE_SIDE → B1 (Morning Hours 08:00-11:00)

**NavigationInstructions JSON**:

```json
{
  "StartLocation": "Side Entrance - Zone B (Preferred morning entrance)",
  "Steps": [
    {
      "Direction": "Use side entrance",
      "Description": "Good morning! During restocking hours (8-11 AM), use the side entrance to avoid congestion.",
      "Landmark": {
        "Description": "Side Entrance - Zone B (Preferred morning entrance)",
        "Location": null
      }
    },
    {
      "Direction": "Direct path to power tools",
      "Description": "Walk 10 meters straight to power tools. This route avoids Aisle A1 where restocking carts may block the path.",
      "Landmark": {
        "Description": "Aisle B1 - Power Tools & Safety Gear",
        "Location": null
      }
    }
  ],
  "EstimatedTime": "15 seconds (10 meters total distance)"
}
```

**Note**: Avoids Zone A congestion during morning restocking (08:00-11:00)

---

## Scenario 8: Restricted Area Access (Authorized Personnel)

**Route**: ENTRANCE_SIDE → AISLE_B1 → AISLE_B2 → AISLE_C1 (Requires allowRestricted flag)

**NavigationInstructions JSON**:

```json
{
  "StartLocation": "Side Entrance - Zone B",
  "Steps": [
    {
      "Direction": "Enter through side entrance",
      "Description": "Use the side entrance. AUTHORIZED PERSONNEL ONLY - Restricted area access required.",
      "Landmark": {
        "Description": "Side Entrance - Zone B",
        "Location": null
      }
    },
    {
      "Direction": "Walk to power tools",
      "Description": "Proceed 10 meters straight ahead.",
      "Landmark": {
        "Description": "Aisle B1 - Power Tools & Safety Gear",
        "Location": null
      }
    },
    {
      "Direction": "Turn right to hand tools",
      "Description": "Turn right and walk 5 meters.",
      "Landmark": {
        "Description": "Aisle B2 - Hand Tools & Fasteners",
        "Location": null
      }
    },
    {
      "Direction": "Enter restricted plumbing area",
      "Description": "Continue 7 meters ahead. **CAUTION**: Restricted area with hazardous materials (propane, solvents). Safety equipment required. Only authorized personnel permitted during store hours.",
      "Landmark": {
        "Description": "Aisle C1 - Plumbing & HVAC (RESTRICTED - Hazardous Materials Storage)",
        "Location": null
      }
    }
  ],
  "EstimatedTime": "30 seconds (22 meters total distance)"
}
```

**WARNING**: Requires `allowRestricted=true` flag. Contains hazardous materials. Authorized personnel only.

---

## Usage Guidelines for Navigation Agent

When generating navigation responses:

1. **Match the scenario** to the user's request based on:
   - Start location (entrance points)
   - Destination (target zone/aisle)
   - Time constraints (morning/evening/rush hour)
   - Item types (temperature-sensitive, hazardous)
   - Authorization level (restricted access)

2. **Return a NavigationInstructions object** matching the C# schema:

   ```csharp
   public class NavigationInstructions
   {
       public string StartLocation { get; set; } = string.Empty;
       public NavigationStep[] Steps { get; set; } = Array.Empty<NavigationStep>();
       public string EstimatedTime { get; set; } = string.Empty;
   }
   ```

3. **Structure of the response**:
   - `StartLocation`: Clear description of where the journey begins (e.g., "Front Entrance - Zone A")
   - `Steps`: Array of NavigationStep objects with Direction, Description, and Landmark
   - `EstimatedTime`: Human-readable time estimate including distance (e.g., "1 minute (41 meters total distance)")

4. **Include contextual information**:
   - Time-based routing policies (congestion windows)
   - Safety restrictions (restricted nodes)
   - Special handling (refrigerated items last)
   - Distance information in EstimatedTime

5. **Adapt scenarios** by combining or modifying the examples above to match user needs.

6. **Always validate** against store-graph.json for valid paths and routing-policies.md for constraints.

7. **JSON Format**: Return a single NavigationInstructions object (not an array) that can be deserialized in C#.
