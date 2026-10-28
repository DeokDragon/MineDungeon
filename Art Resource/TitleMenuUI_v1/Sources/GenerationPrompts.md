# Built-in image_gen prompt set

## Shared logo prompt
Standalone production game title logo, transparent RGBA background, no backdrop, no plaque rectangle, no mockup. Exact main Korean text four syllables in ONE HORIZONTAL LINE: 광산던전 . It MUST read 광 산 던 전, no other Hangul, preserve all correct letter shapes and spacing, broad clear block construction. Exact smaller subtitle below: MINE DUNGEON. Straight-on 2D pixel art game UI logo, clustered crisp stepped pixels and dark near-black outline, restrained mining mood from reference background used ONLY for palette/style, no background/character included. Intended final logo720x240, wide3:1 composition, generous transparent padding around letters. Main lettering large and highly readable at screen size. No extra words, no watermark, no decorative weapons, no glow, no crystals protruding over letters, no mascot.

### A suffix
Variant A: stone-cut Korean lettering, warm grey mineral rock faces with subtle shallow facets, very restrained copper seam/inlay, dark chiseled depth, light sandstone upper bevel for legibility against dark mine. Subtitle small aged bronze, clean block sans capitals. Minimal fine cracks away from critical Hangul strokes.

### B suffix
Variant B: aged copper/bronze cast-metal Korean lettering, warm restrained brass top bevel, charcoal inset sidewalls, a few small iron rivets in solid broad letter faces, no stone slabs behind text. Distinctly simpler solid squared silhouette than stone variant, subtitle cream/desaturated bronze, no flourish.

Reference: TitleMinerSlime_v1/Previews/Title_Placement_A_2752x1536.png, for scene palette only.

## Normal button
One standalone EMPTY game menu button background sprite. Transparent RGBA outside, no text no symbols no icons. Intended final360x80, horizontal4.5:1 rectangular thin dark stained wood plank inset within restrained aged copper/bronze edge and tiny dark iron corner pins, almost-square clipped2px corners. Face dark muted brown charcoal, low contrast subtle horizontal grain, a quiet dark interior so cream Korean labels from Unity can read later.4px copper border in final360x80 logical pixels, corners decorations contained within20px. NORMAL unselected state, no glow, no highlight selection, no drop shadow outside bounds. Exact centered front-on rectangular geometry, clean uniform quiet middle rails for9-slice. Frame style/palette harmonizes reference title mine scene but no source scene included. Crisp clustered pixel art, no blurry antialiasing, no huge ornaments, no bevel taking over the label area. Fill canvas with this one long low rectangle and minimal transparent outer padding, not multiple assets or states.

## State edit shared prompt
Edit ONLY the visual state of this exact empty wooden/copper pixel-art game button. Preserve exact rectangular outline, canvas, corners, same four rivet positions, same wood-grain structures, same border thickness. [STATE] One standalone button, genuine transparent RGBA outside. NO TEXT NO ICONS NO symbols. Do not add ornaments, don't redraw geometry, don't move any part or grow/shrink. Intended export360x80 and9-slice. Keep dark mining palette and clear solid pixel edges.

Hover STATE: HOVER / highlighted state: restrained warmer brighter copper edge, a very slight warm lift in wood face, no magical glow, no outside glow. Border visibility slightly stronger than normal, center stays dark for text.

Pressed STATE: PRESSED / held down state: darker upper interior bevel and wood face, subtle inset depth, slightly subdued copper. No external displacement or scale. Keep the same visible outer frame extents; should read physically pressed rather than disabled.

Export: nearest exact-size PNGs;80-color logos/64-color buttons, binary alpha threshold160; identical outer alpha footprint across button states. Final borders measured conservatively asL28/B20/R28/T20. Prompt intended border thickness is not the same thing as the9-slice fixed corner region.
