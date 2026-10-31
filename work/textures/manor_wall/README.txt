MANOR WALL — FULL-HEIGHT HORIZONTAL REPEAT

Suggested first module: width 2 m, height 3 m, thickness 0.15–0.25 m.
Map the entire front rectangle to the full UV square, U 0–1, V 0–1.
Do not Smart UV Project the front into scattered islands: the panel layout
needs one continuous rectangular front UV island.
Image is Base Color (sRGB). Start with Metallic 0 and Roughness 0.85.
In Unity URP Lit use Metallic 0 and Smoothness about 0.15.
Texture repeats horizontally; top and bottom are deliberately different.
Wood panels are painted in this version, not separate geometry.
Side, back and edge faces need their own suitable UV placement/material.
Place three modules side by side to check both geometry and texture seams.
Origin at a bottom corner is convenient; use the same origin convention
for all modules. Snap spacing equals module width, with no extra gap.
Keep mouldings within module width. Do not add separate end posts on
every module boundary. Use a separate corner piece for 90-degree turns.
Do not bake room lighting into this base-color map.
