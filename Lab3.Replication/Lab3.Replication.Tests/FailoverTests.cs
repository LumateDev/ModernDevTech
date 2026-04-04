using Lab3.Replication.Tests.Models;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lab3.Replication.Tests;

[Collection("Sequential")]
public class FailoverTests
{
    private readonly ITestOutputHelper _output;

    public FailoverTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Scenario4_ReplicaDown_MasterContinuesWorking()
    {
        await TestHelper.ResetAll();

        _output.WriteLine("═══ СЦЕНАРИЙ 4: Падение реплики — мастер продолжает работать ═══");

        // 1. Выключаем реплику B
        _output.WriteLine("  Шаг 1: Выключаем реплику B");
        await TestHelper.TakeOffline(TestHelper.ReplicaBUrl);
        _output.WriteLine("    ✓ Реплика B переведена в offline");

        // 2. Записываем на мастер
        _output.WriteLine("  Шаг 2: Записываем данные на мастер");
        var writeResponse = await TestHelper.PutOnMaster("during-failure", "data-123");
        Assert.Equal(HttpStatusCode.OK, writeResponse.StatusCode);
        var writeResult = await writeResponse.Content.ReadFromJsonAsync<WriteResponse>();
        _output.WriteLine($"    ✓ Записано. Реплик получили: {writeResult!.Replication!.SuccessCount}/{writeResult.Replication.TotalReplicas}");
        Assert.Equal(1, writeResult.Replication.SuccessCount); // Только C получила

        // 3. Реплика B недоступна
        _output.WriteLine("  Шаг 3: Проверяем что реплика B возвращает 503");
        var readB = await TestHelper.GetFrom(TestHelper.ReplicaBUrl, "during-failure");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readB.StatusCode);
        _output.WriteLine("    ✓ Реплика B: 503 Service Unavailable");

        // 4. Реплика C — данные есть
        _output.WriteLine("  Шаг 4: Реплика C отвечает нормально");
        await Task.Delay(300);
        var readC = await TestHelper.GetFrom(TestHelper.ReplicaCUrl, "during-failure");
        Assert.Equal(HttpStatusCode.OK, readC.StatusCode);
        var dataC = await readC.Content.ReadFromJsonAsync<DataResponse>();
        Assert.Equal("data-123", dataC!.Entry!.Value);
        _output.WriteLine($"    ✓ Реплика C: value='{dataC.Entry.Value}'");

