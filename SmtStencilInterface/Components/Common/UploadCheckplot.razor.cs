// <copyright file="UploadCheckplot.razor.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface.Components.Common;

using InterProcessIO;
using Microsoft.AspNetCore.Components.Forms;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;

/// <summary>
/// Code-behind for the UploadCheckplot component.
/// </summary>
public sealed partial class UploadCheckplot
{
    /// <summary>
    /// Establish a 5 MB checkplot file size limit.
    /// </summary>
    public static readonly int MaxFileSize = 1024 * 1024 * 5;

    private IBrowserFile? selectedFile;

    /// <summary>
    /// Gets or sets the byte array representing the uploaded file.
    /// </summary>
    [Parameter]
    public byte[] SerializedFile { get; set; } = new byte[MaxFileSize];

    /// <summary>
    /// Gets or sets a callback invoked when a file is uploaded with the serialized file data.
    /// </summary>
    [Parameter]
    public EventCallback<byte[]> OnFileUploaded { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when the file is cleared.
    /// </summary>
    [Parameter]
    public EventCallback OnFileCleared { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the record already has a checkplot file attached.
    /// </summary>
    [Parameter]
    public bool HasOriginalFile { get; set; } = false;

    /// <summary>
    /// When this page loads, wire the input provider's confirmation event to auto-open an alert (with flag).
    /// Also, set the output's OnNotify event to update the progress bar.
    /// </summary>
    /// <returns>A Task representing that the page has loaded.</returns>
    protected override async Task OnInitializedAsync()
    {
        this.SortList.Add(new ("Model", SortDir.Asc));
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Executes the the actual upload after validation is complete by staging the selected file, then passing its path to the uploader.
    /// </summary>
    /// <returns>A Task representing whether the upload completion status.</returns>
    private async Task<UploadResult> ExecuteUpload()
    {
        this.Reporter.InitializeProgress(1);

        // Improbable, but treat like a cancel
        if (this.selectedFile == null)
        {
            return UploadResult.Canceled;
        }

        // Stream the file data from the element to the server (must use block using statement to close stream before the uploader tries to create a new one)
        using (MemoryStream stream = new ())
        {
            await this.selectedFile.OpenReadStream(MaxFileSize).CopyToAsync(stream);
            this.SerializedFile = stream.ToArray();
            await this.OnFileUploaded.InvokeAsync(this.SerializedFile);
        }

        // Notify parent of the uploaded file
        await this.OnFileUploaded.InvokeAsync(this.SerializedFile);
        return UploadResult.Complete;
    }

    /// <summary>
    /// Clears the selected file and notifies the parent component.
    /// </summary>
    /// <returns>A Task representing the clear operation.</returns>
    private async Task ClearFile()
    {
        this.selectedFile = null;
        this.SerializedFile = new byte[MaxFileSize];
        await this.OnFileCleared.InvokeAsync();
        await this.InvokeAsync(this.StateHasChanged);
    }

    /// <summary>
    /// Set the selected file, with guard check to guarantee no visual flicker.
    /// </summary>
    /// <param name="e">The event representing file selection.</param>
    /// <returns>A Task representing that the file was successfully selected.</returns>
    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        if (this.IsProcessingSelection)
        {
            return;
        }

        this.IsProcessingSelection = true;

        try
        {
            this.selectedFile = e.File;
            await this.Upload("Checkplot file successfully added");
        }
        finally
        {
            this.IsProcessingSelection = false;
        }
    }

    /// <summary>
    /// Upon receiving confirmation, throw the flag to hide the alert and pass the boolean value to the input provider.
    /// In case of cancel, also deselect the file and exit the upload state.
    /// </summary>
    /// <param name="result">Whether to confirm/cancel (t/f).</param>
    private void HandleConfirm(bool result)
    {
        this.IsAwaitingConfirmation = false;
        this.InputProvider.SetConfirmResult(result);
        if (!result)
        {
            this.selectedFile = null;
            this.IsUploading = false;
        }
    }

    /// <summary>
    /// The template method that handles the UI state and lifecycle of an upload.
    /// </summary>
    /// <param name="successMessage">The message to toast with on success.</param>
    /// <returns>A Task signaling that the upload is complete and garbage has been collected.</returns>
    private async Task Upload(string successMessage)
    {
        if (this.IsUploading)
        {
            return;
        }

        this.ProgressPercent = 5;
        _ = this.StartProgressSimulation();

        // Let the UI breathe so the progress bar/spinner appears
        await Task.Delay(100);

        this.IsUploading = true;
        this.Reporter.ClearLogs();

        try
        {
            UploadResult result = await this.ExecuteUpload();

            switch (result)
            {
                case UploadResult.Complete:
                    this.ProgressPercent = 101;
                    await Task.Delay(750);
                    await this.RefreshData();
                    this.ToastService.Notify(new (ToastType.Success, $"{successMessage}!"));
                    break;
                case UploadResult.CompleteWithErrors:
                    this.ProgressPercent = 101;
                    await Task.Delay(750);
                    await this.RefreshData();
                    this.ToastService.Notify(new (ToastType.Warning, $"{successMessage} with errors. Check the summary table and log to see what didn't go through."));
                    break;
                case UploadResult.ErroredOut:
                    Report? error = this.Reporter.Logs.Select(log => log.content).LastOrDefault(log => log.level == ReportLevel.ERROR) ?? this.Reporter.Logs.Select(log => log.content).LastOrDefault();
                    this.ToastService.Notify(new (ToastType.Danger, $"{error?.message ?? "There was an error that prevented your upload from completing"}. Please verify your file."));
                    break;
                case UploadResult.Canceled:
                    this.ProgressPercent = 101;
                    this.ToastService.Notify(new (ToastType.Secondary, "Upload canceled."));
                    break;
            }
        }
        catch (Exception ex)
        {
            this.ToastService.Notify(new (ToastType.Danger, $"\nUpload failed: {ex.Message}"));
        }
        finally
        {
            this.selectedFile = null;
            this.IsUploading = false;
        }
    }
}
