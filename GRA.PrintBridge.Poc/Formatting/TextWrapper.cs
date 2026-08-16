namespace GRA.PrintBridge.Poc.Formatting;

/// <summary>Wraps receipt text to a known native-font character width.</summary>
public static class TextWrapper
{
    public static IReadOnlyList<string> Wrap(string text, int charactersPerLine)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (charactersPerLine <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(charactersPerLine));
        }

        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            WrapParagraph(paragraph.Trim(), charactersPerLine, lines);
        }

        return lines;
    }

    private static void WrapParagraph(string paragraph, int width, ICollection<string> lines)
    {
        if (paragraph.Length == 0)
        {
            lines.Add(string.Empty);
            return;
        }

        var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var current = string.Empty;
        foreach (var word in words)
        {
            if (word.Length > width)
            {
                if (current.Length > 0)
                {
                    lines.Add(current);
                    current = string.Empty;
                }

                for (var index = 0; index < word.Length; index += width)
                {
                    var chunk = word.Substring(index, Math.Min(width, word.Length - index));
                    if (chunk.Length == width || index + width < word.Length)
                    {
                        lines.Add(chunk);
                    }
                    else
                    {
                        current = chunk;
                    }
                }

                continue;
            }

            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (candidate.Length <= width)
            {
                current = candidate;
                continue;
            }

            lines.Add(current);
            current = word;
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }
    }
}
