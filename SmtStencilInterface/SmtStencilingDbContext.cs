// <copyright file="SmtStencilingDbContext.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface;

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Represents the state of the database in a way friendly to EF Core.
/// </summary>
/// <param name="options">The server details and login credentials.</param>
public class SmtStencilingDbContext(DbContextOptions<SmtStencilingDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the model-to-panel mapping table.
    /// </summary>
    public DbSet<ModelPanel> ModelToPanel { get; set; }

    /// <summary>
    /// Gets or sets the model-to-line mapping table (used for autofill).
    /// </summary>
    public DbSet<ModelLine> ModelToLine { get; set; }

    /// <summary>
    /// Gets or sets the stencils table.
    /// </summary>
    public DbSet<Stencil> Stencils { get; set; }

    /// <summary>
    /// Gets or sets the read-only view that connects stencils to their model and status text.
    /// </summary>
    public DbSet<EnhancedStencil> EnhancedStencilView { get; set; }

    /// <summary>
    /// Gets or sets the status codes lookup table.
    /// </summary>
    public DbSet<StatusCode> StatusCodes { get; set; }

    /// <summary>
    /// Gets or sets the stencil status changes history table.
    /// </summary>
    public DbSet<StencilStatusChange> StencilStatusChanges { get; set; }

    /// <summary>
    /// Gets or sets the read-only view that connects stencils to their model and status text.
    /// </summary>
    public DbSet<EnhancedStencilStatusChange> EnhancedStencilStatusChanges { get; set; }

    /// <summary>
    /// Gets or sets the associate information table.
    /// </summary>
    public DbSet<Associate> AssociateInfo { get; set; }
}

/// <summary>
/// Represents a model-to-panel mapping record in the database.
/// </summary>
[Table("ModelToPanel")]
[PrimaryKey(nameof(Id))]
public class ModelPanel
{
    /// <summary>
    /// Gets or sets the unique identifier for this model-to-panel entry.
    /// </summary>
    [Column("id")]
    [Verbose]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the model name for this mapping.
    /// </summary>
    [Column("modelName")]
    [Required(ErrorMessage = "Model name is required")]
    [MaxLength(25, ErrorMessage = "Model name must be no more than 25 characters.")]
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the panel number for this mapping.
    /// </summary>
    [Column("panelNum")]
    [Required(ErrorMessage = "Panel number is required")]
    [MaxLength(25, ErrorMessage = "Panel number must be no more than 25 characters.")]
    public string? PanelNum { get; set; }
}

/// <summary>
/// Represents a model-to-line mapping record in the database.
/// </summary>
[Table("ModelToLine")]
[PrimaryKey(nameof(IcsNum), nameof(WorkCenterCode))]
public class ModelLine
{
    /// <summary>
    /// Gets or sets the ICS number for this model-to-line entry.
    /// </summary>
    [Column("icsNum")]
    public required string IcsNum { get; set; }

    /// <summary>
    /// Gets or sets the work center code for this mapping.
    /// </summary>
    [Column("workCenterCode")]
    public required string WorkCenterCode { get; set; }

    /// <summary>
    /// Gets or sets the model name for this mapping.
    /// </summary>
    [Column("shortDesc")]
    public required string Model { get; set; }
}

/// <summary>
/// Represents a stencil record in the database.
/// </summary>
[Table("Stencils")]
[PrimaryKey(nameof(Barcode))]
public class Stencil
{
    /// <summary>
    /// Gets or sets the stencil barcode.
    /// </summary>
    [Column("barcode")]
    public short Barcode { get; set; }

    /// <summary>
    /// Gets or sets the maker of the stencil.
    /// </summary>
    [Column("maker")]
    [Required(ErrorMessage = "Stencil maker is required.")]
    [MaxLength(25, ErrorMessage = "Stencil maker must be no more than 25 characters.")]
    public string? Maker { get; set; }

