# OREPLUME title trial

These 33 PNGs are byte-identical copies, not replacements for the source art.

- Background: `C:/Users/User/Downloads/TitleScene_background.png` (2752×1536), SHA256 `7e8cc8e61be0408d359ed514f11cbd1ae96d1b54b68dfa1ebd6ac5b24b09715b`.
- Logo A: `Art Resource/TitleMenuUI_v2/Logos/OREPLUME_Logo_A_720x240_v2.png`.
- Buttons: the three text-free Normal/Hover/Pressed PNGs in `TitleMenuUI_v1/Buttons`.
- Slime: six individual 128×128 frames and the contact shadow in `TitleMinerSlime_v1`.
- Lanterns: six individual flame frames each, glass, shared halo, separate Wood/Rock/Floor surface lights in `TitleLantern_v1`. Combined surface images and full-scene previews are not used.

Import: Point, uncompressed, no mipmaps, sRGB, FullRect, UI PPU100, no texture resizing. Button borders L28/B20/R28/T20. Slime ground pivot (0.46875,0.28125), flame pivots (0.5,0.125)/(0.5,0.15625).

The scene fits the complete 1920×1080 composition with letterboxing, including UI, at other aspect ratios. All background-bound artwork shares the 2752×1536 coordinate space and uniform scale. Logo/button/slime placement uses the README A values without cropping transparent padding.

Trial timings: slime 6×0.24s=1.44s; flames 6×0.14s and 6×0.16s with 0.32s barrel offset; light sine periods 2.6/2.9s, phases 0/1.1rad, multiplier 0.88–1.00. Flame RGB and light opacity share each lantern's multiplier; flame alpha stays 1. Surface opacity has an additional **0.35 visual trial factor** to compensate for Linear project compositing versus the art's sRGB preview. Glass and halo retain their source opacity. Final brightness remains an art review choice.

`TitleScreen` is scene-local, with no save loading/writing, run manager, or singleton. Start loads the existing Main scene. Settings is intentionally disabled: the existing PauseMenu settings require Main's save service and camera. Its settings remain available after entering the lobby. The existing confirmation view and Input System event-system configuration are copied from Main; no gameplay behaviour is copied with the confirmation view.

Authoring entry point: `TitleScreenSetup.BuildTrial()` (one-off, refuses to overwrite an existing title scene/art). Build settings prepend Title and preserve the previous scene order. Direct Main/prototype editor testing remains available. No gameplay player artwork is referenced by the title animation.
