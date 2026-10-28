using System.Collections.Generic;

// Reglas independientes de Unity para poder comprobar la validez de las pruebas.
public sealed class InvestigationLedger
{
    public sealed class Incident
    {
        public int Id { get; internal set; }
        public int Attacker { get; internal set; }
        public int Victim { get; internal set; }
        public string Item { get; internal set; }
        public int Generation { get; internal set; }
        public bool Reserved { get; internal set; }
        public bool Used { get; internal set; }
        internal readonly HashSet<int> Observers = new HashSet<int>();
    }
    private readonly List<Incident> incidents = new List<Incident>();
    private readonly Dictionary<int, int> generations = new Dictionary<int, int>();
    private readonly Dictionary<int, int> expulsions = new Dictionary<int, int>();
    private int nextId;
    public IReadOnlyList<Incident> Incidents => incidents;
    private int Generation(int actor) => generations.TryGetValue(actor, out int value) ? value : 0;
    public int ExpulsionsOf(int actor) => expulsions.TryGetValue(actor, out int value) ? value : 0;
    public Incident Register(int attacker, int victim, string item)
    {
        var incident = new Incident { Id = ++nextId, Attacker = attacker, Victim = victim,
            Item = item, Generation = Generation(attacker) };
        incidents.Add(incident);
        return incident;
    }
    public Incident Find(int id) => incidents.Find(i => i.Id == id);
    private bool Current(Incident incident) => incident != null && !incident.Used &&
        incident.Generation == Generation(incident.Attacker);
    public bool Examine(int id, int observer)
    {
        Incident incident = Find(id);
        return Current(incident) && observer != incident.Attacker && incident.Observers.Add(observer);
    }
    public Incident AvailableProof(int observer, int suspect)
    {
        return incidents.FindLast(i => Current(i) && !i.Reserved && i.Attacker == suspect && i.Observers.Contains(observer));
    }
    public bool Reserve(int id, int observer, int suspect)
    {
        Incident incident = Find(id);
        if (!Current(incident) || incident.Reserved || incident.Attacker != suspect || !incident.Observers.Contains(observer)) return false;
        // Una sola persecución del sospechoso puede estar en curso.
        if (incidents.Exists(i => Current(i) && i.Attacker == suspect && i.Reserved)) return false;
        incident.Reserved = true;
        return true;
    }
    public void Release(int id)
    {
        Incident incident = Find(id);
        if (Current(incident)) incident.Reserved = false;
    }
    public bool CompleteArrest(int id, int suspect)
    {
        Incident incident = Find(id);
        if (!Current(incident) || !incident.Reserved || incident.Attacker != suspect) return false;
        int generation = Generation(suspect);
        foreach (Incident old in incidents)
            if (old.Attacker == suspect && old.Generation == generation) { old.Used = true; old.Reserved = false; }
        generations[suspect] = generation + 1;
        expulsions[suspect] = ExpulsionsOf(suspect) + 1;
        return true;
    }
    public void Reset()
    {
        // No reutilizar IDs: un rastro que se destruye al reiniciar no puede apuntar a un incidente nuevo.
        incidents.Clear(); generations.Clear(); expulsions.Clear();
    }
}
