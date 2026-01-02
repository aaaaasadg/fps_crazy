using System;

[Serializable]
public class GameData
{
    public int souls;
    public int[] statLevels;
    public bool[] mapsUnlocked;

    public GameData()
    {
        souls = 0;
        statLevels = new int[Enum.GetValues(typeof(StatType)).Length];

        mapsUnlocked = new bool[3];
        mapsUnlocked[0] = true;  // Map 1 is always unlocked
        mapsUnlocked[1] = false; // Map 2 is initially locked
        mapsUnlocked[2] = true;  // Map 3 (Madness) is always unlocked
    }
}
