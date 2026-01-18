# Testing Relations with ortonville-golf-course.xml

## File Information

**Location**: `OSM Test Data/ortonville-golf-course.xml`

**Contents**: Real-world OSM data from Ortonville, Minnesota containing:
- **20 relations total**
- **14 golf fairway multipolygons** (with inner holes)
- **1 boundary relation** (Ortonville city administrative boundary)
- **1 water multipolygon** (with islands)
- **4 route relations** (roads - these have no way members, so won't be parsed)

## Relations Breakdown

### Multipolygon Relations

#### Golf Fairways (14 relations)
- Tags: `golf=fairway`, `landuse=grass`, `type=multipolygon`
- Structure: Each has 1 outer way + 1 inner way (representing sand traps/holes)
- IDs: 19472381-19472394
- **Perfect test for multipolygon with holes**

#### Water Feature (1 relation)
- Tags: `natural=water`, `source=NHD`, `type=multipolygon`
- Structure: 1 outer way + 2 inner ways (islands)
- ID: 548148
- **Tests multipolygon with multiple inner rings**

### Boundary Relations

#### Ortonville City (1 relation)
- Tags: `boundary=administrative`, `admin_level=8`, `name=Ortonville`
- Structure: 6 outer ways (forming city boundary)
- ID: 136864
- **Tests boundary type with multiple outer ways**

### Route Relations (4 relations - NOT PARSED)
- Tags: `route=road`, `type=route`
- Structure: Many ways with empty roles
- **These won't be parsed because they have empty roles (not "outer"/"inner")**

## Test Cases in Grasshopper

### Test 1: Golf Fairways
```
Component: Extract Relations
Inputs:
  - OSM File Path: /Volumes/CSData/Development/github/Caribou/OSM Test Data/ortonville-golf-course.xml
  - OSM Features: golf=fairway
  - Output Merged: True

Expected Output:
  - Relations: 14 Breps (each with a hole representing sand traps)
  - Tags: Should show golf=fairway, landuse=grass, type=multipolygon
  - Report: "14 items found for golf=fairway"
```

### Test 2: Water Features
```
Component: Extract Relations
Inputs:
  - OSM File Path: /Volumes/CSData/Development/github/Caribou/OSM Test Data/ortonville-golf-course.xml
  - OSM Features: natural=water
  - Output Merged: True

Expected Output:
  - Relations: 1 Brep (with 2 holes representing islands)
  - Tags: Should show natural=water, source=NHD, type=multipolygon
  - Report: "1 item found for natural=water"
```

### Test 3: City Boundary
```
Component: Extract Relations
Inputs:
  - OSM File Path: /Volumes/CSData/Development/github/Caribou/OSM Test Data/ortonville-golf-course.xml
  - OSM Features: boundary=administrative
  - Output Merged: True

Expected Output:
  - Relations: 1 Brep (city outline)
  - Tags: Should show boundary=administrative, name=Ortonville, admin_level=8
  - Report: "1 item found for boundary=administrative"
```

### Test 4: Separate Polylines Mode
```
Component: Extract Relations
Inputs:
  - OSM File Path: /Volumes/CSData/Development/github/Caribou/OSM Test Data/ortonville-golf-course.xml
  - OSM Features: golf=fairway
  - Output Merged: False

Expected Output:
  - Relations: 28 Curves total (14 fairways × 2 curves each: outer + inner)
  - Tree structure: {0}{0}{0} = first fairway outer, {0}{0}{1} = first fairway inner
  - Report: "14 items found for golf=fairway"
```

## Debug Output to Expect

When running with the golf course file, the debug console should show:

```
Pass 1 complete: Found [thousands] nodes
Parsing complete: [X] nodes, [Y] ways, 20 total relations, 16 with way members
Relation 136864: 6 members, tags: boundary=administrative, admin_level=8, name=Ortonville, ...
Relation 548148: 3 members, tags: natural=water, type=multipolygon, ...
Relation 19472381: 2 members, tags: golf=fairway, landuse=grass, type=multipolygon
[... 13 more golf fairways ...]
Checking relation [ID] with [N] tags against 1 requests: [0 or 1] matches
```

## Why This is a Good Test

1. **Real-world data**: Actual OSM export, not synthetic test data
2. **Multiple relation types**: Multipolygons and boundaries
3. **Complex geometry**: Multiple outer/inner rings
4. **Variety of features**: Golf, water, administrative boundaries
5. **Large enough**: 16 parseable relations tests performance
6. **Has holes**: Inner rings test boolean difference operations

## Known Limitations

The 4 route relations (roads) won't be parsed because:
- They have members with empty roles (not "outer" or "inner")
- Current implementation only processes outer/inner members
- This is expected behavior and documented in the implementation

## If Output is Still Empty

Check debug output for:
1. **"0 with way members"** → File has relations but they reference ways that don't exist
2. **"0 matches"** → Your feature tag doesn't match any relation tags
3. **"0 total relations"** → File isn't being read correctly

Most likely issue: Feature tag mismatch. Try these:
- `golf=fairway` (should find 14)
- `natural=water` (should find 1)
- `boundary=administrative` (should find 1)
- `golf` (just the key, should find 14)
