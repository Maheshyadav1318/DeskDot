using DeskDot.Core;
using DeskDot.Services;
using Xunit;

namespace DeskDot.Tests;

public class AgendaServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "deskdot-tests-" + Guid.NewGuid().ToString("N"));

    public AgendaServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
            // temp cleanup is best effort
        }
    }

    [Fact]
    public void Falls_back_to_demo_agenda_when_file_is_missing()
    {
        var service = new AgendaService(Path.Combine(_dir, "agenda.txt"));

        var next = service.GetNext(new TimeOnly(8, 0));

        Assert.Equal(AgendaPlanner.DemoAgenda[0].Title, next.Title);
    }

    [Fact]
    public void Reads_the_user_agenda_file()
    {
        var path = Path.Combine(_dir, "agenda.txt");
        File.WriteAllLines(path, new[] { "# my day", "10:00 Write the tests", "15:00 Ship it" });

        var service = new AgendaService(path);

        Assert.Equal("Write the tests", service.GetNext(new TimeOnly(9, 0)).Title);
        Assert.Equal("Ship it", service.GetNext(new TimeOnly(12, 0)).Title);
        Assert.Equal("Write the tests", service.GetNext(new TimeOnly(23, 0)).Title);
    }

    [Fact]
    public void Falls_back_when_the_file_has_no_valid_entries()
    {
        var path = Path.Combine(_dir, "agenda.txt");
        File.WriteAllLines(path, new[] { "not a time", "", "# comment" });

        var service = new AgendaService(path);

        Assert.Equal(AgendaPlanner.DemoAgenda[0].Title, service.GetNext(new TimeOnly(8, 0)).Title);
    }

    [Fact]
    public void Constructor_rejects_null_path()
    {
        Assert.Throws<ArgumentNullException>(() => new AgendaService((string)null!));
    }
}
