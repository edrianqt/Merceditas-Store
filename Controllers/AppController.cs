using Microsoft.AspNetCore.Mvc;
using MerceditasStore.Models;

namespace MerceditasStore.Controllers;

/// <summary>Shared helpers for showing a one-time message after an action.</summary>
public abstract class AppController : Controller
{
    protected void FlashSuccess(string message) => TempData["Flash"] = message;

    protected void FlashError(string message) => TempData["FlashError"] = message;

    protected void Flash(OpResult result)
    {
        if (result.Ok) FlashSuccess(result.Message);
        else FlashError(result.Message);
    }
}
