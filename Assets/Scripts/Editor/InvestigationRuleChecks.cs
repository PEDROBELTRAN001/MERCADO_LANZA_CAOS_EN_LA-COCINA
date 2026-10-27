#if UNITY_EDITOR
using System;

public static class InvestigationRuleChecks
{
    private static int passed;
    private static void Check(bool result, string text)
    {
        if (!result) throw new Exception(text);
        passed++;
        Console.WriteLine("PASS " + text);
    }
    [UnityEditor.MenuItem("Mercado/Comprobar reglas de investigación")]
    public static void RunFromMenu()
    {
        Main();
        UnityEngine.Debug.Log("Las 25 comprobaciones de las reglas de investigación pasaron.");
    }
    public static int Main()
    {
        var ledger = new InvestigationLedger();
        const int worker = 10, helper = 11, rival = 20, innocent = 30;
        var first = ledger.Register(rival, worker, "Papa");
        Check(ledger.AvailableProof(worker, rival) == null, "Un ataque no examinado no permite denunciar");
        Check(!ledger.Reserve(first.Id, worker, rival), "No reserva pruebas sin examinar");
        Check(!ledger.Examine(first.Id, rival), "El culpable no examina su propia prueba para acusar");
        Check(ledger.Examine(first.Id, worker), "El trabajador examina el rastro");
        Check(!ledger.Examine(first.Id, worker), "Examinar otra vez no duplica la prueba");
        Check(ledger.AvailableProof(worker, rival) == first, "La prueba examinada queda disponible");
        Check(ledger.AvailableProof(worker, innocent) == null, "No acusa a alguien ajeno al incidente");
        Check(!ledger.Reserve(first.Id, worker, innocent), "Rechaza sospechoso incorrecto");
        Check(!ledger.CompleteArrest(first.Id, rival), "No hay expulsión sin denuncia reservada");
        Check(ledger.Examine(first.Id, helper), "Otro trabajador puede investigar sin duplicar el incidente");
        Check(ledger.Reserve(first.Id, worker, rival), "La denuncia válida reserva una prueba");
        Check(!ledger.Reserve(first.Id, helper, rival), "No admite dos persecuciones por la misma prueba");
        ledger.Release(first.Id);
        Check(ledger.ExpulsionsOf(rival) == 0 && ledger.AvailableProof(worker, rival) == first,
            "Escapar conserva la prueba y no cuenta como expulsión");
        var banked = ledger.Register(rival, worker, "Plato terminado");
        ledger.Examine(banked.Id, worker);
        Check(ledger.Reserve(first.Id, worker, rival), "Permite una nueva denuncia tras escapar");
        Check(!ledger.Reserve(banked.Id, worker, rival), "Una prueba distinta no abre una persecución simultánea");
        Check(ledger.CompleteArrest(first.Id, rival) && ledger.ExpulsionsOf(rival) == 1, "Primera captura válida");
        Check(!ledger.CompleteArrest(first.Id, rival) && ledger.ExpulsionsOf(rival) == 1, "La misma captura no se cuenta dos veces");
        Check(banked.Used && ledger.AvailableProof(worker, rival) == null, "La expulsión invalida todas las pruebas previas del rival");
        ledger.Release(first.Id);
        Check(!ledger.Reserve(first.Id, worker, rival), "Liberar una prueba usada no la reactiva");
        Check(!ledger.Examine(banked.Id, helper), "Un rastro antiguo no se vuelve prueba después de la expulsión");
        var second = ledger.Register(rival, worker, "Queso");
        ledger.Examine(second.Id, worker);
        ledger.Reserve(second.Id, worker, rival);
        Check(ledger.CompleteArrest(second.Id, rival) && ledger.ExpulsionsOf(rival) == 2, "Un nuevo sabotaje permite la segunda expulsión");
        var third = ledger.Register(rival, worker, "Api con pastel");
        ledger.Examine(third.Id, worker);
        ledger.Reserve(third.Id, worker, rival);
        Check(ledger.CompleteArrest(third.Id, rival) && ledger.ExpulsionsOf(rival) == 3, "Tres incidentes resueltos permiten la tercera expulsión");
        Check(ledger.ExpulsionsOf(innocent) == 0, "No incrementa expulsiones de otros personajes");
        int lastId = third.Id;
        ledger.Reset();
        Check(ledger.Incidents.Count == 0 && ledger.ExpulsionsOf(rival) == 0, "El reinicio limpia pruebas y expulsiones");
        var nextRound = ledger.Register(rival, worker, "Fruta");
        Check(nextRound.Id > lastId && ledger.Find(first.Id) == null, "Un rastro de la ronda anterior no apunta a un incidente nuevo");
        Console.WriteLine("RESULTADO: " + passed + " comprobaciones correctas.");
        return 0;
    }
}

#endif
