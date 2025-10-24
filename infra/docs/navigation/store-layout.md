# Store Layout Overview

This document captures the reference floor layout used by navigation and location agents.

## Zones

| Zone | Description | Entry Points |
| --- | --- | --- |
| A | Seasonal & Home Improvement | Front Entrance, Side Entrance |
| B | Tools & Hardware | Side Entrance |
| C | Plumbing & Electrical | Aisle C Entry |
| D | Garden & Outdoor | Rear Entrance |
| E | Checkout & Services | Front Entrance |

## Aisle Map

| Aisle | Zone | Sections |
| --- | --- | --- |
| A1 | Zone A | Seasonal Decor, Lighting |
| A2 | Zone A | Paint, Brushes |
| B1 | Zone B | Power Tools, Safety Gear |
| B2 | Zone B | Hand Tools, Fasteners |
| C1 | Zone C | Plumbing, HVAC |
| C2 | Zone C | Electrical, Lighting Controls |
| D1 | Zone D | Outdoor Furniture, Grills |
| E1 | Zone E | Checkout, Customer Service |

## Special Notes

- Refrigerated inventory (ice melt, sensitive paints) is staged at the rear of Zone C, rack C1-R.
- Hazardous materials (propane, solvents) are stored in cages adjacent to A2 and C1.
- Restocking carts may block A1 during morning hours—flag alternate routes when navigating before 11 AM.

---

## Navigation Scenarios

### Scenario 1: Front Entrance to Power Tools (Zone B1)

**Route**: ENTRANCE_FRONT → AISLE_A1 → AISLE_A2 → AISLE_B2 → AISLE_B1

**Navigation Steps**:

1. **Direction**: "Enter through the front entrance"
   - **Description**: "Welcome! Enter through the main front entrance doors."
   - **Landmark**: Front Entrance (Zone A)

2. **Direction**: "Walk straight ahead"
   - **Description**: "Proceed straight for 12 meters past the seasonal displays."
   - **Landmark**: Aisle A1 - Seasonal Decor & Lighting

3. **Direction**: "Continue to the next aisle"
   - **Description**: "Walk 8 meters to Aisle A2."
   - **Landmark**: Aisle A2 - Paint & Brushes

4. **Direction**: "Turn left"
   - **Description**: "Turn left and proceed 6 meters into the Tools zone."
   - **Landmark**: Aisle B2 - Hand Tools & Fasteners

5. **Direction**: "Arrive at destination"
   - **Description**: "Walk 5 meters forward to reach the Power Tools section."
   - **Landmark**: Aisle B1 - Power Tools & Safety Gear

**Estimated Distance**: 41 meters  
**Estimated Time**: ~1 minute

---

### Scenario 2: Side Entrance to Electrical Supplies (Zone C2) - Avoiding Restricted Area

**Route**: ENTRANCE_SIDE → AISLE_B1 → AISLE_B2 → AISLE_C2

**Navigation Steps**:

1. **Direction**: "Enter through side entrance"
   - **Description**: "Use the side entrance to enter the store."
   - **Landmark**: Side Entrance (Zone B)

2. **Direction**: "Proceed to Tools section"
   - **Description**: "Walk 10 meters straight ahead to the Power Tools aisle."
   - **Landmark**: Aisle B1 - Power Tools & Safety Gear

3. **Direction**: "Turn right"
   - **Description**: "Turn right and walk 5 meters to Hand Tools."
   - **Landmark**: Aisle B2 - Hand Tools & Fasteners

4. **Direction**: "Arrive at destination"
   - **Description**: "Continue 7 meters ahead. Note: We're bypassing restricted Aisle C1 per safety policies."
   - **Landmark**: Aisle C2 - Electrical & Lighting Controls

**Estimated Distance**: 22 meters  
**Estimated Time**: ~30 seconds  
**Note**: Route avoids restricted node AISLE_C1

---

