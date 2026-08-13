// <copyright file="CreateModel.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

using Microsoft.EntityFrameworkCore;
using BlazorBootstrap;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class CreateModel : TableManager<ModelPanel, ModelPanel>
{
    private IList<string> availableModels = [];

    /// <summary>
    /// Gets the message to show when there are no models in <see cref="TableManager{TWrite, TRead}.DataView"/>.
    /// </summary>
    public override string EmptyMessage => "No models found matching these criteria.";

    /// <summary>
    /// When this page loads, set the default load.
    /// </summary>
    /// <returns>A Task representing that the page has loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        using (SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync())
        {
            this.availableModels = await context.ModelToLine
                                                .AsNoTracking()
                                                .Select(mtl => mtl.Model)
                                                .Distinct()
                                                .ToListAsync();
        }

        this.SortList.Add(new ("Model", SortDir.Asc));
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Populates <see cref="TableManager{TWrite, TRead}"/>'s filter registry with the filters applicable to the model creation page.
    /// </summary>
    protected override void InitializeFilters() => this.Filters["Model"] = new Filter<string>("Model", string.Empty);

    /// <summary>
    /// Applies the model filter if it is active.
    /// </summary>
    /// <param name="query"> <inheritdoc path="/param[@name='query']" /></param>
    /// <returns><inheritdoc/></returns>
    protected override IQueryable<ModelPanel> ApplyFilters(IQueryable<ModelPanel> query) =>
        query.ApplyFilterIfActive(this.ModelFilter, x => x.Model.Contains(this.ModelFilter.Value!));

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void InsertPostRefreshSequence()
    {
        this.ToastService.Notify(new (ToastType.Success, $"New panel {this.NewItem.PanelNum} created for model {this.NewItem.Model}"));
    }
}
