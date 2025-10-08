using System;
using DirtBikePark.Cli.Presentation;

namespace DirtBikePark.Cli.Presentation.CliPages;

/// <summary>
/// Provides helper methods shared across CLI pages.
/// </summary>
public abstract class CliPageBase
{
    protected MenuRenderer Menu { get; }

    protected CliPageBase(MenuRenderer menu)
    {
        Menu = menu;
    }

    protected static bool TryParseGuid(string value, out Guid id)
        => Guid.TryParse(value, out id);
}
