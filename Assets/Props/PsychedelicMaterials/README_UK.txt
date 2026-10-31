ПСИХОДЕЛІЧНІ АЛЬТЕРНАТИВНІ МАТЕРІАЛИ

Використання: вибери об'єкт із Mesh Renderer та заміни матеріал у
списку Materials відповідним матеріалом із цієї папки.

Звичайний матеріал -> Альтернативний
M_ManorWall -> M_ManorWall_Psy
M_ManorWall_Trim -> M_ManorWall_Trim_Psy
M_ManorFloor -> M_ManorFloor_Psy
M_ManorStoneFloor -> M_ManorStoneFloor_Psy
M_ManorParquetFloor -> M_ManorParquetFloor_Psy
M_ManorCeiling -> M_ManorCeiling_Psy

Для стіни заміни обидва слоти, включно з Trim.
Для підлоги та стелі заміни один слот.
Карта Wall зберігає горизонтальну межу панелей. Карти підлог зберігають
сітку дощок/плит. Нової UV-розгортки або моделі не потрібно.
Використовуються існуючі дзеркальні UV для збігу країв модулів.
Дзеркальна симетрія й повторення орнаменту можуть бути помітними.

Це статичні Base Color текстури та матеріали URP Lit. Emission вимкнений.
Освітлення сцени впливає на їхній вигляд як і на звичайні матеріали.
Автоматичного перемикання, анімації та зв'язку з радіо поки немає.
Це альтернативи: початкові матеріали та prefab не замінені.

Повторна збірка матеріалів: Tools > Echo > Build Psychedelic Materials.
Blender-прев'ю: work/psychedelic_materials/PsychedelicMaterials_Preview.blend.
Стеля у прев'ю показана окремою перевернутою плитою справа.
