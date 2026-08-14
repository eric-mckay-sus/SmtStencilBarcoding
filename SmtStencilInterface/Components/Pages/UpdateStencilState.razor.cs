// <copyright file="UpdateStencilState.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Pages;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Code-behind for the model creation page.
/// </summary>
public partial class UpdateStencilState : TableManager<Stencil, EnhancedStencil>
{
    /// <summary>
    /// Gets or sets the targeted stencil.
    /// </summary>
    [Parameter]
    public EnhancedStencil? Target { get; set; }

    /// <summary>
    /// Gets or sets the action to perform upon pressing the history button.
    /// </summary>
    [Parameter]
    public EventCallback<EnhancedStencil> OpenHistory { get; set; }

    /// <summary>
    /// Gets or sets the event to detect when an item might not appear in the DataView.
    /// </summary>
    [Parameter]
    public EventCallback<EnhancedStencil> OnItemChanged { get; set; }

    /// <summary>
    /// When this page loads, set the default load.
    /// </summary>
    /// <returns>A Task representing that the page has loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        this.SortList.Add(new ("Barcode", SortDir.Desc));
        await base.OnInitializedAsync();
    }
}
