using FluentValidation;

namespace StudentUnionBot.Application.Admin.Commands.CreateBackup;

public class CreateBackupCommandValidator : AbstractValidator<CreateBackupCommand>
{
    public CreateBackupCommandValidator()
    {
        RuleFor(x => x.AdminId)
            .GreaterThan(0)
            .WithMessage("AdminId повинен бути більше 0");
    }
}