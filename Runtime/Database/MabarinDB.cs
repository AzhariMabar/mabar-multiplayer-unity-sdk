using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mabar.Multiplayer.Models;
using Mabar.Multiplayer.Utils;
using UnityEngine.Networking;

namespace Mabar.Multiplayer.Database
{
    // ─── Response models ────────────────────────────────────────────────────────

    public class MabarAuthResult
    {
        public string Uid       { get; set; }
        public string Name      { get; set; }
        public string Token     { get; set; }
        public long   ExpiresAt { get; set; }
    }

    public class MabarUserInfo
    {
        public string Uid   { get; set; }
        public string Name  { get; set; }
        public string Email { get; set; }
        public int    Coins { get; set; }
    }

    public class MabarScoreEntry
    {
        public string ScoreId   { get; set; }
        public string Uid       { get; set; }
        public string Name      { get; set; }
        public float  Score     { get; set; }
        public string GameMode  { get; set; }
        public int    Rank      { get; set; }
        public long   CreatedAt { get; set; }
    }

    public class MabarLeaderboardResult
    {
        public List<MabarScoreEntry> Leaderboard { get; set; }
        public string                GameMode    { get; set; }
    }

    public class MabarSubmitScoreResult
    {
        public bool   Ok      { get; set; }
        public string ScoreId { get; set; }
    }

    public class MabarCoinsResult
    {
        public string Uid   { get; set; }
        public int    Coins { get; set; }
    }

    public class MabarShopItem
    {
        public string ItemId      { get; set; }
        public string Name        { get; set; }
        public string Description { get; set; }
        public int    Price       { get; set; }
        public string Type        { get; set; }
        public bool   Active      { get; set; }
    }

    public class MabarShopResult
    {
        public List<MabarShopItem> Items { get; set; }
    }

    public class MabarBuyResult
    {
        public bool   Ok         { get; set; }
        public string PurchaseId { get; set; }
        public int    Coins      { get; set; }
    }

    public class MabarPurchase
    {
        public string PurchaseId { get; set; }
        public string ItemId     { get; set; }
        public string ItemName   { get; set; }
        public int    Price      { get; set; }
        public long   CreatedAt  { get; set; }
    }

    public class MabarInventoryResult
    {
        public List<MabarPurchase> Items { get; set; }
    }

    public class MabarClan
    {
        public string ClanId      { get; set; }
        public string Name        { get; set; }
        public string Tag         { get; set; }
        public string Description { get; set; }
        public string OwnerId     { get; set; }
        public int    MemberCount { get; set; }
        public long   CreatedAt   { get; set; }
    }

    public class MabarClanMember
    {
        public string Uid      { get; set; }
        public string Role     { get; set; }
        public long   JoinedAt { get; set; }
    }

    public class MabarClansResult
    {
        public List<MabarClan> Clans { get; set; }
    }

    public class MabarClanDetail
    {
        public MabarClan             Clan    { get; set; }
        public List<MabarClanMember> Members { get; set; }
    }

    public class MabarMyClanResult
    {
        public MabarClan Clan { get; set; }
    }

    public class MabarCreateClanResult
    {
        public bool   Ok     { get; set; }
        public string ClanId { get; set; }
    }

    // ─── MabarinDB ─────────────────────────────────────────────────────────────

    /// <summary>
    /// HTTP client for Mabarin auth, leaderboard, shop, and clan APIs.
    /// Derives the HTTP base URL from MultiplayerSettings.ServerUrl (wss→https).
    ///
    /// Quick start:
    ///   var db = new MabarinDB(settings);
    ///   var auth = await db.GuestLoginAsync("Budi");
    ///   db.SetToken(auth.Token);
    ///
    ///   var lb = await db.GetLeaderboardAsync();
    ///   var shop = await db.GetShopItemsAsync();
    ///   var clans = await db.GetClansAsync();
    /// </summary>
    public class MabarinDB
    {
        private readonly string _base;
        private readonly string _appKey;
        private string _token;

        public MabarinDB(MultiplayerSettings settings, string token = null)
        {
            _base   = settings.ServerUrl
                          .Replace("wss://", "https://")
                          .Replace("ws://",  "http://")
                          .TrimEnd('/');
            _appKey = settings.AppKey;
            _token  = token;
        }

        /// <summary>Store the session token returned by LoginAsync / GuestLoginAsync.</summary>
        public void SetToken(string token) => _token = token;

        // ── Internals ──────────────────────────────────────────────────────────

        private async Task<T> Call<T>(string method, string path, object body = null)
        {
            using var req = new UnityWebRequest($"{_base}{path}", method)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            if (body != null)
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtil.Serialize(body)));

            req.SetRequestHeader("Content-Type",    "application/json");
            req.SetRequestHeader("X-Mabar-App-Key", _appKey);
            if (!string.IsNullOrEmpty(_token))
                req.SetRequestHeader("Authorization", $"Bearer {_token}");

