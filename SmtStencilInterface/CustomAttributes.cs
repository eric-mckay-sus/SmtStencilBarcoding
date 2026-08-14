// <copyright file="CustomAttributes.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface;

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Marks a property that should not be displayed in UniversalTable unless the table is expanded.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class VerboseAttribute : Attribute
{
}

/// <summary>
/// Marks a property that should not be displayed in UniversalTable.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotDisplayedAttribute : Attribute
{
}

/// <summary>
/// Verify that an associate exists in the associate database.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateAssociateExistsAttribute : ValidationAttribute
{
    /// <summary>
    /// Checks that the associate-line link's associate is in the associate database.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The metadata for the validation.</param>
    /// <returns>ValidationResult.Success if the associate is in the associate database, otherwise a validation result reporting the failure.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Semantically wrong, but we let other attributes handle it
        if (value is not int associateNum)
        {
            return ValidationResult.Success;
        }

        IDbContextFactory<SmtStencilingDbContext>? dbFactory = validationContext.GetService<IDbContextFactory<SmtStencilingDbContext>>();

        using SmtStencilingDbContext context = dbFactory!.CreateDbContext();

        // FK Check: Does Associate exist?
        if (!context.AssociateInfo.Any(a => a.AssociateNum == associateNum))
        {
            return new ValidationResult($"Associate #{associateNum} does not exist.");
        }

        return ValidationResult.Success;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class UniqueBarcodeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or DBNull)
        {
            return ValidationResult.Success;
        }

        short barcode;
        if (value is short s)
        {
            barcode = s;
        }
        else if (value is int i)
        {
            barcode = checked((short)i);
        }
        else if (value is string text && short.TryParse(text, out short parsed))
        {
            barcode = parsed;
        }
        else
        {
            return ValidationResult.Success;
        }

        IDbContextFactory<SmtStencilingDbContext>? dbFactory = validationContext.GetService<IDbContextFactory<SmtStencilingDbContext>>();
        if (dbFactory is null)
        {
            return ValidationResult.Success;
        }

        using SmtStencilingDbContext context = dbFactory.CreateDbContext();
        if (context.Stencils.Any(s => s.Barcode == barcode))
        {
            return new ValidationResult("Barcode must be unique.");
        }

        return ValidationResult.Success;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ValidateModelNameAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or DBNull)
        {
            return ValidationResult.Success;
        }

        if (value is not int modelId)
        {
            return ValidationResult.Success;
        }

        IDbContextFactory<SmtStencilingDbContext>? dbFactory = validationContext.GetService<IDbContextFactory<SmtStencilingDbContext>>();
        if (dbFactory is null)
        {
            return ValidationResult.Success;
        }

        using SmtStencilingDbContext context = dbFactory.CreateDbContext();
        if (!context.ModelToPanel.Any(m => m.Id == modelId))
        {
            return new ValidationResult("Model name must exist in ModelToPanel.");
        }

        return ValidationResult.Success;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ValidateLocationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or DBNull)
        {
            return ValidationResult.Success;
        }

        string? location = value as string;
        if (string.IsNullOrWhiteSpace(location))
        {
            return ValidationResult.Success;
        }

        IDbContextFactory<SmtStencilingDbContext>? dbFactory = validationContext.GetService<IDbContextFactory<SmtStencilingDbContext>>();
        if (dbFactory is null)
        {
            return ValidationResult.Success;
        }

        using SmtStencilingDbContext context = dbFactory.CreateDbContext();
        if (!context.ModelToLine.Any(m => m.WorkCenterCode == location))
        {
            return new ValidationResult("Location must exist in ModelToLine.");
        }

        return ValidationResult.Success;
    }
}