    /// <summary>
    /// Gets or sets the job number for the stencil.
    /// </summary>
    [Column("jobNum")]
    [Required(ErrorMessage = "Job number is required.")]
    [MaxLength(20, ErrorMessage = "Panel number must be no more than 20 characters.")]
    public string? JobNum { get; set; }

    /// <summary>
    /// Gets or sets the receive date for the stencil.
    /// </summary>
    [Column("receiveDate")]
    [Required(ErrorMessage = "Stencil receive date is required.")]
    public DateOnly? ReceiveDate { get; set; }

    /// <summary>
    /// Gets or sets the cycle count for the stencil.
    /// </summary>
    [Column("cycleCount")]
    [Required(ErrorMessage = "Cycle count is required.")]
    public int? CycleCount { get; set; }

    /// <summary>
    /// Gets or sets the expiration date for the stencil.
    /// </summary>
    [Column("expirationDate")]
    [Required(ErrorMessage = "Expiration date is required.")]
    public DateOnly? ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the thickness of the stencil.
    /// </summary>
    [Column("thickness")]
    [Required(ErrorMessage = "Stencil thickness is required.")]
    public byte? Thickness { get; set; }

    /// <summary>
    /// Gets or sets the status code of the stencil.
    /// </summary>
    [Column("statusCode")]
    [Required(ErrorMessage = "Stencil status is required.")]
    public byte? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the location of the stencil.
    /// </summary>
    [Column("location")]
    [Required(ErrorMessage = "Stencil location is required.")]
    [MaxLength(8, ErrorMessage = "Line name must be no longer than 8 characters (try truncating)")]
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the ID of the model to which this stencil belongs.
    /// </summary>
    [NotDisplayed]
    [Required(ErrorMessage = "Model name is required.")]
    [Column("modelId")]
    public int? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the binary checkplot data for the stencil.
    /// </summary>
    [NotDisplayed]
    [Column("checkplot")]
    public byte[]? Checkplot { get; set; }

