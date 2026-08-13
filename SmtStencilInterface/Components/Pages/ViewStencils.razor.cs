// <copyright file="ViewStencils.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

using Microsoft.JSInterop;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class ViewStencils : TableManager<Stencil, EnhancedStencil>
{
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

    private async Task HandleDownload(EnhancedStencil enhancedStencil)
    {
        string? panelNum;
        using (SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync())
        {
            panelNum = await context.ModelToPanel
                                                .AsNoTracking()
                                                .Where(mp => mp.Model == enhancedStencil.Model)
                                                .Select(mp => mp.PanelNum)
                                                .FirstOrDefaultAsync();
        }

        string fileName = $"{enhancedStencil.Model}_{panelNum}_{enhancedStencil.JobNum}.gbx";
        await this.JS.InvokeVoidAsync("downloadGbxFromStream", fileName, enhancedStencil.Checkplot);
    }
}
