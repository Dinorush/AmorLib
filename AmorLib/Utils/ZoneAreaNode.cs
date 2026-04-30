using LevelGeneration;

namespace AmorLib.Utils;

/// <summary>
/// Stores graph information about a <see cref="LG_Zone"/>.
/// </summary>
public sealed class ZoneNode
{
    /// <summary>
    /// Stores graph information about an <see cref="LG_Gate"/> that connects to a neighboring zone.
    /// </summary>
    public class ZoneEdge
    {
        /// <summary>
        /// The <see cref="LG_Gate"/> this edge is tied to.
        /// </summary>
        public readonly LG_Gate Gate;
        /// <summary>
        /// The <see cref="ZoneNode"/> on the other side of the <see cref="Gate"/>.
        /// </summary>
        public readonly ZoneNode Neighbor;

        /// <summary>
        /// Returns whether the <see cref="Gate"/> is open.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Constructs an edge using the given gate and neighboring zone.
        /// </summary>
        public ZoneEdge(LG_Gate gate, ZoneNode zone)
        {
            Gate = gate;
            Neighbor = zone;
            UpdateOpen();
        }

        internal void UpdateOpen() => IsOpen = Gate.IsTraversable;
    }

    /// <summary>
    /// The <see cref="LG_Zone"/> this node is tied to.
    /// </summary>
    public readonly LG_Zone Zone;
    /// <summary>
    /// The list of <see cref="AreaNode"/>s within this zone.
    /// </summary>
    public AreaNode[] Areas { get; private set; } = null!;
    /// <summary>
    /// The list of <see cref="ZoneEdge"/>s to neighboring <see cref="ZoneNode"/>s.
    /// </summary>
    public ZoneEdge[] Edges { get; private set; } = null!;

    private List<(LG_Gate gate, ZoneNode zone)> _neighborsToAdd = new();
    private readonly Dictionary<ushort, int> _areaGroups = new();
    /// <summary>
    /// The collection of unique group IDs that <see cref="AreaNode"/>s within this zone have.
    /// </summary>
    public IReadOnlyCollection<ushort> AreaGroups => _areaGroups.Keys;
    /// <summary>
    /// The group ID this zone has, used for <see cref="IsZoneReachable()"/> checks.
    /// </summary>
    public ushort Group { get; private set; } = ZoneGraphUtil.NoGroup;

    /// <summary>
    /// Constructs a node using the given zone.
    /// </summary>
    public ZoneNode(LG_Zone zone)
    {
        Zone = zone;
        var gate = zone.m_sourceGate;
        var parentZone = gate?.m_linksFrom?.m_zone;
        if (parentZone != null)
        {
            // Parent will have generated first, so we know it exists
            var parent = ZoneGraphUtil.GetZoneNode(parentZone);
            parent._neighborsToAdd.Add((gate!, this));
            _neighborsToAdd.Add((gate!, parent));
        }
    }

    internal void OnNodesCreated()
    {
        Edges = _neighborsToAdd.ConvertAll<ZoneEdge>(pair => new(pair.gate, pair.zone)).ToArray();
        _neighborsToAdd = null!;

        List<AreaNode> children = new(Zone.m_areas.Count);
        foreach (var area in Zone.m_areas)
            children.Add(ZoneGraphUtil.GetAreaNode(area));
        Areas = children.ToArray();
    }

    /// <summary>
    /// Returns whether there is an open security door path to the target group.
    /// </summary>
    public bool IsZoneReachable(ZoneGraphGroup group) => Group == group.Zone && group.Zone != ZoneGraphUtil.NoGroup;
    /// <summary>
    /// Returns whether there is an open security door path to the target group.
    /// </summary>
    public bool IsZoneReachable(ushort group) => Group == group && group != ZoneGraphUtil.NoGroup;
    /// <summary>
    /// Returns whether there is an open security door path to any group.
    /// </summary>
    public bool IsZoneReachable() => Group != ZoneGraphUtil.NoGroup;
    /// <summary>
    /// Returns whether there is an open path from any room in this zone to the target group.
    /// </summary>
    public bool IsReachable(ZoneGraphGroup group) => _areaGroups.ContainsKey(group.Area);
    /// <summary>
    /// Returns whether there is an open path from any room in this zone to the target group.
    /// </summary>
    public bool IsReachable(ushort group) => _areaGroups.ContainsKey(group);
    /// <summary>
    /// Returns whether there is an open path from any room in this zone to any group.
    /// </summary>
    public bool IsReachable() => _areaGroups.Count > 0;

