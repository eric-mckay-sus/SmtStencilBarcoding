// <copyright file="ViewStencils.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class ViewStencils : TableManager<Stencil, EnhancedStencil>
{
    /// <summary>
    /// Gets or sets the JS handler to refocus the filter after debounce.
    /// </summary>
    [Inject]
    public IJSRuntime JS { get; set; } = default!;

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

    /// <summary>
    /// Detects the table, then saves the results of the query on that table to a CSV
    /// Uses JS runtime to download directly to browser Downloads location.
    /// </summary>
    /// <returns>A Task representing that the browser download has started.</returns>
    private async Task SaveToCSV()
    {
        PropertyInfo[] properties = typeof(EnhancedStencil).GetProperties();
        var csvBuilder = new System.Text.StringBuilder();

        // Header
        csvBuilder.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        // Re run the current query with the current filters and sorts
        using SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync();
        IQueryable<EnhancedStencil> query = context.Set<EnhancedStencil>().AsNoTracking();
        query = this.ApplyFilters(query);
        query = this.ApplySorting(query);
        List<EnhancedStencil> allData = await query.ToListAsync();

        // Loop through each row, parse, then pass to the CSV builder
        foreach (EnhancedStencil item in allData)
        {
            IEnumerable<string> values = properties.Select(p =>
            {
                string val = p.GetValue(item)?.ToString() ?? string.Empty;

                // CSV escaping: wrap in quotes if contains comma, newline, or quotes
                if (val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r'))
                {
                    val = $"\"{val.Replace("\"", "\"\"")}\"";
                }

                return val;
            });
            csvBuilder.AppendLine(string.Join(",", values));
        }

        // Call JS Runtime to perform the download
        string fileName = $"Stencils_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        await this.JS.InvokeVoidAsync("downloadCsvFromStream", fileName, csvBuilder.ToString());
    }
}
