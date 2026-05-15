using Moq;
using OptiGraphExtensions.Entities;
using OptiGraphExtensions.Features.Common.Exceptions;
using OptiGraphExtensions.Features.Synonyms.Models;
using OptiGraphExtensions.Features.Synonyms.Repositories;
using OptiGraphExtensions.Features.Synonyms.Services;
using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Tests.Features.Synonyms.Services;

[TestFixture]
public class SynonymBulkImportServiceTests
{
    private Mock<ISynonymCsvParserService> _mockParser = null!;
    private Mock<ISynonymRepository> _mockRepository = null!;
    private Mock<ISynonymGraphSyncService> _mockGraphSync = null!;
    private SynonymBulkImportService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockParser = new Mock<ISynonymCsvParserService>();
        _mockRepository = new Mock<ISynonymRepository>();
        _mockGraphSync = new Mock<ISynonymGraphSyncService>();
        _service = new SynonymBulkImportService(_mockParser.Object, _mockRepository.Object, _mockGraphSync.Object);

        _mockRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(Array.Empty<Synonym>());
    }

    private void SetupParse(params SynonymCsvParseRow[] rows)
    {
        _mockParser.Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(new SynonymCsvParseResult { Rows = rows });
    }

    private void SetupParseWithGlobalError(string error)
    {
        _mockParser.Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(new SynonymCsvParseResult { GlobalErrors = new[] { error } });
    }

    [Test]
    public async Task BuildPreviewAsync_AllValidRows_AllMarkedValid()
    {
        SetupParse(
            new SynonymCsvParseRow { LineNumber = 1, Synonym = "car", Language = "en", Slot = "ONE" },
            new SynonymCsvParseRow { LineNumber = 2, Synonym = "bike", Language = "en", Slot = "TWO" });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows.Count, Is.EqualTo(2));
        Assert.That(result.Rows.All(r => r.Status == BulkRowStatus.Valid), Is.True);
        Assert.That(result.ValidCount, Is.EqualTo(2));
    }

    [Test]
    public async Task BuildPreviewAsync_ParserGlobalErrors_PropagatedToResult()
    {
        SetupParseWithGlobalError("File too large.");

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.GlobalErrors, Contains.Item("File too large."));
    }

    [Test]
    public async Task BuildPreviewAsync_RowWithParseError_MarkedInvalid()
    {
        SetupParse(new SynonymCsvParseRow { LineNumber = 1, ParseError = "Expected 3 columns." });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.Invalid));
        Assert.That(result.Rows[0].Message, Does.Contain("3 columns"));
    }

    [Test]
    public async Task BuildPreviewAsync_MissingSynonym_MarkedInvalid()
    {
        SetupParse(new SynonymCsvParseRow { LineNumber = 1, Synonym = "", Language = "en", Slot = "ONE" });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.Invalid));
    }

    [Test]
    public async Task BuildPreviewAsync_SynonymOver255Chars_MarkedInvalid()
    {
        SetupParse(new SynonymCsvParseRow { LineNumber = 1, Synonym = new string('a', 256), Language = "en", Slot = "ONE" });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.Invalid));
    }

    [Test]
    public async Task BuildPreviewAsync_MissingLanguage_MarkedInvalid()
    {
        SetupParse(new SynonymCsvParseRow { LineNumber = 1, Synonym = "car", Language = "", Slot = "ONE" });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.Invalid));
    }

    [Test]
    public async Task BuildPreviewAsync_LanguageOver10Chars_MarkedInvalid()
    {
        SetupParse(new SynonymCsvParseRow { LineNumber = 1, Synonym = "car", Language = "very-long-lang", Slot = "ONE" });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.Invalid));
    }

    [TestCase("ONE", SynonymSlot.ONE)]
    [TestCase("one", SynonymSlot.ONE)]
    [TestCase("One", SynonymSlot.ONE)]
    [TestCase("1", SynonymSlot.ONE)]
    [TestCase("TWO", SynonymSlot.TWO)]
    [TestCase("two", SynonymSlot.TWO)]
    [TestCase("Two", SynonymSlot.TWO)]
    [TestCase("2", SynonymSlot.TWO)]
    public async Task BuildPreviewAsync_SlotValueAccepted_ParsedToCorrectSlot(string slotInput, SynonymSlot expected)
    {
        SetupParse(new SynonymCsvParseRow { LineNumber = 1, Synonym = "car", Language = "en", Slot = slotInput });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.Valid));
        Assert.That(result.Rows[0].Slot, Is.EqualTo(expected));
    }

    [Test]
    public async Task BuildPreviewAsync_SlotValueInvalid_MarkedInvalid()
    {
        SetupParse(new SynonymCsvParseRow { LineNumber = 1, Synonym = "car", Language = "en", Slot = "THREE" });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.Invalid));
        Assert.That(result.Rows[0].RawSlot, Is.EqualTo("THREE"));
    }

    [Test]
    public async Task BuildPreviewAsync_DuplicateInFile_SecondOccurrenceMarkedDuplicateInFile()
    {
        SetupParse(
            new SynonymCsvParseRow { LineNumber = 1, Synonym = "car", Language = "en", Slot = "ONE" },
            new SynonymCsvParseRow { LineNumber = 2, Synonym = "car", Language = "en", Slot = "ONE" });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.Valid));
        Assert.That(result.Rows[1].Status, Is.EqualTo(BulkRowStatus.DuplicateInFile));
    }

    [Test]
    public async Task BuildPreviewAsync_DuplicateInDatabase_MarkedDuplicateInDatabase()
    {
        _mockRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new[]
        {
            new Synonym { Id = Guid.NewGuid(), SynonymItem = "car", Language = "en", Slot = SynonymSlot.ONE }
        });
        SetupParse(new SynonymCsvParseRow { LineNumber = 1, Synonym = "car", Language = "en", Slot = "ONE" });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.DuplicateInDatabase));
    }

    [Test]
    public async Task BuildPreviewAsync_DuplicateInDatabase_CaseInsensitive()
    {
        _mockRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new[]
        {
            new Synonym { Id = Guid.NewGuid(), SynonymItem = "CAR", Language = "EN", Slot = SynonymSlot.ONE }
        });
        SetupParse(new SynonymCsvParseRow { LineNumber = 1, Synonym = "car", Language = "en", Slot = "ONE" });

        var result = await _service.BuildPreviewAsync("any");

        Assert.That(result.Rows[0].Status, Is.EqualTo(BulkRowStatus.DuplicateInDatabase));
    }

    [Test]
    public async Task ImportAsync_OnlyValidRowsInserted()
    {
        var preview = new BulkSynonymPreviewResult
        {
            Rows = new[]
            {
                new BulkSynonymPreviewRow { LineNumber = 1, Synonym = "car", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Valid },
                new BulkSynonymPreviewRow { LineNumber = 2, Synonym = "bad", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Invalid },
                new BulkSynonymPreviewRow { LineNumber = 3, Synonym = "dup", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.DuplicateInDatabase }
            }
        };
        List<Synonym>? captured = null;
        _mockRepository.Setup(x => x.CreateManyAsync(It.IsAny<IEnumerable<Synonym>>()))
            .Callback<IEnumerable<Synonym>>(s => captured = s.ToList())
            .ReturnsAsync((IEnumerable<Synonym> s) => s.ToList());

        var result = await _service.ImportAsync(preview, "user1");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Count, Is.EqualTo(1));
        Assert.That(captured[0].SynonymItem, Is.EqualTo("car"));
        Assert.That(result.InsertedCount, Is.EqualTo(1));
        Assert.That(result.InvalidSkippedCount, Is.EqualTo(1));
        Assert.That(result.SkippedDuplicateCount, Is.EqualTo(1));
    }

    [Test]
    public async Task ImportAsync_CallsCreateManyAsyncExactlyOnce()
    {
        var preview = new BulkSynonymPreviewResult
        {
            Rows = new[]
            {
                new BulkSynonymPreviewRow { Synonym = "a", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Valid },
                new BulkSynonymPreviewRow { Synonym = "b", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Valid }
            }
        };
        _mockRepository.Setup(x => x.CreateManyAsync(It.IsAny<IEnumerable<Synonym>>()))
            .ReturnsAsync((IEnumerable<Synonym> s) => s.ToList());

        await _service.ImportAsync(preview, "user1");

        _mockRepository.Verify(x => x.CreateManyAsync(It.IsAny<IEnumerable<Synonym>>()), Times.Once);
    }

    [Test]
    public async Task ImportAsync_CallsGraphSyncForEachAffectedLanguage()
    {
        var preview = new BulkSynonymPreviewResult
        {
            Rows = new[]
            {
                new BulkSynonymPreviewRow { Synonym = "a", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Valid },
                new BulkSynonymPreviewRow { Synonym = "b", Language = "fr", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Valid },
                new BulkSynonymPreviewRow { Synonym = "c", Language = "en", Slot = SynonymSlot.TWO, Status = BulkRowStatus.Valid }
            }
        };
        _mockRepository.Setup(x => x.CreateManyAsync(It.IsAny<IEnumerable<Synonym>>()))
            .ReturnsAsync((IEnumerable<Synonym> s) => s.ToList());

        var result = await _service.ImportAsync(preview, "user1");

        _mockGraphSync.Verify(x => x.SyncSynonymsForLanguageAsync("en"), Times.Once);
        _mockGraphSync.Verify(x => x.SyncSynonymsForLanguageAsync("fr"), Times.Once);
        Assert.That(result.AffectedLanguages, Is.EquivalentTo(new[] { "en", "fr" }));
        Assert.That(result.GraphSyncAttempted, Is.True);
        Assert.That(result.GraphSyncSucceeded, Is.True);
    }

    [Test]
    public async Task ImportAsync_GraphSyncThrows_ReportsFailureButPreservesInsertCount()
    {
        var preview = new BulkSynonymPreviewResult
        {
            Rows = new[]
            {
                new BulkSynonymPreviewRow { Synonym = "a", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Valid }
            }
        };
        _mockRepository.Setup(x => x.CreateManyAsync(It.IsAny<IEnumerable<Synonym>>()))
            .ReturnsAsync((IEnumerable<Synonym> s) => s.ToList());
        _mockGraphSync.Setup(x => x.SyncSynonymsForLanguageAsync("en"))
            .ThrowsAsync(new GraphSyncException("Graph endpoint unreachable"));

        var result = await _service.ImportAsync(preview, "user1");

        Assert.That(result.InsertedCount, Is.EqualTo(1));
        Assert.That(result.GraphSyncAttempted, Is.True);
        Assert.That(result.GraphSyncSucceeded, Is.False);
        Assert.That(result.GraphSyncErrorMessage, Does.Contain("Graph endpoint unreachable"));
    }

    [Test]
    public void ImportAsync_RepositoryThrows_DoesNotCallGraphSync()
    {
        var preview = new BulkSynonymPreviewResult
        {
            Rows = new[]
            {
                new BulkSynonymPreviewRow { Synonym = "a", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Valid }
            }
        };
        _mockRepository.Setup(x => x.CreateManyAsync(It.IsAny<IEnumerable<Synonym>>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.ImportAsync(preview, "user1"));

        _mockGraphSync.Verify(x => x.SyncSynonymsForLanguageAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ImportAsync_NoValidRows_DoesNotCallRepositoryOrGraphSync()
    {
        var preview = new BulkSynonymPreviewResult
        {
            Rows = new[]
            {
                new BulkSynonymPreviewRow { Status = BulkRowStatus.Invalid },
                new BulkSynonymPreviewRow { Status = BulkRowStatus.DuplicateInFile }
            }
        };

        var result = await _service.ImportAsync(preview, "user1");

        Assert.That(result.InsertedCount, Is.EqualTo(0));
        Assert.That(result.GraphSyncAttempted, Is.False);
        _mockRepository.Verify(x => x.CreateManyAsync(It.IsAny<IEnumerable<Synonym>>()), Times.Never);
        _mockGraphSync.Verify(x => x.SyncSynonymsForLanguageAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ImportAsync_SetsCreatedAtAndCreatedByOnAllEntities()
    {
        var preview = new BulkSynonymPreviewResult
        {
            Rows = new[]
            {
                new BulkSynonymPreviewRow { Synonym = "a", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Valid },
                new BulkSynonymPreviewRow { Synonym = "b", Language = "en", Slot = SynonymSlot.ONE, Status = BulkRowStatus.Valid }
            }
        };
        List<Synonym>? captured = null;
        _mockRepository.Setup(x => x.CreateManyAsync(It.IsAny<IEnumerable<Synonym>>()))
            .Callback<IEnumerable<Synonym>>(s => captured = s.ToList())
            .ReturnsAsync((IEnumerable<Synonym> s) => s.ToList());

        var before = DateTime.UtcNow;
        await _service.ImportAsync(preview, "user1");
        var after = DateTime.UtcNow;

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.All(s => s.CreatedBy == "user1"), Is.True);
        Assert.That(captured.All(s => s.CreatedAt >= before && s.CreatedAt <= after), Is.True);
        Assert.That(captured.All(s => s.Id != Guid.Empty), Is.True);
    }
}