    /// <summary>
    /// Gets or sets an optional note for the stencil.
    /// </summary>
    [Verbose]
    [Column("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Represents a stencil record in the view that provides model name and in the database.
/// </summary>
[Table("EnhancedStencilView")]
[PrimaryKey(nameof(Barcode))]
public class EnhancedStencil : IEquatable<EnhancedStencil>
{
    /// <summary>
    /// Gets or sets the name of the model to which this stencil belongs.
    /// </summary>
    [Column("modelName")]
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the stencil barcode.
    /// </summary>
    [Column("barcode")]
    public string? Barcode { get; set; }

    /// <summary>
    /// Gets or sets the maker of the stencil.
    /// </summary>
    [Column("maker")]
    public string? Maker { get; set; }

    /// <summary>
    /// Gets or sets the job number for the stencil.
    /// </summary>
    [Column("jobNum")]
    public string? JobNum { get; set; }

    /// <summary>
    /// Gets or sets the receive date for the stencil.
    /// </summary>
    [Column("receiveDate")]
    public DateOnly ReceiveDate { get; set; }

    /// <summary>
    /// Gets or sets the cycle count for the stencil.
    /// </summary>
    [Column("cycleCount")]
    public int CycleCount { get; set; }

    /// <summary>
    /// Gets or sets the expiration date for the stencil.
    /// </summary>
    [Column("expirationDate")]
    public DateOnly ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the thickness of the stencil.
    /// </summary>
    [Column("thickness")]
    public byte Thickness { get; set; }

    /// <summary>
    /// Gets or sets the status of the stencil.
    /// </summary>
    [Column("statusText")]
    public string? StatusText { get; set; }

    /// <summary>
    /// Gets or sets the location of the stencil.
    /// </summary>
    [Column("location")]
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets the binary checkplot data for the stencil.
    /// </summary>
    [NotDisplayed]
    [Column("checkplot")]
    public byte[]? Checkplot { get; set; }

    /// <summary>
    /// Gets or sets an optional note for the stencil.
    /// </summary>
    [Verbose]
    [Column("note")]
    public string? Note { get; set; }

    public bool Equals(EnhancedStencil? other)
    {
        if (other is null)
        {
            return false;
        }

        return this.Barcode == other.Barcode;
    }

    public override bool Equals(object? obj) => this.Equals(obj as EnhancedStencil);

    public override int GetHashCode() => this.Barcode.GetHashCode();
}

/// <summary>
/// Represents a status code record in the database.
/// </summary>
[Table("StatusCodes")]
[PrimaryKey(nameof(Code))]
public class StatusCode
{
    /// <summary>
    /// Gets or sets the numeric status code.
    /// </summary>
    [Column("statusCode")]
    public byte Code { get; set; }

    /// <summary>
    /// Gets or sets the status description text.
    /// </summary>
    [Column("statusText")]
    public required string Status { get; set; }
}

/// <summary>
/// Represents a stencil status change log record in the database.
/// </summary>
[Table("StencilStatusChanges")]
[Keyless]
public class StencilStatusChange
{
    /// <summary>
    /// Gets or sets the stencil barcode.
    /// </summary>
    [Column("barcode")]
    public short Barcode { get; set; }

    /// <summary>
    /// Gets or sets the state the stencil changed from.
    /// </summary>
    [Column("fromState")]
    public byte FromState { get; set; }

    /// <summary>
    /// Gets or sets the state the stencil changed to.
    /// </summary>
    [Column("toState")]
    public byte ToState { get; set; }

    /// <summary>
    /// Gets or sets the associate ID who performed the status change.
    /// </summary>
    [Column("associateId")]
    public string? AssociateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the status change occurred.
    /// </summary>
    [Column("changeTime")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets an optional note for the status change.
    /// </summary>
    [Verbose]
    [Column("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Represents a stencil status change log record in the database.
/// </summary>
[Table("EnhancedStencilStatusChanges")]
[Keyless]
public class EnhancedStencilStatusChange
{
    /// <summary>
    /// Gets or sets the stencil barcode.
    /// </summary>
    [Column("barcode")]
    public string? Barcode { get; set; }

    /// <summary>
    /// Gets or sets the state the stencil changed from.
    /// </summary>
    [Column("fromStatusText")]
    public string? FromState { get; set; }

    /// <summary>
    /// Gets or sets the state the stencil changed to.
    /// </summary>
    [Column("toStatusText")]
    public string? ToState { get; set; }

    /// <summary>
    /// Gets or sets the associate ID who performed the status change.
    /// </summary>
    [Column("associateId")]
    public string? AssociateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the status change occurred.
    /// </summary>
    [Column("changeTime")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets an optional note for the status change.
    /// </summary>
    [Verbose]
    [Column("note")]
    public string? Note { get; set; }
}

/// <summary>
/// Represents an associate record in the database.
/// </summary>
[PrimaryKey(nameof(BadgeNum))]
public class Associate
{
    /// <summary>
    /// Gets or sets the associate badge number.
    /// </summary>
    [Column("badgeNum")]
    public int BadgeNum { get; set; }

    /// <summary>
    /// Gets or sets the associate number.
    /// </summary>
    [Column("associateNum")]
    public int AssociateNum { get; set; }

    /// <summary>
    /// Gets or sets the associate's name.
    /// </summary>
    [Column("associateName")]
    public string? Name { get; set; }

    /// <summary>
    /// Associates are equal if they share the same badge number.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>True when the objects represent the same associate.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is Associate other)
        {
            return this.BadgeNum == other.BadgeNum;
        }

        return false;
    }

    /// <summary>
    /// Gets the hash code for this associate.
    /// </summary>
    /// <returns>The associate's hash code.</returns>
    public override int GetHashCode() => this.BadgeNum.GetHashCode();

    /// <summary>
    /// Returns a descriptive string representation of this associate.
    /// </summary>
    /// <returns>The associate description.</returns>
    public override string ToString()
    {
        return $"Name: {this.Name}, Assoc #: {this.AssociateNum}, Badge #: {this.BadgeNum}";
    }
}
