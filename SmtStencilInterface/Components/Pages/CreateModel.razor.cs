// <copyright file="CreateModel.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class CreateModel : TableManager<ModelPanel>
{
    /// <summary>
    /// When this page loads, set the default load.
    /// </summary>
    /// <returns>A Task representing that the page has loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        this.SortList.Add(new ("ModelName", SortDir.Asc));
        await base.OnInitializedAsync();
    }
}
