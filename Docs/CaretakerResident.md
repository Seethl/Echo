# Анімований мешканець

Модель і три кліпи надані командою в папці `папочка/caretaker`. Оригінальний `caretaker.fbx` містив тільки площину студії, тому його не використовували як ігрову модель. У Walking/Drinking/Zombie Attack є правильна геометрія та однаковий 65-кістковий скелет Mixamo; матеріалів у цих файлах немає.

## Що підготовлено

- Із `caretaker.blend` відновлені матеріали пальта, сорочки, шкіри, взуття, очниць і ґудзиків. Використано всі 68 деталей авторського Blender-файлу, включно з вухами, плечовими накладками й ґудзиками.
- 68 дрібних деталей об’єднані в один SkinnedMeshRenderer з авторськими вагами й палітрою. Немає нових текстур або залежностей: колір задають URP/Lit матеріали.
- Ваги окремих рукавів, лицевих деталей, долонь і взуття відновлені з авторської моделі замість невдалого автоматичного skinning; полам пальта залишено авторське сполучення таза й грудей. Скелет і всі кліпи нормалізовані в метри без вкладених масштабів.
- Модель має висоту приблизно **1.95 м**, стопи над підлогою з невеликим запасом. Кореневий NavMeshAgent лишився попереднім; розміри проходів і його радіус не змінювалися.
- Рухи трьох кліпів перенесено за світовими обертаннями на авторський 17-кістковий скелет, з фіксованими довжинами кінцівок; горизонтальне переміщення знято з анімації. Root Motion вимкнений. Об’єкт рухає чинний EnemyInvestigator/NavMeshAgent.
- Додано короткий цикл спокійного дихання/слухання. Для бігу окремого кліпу не надано — під час переслідування ходьба прискорюється відповідно до швидкості агента.

## Animator і логіка

`Assets/Props/CaretakerResident/Resident.controller` використовує Idle, Walk, Attack і Drink. Імпорт **Generic**, оскільки підготовлені рухи вже перенесені на авторський скелет; Humanoid/T-pose не потрібні.

Наявний EnemyPresentation керує параметрами Speed, Suspicion, State, IsChasing, IsSearching. До нього додано два необов’язкові параметри IsAttacking та IsDrinking і посилання на реквізит чашки. Відсутні параметри в старих контролерах ігноруються.

Ходьба має два animation events Footstep, які викликають **чинний** EnemyFootstepRelay. Для ManorPrototype увімкнено useAnimationFootsteps, щоб таймер не подвоював кроки. Використовуються попередні звуки та їхня гучність.

EnemyCatchPlayer отримав необов’язковий Attack Windup Seconds (0 у старих сценах; **0.65 с** у ManorPrototype). На доступній відстані атаки запускається замах; вихід із радіуса або перешкода скасовують спробу. Відстань і пряма видимість перевіряються щокадру до фактичного захоплення; пауза зупиняє таймер. Екран поразки й контрольні точки збережені.

RemedyDelivery повідомляє презентації, коли мешканець реально п’є. Сценова тривалість пиття **5 с** відповідає прискореному кліпу Drinking (8.87 с за швидкості 1.8). Під час нього в руці з’являється чашка з чинного prefab посуду; вона ховається при перериванні. Зір і переслідування продовжують працювати через чинну логіку. Після успішного пиття відкривається попереднє завершення лікування.

## Файли та редагування

- Готові FBX, prefab, Animator Controller, матеріали й палітра: `Assets/Props/CaretakerResident`.
- Редагований Blender-файл, повторюваний build_resident.py, оригінали й SHA256: `ArtSource/CaretakerResident`. Модель з матеріалами не потребує Blender для відкриття проєкту.
- Редакторський етап: **Echo → Модель та анімації мешканця**, також доданий у кінець ManorPrototypeBuilder. Працювати поза Play Mode у ManorPrototype. Повторне застосування не створює ще одного мешканця або Animator.
- Візуальний prefab під’єднаний дочірнім об’єктом до наявного Manor Resident. Старі renderer заглушки приховані; AI, слух, патруль, об’єкти навігації, радіо та колайдери збережені.

Змінені спільні скрипти: EnemyPresentation (параметри й чашка), EnemyCatchPlayer (необов’язковий замах), RemedyDelivery (стан пиття), ManorPrototypeBuilder (один виклик). Новий ManorResidentModel. Інші сцени, Unity та пакети не оновлювалися. Початкову папку користувача залишено цілою; зайві zip/backup/службові thumbnails не копіювали в Assets.

## Ручна перевірка

Stop Play → перевідкрити ManorPrototype з диска → Play → Нова гра. У холі спостерігати ходу й поворот мешканця; шумом викликати перевірку, показатися для переслідування. Перевірити атаку, відступ під час замаху, стіну/двері як перешкоду, паузу й повтор після поразки. Наприкінці перелити ліки в чашку, сховатися, спостерігати пиття й завершення. Модель не є фінальною анімаційною постановкою: немає окремого бігу, cloth simulation чи анімації піднімання чашки зі столу.

Нові сирі Mixamo-кліпи потрібно підготувати через цей скрипт або окремо перенести на той самий метровий скелет; просте додавання сирих Generic-кривих із сантиметровими зміщеннями може змінити масштаб.

Відтворення: Blender --background --python build_resident.py підготовляє метровий MotionReference.blend із 65-кістковими рухами; потім build_authored_rig.py переносить їх на авторський ріг і записує фінальний FBX та CaretakerResident.blend.

## Validation (2026-11-01)

Unity 6000.6.7f1 compiled and ran 37 automated Play Mode checks in the isolated D:/EchoMergeVerification project: ATMOSPHERE_PLAY_RESULT: checks=37; errors=False. Covered menu/New Game, valid Generic avatar and original palette, single skinned mesh/Animator, scale and floor alignment, idle/walk deformation and in-place motion, NavMesh-driven walking and existing footstep events, pause, attack windup/cancellation/closed-door obstruction, defeat/checkpoint retry, cup delivery/drinking/pause/vision interruption/treatment completion, full repeat and menu/New Game without duplicates. Upgrade applied twice: idempotent=true, missingScripts=0. Final idle/walk/attack/drinking screenshots inspected. These are focused automated scenarios with temporary warps and issued ingredients, not a continuous manual playthrough or a standalone build test.

Logs: QA/resident-model-upgrade.log and QA/resident-model-play.log in the verification project; screenshots: QA/resident-model-photos. Primary source assets, original scene IDs, existing GUIDs and unrelated files checked against a pre-change SHA256 baseline.
