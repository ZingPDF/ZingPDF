namespace ZingPDF.Fonts.AFM;

/// <summary>
/// Class to parse Adobe Font Metrics (AFM) files
/// </summary>
public class AFMParser
{
    /// <summary>
    /// Parse an AFM file and return a FontMetrics object
    /// </summary>
    public static FontMetrics Parse(Stream afmStream)
    {
        ArgumentNullException.ThrowIfNull(afmStream);

        var metrics = new FontMetrics();
        var kerningPairs = new Dictionary<(char, char), int>();

        using (var reader = new StreamReader(afmStream))
        {
            string? line;
            bool inCharMetrics = false;
            bool inKernPairs = false;

            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                // Parse font global metrics
                if (line.StartsWith("FontName"))
                    metrics.Name = GetStringValue(line);
                else if (line.StartsWith("Ascender"))
                    metrics.Ascent = GetIntValue(line);
                else if (line.StartsWith("Descender"))
                    metrics.Descent = GetIntValue(line);
                else if (line.StartsWith("StdHW"))
                    metrics.StandardHorizontalWidth = GetIntValue(line);
                else if (line.StartsWith("StdVW"))
                    metrics.StandardVerticalWidth = GetIntValue(line);
                else if (line.StartsWith("CapHeight"))
                    metrics.CapHeight = GetIntValue(line);
                else if (line.StartsWith("XHeight"))
                    metrics.XHeight = GetIntValue(line);
                else if (line.StartsWith("ItalicAngle"))
                    metrics.ItalicAngle = GetFloatValue(line);
                else if (line.StartsWith("IsFixedPitch"))
                    metrics.IsFixedPitch = GetStringValue(line).ToLower() == "true";
                else if (line.StartsWith("UnderlinePosition"))
                    metrics.UnderlinePosition = GetIntValue(line);
                else if (line.StartsWith("UnderlineThickness"))
                    metrics.UnderlineThickness = GetIntValue(line);
                
                // Track sections
                else if (line.StartsWith("StartCharMetrics"))
                    inCharMetrics = true;
                else if (line == "EndCharMetrics")
                    inCharMetrics = false;
                else if (line.StartsWith("StartKernPairs"))
                    inKernPairs = true;
                else if (line == "EndKernPairs")
                    inKernPairs = false;
                
                // Parse character metrics
                else if (inCharMetrics && line.StartsWith("C "))
                {
                    // Format: C <charcode> ; WX <width> ; N <name> ; B <bbox> ;
                    ParseCharMetric(line, metrics);
                }
                
                // Parse kerning pairs
                else if (inKernPairs && line.StartsWith("KPX"))
                {
                    // Format: KPX <first> <second> <kerning>
                    ParseKerningPair(line, kerningPairs);
                }
            }
        }

        //// Set default width if not specified
        //if (metrics.DefaultWidth == 0)
        //{
        //    // Use space character width, or average if space not defined
        //    if (metrics.Widths.TryGetValue(' ', out int spaceWidth))
        //    {
        //        metrics.DefaultWidth = spaceWidth;
        //    }
        //    else if (metrics.Widths.Count > 0)
        //    {
        //        metrics.DefaultWidth = (int)metrics.Widths.Values.Average();
        //    }
        //    else
        //    {
        //        metrics.DefaultWidth = 600; // Fallback value
        //    }
        //}

        //// Set average width
        //if (metrics.AvgWidth == 0 && metrics.Widths.Count > 0)
        //{
        //    metrics.AvgWidth = (int)metrics.Widths.Values.Average();
        //}

        //// Set max width
        //if (metrics.MaxWidth == 0 && metrics.Widths.Count > 0)
        //{
        //    metrics.MaxWidth = metrics.Widths.Values.Max();
        //}

        // Add kerning pairs
        if (kerningPairs.Count > 0)
        {
            metrics.KerningPairs = kerningPairs;
        }

        return metrics;
    }

    /// <summary>
    /// Parse an AFM file from a file path and return a FontMetrics object
    /// </summary>
    public static FontMetrics ParseFromFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);

        return Parse(stream);
    }

    /// <summary>
    /// Parse a single character metric line
    /// </summary>
    private static void ParseCharMetric(string line, FontMetrics metrics)
    {
        // Extract character code
        var codeMatch = RegularExpressions.CharacterCode().Match(line);
        if (!codeMatch.Success)
        {
            return;
        }

        int charCode = int.Parse(codeMatch.Groups[1].Value);
        if (charCode < 0)
        {
            return; // Skip undefined characters
        }

        // Extract width
        var widthMatch = RegularExpressions.CharacterWidth().Match(line);
        if (!widthMatch.Success)
        {
            return;
        }
        
        int width = int.Parse(widthMatch.Groups[1].Value);
        
        // Add to widths dictionary
        char c = (char)charCode;
        metrics.Widths[c] = width;
    }

    /// <summary>
    /// Parse a kerning pair line
    /// </summary>
    private static void ParseKerningPair(string line, Dictionary<(char, char), int> kerningPairs)
    {
        var parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 4 && parts[0] == "KPX")
        {
            string first = parts[1];
            string second = parts[2];
            int kerning = int.Parse(parts[3]);
            
            // Only handle simple cases: single characters
            if (first.Length == 1 && second.Length == 1)
            {
                kerningPairs[(first[0], second[0])] = kerning;
            }
        }
    }

    /// <summary>
    /// Get a string value from a line like "Key Value"
    /// </summary>
    private static string GetStringValue(string line)
    {
        int spaceIndex = line.IndexOf(' ');
        if (spaceIndex > 0 && spaceIndex < line.Length - 1)
        {
            return line[(spaceIndex + 1)..].Trim();
        }

        return string.Empty;
    }

    /// <summary>
    /// Get an integer value from a line like "Key Value"
    /// </summary>
    private static int GetIntValue(string line)
    {
        string value = GetStringValue(line);
        if (int.TryParse(value, out int result))
        {
            return result;
        }

        return 0;
    }

    /// <summary>
    /// Get a float value from a line like "Key Value"
    /// </summary>
    private static float GetFloatValue(string line)
    {
        string value = GetStringValue(line);
        if (float.TryParse(value, out float result))
        {
            return result;
        }

        return 0;
    }
}
