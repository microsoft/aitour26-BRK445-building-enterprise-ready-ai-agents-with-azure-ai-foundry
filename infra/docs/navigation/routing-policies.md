# Routing Policies and Constraints

## Priorities

1. Minimize total walking distance using weighted edges from `store-graph.json`.
2. Defer refrigerated or temperature-sensitive pickups until the final 25% of the path length.
3. When multiple routes tie, select the one that enters checkout (E1) from the shortest final edge.

## Handling Restricted Areas

- Node `AISLE_C1` is restricted during store hours while hazardous cages are unlocked.
- Only traverse restricted nodes if `allowRestricted` flag is explicitly set by the user.
- When avoiding restricted nodes, reroute via `AISLE_B2 -> AISLE_C2` if available.

## Congestion Windows

| Zone | Start | End | Guidance |
| --- | --- | --- | --- |
| A | 08:00 | 11:00 | Prefer entry via `ENTRANCE_SIDE` to bypass restocking in A1. |
| B | 12:00 | 13:00 | Allow 1.5x distance tolerance to account for lunch rush. |
| E | 17:00 | 19:00 | Queue at `CHECKOUT_E1` may add 5 minutes; notify user. |

## Output Expectations

- Provide `OrderedStops` referencing node IDs from the graph.
- Include `EstimatedDistance` (meters) and `EstimatedTime` (minutes) assuming 70 meters/min baseline, plus congestion adjustments.
- Document any deviations from default policies in the `Notes` section of the response.
