// <copyright file="EditStencil.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Code-behind for the stencil editing page.
/// </summary>
public partial class EditStencil : TableManager<Stencil, EnhancedStencil>
{
    private string? targetModel;

    private string? targetStatus;

    private IList<string?> availableModels = [];

    private IList<string> availableLines = [];

    private EnhancedStencil? target;

    /// <summary>
    /// When this page loads, set the default load.
    /// </summary>
    /// <returns>A Task representing that the page has loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        using (SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync())
        {
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
    protected override void CloseForm()
    {
        base.CloseForm();
        this.target = null;
        this.targetModel = null;
        this.targetStatus = null;
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

    private void HandleEdit(EnhancedStencil selected)
    {
        if (selected.Equals(this.target))
        {
            this.target = null;
            this.targetModel = null;
            this.targetStatus = null;
            this.IsFormVisible = false;
            this.NewItem = new ();
            return;
        }

        this.target = selected;
        this.targetModel = selected.Model;
        this.targetStatus = selected.StatusText;
        this.IsFormVisible = true;

        short barcodeNum = short.Parse(selected.Barcode[3..]);

        using (SmtStencilingDbContext context = this.DbFactory.CreateDbContext())
        {
            this.NewItem = context.Stencils.FirstOrDefault(s => s.Barcode == barcodeNum);
        }
    }
}
