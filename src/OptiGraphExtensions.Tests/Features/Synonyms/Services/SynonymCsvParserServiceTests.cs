using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Services;

namespace OptiGraphExtensions.Tests.Features.Synonyms.Services;

[TestFixture]
public class SynonymCsvParserServiceTests
{
    private SynonymCsvParserService _parser = null!;

    [SetUp]
    public void Setup()
    {
        _parser = new SynonymCsvParserService();
    }

    [Test]
    public void Parse_EmptyContent_ReturnsGlobalError()
    {
        var result = _parser.Parse(string.Empty);

        Assert.That(result.Rows, Is.Empty);
        Assert.That(result.GlobalErrors, Is.Not.Empty);
    }

    [Test]
    public void Parse_WhitespaceOnly_ReturnsGlobalError()
    {
        var result = _parser.Parse("   \r\n  \n");

        Assert.That(result.Rows, Is.Empty);
        Assert.That(result.GlobalErrors, Is.Not.Empty);
    }

    [Test]
    public void Parse_WithUtf8Bom_StripsBomAndParses()
    {
        var content = "﻿car,en,ONE";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(1));
        Assert.That(result.Rows[0].Synonym, Is.EqualTo("car"));
        Assert.That(result.Rows[0].Language, Is.EqualTo("en"));
        Assert.That(result.Rows[0].Slot, Is.EqualTo("ONE"));
    }

    [Test]
    public void Parse_WithCanonicalHeader_DetectsAndSkipsHeader()
    {
        var content = "synonym,language,slot\r\ncar,en,ONE";

        var result = _parser.Parse(content);

        Assert.That(result.HeaderDetected, Is.True);
        Assert.That(result.Rows.Count, Is.EqualTo(1));
        Assert.That(result.Rows[0].Synonym, Is.EqualTo("car"));
    }

    [Test]
    public void Parse_WithShuffledHeader_DetectsAndMapsColumnsCorrectly()
    {
        var content = "slot,synonym,language\r\nONE,car,en";

        var result = _parser.Parse(content);

        Assert.That(result.HeaderDetected, Is.True);
        Assert.That(result.Rows.Count, Is.EqualTo(1));
        Assert.That(result.Rows[0].Synonym, Is.EqualTo("car"));
        Assert.That(result.Rows[0].Language, Is.EqualTo("en"));
        Assert.That(result.Rows[0].Slot, Is.EqualTo("ONE"));
    }

    [Test]
    public void Parse_WithoutHeader_ReturnsAllRowsAsData()
    {
        var content = "car,en,ONE\r\nbike,en,TWO";

        var result = _parser.Parse(content);

        Assert.That(result.HeaderDetected, Is.False);
        Assert.That(result.Rows.Count, Is.EqualTo(2));
    }

    [Test]
    public void Parse_QuotedFieldWithComma_PreservesInternalCommas()
    {
        var content = "\"car, automobile, vehicle\",en,ONE";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(1));
        Assert.That(result.Rows[0].Synonym, Is.EqualTo("car, automobile, vehicle"));
        Assert.That(result.Rows[0].Language, Is.EqualTo("en"));
    }

    [Test]
    public void Parse_QuotedFieldWithEscapedQuote_PreservesQuoteCharacter()
    {
        var content = "\"shoe \"\"sneaker\"\"\",en,ONE";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(1));
        Assert.That(result.Rows[0].Synonym, Is.EqualTo("shoe \"sneaker\""));
    }

    [Test]
    public void Parse_LfLineEndings_Works()
    {
        var content = "car,en,ONE\nbike,en,TWO";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(2));
    }

    [Test]
    public void Parse_CrlfLineEndings_Works()
    {
        var content = "car,en,ONE\r\nbike,en,TWO";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(2));
    }

    [Test]
    public void Parse_RowWithWrongColumnCount_FlaggedWithParseError()
    {
        var content = "car,en";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(1));
        Assert.That(result.Rows[0].ParseError, Is.Not.Null);
        Assert.That(result.Rows[0].ParseError, Does.Contain("3"));
    }

    [Test]
    public void Parse_RowsHaveLineNumbers()
    {
        var content = "car,en,ONE\r\nbike,en,TWO\r\ntrain,en,ONE";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(3));
        Assert.That(result.Rows[0].LineNumber, Is.EqualTo(1));
        Assert.That(result.Rows[1].LineNumber, Is.EqualTo(2));
        Assert.That(result.Rows[2].LineNumber, Is.EqualTo(3));
    }

    [Test]
    public void Parse_RowsHaveLineNumbersAccountingForHeader()
    {
        var content = "synonym,language,slot\r\ncar,en,ONE\r\nbike,en,TWO";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(2));
        Assert.That(result.Rows[0].LineNumber, Is.EqualTo(2));
        Assert.That(result.Rows[1].LineNumber, Is.EqualTo(3));
    }

    [Test]
    public void Parse_TrimsWhitespaceAroundFields()
    {
        var content = "  car  ,  en  ,  ONE  ";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(1));
        Assert.That(result.Rows[0].Synonym, Is.EqualTo("car"));
        Assert.That(result.Rows[0].Language, Is.EqualTo("en"));
        Assert.That(result.Rows[0].Slot, Is.EqualTo("ONE"));
    }

    [Test]
    public void Parse_TooManyRows_ReturnsGlobalErrorAndStops()
    {
        var lines = new List<string>();
        for (int i = 0; i < 10_001; i++)
        {
            lines.Add($"syn{i},en,ONE");
        }
        var content = string.Join("\r\n", lines);

        var result = _parser.Parse(content);

        Assert.That(result.GlobalErrors, Is.Not.Empty);
    }

    [Test]
    public void Parse_FileTooLarge_ReturnsGlobalError()
    {
        var content = new string('a', 5 * 1024 * 1024 + 1);

        var result = _parser.Parse(content);

        Assert.That(result.GlobalErrors, Is.Not.Empty);
    }

    [Test]
    public void Parse_BlankLinesIgnored()
    {
        var content = "car,en,ONE\r\n\r\nbike,en,TWO";

        var result = _parser.Parse(content);

        Assert.That(result.Rows.Count, Is.EqualTo(2));
    }
}