            await req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"[MabarDB] {path} — {req.error}: {req.downloadHandler.text}");
            return JsonUtil.Deserialize<T>(req.downloadHandler.text);
        }

        private async Task Fire(string method, string path, object body = null)
        {
            using var req = new UnityWebRequest($"{_base}{path}", method)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            if (body != null)
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtil.Serialize(body)));

            req.SetRequestHeader("Content-Type",    "application/json");
            req.SetRequestHeader("X-Mabar-App-Key", _appKey);
            if (!string.IsNullOrEmpty(_token))
                req.SetRequestHeader("Authorization", $"Bearer {_token}");

            await req.SendWebRequest();
        }

        // ── Auth ───────────────────────────────────────────────────────────────

        /// <summary>Guest login — creates an anonymous player session.</summary>
        public Task<MabarAuthResult> GuestLoginAsync(string name = null)
            => Call<MabarAuthResult>("POST", "/api/auth/guest", new { name });

        /// <summary>Register with email + password. Requires Firebase Auth to be enabled.</summary>
        public Task<MabarAuthResult> RegisterAsync(string email, string password, string name = null)
            => Call<MabarAuthResult>("POST", "/api/auth/register", new { email, password, name });

        /// <summary>Login with email + password. Requires webApiKey in Firebase config.</summary>
        public Task<MabarAuthResult> LoginAsync(string email, string password)
            => Call<MabarAuthResult>("POST", "/api/auth/login", new { email, password });

        /// <summary>Get current session info. Requires token from Login / GuestLogin.</summary>
        public Task<MabarUserInfo> MeAsync()
            => Call<MabarUserInfo>("GET", "/api/auth/me");

        /// <summary>Invalidate the current session token.</summary>
        public Task LogoutAsync()
            => Fire("POST", "/api/auth/logout");

        // ── Leaderboard ────────────────────────────────────────────────────────

        /// <summary>Submit a score for the current player.</summary>
        public Task<MabarSubmitScoreResult> SubmitScoreAsync(float score, string gameMode = "default", object metadata = null)
            => Call<MabarSubmitScoreResult>("POST", "/api/db/scores/submit", new { score, gameMode, metadata });

        /// <summary>Get top N scores for a game mode. Default top 10.</summary>
        public Task<MabarLeaderboardResult> GetLeaderboardAsync(string gameMode = "default", int limit = 10)
            => Call<MabarLeaderboardResult>("GET",
               $"/api/db/scores/leaderboard?mode={UnityWebRequest.EscapeURL(gameMode)}&limit={limit}");

        /// <summary>Get coin balance for the current player.</summary>
        public Task<MabarCoinsResult> GetCoinsAsync()
            => Call<MabarCoinsResult>("GET", "/api/db/coins/balance");

        // ── Shop ───────────────────────────────────────────────────────────────

        /// <summary>List all active shop items.</summary>
        public Task<MabarShopResult> GetShopItemsAsync()
            => Call<MabarShopResult>("GET", "/api/db/shop/items");

        /// <summary>Buy a shop item using the player's coins.</summary>
        public Task<MabarBuyResult> BuyItemAsync(string itemId)
            => Call<MabarBuyResult>("POST", "/api/db/shop/buy", new { itemId });

        /// <summary>Get all items the current player has purchased.</summary>
        public Task<MabarInventoryResult> GetInventoryAsync()
            => Call<MabarInventoryResult>("GET", "/api/db/shop/inventory");

        // ── Clans ──────────────────────────────────────────────────────────────

        /// <summary>List top clans sorted by member count.</summary>
        public Task<MabarClansResult> GetClansAsync(int limit = 20)
            => Call<MabarClansResult>("GET", $"/api/db/community/clans?limit={limit}");

        /// <summary>Create a new clan. Player must not already be in a clan.</summary>
        public Task<MabarCreateClanResult> CreateClanAsync(string name, string tag, string description = "")
            => Call<MabarCreateClanResult>("POST", "/api/db/community/clans", new { name, tag, description });

        /// <summary>Get clan details and member list.</summary>
        public Task<MabarClanDetail> GetClanAsync(string clanId)
            => Call<MabarClanDetail>("GET", $"/api/db/community/clans/{UnityWebRequest.EscapeURL(clanId)}");

        /// <summary>Join a clan. Player must not already be in a clan.</summary>
        public Task JoinClanAsync(string clanId)
            => Fire("POST", $"/api/db/community/clans/{UnityWebRequest.EscapeURL(clanId)}/join");

        /// <summary>Leave the clan the current player is in.</summary>
        public Task LeaveClanAsync(string clanId)
            => Fire("POST", $"/api/db/community/clans/{UnityWebRequest.EscapeURL(clanId)}/leave");

        /// <summary>Get the clan the current player belongs to. Returns null if not in any clan.</summary>
        public Task<MabarMyClanResult> GetMyClanAsync()
            => Call<MabarMyClanResult>("GET", "/api/db/community/my-clan");
    }
}
