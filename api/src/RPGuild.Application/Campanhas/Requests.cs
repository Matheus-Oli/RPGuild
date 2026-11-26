using FluentValidation;

namespace RPGuild.Application.Campanhas;

/// <param name="Sistema">Slug do sistema (ex.: "dnd2024"); precisa estar ativo. Não muda depois de criada.</param>
/// <param name="CapaPath">Path devolvido pelo POST /api/uploads/sign (tipo "capa"), na pasta do usuário.</param>
/// <param name="BannerPath">Path devolvido pelo POST /api/uploads/sign (tipo "banner"), na pasta do usuário.</param>
public sealed record CriarCampanhaRequest(string Nome, string? Descricao, string Sistema, string? CapaPath, string? BannerPath);

/// <summary>
/// Não tem sistema: ele não muda depois de criada. Capa ou banner null remove a imagem.
/// </summary>
public sealed record EditarCampanhaRequest(string Nome, string? Descricao, string? CapaPath, string? BannerPath);

/// <param name="CampaignIds">Todas as campanhas do usuário, na ordem da grade.</param>
public sealed record OrdenarCampanhasRequest(IReadOnlyList<Guid> CampaignIds);

/// <param name="ConfirmacaoNome">O nome da campanha, igual.</param>
/// <param name="ConfirmacaoFrase">A frase "desejo excluir a campanha", igual.</param>
public sealed record ExcluirCampanhaRequest(string ConfirmacaoNome, string ConfirmacaoFrase);

public static class RegrasDeCampanha
{
    public const int NomeTamanhoMaximo = 80;
    public const int DescricaoTamanhoMaximo = 2000;
    public const int ImagemPathTamanhoMaximo = 500;

    public static IRuleBuilderOptions<T, string> NomeDaCampanha<T>(this IRuleBuilder<T, string> regra) => regra
        .Must(nome => !string.IsNullOrWhiteSpace(nome)).WithMessage("Informe o nome da campanha.")
        .MaximumLength(NomeTamanhoMaximo).WithMessage($"O nome pode ter no máximo {NomeTamanhoMaximo} caracteres.");

    public static IRuleBuilderOptions<T, string?> DescricaoDaCampanha<T>(this IRuleBuilder<T, string?> regra) => regra
        .MaximumLength(DescricaoTamanhoMaximo).WithMessage($"A descrição pode ter no máximo {DescricaoTamanhoMaximo} caracteres.");

    public static IRuleBuilderOptions<T, string?> ImagemDaCampanha<T>(this IRuleBuilder<T, string?> regra) => regra
        .MaximumLength(ImagemPathTamanhoMaximo).WithMessage($"O caminho da imagem pode ter no máximo {ImagemPathTamanhoMaximo} caracteres.");
}

public sealed class CriarCampanhaRequestValidator : AbstractValidator<CriarCampanhaRequest>
{
    public CriarCampanhaRequestValidator()
    {
        RuleFor(r => r.Nome).NomeDaCampanha();
        RuleFor(r => r.Descricao).DescricaoDaCampanha();
        RuleFor(r => r.Sistema).NotEmpty().WithMessage("Informe o sistema.");
        RuleFor(r => r.CapaPath).ImagemDaCampanha();
        RuleFor(r => r.BannerPath).ImagemDaCampanha();
    }
}

public sealed class EditarCampanhaRequestValidator : AbstractValidator<EditarCampanhaRequest>
{
    public EditarCampanhaRequestValidator()
    {
        RuleFor(r => r.Nome).NomeDaCampanha();
        RuleFor(r => r.Descricao).DescricaoDaCampanha();
        RuleFor(r => r.CapaPath).ImagemDaCampanha();
        RuleFor(r => r.BannerPath).ImagemDaCampanha();
    }
}

/// <summary>
/// Formato da lista. Se ela tem exatamente as campanhas do usuário, só o caso de uso sabe.
/// </summary>
public sealed class OrdenarCampanhasRequestValidator : AbstractValidator<OrdenarCampanhasRequest>
{
    public OrdenarCampanhasRequestValidator()
    {
        RuleFor(r => r.CampaignIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe as campanhas na ordem da grade.")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("A lista tem campanha repetida.");
    }
}

public sealed class ExcluirCampanhaRequestValidator : AbstractValidator<ExcluirCampanhaRequest>
{
    public ExcluirCampanhaRequestValidator()
    {
        RuleFor(r => r.ConfirmacaoNome)
            .Must(texto => !string.IsNullOrWhiteSpace(texto)).WithMessage("Digite o nome da campanha para confirmar.");
        RuleFor(r => r.ConfirmacaoFrase)
            .Must(texto => !string.IsNullOrWhiteSpace(texto)).WithMessage("Digite a frase de confirmação.");
    }
}
