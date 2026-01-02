using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const string LEADERBOARD_KEY = "Leaderboard_v1";
    private const int MAX_ENTRIES = 10;

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public int kills;

        public LeaderboardEntry(string name, int kills)
        {
            this.playerName = name;
            this.kills = kills;
        }
    }

    [System.Serializable]
    public class LeaderboardData
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    private LeaderboardData leaderboardData = new LeaderboardData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLeaderboard();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadLeaderboard()
    {
        if (PlayerPrefs.HasKey(LEADERBOARD_KEY))
        {
            string json = PlayerPrefs.GetString(LEADERBOARD_KEY);
            leaderboardData = JsonUtility.FromJson<LeaderboardData>(json);
            
            // Ensure entries list exists
            if (leaderboardData.entries == null)
            {
                leaderboardData.entries = new List<LeaderboardEntry>();
            }
        }
        else
        {
            leaderboardData = new LeaderboardData();
        }
    }

    private void SaveLeaderboard()
    {
        string json = JsonUtility.ToJson(leaderboardData);
        PlayerPrefs.SetString(LEADERBOARD_KEY, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Submits a score and returns the player's rank (1-based). Only saves if it's the player's best score.
    /// </summary>
    public int SubmitScore(string playerName, int kills)
    {
        // Find existing entry for this player
        LeaderboardEntry existingEntry = leaderboardData.entries.FirstOrDefault(e => e.playerName == playerName);

        // Only update if this is a better score
        if (existingEntry != null)
        {
            if (kills > existingEntry.kills)
            {
                existingEntry.kills = kills;
            }
        }
        else
        {
            // New player - add entry
            leaderboardData.entries.Add(new LeaderboardEntry(playerName, kills));
        }

        // Sort by kills (descending)
        leaderboardData.entries = leaderboardData.entries.OrderByDescending(e => e.kills).ToList();

        // Keep only top MAX_ENTRIES
        if (leaderboardData.entries.Count > MAX_ENTRIES)
        {
            leaderboardData.entries = leaderboardData.entries.Take(MAX_ENTRIES).ToList();
        }

        SaveLeaderboard();

        // Find and return player's rank (1-based)
        for (int i = 0; i < leaderboardData.entries.Count; i++)
        {
            if (leaderboardData.entries[i].playerName == playerName)
            {
                return i + 1;
            }
        }

        return leaderboardData.entries.Count + 1; // Not in top list
    }

    /// <summary>
    /// Gets the player's current rank based on their best score.
    /// </summary>
    public int GetPlayerRank(string playerName)
    {
        LeaderboardEntry entry = leaderboardData.entries.FirstOrDefault(e => e.playerName == playerName);
        if (entry == null) return -1; // No entry found

        // Find rank
        for (int i = 0; i < leaderboardData.entries.Count; i++)
        {
            if (leaderboardData.entries[i].playerName == playerName)
            {
                return i + 1;
            }
        }

        return -1;
    }

    /// <summary>
    /// Gets all leaderboard entries sorted by kills (descending).
    /// </summary>
    public List<LeaderboardEntry> GetLeaderboard()
    {
        return new List<LeaderboardEntry>(leaderboardData.entries);
    }

    /// <summary>
    /// Gets player's best kills.
    /// </summary>
    public int GetPlayerBestKills(string playerName)
    {
        LeaderboardEntry entry = leaderboardData.entries.FirstOrDefault(e => e.playerName == playerName);
        return entry != null ? entry.kills : 0;
    }
}