    internal void Reset()
    {
        _areaGroups.Clear();
        Group = ZoneGraphUtil.NoGroup;
    }

    internal void SetGroup(ushort group)
    {
        Group = group;
    }

    internal void UpdateEdges()
    {
        foreach (var edge in Edges)
            edge.UpdateOpen();
    }

    internal void OnAreaReachable(ushort newGroup, ushort oldGroup)
    {
        if (_areaGroups.TryGetValue(oldGroup, out var count) && --count == 0)
            _areaGroups.Remove(oldGroup);

        if (newGroup == ZoneGraphUtil.NoGroup) return;

        count = _areaGroups.GetValueOrDefault(newGroup);
        _areaGroups[newGroup] = count + 1;
    }
}

/// <summary>
/// The class used to store graph information about a <see cref="LG_Area"/>.
/// </summary>
public sealed class AreaNode
{
    /// <summary>
    /// Stores graph information about an <see cref="LG_Gate"/> that connects to a neighboring area.
    /// </summary>
    public class AreaEdge
    {
        /// <summary>
        /// The <see cref="LG_Gate"/> this edge is tied to.
        /// </summary>
        public readonly LG_Gate Gate;
        /// <summary>
        /// The <see cref="AreaNode"/> on the other side of the <see cref="Gate"/>.
        /// </summary>
        public readonly AreaNode Neighbor;

        /// <summary>
        /// Returns whether the <see cref="Gate"/> is open.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Constructs an edge using the given gate and neighboring area.
        /// </summary>
        public AreaEdge(LG_Gate gate, AreaNode area)
        {
            Gate = gate;
            Neighbor = area;
            UpdateOpen();
        }

        internal void UpdateOpen() => IsOpen = Gate.IsTraversable;
    }

    /// <summary>
    /// The <see cref="LG_Area"/> this node is tied to.
    /// </summary>
    public readonly LG_Area Area;

    /// <summary>
    /// The group ID this area has, used for <see cref="IsReachable()"/> checks.
    /// </summary>
    public ushort Group { get; private set; } = ZoneGraphUtil.NoGroup;

    /// <summary>
    /// The <see cref="ZoneNode"/> that contains this node.
    /// </summary>
    public ZoneNode Zone { get; private set; } = null!;
    /// <summary>
    /// The list of <see cref="AreaEdge"/>s to neighboring <see cref="AreaNode"/>s.
    /// </summary>
    public AreaEdge[] Edges { get; private set; } = null!;

    /// <summary>
    /// Constructs a node using the given area.
    /// </summary>
    public AreaNode(LG_Area area)
    {
        Area = area;
    }

    /// <summary>
    /// Returns whether there is an open path to the target group.
    /// </summary>
    public bool IsReachable(ZoneGraphGroup group) => Group == group.Area && group.Area != ZoneGraphUtil.NoGroup;
    /// <summary>
    /// Returns whether there is an open path to the target group.
    /// </summary>
    public bool IsReachable(ushort group) => Group == group && group != ZoneGraphUtil.NoGroup;
    /// <summary>
    /// Returns whether there is an open path to any group.
    /// </summary>
    public bool IsReachable() => Group != ZoneGraphUtil.NoGroup;

    internal void OnNodesCreated()
    {
        Zone = ZoneGraphUtil.GetZoneNode(Area.m_zone);
        List<AreaEdge> edges = new();
        foreach (var gate in Area.m_gates)
        {
            var area = gate.m_linksFrom;
            if (area == null) continue;

            if (area.UID == Area.UID)
                area = gate.m_linksTo;

            if (area == null || gate.ExpanderStatus == LG_ZoneExpanderStatus.Blocked) continue;

            edges.Add(new(gate, ZoneGraphUtil.GetAreaNode(area)));
        }
        Edges = edges.ToArray();
    }

    internal void Reset()
    {
        Group = ZoneGraphUtil.NoGroup;
    }

    internal void SetGroup(ushort group)
    {
        Zone.OnAreaReachable(group, Group);
        Group = group;
    }

    internal void UpdateEdges()
    {
        foreach (var edge in Edges)
            edge.UpdateOpen();
    }
}