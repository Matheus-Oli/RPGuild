namespace RPGuild.Application.Contas;

/// <summary>
/// Resultado de login e refresh. O refresh token vai para o cookie; só o access token vai no corpo.
/// </summary>
public sealed record SessaoAutenticada(AccessToken Access, RefreshTokenEmitido Refresh);

public sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
