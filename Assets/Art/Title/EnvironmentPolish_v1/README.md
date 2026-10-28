# OREPLUME title environment polish v1

Eight byte-identical copies from `Art Resource/TitleEnvironmentPolish_v1`: the no-mark 2752×1536 background, six 1024×1024 Wood/Rock/Floor light textures and the shared 384×384 halo. Original background and lantern assets remain in `OREPLUME_v1`.

Import: Sprite Single, FullRect, centered pivot, PPU100, Point, sRGB, uncompressed, mipmaps off, NPOT scaling off, maximum size4096. No transparent padding is cropped. All use the existing normal UI alpha blend.

`Title.unity` replaces nine sprite references on its existing Images: background, six surface lights and the two halo Images sharing one texture. No overlay is added on top of an old overlay. Existing flame frames, glass, logo A, buttons, slime, transforms, draw order and animation code remain unchanged.

Trial settings remain surface opacity0.35, light multiplier0.88–1.00 with periods2.6/2.9s and barrel phase1.1rad; flames6×0.14/0.16s with barrel offset0.32s; slime6×0.24s. The optional frame-energy proposal in the source README is not implemented. These are not final approved brightness values.

The initial one-off `TitleScreenSetup.BuildTrial()` was not rerun. The saved Title scene holds the polished sprite assignments. No Game View custom resolution entries were added, removed or restored during this work.
