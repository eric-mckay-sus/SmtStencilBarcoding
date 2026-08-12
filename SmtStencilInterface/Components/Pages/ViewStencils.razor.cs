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
    /// When this page loads, set the default load.
    /// </summary>
    /// <returns>A Task representing that the page has loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        this.SortList.Add(new ("Barcode", SortDir.Asc));
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

        if (this.StatusFilter is { IsActive: true, Value: not null })
        {
            query = query.Where(x => x.StatusText.Contains(this.StatusFilter.Value));
        }

        if (this.LocationFilter is { IsActive: true, Value: not null })
        {
            query = query.Where(x => x.Location.Contains(this.LocationFilter.Value));
        }

        return query;
    }

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
