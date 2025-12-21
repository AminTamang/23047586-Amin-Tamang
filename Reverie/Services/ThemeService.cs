using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.JSInterop;

public class ThemeService
{
    private readonly IJSRuntime _js;

    public string Theme { get; private set; } = "light";
    public string Accent { get; private set; } = "#7c3aed";

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitAsync()
    {
        Theme = await _js.InvokeAsync<string>("theme.get") ?? "light";
        Accent = await _js.InvokeAsync<string>("theme.getAccent") ?? "#7c3aed";
        await ApplyAsync();
    }

    public async Task SetTheme(string theme)
    {
        Theme = theme;
        await ApplyAsync();
    }

    public async Task SetAccent(string color)
    {
        Accent = color;
        await _js.InvokeVoidAsync("theme.setAccent", color);
    }

    private async Task ApplyAsync()
    {
        await _js.InvokeVoidAsync("theme.apply", Theme);
        await _js.InvokeVoidAsync("theme.setAccent", Accent);
    }
}

