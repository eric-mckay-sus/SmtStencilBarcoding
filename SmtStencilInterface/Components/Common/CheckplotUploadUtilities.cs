// <copyright file="CheckplotUploadUtilities.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Common;

/// <summary>
/// Contains methods for handling the details of an upload (e.g., the highlight of the drag/drop area, the loading bar).
/// </summary>
public sealed partial class UploadCheckplot : IDisposable
{
    /// <summary>
    /// Gets or sets a value indicating whether the user is dragging a file over the file input location.
    /// </summary>
    private bool IsDragging { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the user is uploading a file right now.
    /// </summary>
    private bool IsUploading { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="TableManager{TWrite, TRead}.InputProvider"/> is awaiting confirmation (i.e. whether to display the confirmation dialog).
    /// </summary>
    private bool IsAwaitingConfirmation { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the file manager is currently selecting a file. Guards against file selection flicker.
    /// </summary>
    private bool IsProcessingSelection { get; set; } = false;

    /// <summary>
    /// Gets or sets the actual progress through the upload.
    /// </summary>
    private int ProgressPercent { get; set; } = 0;

    /// <summary>
    /// Gets or sets the displayed (elastic) progress through the upload.
    /// </summary>
    private double DisplayPercent { get; set; } = 0;

    /// <summary>
    /// Gets or sets the control for the creation/disposal of <see cref="ProgressTimer"/>.
    /// </summary>
    private CancellationTokenSource? TimerCts { get; set; }

    /// <summary>
    /// Gets or sets the timer used for the elastic loading bar.
    /// </summary>
    private PeriodicTimer? ProgressTimer { get; set; }

    /// <summary>
    /// Signature and pattern in order to implement IDisposable.
    /// Note: GC stands for garbage collector, which internally calls Dispose(false). By calling Dispose(true) here, we effectively circumvent that with the manual disposal.
    /// </summary>
    public void Dispose()
    {
        // Fire the CancellationToken, dispose immediately
        this.TimerCts?.Cancel();
        this.TimerCts?.Dispose();

        // Dispose the timer
        this.ProgressTimer?.Dispose();

        // Want to somehow clean up files from this run or mark that they are finished for future handling
        this.IsUploading = false;
    }

    /// <summary>
    /// An 'elastic' progress bar (displayed completion approaches actual completion at higher rate the further they are apart)
    /// Matching the actual upload progress looks too fast, so to give the user good feedback, slow it down artificially.
    /// </summary>
    /// <returns>A Task representing that the elastic loading bar is active.</returns>
    private async Task StartProgressSimulation()
    {
        // Cancel any existing progress timer
        if (this.TimerCts != null)
        {
            await this.TimerCts.CancelAsync();
            this.TimerCts?.Dispose();
        }

        this.TimerCts = new ();
        CancellationToken token = this.TimerCts.Token;

        this.DisplayPercent = 0;
        this.ProgressTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(16)); // Assuming 60 Hz, this is the hardware limit

        try
        {
            while (await this.ProgressTimer.WaitForNextTickAsync(token))
            {
                // Simple "Ease-Out" logic:
                // Move 10% of the remaining distance to the target each tick
                double diff = this.ProgressPercent - this.DisplayPercent;

                // Standard elastic progress
                if (diff > 0.1)
                {
                    this.DisplayPercent += diff * 0.15; // this factor is the elasticity parameter
                    await this.InvokeAsync(this.StateHasChanged);
                }

                // Finished, jump to end
                else if (this.ProgressPercent > 95 && diff < 5)
                {
                    this.DisplayPercent = 100;
                    await this.InvokeAsync(this.StateHasChanged);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // This exception is always thrown when a CancellationToken is used
        }
        finally
        {
            this.ProgressTimer?.Dispose();
        }
    }

    /// <summary>
    /// When a file is dragged into the upload box, throw the drag flag.
    /// </summary>
    private void HandleDragEnter() => this.IsDragging = true;

    /// <summary>
    /// When a file hovers over the upload box, throw the drag flag.
    /// </summary>
    private void HandleDragOver() => this.IsDragging = true;

    /// <summary>
    /// When a file is dragged out of into the upload box, reset the drag flag.
    /// </summary>
    private void HandleDragLeave() => this.IsDragging = false;

    /// <summary>
    /// When a file is placed into the upload box, reset the drag flag.
    /// </summary>
    private void HandleDrop() => this.IsDragging = false;
}
