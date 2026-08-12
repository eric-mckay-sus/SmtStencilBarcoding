// <copyright file="CreateStencil.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

using BlazorBootstrap;
using Microsoft.EntityFrameworkCore;

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
    /// Applies the model filter if it is active.
    /// </summary>
    /// <param name="query"> <inheritdoc path="/param[@name='query']" /></param>
    /// <returns><inheritdoc/></returns>
    protected override IQueryable<EnhancedStencil> ApplyFilters(IQueryable<EnhancedStencil> query)
    {
        if (this.ModelFilter is { IsActive: true, Value: not null })
        {
            query = query.Where(x => x.Model.Contains(this.ModelFilter.Value));
        }

        if (this.BarcodeFilter is { IsActive: true, Value: not null })
        {
            query = query.Where(x => x.Barcode.Contains(this.BarcodeFilter.Value));
        }

        return query;
    }

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
            this.target = context.EnhancedStencilView.FirstOrDefault(es => es.Barcode.Contains(this.NewItem.Barcode.ToString()));
        }

        this.ToastService.Notify(new (ToastType.Success, $"New stencil MSK{this.NewItem.Barcode} created successfully!"));

        this.targetModel = null;
        this.targetStatus = null;
        this.dummyThickness = null;
    }

    private async Task ResolveModel()
    {
        using (SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync())
        {
            this.NewItem.ModelId = await context.ModelToPanel
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
            this.NewItem.StatusCode = await context.StatusCodes
                                                    .AsNoTracking()
                                                    .Where(sc => sc.Status == this.targetStatus)
                                                    .Select(sc => sc.Code)
                                                    .FirstAsync();
        }
    }

    private async Task ResolveThickness() => this.NewItem.Thickness = (byte)this.dummyThickness;
}
