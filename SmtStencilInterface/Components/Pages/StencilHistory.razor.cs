// <copyright file="StencilHistory.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class StencilHistory : TableManager<StencilStatusChange, EnhancedStencilStatusChange>
{
    /// <summary>
    /// Gets the message to show when there are no status updates in <see cref="TableManager{TWrite, TRead}.DataView"/>.
    /// </summary>
    public override string EmptyMessage => "No status updates found matching these criteria.";

    /// <summary>
    /// Gets the optional location filter.
    /// </summary>
    private Filter<string?> PanelNumFilter => this.GetFilter<string?>("PanelNum");

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
    }

    protected override IQueryable<EnhancedStencilStatusChange> ApplyFilters(IQueryable<EnhancedStencilStatusChange> query) =>
    query
        .ApplyFilterIfActive(this.ModelFilter, x => x.Model.Contains(this.ModelFilter.Value!))
        .ApplyFilterIfActive(this.BarcodeFilter, x => x.Barcode.Contains(this.BarcodeFilter.Value!))
        .ApplyFilterIfActive(this.PanelNumFilter, x => x.PanelNum.Contains(this.PanelNumFilter.Value!));
}
