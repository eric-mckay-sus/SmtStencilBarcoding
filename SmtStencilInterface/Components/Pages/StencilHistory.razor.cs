// <copyright file="StencilHistory.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

using Microsoft.AspNetCore.Components;

namespace SmtStencilInterface.Components.Pages;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class StencilHistory : TableManager<StencilStatusChange, EnhancedStencilStatusChange>
{
    private DateTime? updatedAfter;
    private DateTime? updatedBefore;

    /// <summary>
    /// Gets the message to show when there are no status updates in <see cref="TableManager{TWrite, TRead}.DataView"/>.
    /// </summary>
    public override string EmptyMessage => "No status updates found matching these criteria.";

    /// <summary>
    /// Gets the location filter.
    /// </summary>
    private Filter<string?> PanelNumFilter => this.GetFilter<string?>("PanelNum");

    /// <summary>
    /// Gets the filter for stencils updated after a target date.
    /// </summary>
    private Filter<DateTime?> UpdatedAfterFilter => this.GetFilter<DateTime?>("UpdatedAfter");

    /// <summary>
    /// Gets the filter for stencils updated before a target date.
    /// </summary>
    private Filter<DateTime?> UpdatedBeforeFilter => this.GetFilter<DateTime?>("UpdatedBefore");

    /// <summary>
    /// When this page loads, set the default load.
    /// </summary>
    /// <returns>A Task representing that the page has loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        this.SortList.Add(new ("Timestamp", SortDir.Desc));
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Populates <see cref="TableManager{TWrite, TRead}"/>'s filter registry with the filters applicable to the stencil history page.
    /// </summary>
    protected override void InitializeFilters()
    {
        this.Filters["Model"] = new Filter<string>("Model", string.Empty);
        this.Filters["Barcode"] = new Filter<string>("Barcode", string.Empty);
        this.Filters["PanelNum"] = new Filter<string>("PanelNum", string.Empty);
        this.Filters["UpdatedAfter"] = new Filter<DateTime?>("UpdatedAfter", default);
        this.Filters["UpdatedBefore"] = new Filter<DateTime?>("UpdatedBefore", default);
    }

    /// <summary>
    /// Applies the model, barcode, panel number, and date filters if they are active.
    /// </summary>
    /// <param name="query"> <inheritdoc path="/param[@name='query']" /></param>
    /// <returns><inheritdoc/></returns>
    protected override IQueryable<EnhancedStencilStatusChange> ApplyFilters(IQueryable<EnhancedStencilStatusChange> query)
    {
        query = query
            .ApplyFilterIfActive(this.ModelFilter, x => x.Model.Contains(this.ModelFilter.Value!))
            .ApplyFilterIfActive(this.BarcodeFilter, x => x.Barcode.Contains(this.BarcodeFilter.Value!))
            .ApplyFilterIfActive(this.PanelNumFilter, x => x.PanelNum.Contains(this.PanelNumFilter.Value!));

        if (this.updatedAfter.HasValue)
        {
            query = query.Where(x => x.Timestamp > this.updatedAfter.Value);
        }

        if (this.updatedBefore.HasValue)
        {
            query = query.Where(x => x.Timestamp < this.updatedBefore.Value);
        }

        return query;
    }

    private async Task ClearDateFilters()
    {
        this.updatedAfter = null;
        this.updatedBefore = null;
        await this.RefreshData();
    }

}
