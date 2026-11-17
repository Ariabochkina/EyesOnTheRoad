using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

public class DialogueScriptParser
{
    public class ParsedEpisode
    {
        public string name;
        public List<DialogueLine> lines = new List<DialogueLine>();
    }

    static readonly Regex AnswerPattern = new Regex(@"^([0-9]+)\)\s*(.*)$");
    static readonly Regex TrailingShiftPattern = new Regex(@"\[([^\]]*)\]\s*$");
    static readonly Regex ShiftPattern = new Regex(@"^\s*([+-]?[0-9]+(?:[.,][0-9]+)?)\s*/\s*([+-]?[0-9]+(?:[.,][0-9]+)?)\s*$");
    static readonly Regex SilencePattern = new Regex(@"^молчание\s*(.*)$", RegexOptions.IgnoreCase);
    static readonly Regex TimerPattern = new Regex(@"^таймер\s+([0-9]+(?:[.,][0-9]+)?)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex VariantPattern = new Regex(@"^(близость|привязанность|безразличие|враждебность)\s*:\s*(.*)$", RegexOptions.IgnoreCase);
    static readonly Regex FlagPattern = new Regex(@"^флаг\s+(.+)$", RegexOptions.IgnoreCase);

    public readonly List<ParsedEpisode> Episodes = new List<ParsedEpisode>();
    public readonly List<string> Errors = new List<string>();

    ParsedEpisode episode;
    DialogueLine line;
    List<string> flagTarget;
    bool continuesText;
    int rowNumber;

    public void Parse(string source)
    {
        string[] rows = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (int i = 0; i < rows.Length; i++)
        {
            rowNumber = i + 1;
            ParseRow(rows[i]);
        }

        foreach (ParsedEpisode parsed in Episodes)
        {
            if (parsed.lines.Count == 0)
                Errors.Add($"эпизод «{parsed.name}» пустой");
        }
    }

    void ParseRow(string raw)
    {
        string row = raw.Trim();

        if (row.Length == 0)
        {
            continuesText = false;
            return;
        }

        if (row.StartsWith("//"))
            return;

        if (row.StartsWith("#"))
        {
            StartEpisode(row.Substring(1).Trim());
            return;
        }

        if (episode == null)
        {
            Error("текст до первого эпизода — начни с «# Имя»");
            return;
        }

        bool indented = char.IsWhiteSpace(raw[0]);
        if (!indented)
        {
            AddSpeakerText(row);
            return;
        }

        continuesText = false;

        if (line == null)
        {
            Error("строка с отступом до первой реплики");
            return;
        }

        ParseDetail(row);
    }

    void StartEpisode(string name)
    {
        line = null;
        flagTarget = null;
        continuesText = false;

        if (name.Length == 0)
        {
            Error("у эпизода нет имени");
            episode = null;
            return;
        }

        if (Episodes.Exists(e => e.name == name))
            Error($"эпизод «{name}» уже есть в этом файле");

        episode = new ParsedEpisode { name = name };
        Episodes.Add(episode);
    }

    void AddSpeakerText(string row)
    {
        if (continuesText && line != null)
        {
            line.text += "\n" + row;
            return;
        }

        line = new DialogueLine { text = row };
        episode.lines.Add(line);
        flagTarget = null;
        continuesText = true;
    }

    void ParseDetail(string row)
    {
        Match match = VariantPattern.Match(row);
        if (match.Success)
        {
            SetVariant(match.Groups[1].Value.ToLowerInvariant(), match.Groups[2].Value.Trim());
            return;
        }

        match = TimerPattern.Match(row);
        if (match.Success)
        {
            line.seconds = ParseNumber(match.Groups[1].Value);
            return;
        }

        match = SilencePattern.Match(row);
        if (match.Success)
        {
            if (TryParseShift(match.Groups[1].Value.Trim(), out float affinity, out float tension))
            {
                line.silenceAffinity = affinity;
                line.silenceTension = tension;
            }
            flagTarget = line.silenceFlags;
            return;
        }

        match = AnswerPattern.Match(row);
        if (match.Success)
        {
            AddAnswer(int.Parse(match.Groups[1].Value), match.Groups[2].Value.Trim());
            return;
        }

        match = FlagPattern.Match(row);
        if (match.Success)
        {
            AddFlags(match.Groups[1].Value);
            return;
        }

        Error($"не понимаю строку «{row}»");
    }

    void SetVariant(string key, string text)
    {
        if (text.Length == 0)
        {
            Error($"пустой вариант «{key}»");
            return;
        }

        switch (key)
        {
            case "близость": line.quietCloseness = text; break;
            case "привязанность": line.clingy = text; break;
            case "безразличие": line.indifferent = text; break;
            case "враждебность": line.hostile = text; break;
        }
    }

    void AddAnswer(int number, string body)
    {
        int expected = line.answers.Count + 1;
        if (number != expected)
        {
            Error($"ожидался ответ {expected}), а не {number})");
            return;
        }

        if (number > DialogueLine.MaxAnswers)
        {
            Error($"ответов не больше {DialogueLine.MaxAnswers}");
            return;
        }

        var answer = new DialogueAnswer();
        Match shift = TrailingShiftPattern.Match(body);
        if (shift.Success)
        {
            body = body.Substring(0, shift.Index).Trim();
            TryParseShift(shift.Value, out answer.affinity, out answer.tension);
        }

        if (body.Length == 0)
        {
            Error($"у ответа {number}) нет текста");
            return;
        }

        if (body.Contains("[") || body.Contains("]"))
            Error($"в ответе {number}) лишняя скобка — сдвиг пишется в конце: [+3 / -2]");

        answer.text = body;
        line.answers.Add(answer);
        flagTarget = answer.flags;
    }

    void AddFlags(string list)
    {
        if (flagTarget == null)
        {
            Error("флаг пишется под ответом или под «молчание»");
            return;
        }

        foreach (string part in list.Split(','))
        {
            string flag = part.Trim();
            if (flag.Length > 0 && !flagTarget.Contains(flag))
                flagTarget.Add(flag);
        }
    }

    bool TryParseShift(string bracket, out float affinity, out float tension)
    {
        affinity = 0f;
        tension = 0f;

        if (bracket.Length == 0)
            return true;

        if (!bracket.StartsWith("[") || !bracket.EndsWith("]"))
        {
            Error($"сдвиг «{bracket}» должен быть в квадратных скобках: [+3 / -2]");
            return false;
        }

        Match match = ShiftPattern.Match(bracket.Substring(1, bracket.Length - 2));
        if (!match.Success)
        {
            Error($"не понимаю сдвиг «{bracket}» — нужно [расположение / накал], например [+3 / -2]");
            return false;
        }

        affinity = ParseNumber(match.Groups[1].Value);
        tension = ParseNumber(match.Groups[2].Value);
        return true;
    }

    static float ParseNumber(string value)
    {
        return float.Parse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    void Error(string message)
    {
        Errors.Add($"строка {rowNumber}: {message}");
    }
}