        // 5. Восстанавливаем реплику B
        _output.WriteLine("  Шаг 5: Восстанавливаем реплику B");
        await TestHelper.BringOnline(TestHelper.ReplicaBUrl);
        _output.WriteLine("    ✓ Реплика B переведена в online");

        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }

    [Fact]
    public async Task Scenario5_ReplicaRecovery_SyncFromMaster()
    {
        await TestHelper.ResetAll();

        _output.WriteLine("═══ СЦЕНАРИЙ 5: Восстановление реплики — синхронизация с мастера ═══");

        // 1. Выключаем реплику B
        _output.WriteLine("  Шаг 1: Выключаем реплику B");
        await TestHelper.TakeOffline(TestHelper.ReplicaBUrl);

        // 2. Записываем данные пока реплика B лежит
        _output.WriteLine("  Шаг 2: Записываем 3 ключа, пока реплика B offline");
        await TestHelper.PutOnMaster("recovery-1", "aaa");
        await TestHelper.PutOnMaster("recovery-2", "bbb");
        await TestHelper.PutOnMaster("recovery-3", "ccc");
        _output.WriteLine("    ✓ Записаны: recovery-1, recovery-2, recovery-3");

        // 3. Включаем реплику B
        _output.WriteLine("  Шаг 3: Включаем реплику B");
        await TestHelper.BringOnline(TestHelper.ReplicaBUrl);

        // 4. Проверяем — данных ещё нет
        _output.WriteLine("  Шаг 4: Проверяем — данных на реплике B нет");
        var readBefore = await TestHelper.GetFrom(TestHelper.ReplicaBUrl, "recovery-1");
        Assert.Equal(HttpStatusCode.NotFound, readBefore.StatusCode);
        _output.WriteLine("    ✓ recovery-1 не найден (ожидаемо)");

        // 5. Синхронизируем с мастера
        _output.WriteLine("  Шаг 5: Запускаем синхронизацию реплики B с мастера");
        var syncResponse = await TestHelper.SyncFromMaster(TestHelper.ReplicaBUrl, TestHelper.MasterUrl);
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);
        var syncBody = await syncResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"    Ответ: {syncBody}");

        // 6. Проверяем — данные появились
        _output.WriteLine("  Шаг 6: Проверяем данные после синхронизации");
        var readAfter = await TestHelper.GetFrom(TestHelper.ReplicaBUrl, "recovery-1");
        Assert.Equal(HttpStatusCode.OK, readAfter.StatusCode);
        var dataAfter = await readAfter.Content.ReadFromJsonAsync<DataResponse>();
        Assert.Equal("aaa", dataAfter!.Entry!.Value);
        _output.WriteLine($"    ✓ recovery-1 = '{dataAfter.Entry.Value}'");

        var allB = await TestHelper.GetAllFrom(TestHelper.ReplicaBUrl);
        var dataAllB = await allB.Content.ReadFromJsonAsync<AllDataResponse>();
        _output.WriteLine($"    ✓ Всего записей на реплике B: {dataAllB!.Count}");
        Assert.Equal(3, dataAllB.Count);

        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }

    [Fact]
    public async Task Scenario6_MasterDown_ReplicasStillServeReads()
    {
        await TestHelper.ResetAll();

        _output.WriteLine("═══ СЦЕНАРИЙ 6: Падение мастера — реплики продолжают обслуживать чтение ═══");

        // 1. Записываем данные
        _output.WriteLine("  Шаг 1: Записываем данные на мастер");
        await TestHelper.PutOnMaster("master-data", "important-value");
        await Task.Delay(300);
        _output.WriteLine("    ✓ Данные записаны и реплицированы");

        // 2. Выключаем мастер
        _output.WriteLine("  Шаг 2: Выключаем мастер (имитация сбоя)");
        await TestHelper.TakeOffline(TestHelper.MasterUrl);

        // 3. Мастер недоступен
        _output.WriteLine("  Шаг 3: Мастер возвращает 503");
        var masterRead = await TestHelper.GetFrom(TestHelper.MasterUrl, "master-data");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, masterRead.StatusCode);
        _output.WriteLine("    ✓ Мастер: 503 Service Unavailable");

        // 4. Реплики отвечают
        _output.WriteLine("  Шаг 4: Реплики продолжают обслуживать чтение");
        var readB = await TestHelper.GetFrom(TestHelper.ReplicaBUrl, "master-data");
        Assert.Equal(HttpStatusCode.OK, readB.StatusCode);
        var dataB = await readB.Content.ReadFromJsonAsync<DataResponse>();
        _output.WriteLine($"    ✓ Реплика B: value='{dataB!.Entry!.Value}'");

        var readC = await TestHelper.GetFrom(TestHelper.ReplicaCUrl, "master-data");
        Assert.Equal(HttpStatusCode.OK, readC.StatusCode);
        var dataC = await readC.Content.ReadFromJsonAsync<DataResponse>();
        _output.WriteLine($"    ✓ Реплика C: value='{dataC!.Entry!.Value}'");

        // 5. Запись невозможна
        _output.WriteLine("  Шаг 5: Попытка записи на мастер — невозможна");
        var writeResponse = await TestHelper.PutOnMaster("new-key", "new-value");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, writeResponse.StatusCode);
        _output.WriteLine("    ✓ Запись отклонена: 503");

        // Восстанавливаем
        await TestHelper.BringOnline(TestHelper.MasterUrl);
        _output.WriteLine("  Шаг 6: Мастер восстановлен");

        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }

    [Fact]
    public async Task Scenario7_NetworkDelay_EventualConsistency()
    {
        await TestHelper.ResetAll();

        _output.WriteLine("═══ СЦЕНАРИЙ 7: Сетевая задержка — Eventual Consistency ═══");

        // 1. Устанавливаем задержку на реплике B (3 секунды)
        _output.WriteLine("  Шаг 1: Устанавливаем задержку 3000 мс на реплике B");
        await TestHelper.SetDelay(TestHelper.ReplicaBUrl, 3000);

        // 2. Записываем на мастер
        _output.WriteLine("  Шаг 2: Записываем на мастер (запись будет ждать репликации)");
        var writeTask = TestHelper.PutOnMaster("delayed-key", "delayed-value");

        // 3. Пока репликация не завершена, пробуем прочитать
        _output.WriteLine("  Шаг 3: Немедленно читаем с реплики C (без задержки)");
        await Task.Delay(100); // Минимальная пауза
        var readC = await TestHelper.GetFrom(TestHelper.ReplicaCUrl, "delayed-key");
        // C должна уже получить данные (задержки нет)
        _output.WriteLine($"    Реплика C статус: {(int)readC.StatusCode}");

        // Ждём завершения записи (включая задержку на B)
        var writeResponse = await writeTask;
        Assert.Equal(HttpStatusCode.OK, writeResponse.StatusCode);
        _output.WriteLine("  Шаг 4: Запись завершена (дождались репликации на B с задержкой)");

        // 4. Теперь обе реплики имеют данные
        _output.WriteLine("  Шаг 5: Проверяем обе реплики");
        await TestHelper.SetDelay(TestHelper.ReplicaBUrl, 0); // Убираем задержку для чтения

        var readBFinal = await TestHelper.GetFrom(TestHelper.ReplicaBUrl, "delayed-key");
        Assert.Equal(HttpStatusCode.OK, readBFinal.StatusCode);
        var dataBFinal = await readBFinal.Content.ReadFromJsonAsync<DataResponse>();
        _output.WriteLine($"    ✓ Реплика B: value='{dataBFinal!.Entry!.Value}'");

        var readCFinal = await TestHelper.GetFrom(TestHelper.ReplicaCUrl, "delayed-key");
        Assert.Equal(HttpStatusCode.OK, readCFinal.StatusCode);
        var dataCFinal = await readCFinal.Content.ReadFromJsonAsync<DataResponse>();
        _output.WriteLine($"    ✓ Реплика C: value='{dataCFinal!.Entry!.Value}'");

        _output.WriteLine("  Итог: данные в итоге согласованы (eventual consistency)");
        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }

    [Fact]
    public async Task Scenario8_DataOverwrite_LatestVersionWins()
    {
        await TestHelper.ResetAll();

        _output.WriteLine("═══ СЦЕНАРИЙ 8: Перезапись данных — побеждает последняя версия ═══");

        // 1. Записываем несколько версий одного ключа
        _output.WriteLine("  Шаг 1: Записываем 3 версии ключа 'counter'");
        await TestHelper.PutOnMaster("counter", "version-1");
        await TestHelper.PutOnMaster("counter", "version-2");
        await TestHelper.PutOnMaster("counter", "version-3");
        await Task.Delay(300);

        // 2. Проверяем что на всех узлах последняя версия
        _output.WriteLine("  Шаг 2: Проверяем все узлы");

        var readMaster = await TestHelper.GetFrom(TestHelper.MasterUrl, "counter");
        var dataMaster = await readMaster.Content.ReadFromJsonAsync<DataResponse>();
        _output.WriteLine($"    Master: value='{dataMaster!.Entry!.Value}', version={dataMaster.Entry.Version}");
        Assert.Equal("version-3", dataMaster.Entry.Value);

        var readB = await TestHelper.GetFrom(TestHelper.ReplicaBUrl, "counter");
        var dataB = await readB.Content.ReadFromJsonAsync<DataResponse>();
        _output.WriteLine($"    Replica B: value='{dataB!.Entry!.Value}', version={dataB.Entry.Version}");
        Assert.Equal("version-3", dataB.Entry.Value);

        var readC = await TestHelper.GetFrom(TestHelper.ReplicaCUrl, "counter");
        var dataC = await readC.Content.ReadFromJsonAsync<DataResponse>();
        _output.WriteLine($"    Replica C: value='{dataC!.Entry!.Value}', version={dataC.Entry.Version}");
        Assert.Equal("version-3", dataC.Entry.Value);

        _output.WriteLine("  ✓ Все узлы содержат последнюю версию данных");
        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }
}