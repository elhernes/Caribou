# Diagnostic Output for Relation Parsing

## How to View Diagnostic Information

The relation parsing now outputs diagnostic information directly in the **Report** output of the Extract Relations component. This is visible in Grasshopper without needing any external tools.

## What You'll See

### Report Output Structure

The Report output is a tree with multiple branches:

**Branch {0}** - First feature request results:
- Feature name (e.g., "Golf")
- Full tag (e.g., "golf=fairway")
- Count (e.g., "14 found")
- Color information
- Parent feature (if applicable)

**Branch {1}** - Second feature request (if multiple features requested)
- Same structure as above

**Last Branch** - Parsing Summary (NEW):
- "Parsing Summary:"
- Diagnostic line showing: "Parsed X nodes, Y ways, Z relations (W with way members)"

### Example Output

For ortonville-golf-course.xml with feature `golf=fairway`:

```
{0}
  - "Fairway"
  - "golf=fairway"
  - "14 found"
  - [color info]
  - "Golf"
  - "Golf::Fairway"

{1}
  - "Parsing Summary:"
  - "Parsed 8234 nodes, 1567 ways, 20 relations (16 with way members)"
```

## Interpreting the Diagnostic Line

**Format**: `Parsed X nodes, Y ways, Z relations (W with way members)`

### What Each Number Means

**X nodes**: Total unique nodes (coordinate points) in the file
- These form the vertices of ways

**Y ways**: Total ways (polylines) in the file
- Ways are sequences of nodes that form lines/polygons

**Z relations**: Total relation elements found in the file
- Includes ALL relations, even those we can't parse yet

**W with way members**: Relations that have at least one way-type member
- Only these can be parsed currently
- Node-type and relation-type members are skipped

### What to Look For

#### ✅ Working Correctly
```
Parsed 8234 nodes, 1567 ways, 20 relations (16 with way members)
```
- Relations were found
- Some have way members
- If output is empty, check if your feature tag matches

#### ⚠️ No Relations Found
```
Parsed 8234 nodes, 1567 ways, 0 relations (0 with way members)
```
- File has no `<relation>` elements
- Use a different OSM file (try ortonville-golf-course.xml)

#### ⚠️ Relations But No Way Members
```
Parsed 8234 nodes, 1567 ways, 20 relations (0 with way members)
```
- Relations exist but only have node-type or relation-type members
- These aren't supported yet (future enhancement)
- Example: Route relations often only have nested relations

#### ⚠️ Way Members But Empty Output
```
Parsed 8234 nodes, 1567 ways, 20 relations (16 with way members)
```
But the Relations output is still empty:
- Relations were parsed successfully
- **Issue**: Your feature tag doesn't match any relation tags
- **Fix**: Check what tags the relations have
  - Try just the key: `golf` instead of `golf=fairway`
  - Try different values: `boundary=administrative`
  - Try `natural=water`

## Common Scenarios

### Scenario 1: Empty Output, No Relations Parsed
**Report shows**: `0 relations (0 with way members)`
**Cause**: File has no relations
**Solution**: Use ortonville-golf-course.xml which has 16 parseable relations

### Scenario 2: Empty Output, Relations Parsed
**Report shows**: `20 relations (16 with way members)` + count shows "0 found"
**Cause**: Feature tag mismatch
**Solution**: Check relation tags in the OSM file or try:
- `golf=fairway`
- `natural=water`
- `boundary=administrative`
- Just the key: `golf`

### Scenario 3: Some Found
**Report shows**: `20 relations (16 with way members)` + count shows "14 found"
**Result**: ✅ Working! 14 relations matched your feature
**Geometry**: Check Relations output for Breps or Curves

## Testing with Golf Course File

### Expected Output for Different Features

**Feature: `golf=fairway`**
```
{0}
  - "Fairway"
  - "golf=fairway"
  - "14 found"

{1}
  - "Parsing Summary:"
  - "Parsed ~8000 nodes, ~1500 ways, 20 relations (16 with way members)"
```

**Feature: `natural=water`**
```
{0}
  - "Water"
  - "natural=water"
  - "1 found"

{1}
  - "Parsing Summary:"
  - "Parsed ~8000 nodes, ~1500 ways, 20 relations (16 with way members)"
```

**Feature: `boundary=administrative`**
```
{0}
  - "Administrative"
  - "boundary=administrative"
  - "1 found"

{1}
  - "Parsing Summary:"
  - "Parsed ~8000 nodes, ~1500 ways, 20 relations (16 with way members)"
```

## Why Only 16 of 20 Relations Are Parseable

The golf course file has 20 total relations:
- **14 golf fairways** - multipolygons with way members ✅
- **1 water feature** - multipolygon with way members ✅
- **1 city boundary** - boundary with way members ✅
- **4 route relations** - have way members BUT empty roles ❌

The 4 route relations aren't parsed because they use empty roles (`role=""`) instead of "outer"/"inner". This is a known limitation documented in the implementation.

## No External Tools Needed

Unlike the previous debug approach (which required `log stream`), this diagnostic information is built directly into the Report output that's always visible in Grasshopper. Just connect a Panel to the Report output to see it.
