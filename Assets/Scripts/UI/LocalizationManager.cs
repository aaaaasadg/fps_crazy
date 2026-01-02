using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public event System.Action OnLanguageChanged;

    public enum Language
    {
        English,
        Russian,
        Turkish
    }

    private Language currentLanguage = Language.English;
    private const string PrefsKey = "GameLanguage";

    private Dictionary<string, Dictionary<Language, string>> translations;

    [Header("Font Assets by Language")]
    [SerializeField] private TMP_FontAsset englishFont; // Inter font
    [SerializeField] private TMP_FontAsset russianFont; // Roboto or similar Cyrillic font
    [SerializeField] private TMP_FontAsset turkishFont; // Inter or Roboto (Turkish uses Latin)
    
    [Header("Global Font Settings")]
    [SerializeField, Range(0f, 1f)] private float outlineThickness = 0.1f; // Adjustable thickness

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTranslations();
            LoadLanguage();
            ApplyLanguage();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeTranslations()
    {
        translations = new Dictionary<string, Dictionary<Language, string>>();

        // Settings
        AddTranslation("Sensitivity", "Sensitivity", "Чувствительность", "Hassasiyet");
        AddTranslation("SFX Volume", "SFX Volume", "Громкость звуков", "Ses Efektleri");
        AddTranslation("Music Volume", "Music Volume", "Громкость музыки", "Müzik Sesi");
        AddTranslation("Back", "Back", "Назад", "Geri");
        AddTranslation("Settings", "Settings", "Настройки", "Ayarlar");
        AddTranslation("Resume", "Resume", "Продолжить", "Devam Et");
        AddTranslation("Main Menu", "Main Menu", "Главное меню", "Ana Menü");
        AddTranslation("Restart", "Restart", "Перезапуск", "Yeniden Başlat");

        // HUD
        AddTranslation("Lv", "Lv.", "Ур.", "Sev.");
        AddTranslation("DPS", "DPS", "УВС", "SPS");

        // Death Screen
        AddTranslation("Level", "Level", "Уровень", "Seviye");
        AddTranslation("Kills", "Kills", "Убийств", "Öldürme");
        AddTranslation("Souls Earned", "Souls Earned", "Получено душ", "Kazanılan Ruh");
        AddTranslation("Gold Earned", "Gold Earned", "Получено золота", "Kazanılan Altın");
        AddTranslation("Time Survived", "Time Survived", "Время выживания", "Hayatta Kalma Süresi");
        AddTranslation("Your place on leaderboard", "Your place on leaderboard: {0}", "Ваше место в таблице: {0}", "Liderlik tablosundaki yeriniz: {0}");

        // Shop
        AddTranslation("Souls", "Souls", "Души", "Ruhlar");
        AddTranslation("Lvl", "Lvl", "Ур.", "Sev.");

        // Leaderboard
        AddTranslation("kills", "kills", "убийств", "öldürme");

        // Rarity
        AddTranslation("Common", "Common", "Обычный", "Yaygın");
        AddTranslation("Rare", "Rare", "Редкий", "Nadir");
        AddTranslation("Epic", "Epic", "Эпический", "Efsanevi");
        AddTranslation("Legendary", "Legendary", "Легендарный", "Efsanevi");

        // Upgrade
        AddTranslation("Upgrade", "Upgrade", "Улучшение", "Yükseltme");

        // Map Select
        AddTranslation("Stone Island", "Stone Island", "Каменный остров", "Taş Adası");
        AddTranslation("Wild Forest", "Wild Forest", "Дикий лес", "Vahşi Orman");
        AddTranslation("Madness", "Madness", "Безумие", "Çılgınlık");
        AddTranslation("LOCKED", "LOCKED: Defeat Boss in Stone Island", "ЗАБЛОКИРОВАНО: Победите босса на Каменном острове", "KİLİTLİ: Taş Adası'ndaki Boss'u Yen");
        AddTranslation("Map2_Locked_Kill3rdBoss", "LOCKED: Kill 3rd Boss (10 min)", "ЗАБЛОКИРОВАНО: Убейте 3-го босса (10 мин)", "KİLİTLİ: 3. Boss'u Öldür (10 dk)");

        // Buttons
        AddTranslation("Play", "Play", "Играть", "Oyna");
        AddTranslation("Shop", "Shop", "Магазин", "Dükkan");
        AddTranslation("Quit", "Quit", "Выход", "Çıkış");
        AddTranslation("Leaderboard", "Leaderboard", "Таблица лидеров", "Liderlik Tablosu");

        // Upgrade Names
        AddTranslation("Power", "Power", "Сила", "Güç");
        AddTranslation("Vitality", "Vitality", "Жизненная сила", "Canlılık");
        AddTranslation("Haste", "Haste", "Скорость", "Hız");
        AddTranslation("Agility", "Agility", "Ловкость", "Çeviklik");
        AddTranslation("Multishot", "Multishot", "Мультивыстрел", "Çoklu Ateş");
        AddTranslation("Fortune", "Fortune", "Удача", "Şans");
        AddTranslation("Deep Pockets", "Deep Pockets", "Большие карманы", "Derin Cepler");
        AddTranslation("Wisdom", "Wisdom", "Мудрость", "Bilgelik");
        AddTranslation("Greed", "Greed", "Жадность", "Açgözlülük");
        AddTranslation("Pierce", "Pierce", "Пробивание", "Delme");
        AddTranslation("Magnet", "Magnet", "Магнит", "Mıknatıs");
        AddTranslation("Lethality", "Lethality", "Смертоносность", "Öldürücülük");
        AddTranslation("Focus", "Focus", "Фокус", "Odak");
        AddTranslation("Blast Radius", "Blast Radius", "Радиус взрыва", "Patlama Yarıçapı");
        AddTranslation("Knockback", "Knockback", "Отбрасывание", "Geri İtme");
        AddTranslation("Regeneration", "Regeneration", "Регенерация", "Yenilenme");
        AddTranslation("Quick Mag", "Quick Mag", "Быстрый магазин", "Hızlı Şarjör");
        AddTranslation("Ricochet", "Ricochet", "Рикошет", "Sekme");
        AddTranslation("Armor", "Armor", "Броня", "Zırh");
        
        // Upgrade Names (actual asset names)
        AddTranslation("Impact", "Impact", "Удар", "Darbe");
        AddTranslation("Chain Shot", "Chain Shot", "Цепной выстрел", "Zincir Atış");
        AddTranslation("Drill Rounds", "Drill Rounds", "Бронебойные", "Delici Mermi");

        // Upgrade Descriptions (keys match asset files - no periods)
        AddTranslation("Increases damage", "Increases damage", "Увеличивает урон", "Hasarı artırır");
        AddTranslation("Increases Max HP", "Increases Max HP", "Увеличивает максимальное HP", "Maksimum Canı artırır");
        AddTranslation("Increases fire rate", "Increases fire rate", "Увеличивает скорострельность", "Ateş hızını artırır");
        AddTranslation("Increases movement speed", "Increases movement speed", "Увеличивает скорость передвижения", "Hareket hızını artırır");
        AddTranslation("Adds an extra projectile", "Adds an extra projectile", "Добавляет дополнительный снаряд", "Ekstra mermi ekler");
        AddTranslation("Increases luck", "Increases luck", "Увеличивает удачу", "Şansı artırır");
        AddTranslation("Increases magazine size", "Increases magazine size", "Увеличивает размер магазина", "Şarjör boyutunu artırır");
        AddTranslation("Increases XP gain", "Increases XP gain", "Увеличивает получение опыта", "Deneyim kazanımını artırır");
        AddTranslation("Increases Gold gain", "Increases Gold gain", "Увеличивает получение золота", "Altın kazanımını artırır");
        AddTranslation("Projectiles pierce enemies", "Projectiles pierce enemies", "Снаряды пронзают врагов", "Mermiler düşmanları deler");
        AddTranslation("Increases pickup range", "Increases pickup range", "Увеличивает радиус подбора", "Toplama menzilini artırır");
        AddTranslation("Increases critical damage", "Increases critical damage", "Увеличивает критический урон", "Kritik hasarı artırır");
        AddTranslation("Increases critical chance", "Increases critical chance", "Увеличивает шанс критического удара", "Kritik şansını artırır");
        AddTranslation("Increases explosion radius", "Increases explosion radius", "Увеличивает радиус взрыва", "Patlama yarıçapını artırır");
        AddTranslation("Increases knockback force", "Increases knockback force", "Увеличивает силу отбрасывания", "Geri itme gücünü artırır");
        AddTranslation("Regenerates HP over time", "Regenerates HP over time", "Регенерирует HP со временем", "Zamanla Can yeniler");
        AddTranslation("Reduces reload time", "Reduces reload time", "Уменьшает время перезарядки", "Yeniden yükleme süresini azaltır");
        AddTranslation("Bounces to nearby enemies", "Bounces to nearby enemies", "Отскакивает к ближайшим врагам", "Yakındaki düşmanlara sıçrar");
        AddTranslation("Reduces damage taken", "Reduces damage taken", "Уменьшает получаемый урон", "Alınan hasarı azaltır");

        // Item Names
        AddTranslation("Whetstone", "Whetstone", "Точильный камень", "Bileyici Taş");
        AddTranslation("Bouncy Ball", "Bouncy Ball", "Прыгучий мяч", "Zıplayan Top");
        AddTranslation("Knowledge Tome", "Knowledge Tome", "Книга знаний", "Bilgi Kitabı");
        AddTranslation("Lucky Clover", "Lucky Clover", "Счастливый клевер", "Şanslı Yonca");
        AddTranslation("Healthy Heart", "Healthy Heart", "Здоровое сердце", "Sağlıklı Kalp");
        AddTranslation("Gold Coin", "Gold Coin", "Золотая монета", "Altın Para");
        AddTranslation("Split Shot", "Split Shot", "Раздельный выстрел", "Bölünmüş Ateş");
        AddTranslation("Heavy Hammer", "Heavy Hammer", "Тяжёлый молот", "Ağır Çekiç");
        AddTranslation("Iron Plate", "Iron Plate", "Железная пластина", "Demir Plaka");
        AddTranslation("Wind Boots", "Wind Boots", "Сапоги ветра", "Rüzgar Botları");
        AddTranslation("Explosive Powder", "Explosive Powder", "Взрывчатый порох", "Patlayıcı Toz");
        AddTranslation("Assassin Dagger", "Assassin Dagger", "Кинжал убийцы", "Suikastçı Hançeri");
        AddTranslation("Drill Tip", "Drill Tip", "Буровое остриё", "Delici Uç");
        AddTranslation("Extended Mag", "Extended Mag", "Расширенный магазин", "Uzatılmış Şarjör");
        AddTranslation("Rapid Trigger", "Rapid Trigger", "Быстрый спуск", "Hızlı Tetik");
        AddTranslation("Scope", "Scope", "Прицел", "Nişangah");
        AddTranslation("Oiled Mag", "Oiled Mag", "Смазанный магазин", "Yağlı Şarjör");
        AddTranslation("Aerodynamics", "Aerodynamics", "Аэродинамика", "Aerodinamik");
        AddTranslation("Troll Blood", "Troll Blood", "Кровь тролля", "Trol Kanı");

        // StatType Enum Aliases (for ShopUI)
        AddTranslation("MaxHP", "Vitality", "Жизненная сила", "Canlılık");
        AddTranslation("Damage", "Power", "Сила", "Güç");
        AddTranslation("FireRate", "Haste", "Скорость", "Hız");
        AddTranslation("MoveSpeed", "Agility", "Ловкость", "Çeviklik");
        AddTranslation("ReloadSpeed", "Quick Mag", "Быстрый магазин", "Hızlı Şarjör");
        AddTranslation("ProjectileCount", "Multishot", "Мультивыстрел", "Çoklu Ateş");
        AddTranslation("ProjectilePierce", "Pierce", "Пробивание", "Delme");
        AddTranslation("RicochetBounces", "Ricochet", "Рикошет", "Sekme");
        AddTranslation("Knockback", "Knockback", "Отбрасывание", "Geri İtme");
        AddTranslation("AoERadius", "Blast Radius", "Радиус взрыва", "Patlama Yarıçapı");
        AddTranslation("XPGain", "Wisdom", "Мудрость", "Bilgelik");
        AddTranslation("GoldGain", "Greed", "Жадность", "Açgözlülük");
        AddTranslation("DamageReduction", "Armor", "Броня", "Zırh");
        AddTranslation("Luck", "Fortune", "Удача", "Şans");
        AddTranslation("PickupRange", "Magnet", "Магнит", "Mıknatıs");
        AddTranslation("CritChance", "Focus", "Фокус", "Odak");
        AddTranslation("CritDamage", "Lethality", "Смертоносность", "Öldürücülük");
        AddTranslation("MagazineSize", "Deep Pockets", "Большие карманы", "Derin Cepler");
        AddTranslation("ProjectileSpeed", "Aerodynamics", "Аэродинамика", "Aerodinamik");
        AddTranslation("HPRegen", "Regeneration", "Регенерация", "Yenilenme");

        // Units
        AddTranslation(" HP", " HP", " ОЗ", " Can");
        AddTranslation(" HP/s", " HP/s", " ОЗ/с", " Can/sn");

        // Item Descriptions (most reuse upgrade descriptions, but some are unique)
        AddTranslation("Increases projectile speed", "Increases projectile speed", "Увеличивает скорость снарядов", "Mermi hızını artırır");
        
        // Tutorial
        AddTranslation("Tutorial_Move", "Use W A S D to move\nPress SPACE to jump", "Используйте Ц Ф Ы В для перемещения\nНажмите ПРОБЕЛ для прыжка", "Hareket etmek için W A S D kullanın\nZıplamak için SPACE'e basın");
        AddTranslation("Tutorial_Move_Mobile", "Use controller to walk and jump", "Используйте джойстик для передвижения и прыжка", "Yürümek ve zıplamak için kontrolcüyü kullanın");
        AddTranslation("Tutorial_Shoot", "Press LEFT MOUSE BUTTON to shoot enemies", "Нажмите ЛЕВУЮ КНОПКУ МЫШИ для стрельбы", "Düşmanlara ateş etmek için SOL FARE TUŞUNA basın");
        AddTranslation("Tutorial_Shoot_Mobile", "Press on screen to look around and shoot", "Нажмите на экран, чтобы осмотреться и стрелять", "Etrafa bakmak ve ateş etmek için ekrana basın");
        AddTranslation("Tutorial_Damage", "Deal damage to enemies!", "Наносите урон врагам!", "Düşmanlara hasar verin!");
        AddTranslation("Tutorial_XP", "Collect XP orbs to gain experience", "Собирайте сферы опыта для получения уровня", "Deneyim kazanmak için XP kürelerini toplayın");
        AddTranslation("Tutorial_LevelUp", "Level Up to choose new upgrades!", "Повысьте уровень, чтобы выбрать улучшения!", "Yeni yükseltmeler seçmek için Seviye Atlayın!");
        AddTranslation("Tutorial_Survive", "Survive as long as you can!", "Выживайте как можно дольше!", "Yapabildiğiniz kadar hayatta kalın!");

        // Pause Menu
        AddTranslation("Paused", "Pause", "Пауза", "Duraklatıldı");

        // Weapon Select (Generic headers or specific weapon names can be added here as needed)
        AddTranslation("Select Weapon", "Select Weapon", "Выберите оружие", "Silah Seç");

        // Weapon Stats Labels (for Weapon Select Screen)
        AddTranslation("Stat_Damage", "Damage", "Урон", "Hasar");
        AddTranslation("Stat_FireRate", "Fire Rate", "Скорострельность", "Atış Hızı");
        AddTranslation("Stat_Ammo", "Ammo", "Боеприпасы", "Cephane");
        AddTranslation("Stat_Range", "Range", "Дальность", "Menzil");
        AddTranslation("Stat_Reload", "Reload Time", "Перезарядка", "Yenileme");
        AddTranslation("Stat_Crit", "Crit Chance", "Шанс крита", "Kritik Şansı");
        
        // Interaction
        AddTranslation("Interaction_OpenChest", "Press E to Open ({0} G)", "Нажмите E, чтобы открыть ({0} G)", "Açmak için E'ye basın ({0} G)");
        AddTranslation("Interaction_OpenChest_Free", "Press E to Open (FREE)", "Нажмите E, чтобы открыть (БЕСПЛАТНО)", "Açmak için E'ye basın (ÜCRETSİZ)");
        AddTranslation("Interaction_Altar", "Press E to Sacrifice {0}% HP for Level Up", "Нажмите E, чтобы пожертвовать {0}% здоровья для повышения уровня", "Seviye Atlamak için {0}% Can feda etmek üzere E'ye basın");
        AddTranslation("Interaction_Altar_Fail", "Cannot use: Would reduce HP below 1", "Нельзя использовать: ОЗ упадет ниже 1", "Kullanılamaz: Can 1'in altına düşecek");
        AddTranslation("Interaction_Tombstone", "Press E to spawn a horde", "Нажмите E, чтобы призвать орду", "Sürüyü çağırmak için E'ye basın");
        
        // Rewarded Ads
        AddTranslation("Reroll", "Reroll", "Переролл", "Yeniden At");
        AddTranslation("Double Souls", "Double Souls", "Удвоить души", "Ruhları İkiye Katla");
        
        
        // Weapon 0: Revolver (Example)
        AddTranslation("Revolver", "Revolver", "Револьвер", "Tabanca");
        AddTranslation("Shoots bullets in a straight line.", "Shoots bullets in a straight line.", "Стреляет пулями по прямой.", "Mermileri düz bir çizgide atar.");
        AddTranslation("Bonus: +10% Damage", "Bonus: +10% Damage", "Бонус: +10% Урона", "Bonus: +%10 Hasar");
        AddTranslation("Milestones: Level 5", "Milestones: Level 5", "Достижения: Уровень 5", "Dönüm Noktaları: Seviye 5");

        // Weapon 1: Shotgun (Example)
        AddTranslation("Shotgun", "Shotgun", "Дробовик", "Pompalı");
        AddTranslation("Shoots a spread of bullets.", "Shoots a spread of bullets.", "Стреляет дробью.", "Mermileri saçarak atar.");
        AddTranslation("Bonus: +1 Projectile", "Bonus: +1 Projectile", "Бонус: +1 Снаряд", "Bonus: +1 Mermi");
        AddTranslation("Milestones: Level 10", "Milestones: Level 10", "Достижения: Уровень 10", "Dönüm Noktaları: Seviye 10");

        // Weapon 2: Assault Rifle (Example)
        AddTranslation("Assault Rifle", "Assault Rifle", "Штурмовая винтовка", "Saldırı Tüfeği");
        AddTranslation("Fast firing automatic rifle.", "Fast firing automatic rifle.", "Скорострельная автоматическая винтовка.", "Hızlı ateş eden otomatik tüfek.");
        AddTranslation("Bonus: +20% Fire Rate", "Bonus: +20% Fire Rate", "Бонус: +20% Скорострельность", "Bonus: +%20 Atış Hızı");
        AddTranslation("Milestones: Level 15", "Milestones: Level 15", "Достижения: Уровень 15", "Dönüm Noktaları: Seviye 15");

        // Weapon 3: SMG (Example)
        AddTranslation("SMG", "SMG", "ПП", "Hafif Makineli");
        AddTranslation("High fire rate, low range.", "High fire rate, low range.", "Высокая скорострельность, малая дальность.", "Yüksek atış hızı, düşük menzil.");
        AddTranslation("Bonus: +15% Move Speed", "Bonus: +15% Move Speed", "Бонус: +15% Скорость", "Bonus: +%15 Hareket Hızı");
        AddTranslation("Milestones: Level 20", "Milestones: Level 20", "Достижения: Уровень 20", "Dönüm Noktaları: Seviye 20");

        // Generic Weapon Stats
        AddTranslation("Damage", "Damage", "Урон", "Hasar");
        AddTranslation("Fire Rate", "Fire Rate", "Скорострельность", "Atış Hızı");
        AddTranslation("Ammo", "Ammo", "Боеприпасы", "Cephane");
        AddTranslation("Range", "Range", "Дальность", "Menzil");
        AddTranslation("Reload Time", "Reload Time", "Время перезарядки", "Yenileme Süresi");
        AddTranslation("Crit Chance", "Crit Chance", "Шанс крита", "Kritik Şansı");
        
        // Milestones
        AddTranslation("Milestones:", "Milestones:", "Достижения:", "Dönüm Noktaları:");
        AddTranslation("Bonus:", "Bonus:", "Бонус:", "Bonus:");
        
        // Milestone Stat Names (for in-game level up notifications)
        AddTranslation("Milestone_CritChance", "Crit Chance", "Шанс крита", "Kritik Şansı");
        AddTranslation("Milestone_CritDamage", "Crit Damage", "Крит. урон", "Kritik Hasar");
        AddTranslation("Milestone_Damage", "Damage", "Урон", "Hasar");
        AddTranslation("Milestone_MaxHP", "Max HP", "Макс. HP", "Maks. Can");
        AddTranslation("Milestone_DamageReduction", "Damage Reduction", "Защита", "Hasar Azaltma");
        AddTranslation("Milestone_HPRegen", "HP Regen", "Реген. HP", "Can Yenileme");
        AddTranslation("Milestone_Ricochet", "Ricochet", "Рикошет", "Sekme");
        AddTranslation("Milestone_Projectile", "Projectile", "Снаряд", "Mermi");
        AddTranslation("Milestone_FireRate", "Fire Rate", "Скорострельность", "Atış Hızı");
        AddTranslation("Milestone_PickupRange", "Pickup Range", "Радиус подбора", "Toplama Menzili");
        AddTranslation("Milestone_GoldGain", "Gold Gain", "Золото", "Altın Kazanımı");
        AddTranslation("Milestone_XPGain", "XP Gain", "Опыт", "XP Kazanımı");
        
        // Stats Panel (pause menu)
        AddTranslation("Stats_MaxHP", "Max HP", "Макс. HP", "Maks. Can");
        AddTranslation("Stats_HPRegen", "HP Regen", "Реген. HP", "Can Yenileme");
        AddTranslation("Stats_MoveSpeed", "Move Speed", "Скорость", "Hareket Hızı");
        AddTranslation("Stats_Damage", "Damage", "Урон", "Hasar");
        AddTranslation("Stats_FireRate", "Fire Rate", "Скорострельность", "Atış Hızı");
        AddTranslation("Stats_ReloadSpeed", "Reload Speed", "Перезарядка", "Şarjör Hızı");
        AddTranslation("Stats_Projectiles", "Projectiles", "Снаряды", "Mermiler");
        AddTranslation("Stats_Pierce", "Pierce", "Пробивание", "Delme");
        AddTranslation("Stats_Ricochet", "Ricochet", "Рикошет", "Sekme");
        AddTranslation("Stats_Knockback", "Knockback", "Отбрасывание", "Geri İtme");
        AddTranslation("Stats_AoERadius", "AoE Radius", "Радиус взрыва", "AoE Yarıçapı");
        AddTranslation("Stats_XPGain", "XP Gain", "Получ. опыта", "XP Kazanımı");
        AddTranslation("Stats_GoldGain", "Gold Gain", "Получ. золота", "Altın Kazanımı");
        AddTranslation("Stats_DamageReduction", "Damage Reduction", "Снижение урона", "Hasar Azaltma");
        AddTranslation("Stats_Luck", "Luck", "Удача", "Şans");
        AddTranslation("Stats_PickupRange", "Pickup Range", "Радиус подбора", "Toplama Menzili");
        AddTranslation("Stats_CritChance", "Crit Chance", "Шанс крита", "Kritik Şansı");
        AddTranslation("Stats_CritDamage", "Crit Damage", "Крит. урон", "Kritik Hasar");
        AddTranslation("Stats_MagazineSize", "Magazine Size", "Размер магазина", "Şarjör Boyutu");
        AddTranslation("Stats_ProjectileSpeed", "Projectile Speed", "Скорость снаряда", "Mermi Hızı");
        AddTranslation("Stats_Faster", "faster", "быстрее", "daha hızlı");
        AddTranslation("Stats_Taken", "taken", "получено", "alınan");
        
        // Base Weapon Stats (stats panel)
        AddTranslation("Base_Damage", "Base Damage", "Базовый урон", "Temel Hasar");
        AddTranslation("Base_FireRate", "Base Fire Rate", "Базовая скорострельность", "Temel Atış Hızı");
        AddTranslation("Base_MagazineSize", "Base Magazine Size", "Базовый размер магазина", "Temel Şarjör Boyutu");
        AddTranslation("Base_ProjectileSpeed", "Base Projectile Speed", "Базовая скорость снаряда", "Temel Mermi Hızı");
        AddTranslation("Base_ReloadTime", "Base Reload Time", "Базовое время перезарядки", "Temel Yenileme Süresi");
        AddTranslation("Base_Knockback", "Base Knockback", "Базовое отбрасывание", "Temel Geri İtme");
        AddTranslation("Base_AoERadius", "Base AoE Radius", "Базовый радиус взрыва", "Temel AoE Yarıçapı");
        AddTranslation("Base_ProjectileCount", "Base Projectile Count", "Базовое кол-во снарядов", "Temel Mermi Sayısı");
        AddTranslation("Base_Pierce", "Base Pierce", "Базовое пробивание", "Temel Delme");
        AddTranslation("Base_Ricochet", "Base Ricochet", "Базовый рикошет", "Temel Sekme");
        AddTranslation("Base_CritChance", "Base Crit Chance", "Базовый шанс крита", "Temel Kritik Şansı");
        AddTranslation("Base_CritDamage", "Base Crit Damage", "Базовый крит. урон", "Temel Kritik Hasar");
    }

    private void AddTranslation(string key, string english, string russian, string turkish)
    {
        translations[key] = new Dictionary<Language, string>
        {
            { Language.English, english },
            { Language.Russian, russian },
            { Language.Turkish, turkish }
        };
    }

    public void SetLanguage(Language lang)
    {
        if (currentLanguage != lang)
        {
            currentLanguage = lang;
            PlayerPrefs.SetInt(PrefsKey, (int)lang);
            PlayerPrefs.Save();
            ApplyLanguage();
            OnLanguageChanged?.Invoke();
        }
    }

    public Language GetCurrentLanguage()
    {
        return currentLanguage;
    }

    private void LoadLanguage()
    {
        // Load saved language preference (if any)
        if (PlayerPrefs.HasKey(PrefsKey))
        {
            currentLanguage = (Language)PlayerPrefs.GetInt(PrefsKey);
            Debug.Log($"[LocalizationManager] Loaded saved language: {currentLanguage}");
        }
        else
        {
            // Try to detect from browser, otherwise default to English
            currentLanguage = Language.English;
            Debug.Log("[LocalizationManager] No saved language, attempting browser detection");
            DetectLanguageFromBrowser();
        }
    }

    /// <summary>
    /// Detects language from browser settings (for WebGL builds).
    /// Falls back to saved preference or English if detection fails.
    /// </summary>
    public void DetectLanguageFromBrowser()
    {
        Debug.Log("[LocalizationManager] ===== DETECTING LANGUAGE FROM BROWSER =====");
        
        string browserLang = "en"; // Default
        
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // Try to get language from browser
            browserLang = Application.systemLanguage.ToString().ToLower();
            Debug.Log($"[LocalizationManager] Browser language: {browserLang}");
        }
        catch
        {
            Debug.LogWarning("[LocalizationManager] Could not detect browser language, using default");
        }
