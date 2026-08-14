// <copyright file="UpdateStencilState.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class UpdateStencilState : TableManager<Stencil, EnhancedStencil>
{
    private bool blazorStop = true;

    private StencilStatusUpdateForm formModel = new ();

    private IList<string> availableStatuses = [];

    /// <summary>
    /// Gets or sets the targeted stencil.
    /// </summary>
    [Parameter]
    public EnhancedStencil? Target { get; set; }

    /// <summary>
    /// Gets or sets the action to perform upon pressing the history button.
    /// </summary>
    [Parameter]
    public EventCallback<EnhancedStencil> OpenHistory { get; set; }

    /// <summary>
    /// Gets or sets the event to detect when an item might not appear in the DataView.
    /// </summary>
    [Parameter]
    public EventCallback<EnhancedStencil> OnItemChanged { get; set; }

    /// <summary>
    /// Gets the message to show when there are no stencils in <see cref="TableManager{TWrite, TRead}.DataView"/>.
    /// </summary>
    public override string EmptyMessage => "No stencils found matching these criteria.";

    /// <summary>
    /// Gets the optional status text filter.
    /// </summary>
    private Filter<string?> StatusTextFilter => this.GetFilter<string?>("StatusText");

    /// <summary>
    /// Gets the optional location filter.
    /// </summary>
    private Filter<string?> LocationFilter => this.GetFilter<string?>("Location");

    /// <summary>
    /// When this page loads, set the default load.
    /// </summary>
    /// <returns>A Task representing that the page has loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        using (SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync())
        {
            this.availableStatuses = await context.StatusCodes
                                                .AsNoTracking()
                                                .Select(sc => sc.Status)
                                                .ToListAsync();
        }

        this.SortList.Add(new ("ReceiveDate", SortDir.Desc));
        this.SortList.Add(new ("Barcode", SortDir.Desc));
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Populates <see cref="TableManager{TWrite, TRead}"/>'s filter registry with the filters applicable to the stencil view page.
    /// </summary>
    protected override void InitializeFilters()
    {
        this.Filters["Model"] = new Filter<string>("Model", string.Empty);
        this.Filters["Barcode"] = new Filter<string>("Barcode", string.Empty);
        this.Filters["StatusText"] = new Filter<string>("StatusText", string.Empty);
        this.Filters["Location"] = new Filter<string>("Location", string.Empty);
    }

    /// <summary>
    /// Applies the model, barcode, status, and location filters if they are active.
    /// </summary>
    /// <param name="query"> <inheritdoc path="/param[@name='query']" /></param>
    /// <returns><inheritdoc/></returns>
    protected override IQueryable<EnhancedStencil> ApplyFilters(IQueryable<EnhancedStencil> query) =>
        query
            .ApplyFilterIfActive(this.ModelFilter, x => x.Model.Contains(this.ModelFilter.Value!))
            .ApplyFilterIfActive(this.BarcodeFilter, x => x.Barcode.Contains(this.BarcodeFilter.Value!))
            .ApplyFilterIfActive(this.LocationFilter, x => x.Location.Contains(this.LocationFilter.Value!))
            .ApplyFilterIfActive(this.StatusTextFilter, x => x.StatusText.Contains(this.StatusTextFilter.Value!));

    /// <summary>
    /// Skip <see cref="TableManager{TRead, TWrite}.RefreshData(bool)"/> if there's no change that requires a DB hit.
    /// </summary>
    /// <returns>A Task representing that the conditional load has completed.</returns>
    protected override async Task OnParametersSetAsync()
    {
        if (this.TotalCount == 0)
        {
            await base.OnParametersSetAsync();
        }
    }

    /// <summary>
    /// When rendering the page, detect if <see cref="Target"/> may now be absent.
    /// </summary>
    /// <param name="firstRender"><inheritdoc path="/param[@name='firstRender']"/></param>
    /// <returns>A Task representing that post-render operations have completed.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!this.blazorStop)
        {
            this.blazorStop = true;

            // The UI has rendered the new page results, so now it's safe to close.
            await this.OnItemChanged.InvokeAsync(this.Target);
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void CloseForm()
    {
        base.CloseForm();
        this.Target = null;
        this.formModel = new ();
    }

    /// <summary>
    /// <inheritdoc cref="TableManager{TWrite, TRead}.HandleValidSubmit(bool)" />
    /// </summary>
    /// <param name="isInsert">Whether this is an insert operation (or an update operation).</param>
    /// <returns><inheritdoc/></returns>
    protected override async Task HandleValidSubmit(bool isInsert = false)
    {
        using SmtStencilingDbContext context = this.DbFactory.CreateDbContext();
        using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Set the SQL Server SESSION_CONTEXT for the trigger
            await context.Database.ExecuteSqlRawAsync(
                "EXEC sp_set_session_context @key=N'ModifiedByAssociateId', @value={0}; " +
                "EXEC sp_set_session_context @key=N'StatusChangeNote', @value={1};",
                this.formModel.AssociateNum ?? (object)DBNull.Value,
                string.IsNullOrWhiteSpace(this.formModel.Note) ? DBNull.Value : this.formModel.Note);

            // Perform standard update/insert flow
            context.Set<Stencil>().Update(this.EditItem);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            this.InsertPreRefreshSequence();
            await this.RefreshData();
            this.InsertPostRefreshSequence();
            this.CloseForm();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            this.ErrorMessage = "A database error occurred. The data may have changed since you opened the form.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            this.ErrorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            // Clean up session context to prevent connection pooling contamination
            try
            {
                using SmtStencilingDbContext cleanupContext = this.DbFactory.CreateDbContext();
                await cleanupContext.Database.ExecuteSqlRawAsync(
                    "EXEC sp_set_session_context @key=N'ModifiedByAssociateId', @value=NULL; " +
                    "EXEC sp_set_session_context @key=N'StatusChangeNote', @value=NULL;");
            }
            catch
            {
                // Swallow cleanup errors if connection fails, as it's non-critical
            }
        }
    }

    private async Task ResolveStatus()
    {
        using SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync();
        this.EditItem.StatusCode = await context.StatusCodes
                                                .AsNoTracking()
                                                .Where(sc => sc.Status == this.formModel.NewStatus)
                                                .Select(sc => sc.Code)
                                                .FirstOrDefaultAsync();
    }

    private void HandleEdit(EnhancedStencil selected)
    {
        if (selected.Equals(this.Target))
        {
            this.CloseForm();
            return;
        }

        this.Target = selected;
        this.IsFormVisible = true;

        short barcodeNum = short.Parse(selected.Barcode[3..]);

        using (SmtStencilingDbContext context = this.DbFactory.CreateDbContext())
        {
            this.EditItem = context.Stencils.FirstOrDefault(s => s.Barcode == barcodeNum);
        }
    }
}

/// <summary>
/// Container for the data in the status update form.
/// </summary>
public class StencilStatusUpdateForm
{
    /// <summary>
    /// Gets or sets the user's associate number.
    /// </summary>
    [Required(ErrorMessage = "Associate number is required.")]
    [ValidateAssociateExists]
    public int? AssociateNum { get; set; }

    /// <summary>
    /// Gets or sets the note to attach in the historical log.
    /// </summary>
    [MaxLength(50, ErrorMessage = "Note must be no longer than 50 characters.")]
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets the new stencil status.
    /// </summary>
    [Required(ErrorMessage = "New status is required.")]
    public string? NewStatus { get; set; }
}
