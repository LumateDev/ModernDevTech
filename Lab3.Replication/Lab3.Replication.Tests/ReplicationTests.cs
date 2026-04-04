using Lab3.Replication.Tests.Models;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lab3.Replication.Tests;

[Collection("Sequential")]
public class ReplicationTests
{
    private readonly ITestOutputHelper _output;

    public ReplicationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Scenario1_WriteOnMaster_ReadFromReplicas()
    {
        await TestHelper.ResetAll();

        _output.WriteLine("═══ СЦЕНАРИЙ 1: Запись на мастер → чтение с реплик ═══");

        // 1. Записываем на мастер
        _output.WriteLine("  Шаг 1: Записываем key='greeting', value='hello world' на мастер");
        var writeResponse = await TestHelper.PutOnMaster("greeting", "hello world");
        Assert.Equal(HttpStatusCode.OK, writeResponse.StatusCode);
        var writeResult = await writeResponse.Content.ReadFromJsonAsync<WriteResponse>();
        _output.WriteLine($"    ✓ Записано, version={writeResult!.Entry!.Version}");
        _output.WriteLine($"    Реплик получили: {writeResult.Replication!.SuccessCount}/{writeResult.Replication.TotalReplicas}");

        // Небольшая пауза для стабилизации
        await Task.Delay(300);

        // 2. Читаем с реплики B
        _output.WriteLine("  Шаг 2: Читаем с реплики B");
        var readB = await TestHelper.GetFrom(TestHelper.ReplicaBUrl, "greeting");
        Assert.Equal(HttpStatusCode.OK, readB.StatusCode);
        var dataB = await readB.Content.ReadFromJsonAsync<DataResponse>();
        _output.WriteLine($"    Узел: {dataB!.NodeId}, Значение: {dataB.Entry!.Value}");
        Assert.Equal("hello world", dataB.Entry.Value);
        _output.WriteLine("    ✓ Реплика B содержит корректные данные");

        // 3. Читаем с реплики C
        _output.WriteLine("  Шаг 3: Читаем с реплики C");
        var readC = await TestHelper.GetFrom(TestHelper.ReplicaCUrl, "greeting");
        Assert.Equal(HttpStatusCode.OK, readC.StatusCode);
        var dataC = await readC.Content.ReadFromJsonAsync<DataResponse>();
        _output.WriteLine($"    Узел: {dataC!.NodeId}, Значение: {dataC.Entry!.Value}");
        Assert.Equal("hello world", dataC.Entry.Value);
        _output.WriteLine("    ✓ Реплика C содержит корректные данные");

        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }

    [Fact]
    public async Task Scenario2_ReplicaRejectsWrites()
    {
        await TestHelper.ResetAll();

        _output.WriteLine("═══ СЦЕНАРИЙ 2: Реплика отклоняет запись ═══");

        var writeToReplica = await TestHelper.Client.PostAsJsonAsync(
            $"{TestHelper.ReplicaBUrl}/api/data/test-key", new { value = "test" });

        _output.WriteLine($"  Статус: {(int)writeToReplica.StatusCode} {writeToReplica.StatusCode}");
        Assert.Equal(HttpStatusCode.Forbidden, writeToReplica.StatusCode);
        _output.WriteLine("  ✓ Реплика корректно отклонила запись (403 Forbidden)");
        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }

    [Fact]
    public async Task Scenario3_MultipleWritesReplicateCorrectly()
    {
        await TestHelper.ResetAll();

        _output.WriteLine("═══ СЦЕНАРИЙ 3: Множественные записи корректно реплицируются ═══");

        // Записываем 5 ключей
        for (int i = 1; i <= 5; i++)
        {
            await TestHelper.PutOnMaster($"item-{i}", $"value-{i}");
            _output.WriteLine($"  Записан item-{i}");
        }

        await Task.Delay(500);

        // Проверяем реплику B
        var allB = await TestHelper.GetAllFrom(TestHelper.ReplicaBUrl);
        var dataB = await allB.Content.ReadFromJsonAsync<AllDataResponse>();
        _output.WriteLine($"  Реплика B: {dataB!.Count} записей");
        Assert.Equal(5, dataB.Count);

        // Проверяем реплику C
        var allC = await TestHelper.GetAllFrom(TestHelper.ReplicaCUrl);
        var dataC = await allC.Content.ReadFromJsonAsync<AllDataResponse>();
        _output.WriteLine($"  Реплика C: {dataC!.Count} записей");
        Assert.Equal(5, dataC.Count);

        _output.WriteLine("  ✓ Все 5 записей реплицированы на оба узла");
        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }
}