#endif
        
        Language detectedLang = Language.English;
        
        // Convert browser language to our enum
        if (browserLang.Contains("russian") || browserLang.Contains("ru"))
        {
            detectedLang = Language.Russian;
        }
        else if (browserLang.Contains("turkish") || browserLang.Contains("tr"))
        {
            detectedLang = Language.Turkish;
        }
        else
        {
            detectedLang = Language.English;
        }
        
        Debug.Log($"[LocalizationManager] 🔄 Detected language: {detectedLang}");
        
        // Only set if no saved preference exists
        if (!PlayerPrefs.HasKey(PrefsKey))
        {
            currentLanguage = detectedLang;
            PlayerPrefs.SetInt(PrefsKey, (int)detectedLang);
            PlayerPrefs.Save();
            
            Debug.Log($"[LocalizationManager] 🔀 Setting language to: {detectedLang}");
            
            // Force refresh all UI
            ApplyLanguage();
            OnLanguageChanged?.Invoke();
        }
        
        Debug.Log("[LocalizationManager] ✅ Language detection complete!");
    }

    public string GetLocalizedString(string key, params object[] args)
    {
        if (translations == null || !translations.ContainsKey(key))
        {
            return key;
        }

        if (!translations[key].ContainsKey(currentLanguage))
        {
            return translations[key][Language.English];
        }

        string text = translations[key][currentLanguage];
        
        if (args != null && args.Length > 0)
        {
            try
            {
                text = string.Format(text, args);
            }
            catch
            {
                return text;
            }
        }

        return text;
    }

    private void ApplyLanguage()
    {
        LocalizedText[] allLocalizedTexts = FindObjectsByType<LocalizedText>(FindObjectsSortMode.None);
        foreach (LocalizedText localizedText in allLocalizedTexts)
        {
            if (localizedText != null)
            {
                localizedText.UpdateText();
            }
        }
        
        // Also refresh UpgradeSystem so upgrade/item names are updated
        UpgradeSystem upgradeSystem = FindFirstObjectByType<UpgradeSystem>();
        if (upgradeSystem != null)
        {
            // This will refresh any currently displayed upgrades
            // Note: This requires UpgradeSystem to handle refresh if needed
        }
    }

    public void RefreshAll()
    {
        ApplyLanguage();
    }

    /// <summary>
    /// Gets the appropriate font asset for the current language.
    /// </summary>
    public TMP_FontAsset GetFontForCurrentLanguage()
    {
        switch (currentLanguage)
        {
            case Language.English:
                return englishFont != null ? englishFont : TMP_Settings.defaultFontAsset;
            case Language.Russian:
                return russianFont != null ? russianFont : TMP_Settings.defaultFontAsset;
            case Language.Turkish:
                return turkishFont != null ? turkishFont : (englishFont != null ? englishFont : TMP_Settings.defaultFontAsset);
            default:
                return TMP_Settings.defaultFontAsset;
        }
    }

    /// <summary>
    /// Gets the font asset for a specific language.
    /// </summary>
    public TMP_FontAsset GetFontForLanguage(Language lang)
    {
        switch (lang)
        {
            case Language.English:
                return englishFont != null ? englishFont : TMP_Settings.defaultFontAsset;
            case Language.Russian:
                return russianFont != null ? russianFont : TMP_Settings.defaultFontAsset;
            case Language.Turkish:
                return turkishFont != null ? turkishFont : (englishFont != null ? englishFont : TMP_Settings.defaultFontAsset);
            default:
                return TMP_Settings.defaultFontAsset;
        }
    }

    public void ApplyFont(TextMeshProUGUI text, TMP_FontAsset font)
    {
        if (text == null) return;

        if (font != null)
        {
            text.font = font;
        }

        // Force outline thickness for consistency across all languages
        // Accessing fontMaterial creates an instance specific to this text object
        Material mat = text.fontMaterial;
        if (mat != null && mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
        {
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineThickness);
            text.UpdateMeshPadding();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Allow realtime updates in the editor when changing the slider
        if (Application.isPlaying || Instance != null)
        {
            ApplyLanguage();
        }
    }
#endif
}

