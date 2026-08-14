// <copyright file="CreateStencil.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

using BlazorBootstrap;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class CreateStencil : TableManager<Stencil, EnhancedStencil>
{
    private IList<string> availableStatuses = [];

    private IList<string?> availableModels = [];

    private IList<string> availableLines = [];

    private string? targetModel;

    private string? targetStatus;

    private short? dummyThickness;

    private EnhancedStencil? target;

    /// <summary>
    /// Gets the message to show when there are no stencils in <see cref="TableManager{TWrite, TRead}.DataView"/>.
    /// </summary>
    public override string EmptyMessage => "No stencils found matching these criteria.";

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
                                                .Distinct()
                                                .ToListAsync();

            this.availableModels = await context.ModelToPanel
                                                .AsNoTracking()
                                                .Select(mp => mp.Model)
                                                .Distinct()
                                                .ToListAsync();

            this.availableLines = await context.ModelToLine
                                                .AsNoTracking()
                                                .Select(mtl => mtl.WorkCenterCode)
                                                .Distinct()
                                                .ToListAsync();
        }

        this.SortList.Add(new ("ReceiveDate", SortDir.Desc));
        this.SortList.Add(new ("Barcode", SortDir.Desc));
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Populates <see cref="TableManager{TWrite, TRead}"/>'s filter registry with the filters applicable to the stencil creation page.
    /// </summary>
    protected override void InitializeFilters()
    {
        this.Filters["Model"] = new Filter<string>("Model", string.Empty);
        this.Filters["Barcode"] = new Filter<string>("Barcode", string.Empty);
    }

    /// <summary>
    /// Applies the model/barcode filters if they are active.
    /// </summary>
    /// <param name="query"> <inheritdoc path="/param[@name='query']" /></param>
    /// <returns><inheritdoc/></returns>
    protected override IQueryable<EnhancedStencil> ApplyFilters(IQueryable<EnhancedStencil> query) =>
        query
            .ApplyFilterIfActive(this.ModelFilter, x => x.Model.Contains(this.ModelFilter.Value!))
            .ApplyFilterIfActive(this.BarcodeFilter, x => x.Barcode.Contains(this.BarcodeFilter.Value!));

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void InsertPreRefreshSequence() => this.ModelFilter.Value = this.targetModel;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void InsertPostRefreshSequence()
    {
        using (SmtStencilingDbContext context = this.DbFactory.CreateDbContext())
        {
            this.target = context.EnhancedStencilView.FirstOrDefault(es => es.Barcode.Contains(this.EditItem.Barcode.ToString()));
        }

        this.ToastService.Notify(new (ToastType.Success, $"New stencil MSK{this.EditItem.Barcode} created successfully!"));

        this.targetModel = null;
        this.targetStatus = null;
        this.dummyThickness = null;
    }

    protected override async Task HandleValidSubmit(bool isInsert = false)
    {
        this.ErrorMessage = null;
        using SmtStencilingDbContext context = this.DbFactory.CreateDbContext();
        using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (EditItem.Barcode is not null)
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Stencils ON");
            }

            if (isInsert)
            {
                context.Set<Stencil>().Add(this.EditItem);
            }
            else
            {
                context.Set<Stencil>().Update(this.EditItem);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            this.InsertPreRefreshSequence();
            await this.RefreshData();
            this.InsertPostRefreshSequence();
            this.CloseForm();
        }
        catch (DbUpdateException)
        {
            // Fallback for race conditions (form validation handled elsewhere)
            this.ErrorMessage = "A database error occurred. The data may have changed since you opened the form.";
            await transaction.RollbackAsync();
        }
        catch (Exception)
        {
            this.ErrorMessage = "An unexpected error occurred. Please try again.";
            await transaction.RollbackAsync();
        }
    }

    private async Task ResolveModel()
    {
        using (SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync())
        {
            this.EditItem.ModelId = await context.ModelToPanel
                                                .AsNoTracking()
                                                .Where(mp => mp.Model == this.targetModel)
                                                .Select(mp => mp.Id)
                                                .FirstOrDefaultAsync();
        }
    }

    private async Task ResolveStatus()
    {
        using (SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync())
        {
            this.EditItem.StatusCode = await context.StatusCodes
                                                    .AsNoTracking()
                                                    .Where(sc => sc.Status == this.targetStatus)
                                                    .Select(sc => sc.Code)
                                                    .FirstOrDefaultAsync();
        }
    }

    private async Task ResolveThickness() => this.EditItem.Thickness = (byte)this.dummyThickness;
}
