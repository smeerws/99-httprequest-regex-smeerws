using UnityEngine;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

public class TeacherScraper : MonoBehaviour
{
    private const string OverviewUrl = "https://www.htl-salzburg.ac.at/lehrerinnen.html";
    private const string BaseUrl = "https://www.htl-salzburg.ac.at";

    private HttpClient client;

    private void Awake()
    {
        client = new HttpClient();
    }

    private async void Start()
    {
        // Einmalige Demo beim Start
        await FetchTeachersFromOverviewAsync(maxTeachers: 5);
    }

    private async Task FetchTeachersFromOverviewAsync(int maxTeachers)
    {
        string html;
        try
        {
            html = await client.GetStringAsync(OverviewUrl);
            Debug.Log("Übersichtsseite geladen.");
        }
        catch (HttpRequestException e)
        {
            Debug.LogError("Fehler beim Laden der Übersichtsseite: " + e.Message);
            return;
        }

        File.WriteAllText(Application.dataPath + "/teacher_overview_dump.html", html);
        Debug.Log("Dump geschrieben nach: " + Application.dataPath + "/teacher_overview_dump.html");

        // 1) Detail-Links aus der Übersichtsseite holen
        var teacherLinks = ParseTeacherLinks(html)
            .Take(maxTeachers)
            .ToList();

        Debug.Log($"Gefundene Detail-Links: {teacherLinks.Count}");

        // 2) Für jede/n Lehrer:in die Detailseite laden und Infos ausgeben
        foreach (var t in teacherLinks)
        {
            await FetchTeacherDetailAsync(t);
        }
    }

    // Repräsentiert einen Eintrag aus der Übersicht
    private class TeacherLink
    {
        public string Name;
        public string RelativeUrl;
    }

    /// <summary>
    /// Sucht in der Übersichtsseite alle Links auf /lehrerinnen-details/…html
    /// und liest Name + URL aus.
    /// </summary>
    private IEnumerable<TeacherLink> ParseTeacherLinks(string html)
    {
        // Sehr vereinfachte Regex:
        // href="/lehrerinnen-details/IRGENDWAS.html">ANZEIGENAME</a>
        var regex = new Regex(
            "href=\"(/lehrerinnen-details/[^\"]+\\.html)\"[^>]*>([^<]+)</a>",
            RegexOptions.IgnoreCase);

        var matches = regex.Matches(html);

        foreach (Match m in matches)
        {
            if (m.Groups.Count < 3) continue;

            string url = m.Groups[1].Value;
            string name = m.Groups[2].Value.Trim();

            yield return new TeacherLink
            {
                Name = name,
                RelativeUrl = url
            };
        }
    }

    private async Task FetchTeacherDetailAsync(TeacherLink teacher)
    {
        string fullUrl = BaseUrl + teacher.RelativeUrl;
        string html;

        try
        {
            html = await client.GetStringAsync(fullUrl);
        }
        catch (HttpRequestException e)
        {
            Debug.LogError($"Fehler bei {teacher.Name}: {e.Message}");
            return;
        }

        string room = ExtractRoom(html);
        string officeHour = ExtractOfficeHour(html);
        string email = ExtractEmail(html);

        Debug.Log(
            $"Lehrer: {teacher.Name}\n" +
            $"  URL: {fullUrl}\n" +
            $"  Raum: {room}\n" +
            $"  Sprechstunde: {officeHour}\n" +
            $"  E-Mail: {email}"
        );
    }

    // --- Regex-Helfer für die Detailseite ---

    private string ExtractRoom(string html)
    {
        // Beispiel: "G 009"
        var match = Regex.Match(html, @"\b[A-Z]\s?\d{3}\b");
        return match.Success ? match.Value : "Raum nicht gefunden";
    }

    private string ExtractOfficeHour(string html)
    {
        // Beispiel: "Dienstag 12:30 - 13:20 Uhr"
        var match = Regex.Match(
            html,
            @"[A-ZÄÖÜ][a-zäöü]+ \d{1,2}:\d{2}\s*-\s*\d{1,2}:\d{2} Uhr");

        return match.Success ? match.Value : "Sprechstunde nicht gefunden";
    }

    private string ExtractEmail(string html)
    {
        // Basic E-Mail Pattern
        var match = Regex.Match(
            html,
            @"[a-zA-Z0-9\.\-_]+@[a-zA-Z0-9\.\-]+\.[a-zA-Z]{2,}");

        return match.Success ? match.Value : "E-Mail nicht gefunden";
    }
}
