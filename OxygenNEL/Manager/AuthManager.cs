using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace OxygenNEL.Manager;

public sealed class AuthManager
{
    public static AuthManager Instance { get; } = new AuthManager();

    const string BaseUrl = "https://api.fandmc.cn";

    static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    readonly HttpClient _http;
    readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

    public string Token { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public long UserId { get; private set; }
    public string? Avatar { get; private set; }
    public string? Rank { get; private set; }
    public bool IsBanned { get; private set; }
    public bool IsAdmin { get; private set; }
    public string CachedSalt { get; private set; } = string.Empty;
    public string CachedGameVersion { get; private set; } = string.Empty;
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

    public async Task<string> GetCrcSaltAsyncIfNeeded(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(CachedSalt))
        {
            await GetCrcSaltAsync(ct);
        }
        return CachedSalt;
    }

    AuthManager()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(20)
        };
    }

    public string GetAuthFilePath()
    {
        var baseDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        return Path.Combine(baseDir, "auth.dat");
    }

    public void LoadFromDisk()
    {
        try
        {
            var path = GetAuthFilePath();
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) return;
            var data = JsonSerializer.Deserialize<AuthData>(json, JsonOptions);
            if (data == null) return;
            if (string.IsNullOrWhiteSpace(data.Token)) return;
            Token = data.Token.Trim();
            Username = data.Username?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "读取 auth.dat 失败");
        }
    }

    public void SaveToDisk()
    {
        try
        {
            var path = GetAuthFilePath();
            var data = new AuthData { Token = Token, Username = Username };
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "保存 auth.dat 失败");
        }
    }

    public void Clear()
    {
        Token = string.Empty;
        Username = string.Empty;
        Email = string.Empty;
        UserId = 0;
        Avatar = null;
        Rank = null;
        IsBanned = false;
        IsAdmin = false;
        try
        {
            var path = GetAuthFilePath();
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "删除 auth.dat 失败");
        }
    }

    public async Task<ApiResult> SendRegisterMailAsync(string email, CancellationToken ct = default)
    {
        return await PostAsync<ApiBaseResponse>("/auth/register_mail", new { email }, ct);
    }

    public async Task<ApiResult> VerifyCodeAsync(string email, string code, CancellationToken ct = default)
    {
        return await PostAsync<ApiBaseResponse>("/auth/verify_code", new { email, code }, ct);
    }

    public async Task<ApiResult> RegisterNextAsync(string email, string username, string password, CancellationToken ct = default)
    {
        var res = await PostAsync<LoginResponse>("/auth/register_next", new { email, username, password }, ct);
        if (!res.Success) return res;
        if (res.Token != null)
        {
            Token = res.Token;
            Username = username;
            Email = email;
            SaveToDisk();
            _ = FetchUserInfoAsync(ct);
        }
        return res;
    }

    public async Task<ApiResult> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default)
    {
        var res = await PostAsync<LoginResponse>("/auth/login", new { username = usernameOrEmail, password }, ct);
        if (!res.Success) return res;
        if (res.Token == null) return new ApiResult(false, "登录失败：未返回 token");
        Token = res.Token;
        SaveToDisk();
        _ = FetchUserInfoAsync(ct);
        return res;
    }

    public async Task<TokenAuthResult> TokenAuthAsync(CancellationToken ct = default)
    {
        if (!IsLoggedIn) return new TokenAuthResult(false, "未登录");

        await _gate.WaitAsync(ct);
        try
        {
            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsJsonAsync("/auth/token_auth", new { token = Token }, JsonOptions, ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Token认证网络请求失败");
                return new TokenAuthResult(false, "网络请求失败: " + ex.Message);
            }

            var raw = await resp.Content.ReadAsStringAsync(ct);
            Log.Information("Token认证返回: {Raw}", raw);

            TokenAuthResponse? data;
            try
            {
                data = JsonSerializer.Deserialize<TokenAuthResponse>(raw, JsonOptions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "解析Token认证响应失败");
                data = null;
            }

            if (data == null || !data.Success)
            {
                var msg = data?.Message ?? ("请求失败: " + (int)resp.StatusCode);
                Log.Warning("Token认证失败: {Message}", msg);
                return new TokenAuthResult(false, msg);
            }

            if (data.User != null)
            {
                UserId = data.User.Id;
                Username = data.User.Username ?? string.Empty;
                Email = data.User.Email ?? string.Empty;
                Rank = data.User.Rank;
                IsAdmin = data.User.IsAdmin;
            }
            Log.Information("Token认证成功: UserId={UserId}, Username={Username}", UserId, Username);
            return new TokenAuthResult(true, "成功");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<UserInfoResult> FetchUserInfoAsync(CancellationToken ct = default)
    {
        if (!IsLoggedIn) return new UserInfoResult(false, "未登录");

        await _gate.WaitAsync(ct);
        try
        {
            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsJsonAsync("/auth/me", new { token = Token }, JsonOptions, ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取用户信息网络请求失败");
                return new UserInfoResult(false, "网络请求失败: " + ex.Message);
            }

            var raw = await resp.Content.ReadAsStringAsync(ct);
            Log.Information("获取用户信息返回: {Raw}", raw);

            MeResponse? data;
            try
            {
                data = JsonSerializer.Deserialize<MeResponse>(raw, JsonOptions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "解析用户信息失败");
                data = null;
            }

            if (data == null || !data.Success)
            {
                var msg = data?.Message ?? ("请求失败: " + (int)resp.StatusCode);
                Log.Warning("获取用户信息失败: {Message}", msg);
                return new UserInfoResult(false, msg);
            }

            UserId = data.Id ?? 0;
            Username = data.Username ?? string.Empty;
            Email = data.Email ?? string.Empty;
            Avatar = data.Avatar;
            Rank = data.Rank;
            IsBanned = data.Banned == 1;
            IsAdmin = data.IsAdmin == 1;
            Log.Information("用户信息已更新: UserId={UserId}, Username={Username}, HasAvatar={HasAvatar}", UserId, Username, !string.IsNullOrEmpty(Avatar));
            return new UserInfoResult(true, "成功");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CrcSaltResult> GetCrcSaltAsync(CancellationToken ct = default)
    {
        if (!IsLoggedIn) return new CrcSaltResult(false, "未登录", null, null, null);
        
        if (!string.IsNullOrEmpty(CachedSalt))
            return new CrcSaltResult(true, "成功", CachedSalt, CachedGameVersion, UserId);

        await _gate.WaitAsync(ct);
        try
        {
            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsJsonAsync("/api/get/crcsalt", new { token = Token }, JsonOptions, ct);
            }
            catch (Exception ex)
            {
                return new CrcSaltResult(false, "网络请求失败: " + ex.Message, null, null, null);
            }

            CrcSaltResponse? data;
            try
            {
                data = await resp.Content.ReadFromJsonAsync<CrcSaltResponse>(JsonOptions, ct);
            }
            catch
            {
                data = null;
            }

            if (data == null || !data.Success)
            {
                var msg = data?.Message ?? ("请求失败: " + (int)resp.StatusCode);
                return new CrcSaltResult(false, msg, null, null, null);
            }

            CachedSalt = data.Salt ?? string.Empty;
            CachedGameVersion = data.GameVersion ?? string.Empty;
            if (data.Id.HasValue) UserId = data.Id.Value;
            return new CrcSaltResult(true, "成功", CachedSalt, CachedGameVersion, data.Id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void ClearCrcSaltCache()
    {
        CachedSalt = string.Empty;
        CachedGameVersion = string.Empty;
    }

    public async Task<UserUrlResult> GenerateUserUrlAsync(CancellationToken ct = default)
    {
        if (!IsLoggedIn) return new UserUrlResult(false, "未登录", null);

        await _gate.WaitAsync(ct);
        try
        {
            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsJsonAsync("/api/generate_user_url", new { accountToken = Token }, JsonOptions, ct);
            }
            catch (Exception ex)
            {
                return new UserUrlResult(false, "网络请求失败: " + ex.Message, null);
            }

            UserUrlResponse? data;
            try
            {
                data = await resp.Content.ReadFromJsonAsync<UserUrlResponse>(JsonOptions, ct);
            }
            catch
            {
                data = null;
            }

            if (data == null || !data.Success)
            {
                var msg = data?.Message ?? ("请求失败: " + (int)resp.StatusCode);
                return new UserUrlResult(false, msg, null);
            }

            return new UserUrlResult(true, "成功", data.UserUrl);
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task<ApiResult> PostAsync<TResponse>(string path, object body, CancellationToken ct)
        where TResponse : ApiBaseResponse
    {
        await _gate.WaitAsync(ct);
        try
        {
            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
            }
            catch (Exception ex)
            {
                return new ApiResult(false, "网络请求失败: " + ex.Message);
            }

            TResponse? data;
            try
            {
                data = await resp.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
            }
            catch
            {
                data = null;
            }

            if (data == null)
            {
                var msg = resp.IsSuccessStatusCode ? "响应解析失败" : ("请求失败: " + (int)resp.StatusCode);
                return new ApiResult(false, msg);
            }

            if (!data.Success) return new ApiResult(false, data.Message ?? "请求失败");

            if (data is LoginResponse login)
            {
             
                return new ApiResult(true, login.Message ?? "成功", login.Token);
            }

            return new ApiResult(true, data.Message ?? "成功");
        }
        finally
        {
            _gate.Release();
        }
    }

    sealed class AuthData
    {
        public string Token { get; set; } = string.Empty;
        public string? Username { get; set; }
    }

    class ApiBaseResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    sealed class LoginResponse : ApiBaseResponse
    {
        public string? Token { get; set; }
    }

    sealed class TokenAuthResponse : ApiBaseResponse
    {
        public TokenAuthUser? User { get; set; }
    }

    sealed class TokenAuthUser
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Rank { get; set; }
        public bool IsAdmin { get; set; }
    }

    sealed class MeResponse : ApiBaseResponse
    {
        public long? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Avatar { get; set; }
        public string? Rank { get; set; }
        public int Banned { get; set; }
        public int IsAdmin { get; set; }
    }

    sealed class CrcSaltResponse : ApiBaseResponse
    {
        public string? Salt { get; set; }
        public string? GameVersion { get; set; }
        public long? Id { get; set; }
    }

    sealed class UserUrlResponse : ApiBaseResponse
    {
        public string? UserUrl { get; set; }
    }
}

public readonly record struct ApiResult(bool Success, string Message, string? Token = null);
public readonly record struct UserInfoResult(bool Success, string Message);
public readonly record struct TokenAuthResult(bool Success, string Message);
public readonly record struct CrcSaltResult(bool Success, string Message, string? Salt, string? GameVersion, long? Id);
public readonly record struct UserUrlResult(bool Success, string Message, string? UserUrl);

