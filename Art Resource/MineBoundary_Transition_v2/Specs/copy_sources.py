from pathlib import Path
import shutil,json
root=Path('Art Resource/MineBoundary_Transition_v2')
for n in ['Sources','Previews','Textures/Corners','Textures/EndCaps','Textures/Straights','Textures/WallRoots','Specs']:(root/n).mkdir(parents=True,exist_ok=True)
paths={"convex":"C:\\Users\\User\\.codex\\generated_images\\01a177e7-d53e-7273-979b-0b666b9c7508\\exec-ac76db03-166c-4440-810a-079b14dee33b.png","concave":"C:\\Users\\User\\.codex\\generated_images\\01a177e7-d53e-7273-979b-0b666b9c7508\\exec-f28bc1dc-e9a5-4d3e-86c3-af41a51d4a01.png","cap":"C:\\Users\\User\\.codex\\generated_images\\01a177e7-d53e-7273-979b-0b666b9c7508\\exec-36790dc5-a6f8-4c58-978a-9c67547627a0.png","straight1":"C:\\Users\\User\\.codex\\generated_images\\01a177e7-d53e-7273-979b-0b666b9c7508\\exec-daee3d96-7cd1-44fb-9d4b-056ad6a24df3.png","straight2":"C:\\Users\\User\\.codex\\generated_images\\01a177e7-d53e-7273-979b-0b666b9c7508\\exec-d65d98b0-10b0-4161-bff1-479163c1202c.png","root":"C:\\Users\\User\\.codex\\generated_images\\01a177e7-d53e-7273-979b-0b666b9c7508\\exec-52c62077-edd2-45af-8a49-e174ce445cc6.png"}
for n,p in paths.items():shutil.copy2(p,root/'Sources'/('Generated_'+n+'.png'))
for n in ['game-after.png','region1-left-tip-comparison.png','region2-middle-tip-comparison.png','region3-right-steps-comparison.png']:shutil.copy2(Path('Library/BoundaryCornerFix')/n,root/'Sources'/n)

