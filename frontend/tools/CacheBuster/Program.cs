using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CacheBuster;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("=== Blazor WASM Cache Buster ===");

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: CacheBuster <wwwroot_path> [--verify-only]");
            return 1;
        }

        bool verifyOnly = args.Any(a => a.Equals("--verify-only", StringComparison.OrdinalIgnoreCase));
        string targetDir = args.First(a => !a.StartsWith("--", StringComparison.Ordinal));

        // If path points to publish root containing wwwroot, adjust to wwwroot
        if (Directory.Exists(Path.Combine(targetDir, "wwwroot")))
        {
            targetDir = Path.Combine(targetDir, "wwwroot");
        }

        string indexPath = Path.Combine(targetDir, "index.html");
        if (!File.Exists(indexPath))
        {
            Console.Error.WriteLine($"[ERROR] index.html not found at: {indexPath}");
            return 1;
        }

        Console.WriteLine($"Target directory: {targetDir}");

        if (verifyOnly)
        {
            return VerifyDeployment(targetDir, indexPath);
        }

        return ProcessAndCacheBust(targetDir, indexPath);
    }

    static int ProcessAndCacheBust(string targetDir, string indexPath)
    {
        try
        {
            string html = File.ReadAllText(indexPath, Encoding.UTF8);

            // Regex matches local CSS/JS references in link and script tags, ignoring external URLs and _framework bootstrap
            var regex = new Regex(
                @"(?<prefix>(?:<link\b[^>]*?\bhref=|<script\b[^>]*?\bsrc=)[""'])(?<path>(?!https?:\/\/|\/\/|_framework\/)(?<relpath>(?:[a-zA-Z0-9_\-\.\/]+?\.(?:css|js))))(?:\?[^""']*)?(?<suffix>[""'])",
                RegexOptions.IgnoreCase);

            var matches = regex.Matches(html);
            if (matches.Count == 0)
            {
                Console.WriteLine("[INFO] No local static asset references found in index.html to cache bust.");
                return 0;
            }

            var processedAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var updatedHtml = html;

            using var sha256 = SHA256.Create();

            foreach (Match match in matches)
            {
                string relPath = match.Groups["relpath"].Value.TrimStart('/');
                string dir = Path.GetDirectoryName(relPath)?.Replace('\\', '/') ?? "";
                if (!string.IsNullOrEmpty(dir)) dir += "/";
                
                string ext = Path.GetExtension(relPath);
                string rawFileNameWithoutExt = Path.GetFileNameWithoutExtension(relPath);
                // Strip any existing 8-char hex hash(es) to ensure idempotency
                string baseFileName = Regex.Replace(rawFileNameWithoutExt, @"(\.[0-9a-fA-F]{8})+$", "");

                string cleanRelPath = $"{dir}{baseFileName}{ext}";
                string cleanFullPath = Path.Combine(targetDir, cleanRelPath.Replace('/', Path.DirectorySeparatorChar));
                string originalFullPath = Path.Combine(targetDir, relPath.Replace('/', Path.DirectorySeparatorChar));

                string sourcePathToRead = File.Exists(cleanFullPath) ? cleanFullPath : (File.Exists(originalFullPath) ? originalFullPath : null!);

                if (sourcePathToRead == null)
                {
                    Console.WriteLine($"[WARN] Referenced file does not exist on disk: {relPath}");
                    continue;
                }

                // Compute SHA256 content hash (first 8 hex characters)
                byte[] fileBytes = File.ReadAllBytes(sourcePathToRead);
                byte[] hashBytes = sha256.ComputeHash(fileBytes);
                string hash = BitConverter.ToString(hashBytes, 0, 4).Replace("-", "").ToLowerInvariant();

                string newRelPath = $"{dir}{baseFileName}.{hash}{ext}";
                string newFullPath = Path.Combine(targetDir, newRelPath.Replace('/', Path.DirectorySeparatorChar));

                // Write hashed asset copy
                File.WriteAllBytes(newFullPath, fileBytes);

                // Generate pre-compressed files (.gz and .br)
                GenerateCompressedVariants(newFullPath, fileBytes);

                // Replace in HTML
                string originalMatch = match.Value;
                string replacement = $"{match.Groups["prefix"].Value}{newRelPath}{match.Groups["suffix"].Value}";
                updatedHtml = updatedHtml.Replace(originalMatch, replacement);

                if (processedAssets.Add(cleanRelPath))
                {
                    Console.WriteLine($"[CACHE-BUST] {cleanRelPath} -> {newRelPath} (hash: {hash})");
                }
            }

            // Write modified index.html
            File.WriteAllText(indexPath, updatedHtml, Encoding.UTF8);

            // Re-generate compression for index.html
            byte[] indexBytes = Encoding.UTF8.GetBytes(updatedHtml);
            GenerateCompressedVariants(indexPath, indexBytes);

            Console.WriteLine("[SUCCESS] index.html successfully updated with hashed assets.");

            // Self-verify after cache busting
            return VerifyDeployment(targetDir, indexPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FATAL] Error during cache busting: {ex}");
            return 1;
        }
    }

    static void GenerateCompressedVariants(string filePath, byte[] contentBytes)
    {
        try
        {
            // GZip
            using (var fs = File.Create(filePath + ".gz"))
            using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
            {
                gz.Write(contentBytes, 0, contentBytes.Length);
            }

            // Brotli
            using (var fs = File.Create(filePath + ".br"))
            using (var br = new BrotliStream(fs, CompressionLevel.Optimal))
            {
                br.Write(contentBytes, 0, contentBytes.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Compression error for {filePath}: {ex.Message}");
        }
    }

    static int VerifyDeployment(string targetDir, string indexPath)
    {
        Console.WriteLine("\n--- Verifying Deployment Assets ---");
        string html = File.ReadAllText(indexPath, Encoding.UTF8);

        var regex = new Regex(
            @"(?:<link\b[^>]*?\bhref=|<script\b[^>]*?\bsrc=)[""'](?<path>(?!https?:\/\/|\/\/|_framework\/)[^""'\?#]+)(?:\?[^""']*)?[""']",
            RegexOptions.IgnoreCase);

        var matches = regex.Matches(html);
        int errors = 0;
        int checkedCount = 0;

        foreach (Match match in matches)
        {
            string relPath = match.Groups["path"].Value.TrimStart('/');
            if (string.IsNullOrWhiteSpace(relPath) || relPath == "." || relPath == "/")
                continue;

            string fullPath = Path.Combine(targetDir, relPath.Replace('/', Path.DirectorySeparatorChar));

            checkedCount++;
            if (!File.Exists(fullPath))
            {
                Console.Error.WriteLine($"[FAIL] Missing asset: {relPath} (expected at {fullPath})");
                errors++;
            }
            else
            {
                bool hasGz = File.Exists(fullPath + ".gz");
                bool hasBr = File.Exists(fullPath + ".br");
                Console.WriteLine($"[PASS] {relPath} (size: {new FileInfo(fullPath).Length} bytes, gz: {hasGz}, br: {hasBr})");
            }
        }

        if (errors > 0)
        {
            Console.Error.WriteLine($"\n[RESULT] Verification FAILED with {errors} error(s).");
            return 1;
        }

        Console.WriteLine($"\n[RESULT] Verification PASSED! Checked {checkedCount} local asset(s) referenced in index.html.");
        return 0;
    }
}
