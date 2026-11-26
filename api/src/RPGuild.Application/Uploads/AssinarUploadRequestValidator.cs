using FluentValidation;

namespace RPGuild.Application.Uploads;

public sealed class AssinarUploadRequestValidator : AbstractValidator<AssinarUploadRequest>
{
    public AssinarUploadRequestValidator()
    {
        RuleFor(r => r.Tipo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o tipo.")
            .Must(TiposDeUpload.EhSuportado).WithMessage(r => $"O tipo \"{r.Tipo}\" ainda não é suportado.");

        RuleFor(r => r.ContentType)
            .Must(TiposDeImagem.EhPermitido)
            .WithMessage($"Tipo de arquivo não permitido. Use um destes: {string.Join(", ", TiposDeImagem.Permitidos)}.");
    }
}