### Scenario 3: Front Entrance to Checkout (Zone E1)

**Route**: ENTRANCE_FRONT → CHECKOUT_E1

**Navigation Steps**:

1. **Direction**: "Enter through front entrance"
   - **Description**: "Welcome! Enter through the main front entrance."
   - **Landmark**: Front Entrance (Zone A)

2. **Direction**: "Turn right to checkout"
   - **Description**: "Turn right and walk 15 meters to the checkout area."
   - **Landmark**: Checkout E1 - Customer Service

**Estimated Distance**: 15 meters  
**Estimated Time**: ~15 seconds

---

### Scenario 4: Full Store Tour (All Zones)

**Route**: ENTRANCE_FRONT → A1 → A2 → B2 → B1 → B2 → C2 → D1 → CHECKOUT_E1

**Navigation Steps**:

1. **Direction**: "Enter through front entrance"
   - **Description**: "Start your store tour at the main entrance."
   - **Landmark**: Front Entrance (Zone A)

2. **Direction**: "Explore Seasonal & Home"
   - **Description**: "Walk 12 meters to browse seasonal decorations and lighting."
   - **Landmark**: Aisle A1 - Seasonal Decor & Lighting

3. **Direction**: "Continue to Paint section"
   - **Description**: "Move 8 meters ahead to the Paint & Brushes area."
   - **Landmark**: Aisle A2 - Paint & Brushes

4. **Direction**: "Enter Tools zone"
   - **Description**: "Walk 6 meters to the Hand Tools section."
   - **Landmark**: Aisle B2 - Hand Tools & Fasteners

5. **Direction**: "Visit Power Tools"
   - **Description**: "Go 5 meters to see our Power Tools collection."
   - **Landmark**: Aisle B1 - Power Tools & Safety Gear

6. **Direction**: "Return to Hand Tools"
   - **Description**: "Walk back 5 meters."
   - **Landmark**: Aisle B2 - Hand Tools & Fasteners

7. **Direction**: "Browse Electrical"
   - **Description**: "Continue 7 meters to Electrical & Lighting Controls. (Bypassing restricted Aisle C1)"
   - **Landmark**: Aisle C2 - Electrical & Lighting Controls

8. **Direction**: "Visit Garden section"
   - **Description**: "Walk 9 meters to the Garden & Outdoor area."
   - **Landmark**: Aisle D1 - Outdoor Furniture & Grills

9. **Direction**: "Proceed to checkout"
   - **Description**: "Finish your tour by walking 11 meters to checkout."
   - **Landmark**: Checkout E1 - Customer Service

**Estimated Distance**: 63 meters  
**Estimated Time**: ~1.5 minutes

---

### Scenario 5: Quick Path to Plumbing (Restricted Access)

**Route**: ENTRANCE_SIDE → AISLE_B1 → AISLE_B2 → AISLE_C1 (Requires allowRestricted flag)

**Navigation Steps**:

1. **Direction**: "Enter through side entrance"
   - **Description**: "Use the side entrance for quickest access."
   - **Landmark**: Side Entrance (Zone B)

2. **Direction**: "Walk to Power Tools"
   - **Description**: "Proceed 10 meters straight ahead."
   - **Landmark**: Aisle B1 - Power Tools & Safety Gear

3. **Direction**: "Turn right to Hand Tools"
   - **Description**: "Turn right and walk 5 meters."
   - **Landmark**: Aisle B2 - Hand Tools & Fasteners

4. **Direction**: "Enter restricted plumbing area"
   - **Description**: "Continue 7 meters ahead. **CAUTION**: This route passes through a restricted area with hazardous materials. Authorized personnel only."
   - **Landmark**: Aisle C1 - Plumbing & HVAC (RESTRICTED)

**Estimated Distance**: 22 meters  
**Estimated Time**: ~30 seconds  
**WARNING**: Requires `allowRestricted` flag. Contains hazardous materials storage area.
