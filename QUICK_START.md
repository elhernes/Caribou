# Quick Start - Testing Relation Parsing

## 1. Load the Plugin

**File**: `/Volumes/CSData/Development/github/Caribou/Caribou/bin/Debug/net7.0-windows/Caribou.gha`

Copy to your Grasshopper components folder or load directly in Grasshopper.

## 2. Set Up Component

In Grasshopper:

1. Add component: **Extract Relations** (search for "Relations")
2. Connect inputs:
   - **OSM File Path**: `/Volumes/CSData/Development/github/Caribou/OSM Test Data/ortonville-golf-course.xml`
   - **OSM Features**: `golf=fairway`
   - **Output Merged**: `True` (default)
3. Connect outputs to Panels:
   - **Relations** → Will show Breps if working
   - **Tags** → Will show relation metadata
   - **Report** → **IMPORTANT: Connect this to see diagnostic info**

## 3. Check the Report Output

The Report output will show:

### If Working (Expected)
```
{0}
  - "Fairway"
  - "golf=fairway"
  - "14 found"          ← 14 golf fairways found!
  ...

{1}
  - "Parsing Summary:"
  - "Parsed ~8000 nodes, ~1500 ways, 20 relations (16 with way members)"
                                                      ↑
                                          This means parsing worked!
```

### If Empty Output
```
{0}
  - "Fairway"
  - "golf=fairway"
  - "0 found"           ← Nothing matched your feature

{1}
  - "Parsing Summary:"
  - "Parsed ~8000 nodes, ~1500 ways, 20 relations (16 with way members)"
                                                      ↑
                                    Relations were parsed, but tag didn't match
```

**Fix**: Try different features:
- `golf` (just the key)
- `natural=water`
- `boundary=administrative`

## 4. What You Should See

### Relations Output (if working)
- **14 Breps** (golf fairways)
- Each Brep should have a **hole** (representing sand traps)
- Breps should be positioned in geographic space

### Tags Output
- Shows metadata for each relation
- Look for: `golf=fairway`, `landuse=grass`, `type=multipolygon`

### Report Output
- First branch: Feature match results ("14 found")
- Last branch: **Parsing Summary** with statistics

## 5. Test Other Features

### Water Features (1 expected)
```
OSM Features: natural=water
Expected: 1 Brep with 2 holes (islands)
```

### City Boundary (1 expected)
```
OSM Features: boundary=administrative
Expected: 1 Brep (city outline)
```

## 6. If Still Empty

**Check the Report's "Parsing Summary" line**:

### Scenario A: `0 relations (0 with way members)`
- **Problem**: File has no relations
- **Fix**: Verify file path is correct
- **Try**: Use the full path shown above

### Scenario B: `20 relations (0 with way members)`
- **Problem**: Relations only have node-type or relation-type members
- **Fix**: These aren't supported yet - use golf course file which has way-type members

### Scenario C: `20 relations (16 with way members)` but "0 found"
- **Problem**: Your feature tag doesn't match
- **Fix**: Try these exact tags:
  - `golf=fairway` (should find 14)
  - `natural=water` (should find 1)
  - `boundary=administrative` (should find 1)
  - `golf` (just the key, should find 14)

## 7. Try Separate Polylines Mode

Change **Output Merged** to `False`:
- **Relations output**: Now shows separate curves instead of merged Breps
- **Expected**: 28 curves (14 fairways × 2 curves each: outer + inner)
- **Tree structure**: `{0}{0}{0}` = first fairway outer ring, `{0}{0}{1}` = first fairway inner ring

## Success Criteria

✅ Report shows "14 found for golf=fairway"
✅ Report shows "Parsed ... 20 relations (16 with way members)"
✅ Relations output contains 14 Breps
✅ Breps have visible holes (sand traps)
✅ Tags output shows golf=fairway, type=multipolygon

## Need Help?

Share the contents of the **Report output** - it will show exactly what's happening during parsing.

See [DIAGNOSTIC_OUTPUT.md](DIAGNOSTIC_OUTPUT.md) for detailed interpretation of the Report.
