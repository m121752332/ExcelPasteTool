using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Reflection;

namespace ExcelPasteTool;

public partial class AboutView : UserControl
{
    private TextBlock? _appTitleText;
    private TextBlock? _versionText;
    private TextBlock? _copyright;
    private TextBlock? _description;

    public AboutView()
    {
        AvaloniaXamlLoader.Load(this);
        _appTitleText = this.FindControl<TextBlock>("AppTitleText");
        _versionText = this.FindControl<TextBlock>("VersionText");
        _copyright = this.FindControl<TextBlock>("CopyrightText");
        _description = this.FindControl<TextBlock>("DescriptionText");

        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var name = asm.GetName();
        _appTitleText!.Text = name.Name ?? "DataSplitter Pro";
        _versionText!.Text = $"v{(name.Version?.ToString(3) ?? "1.0.0")}";

        var company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "GameStudioPro";
        var copyright = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
        _copyright!.Text = copyright ?? $"? 2025 {company}";

        var desc = asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "Excel Data Splitter Tool";
        _description!.Text = desc;
    }
}
