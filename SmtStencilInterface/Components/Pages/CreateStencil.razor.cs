// <copyright file="CreateStencil.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class CreateStencil : TableManager<Stencil, EnhancedStencil>
{
    private IList<string> availableStatuses = [];

    private IList<string?> availableModels = [];

    private IList<string> availableLines = [];

    private string targetModel = string.Empty;

    private string targetStatus = string.Empty;

    private short dummyThickness = 0;

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
        await base.OnInitializedAsync();
    }

    private async Task ResolveModel()
    {
        using (SmtStencilingDbContext context = await this.DbFactory.CreateDbContextAsync())
        {
            this.NewItem.ModelId = await context.ModelToPanel
                                                .AsNoTracking()
                                                .Where(mp => mp.Model == this.targetModel)
                                                .Select(mp => mp.Id)
                                                .FirstAsync();
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

    private async Task ResolveThickness()
    {
        this.NewItem.Thickness = (byte)this.dummyThickness;
    }
}
