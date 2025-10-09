using FluentValidation;

namespace StudentUnionBot.Application.Admin.Commands.RestoreBackup;

public class RestoreBackupCommandValidator : AbstractValidator<RestoreBackupCommand>
{
    public RestoreBackupCommandValidator()
    {
        RuleFor(x => x.AdminId)
            .GreaterThan(0)
            .WithMessage("AdminId повинен бути більше 0");

        RuleFor(x => x.BackupFilePath)
            .NotEmpty()
            .WithMessage("Шлях до файлу резервної копії не може бути порожнім")
            .Must(path => File.Exists(path))
            .WithMessage("Файл резервної копії не існує");
    }
}