using Agents;
using AIGraph;
using AmorLib.Events;
using GTFO.API;
using LevelGeneration;
using Player;

namespace AmorLib.Utils;

/// <summary>
/// Stores group IDs used for reachable groups.
/// </summary>
public record struct ZoneGraphGroup
{
    /// <summary>
    /// The zone group ID.
    /// </summary>
    public ushort Zone = ZoneGraphUtil.NoGroup;
    /// <summary>
    /// The area group ID.
    /// </summary>
    public ushort Area = ZoneGraphUtil.NoGroup;

    /// <summary>
    /// Creates a graph group with IDs set to <see cref="ZoneGraphUtil.NoGroup"/>.
    /// </summary>
    public ZoneGraphGroup()
    {
        Zone = ZoneGraphUtil.NoGroup;
        Area = ZoneGraphUtil.NoGroup;
    }

    /// <summary>
    /// Resets the group IDs to <see cref="ZoneGraphUtil.NoGroup"/>.
    /// </summary>
    public void Reset()
    {
        Zone = ZoneGraphUtil.NoGroup;
        Area = ZoneGraphUtil.NoGroup;
    }
}

[CallConstructorOnLoad]
/// <summary>
/// Contains functions to get whether a room is reachable with fast performance.
/// </summary>
public sealed class ZoneGraphUtil // contributed by: Dinorush
{
    /// <summary>
    /// Callback that runs whenever there is a change in reachability to any room or zone.
    /// </summary>
    public static event Action? OnReachableUpdate;

    /// <summary>
    /// Returns <see langword="true"/> when the zone graph is built and functions are ready to be called (after OnBuildDone), otherwise <see langword="false"/>.
    /// </summary>
    public static bool IsReady { get; private set; } = false;

    /// <summary>
    /// Gets the corresponding <see cref="ZoneNode"/> for the zone that the agent is in.
    /// </summary>
    public static ZoneNode GetZoneNode(Agent agent) => GetZoneNode(agent.CourseNode.m_zone);
    /// <summary>
    /// Gets the corresponding <see cref="ZoneNode"/> for the zone that the course node is in.
    /// </summary>
    public static ZoneNode GetZoneNode(AIG_CourseNode courseNode) => GetZoneNode(courseNode.m_zone);
    /// <summary>
    /// Gets the corresponding <see cref="ZoneNode"/> for the zone.
    /// </summary>
    public static ZoneNode GetZoneNode(LG_Zone zone) => Current._zoneToNode[zone.ID];
    /// <summary>
    /// Gets the corresponding <see cref="AreaNode"/> for the room that the agent is in.
    /// </summary>
    public static AreaNode GetAreaNode(Agent agent) => GetAreaNode(agent.CourseNode.m_area);
    /// <summary>
    /// Gets the corresponding <see cref="AreaNode"/> for the course node.
    /// </summary>
    public static AreaNode GetAreaNode(AIG_CourseNode courseNode) => GetAreaNode(courseNode.m_area);
    /// <summary>
    /// Gets the corresponding <see cref="AreaNode"/> for the area.
    /// </summary>
    public static AreaNode GetAreaNode(LG_Area area) => Current._areaToNode[area.UID];

    /// <summary>
    /// Gets the group IDs tied to the player.
    /// </summary>
    public static ZoneGraphGroup GetPlayerGroup(PlayerAgent player) => Current._playerToGroup.GetValueOrDefault(player.PlayerSlotIndex);

    /// <summary>
    /// Gets whether there is an open path from <paramref name="courseNode"/> to <paramref name="player"/>.
    /// </summary>
    /// <param name="player">The target player to check reachability to.</param>
    /// <param name="courseNode">The course node to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open path from <paramref name="courseNode"/> to <paramref name="player"/>, otherwise <see langword="false"/>.</returns>
    public static bool IsReachable(PlayerAgent player, AIG_CourseNode courseNode) => GetAreaNode(courseNode).IsReachable(GetPlayerGroup(player));
    /// <summary>
    /// Gets whether there is an open path from <paramref name="area"/> to <paramref name="player"/>.
    /// </summary>
    /// <param name="player">The target player to check reachability to.</param>
    /// <param name="area">The area to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open path from <paramref name="area"/> to <paramref name="player"/>, otherwise <see langword="false"/>.</returns>
    public static bool IsReachable(PlayerAgent player, LG_Area area) => GetAreaNode(area).IsReachable(GetPlayerGroup(player));
    /// <summary>
    /// Gets whether there is an open path from any room in <paramref name="zone"/> to <paramref name="player"/>.
    /// </summary>
    /// <param name="player">The target player to check reachability to.</param>
    /// <param name="zone">The zone to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open path from any room in <paramref name="zone"/> to <paramref name="player"/>, otherwise <see langword="false"/>.</returns>
    public static bool IsReachable(PlayerAgent player, LG_Zone zone) => GetZoneNode(zone).IsReachable(GetPlayerGroup(player));
    /// <summary>
    /// Gets whether there is an open path from <paramref name="courseNode"/> to any player.
    /// </summary>
    /// <param name="courseNode">The course node to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open path from <paramref name="courseNode"/> to any player, otherwise <see langword="false"/>.</returns>
    public static bool IsReachable(AIG_CourseNode courseNode) => GetAreaNode(courseNode).IsReachable();
    /// <summary>
    /// Gets whether there is an open path from <paramref name="area"/> to any player.
    /// </summary>
    /// <param name="area">The area to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open path from <paramref name="area"/> to any player, otherwise <see langword="false"/>.</returns>
    public static bool IsReachable(LG_Area area) => GetAreaNode(area).IsReachable();
    /// <summary>
    /// Gets whether there is an open path from any room in <paramref name="zone"/> to any player.
    /// </summary>
    /// <param name="zone">The zone to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open path from any room in <paramref name="zone"/> to any player, otherwise <see langword="false"/>.</returns>
    public static bool IsReachable(LG_Zone zone) => GetZoneNode(zone).IsReachable();

    /// <summary>
    /// Gets whether there is an open security door path from <paramref name="courseNode"/> to <paramref name="player"/>.
    /// </summary>
    /// <param name="player">The target player to check reachability to.</param>
    /// <param name="courseNode">The course node to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open security door path from <paramref name="courseNode"/> to <paramref name="player"/>, otherwise <see langword="false"/>.</returns>
    public static bool IsZoneReachable(PlayerAgent player, AIG_CourseNode courseNode) => GetZoneNode(courseNode.m_zone).IsZoneReachable(GetPlayerGroup(player));
    /// <summary>
    /// Gets whether there is an open security door path from <paramref name="area"/> to <paramref name="player"/>.
    /// </summary>
    /// <param name="player">The target player to check reachability to.</param>
    /// <param name="area">The area to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open security door path from <paramref name="area"/> to <paramref name="player"/>, otherwise <see langword="false"/>.</returns>
    public static bool IsZoneReachable(PlayerAgent player, LG_Area area) => GetZoneNode(area.m_zone).IsZoneReachable(GetPlayerGroup(player));
    /// <summary>
    /// Gets whether there is an open security door path from <paramref name="zone"/> to <paramref name="player"/>.
    /// </summary>
    /// <param name="player">The target player to check reachability to.</param>
    /// <param name="zone">The zone to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open security door path from <paramref name="zone"/> to <paramref name="player"/>, otherwise <see langword="false"/>.</returns>
    public static bool IsZoneReachable(PlayerAgent player, LG_Zone zone) => GetZoneNode(zone).IsZoneReachable(GetPlayerGroup(player));
    /// <summary>
    /// Gets whether there is an open security door path from <paramref name="courseNode"/> to any player.
    /// </summary>
    /// <param name="courseNode">The course node to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open security door path from <paramref name="courseNode"/> to any player, otherwise <see langword="false"/>.</returns>
    public static bool IsZoneReachable(AIG_CourseNode courseNode) => GetZoneNode(courseNode.m_zone).IsZoneReachable();
    /// <summary>
    /// Gets whether there is an open security door path from <paramref name="area"/> to any player.
    /// </summary>
    /// <param name="area">The area to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open security door path from <paramref name="area"/> to any player, otherwise <see langword="false"/>.</returns>
    public static bool IsZoneReachable(LG_Area area) => GetZoneNode(area.m_zone).IsZoneReachable();
    /// <summary>
    /// Gets whether there is an open security door path from <paramref name="zone"/> to any player.
    /// </summary>
    /// <param name="zone">The zone to initiate the check from.</param>
    /// <returns><see langword="true"/> if there is an open security door path from <paramref name="zone"/> to any player, otherwise <see langword="false"/>.</returns>
    public static bool IsZoneReachable(LG_Zone zone) => GetZoneNode(zone).IsZoneReachable();

    /// <summary>
    /// ID that represents no group.
    /// </summary>
    public const ushort NoGroup = 0;

    internal static readonly ZoneGraphUtil Current = new();

    private readonly Dictionary<int, ZoneNode> _zoneToNode = new();
    private readonly Dictionary<int, AreaNode> _areaToNode = new();
    private readonly Dictionary<int, ZoneGraphGroup> _playerToGroup = new();
    private ZoneGraphGroup _lastGroup = new();

    static ZoneGraphUtil()
    {
        LevelAPI.OnBuildStart += () => LG_Area.s_areaUIDCounter = 0;
        LevelAPI.OnBuildDone += Current.BuildZoneGraph;
        LevelAPI.OnLevelCleanup += Current.Cleanup;
        SNetEvents.OnCheckpointReload += Current.RefreshOnCheckpoint;
    }

    // Debug logging - call in static constructor when needed for testing
    private static void DebugLogging()
    {
        OnReachableUpdate += () =>
        {
            var playerGroups = string.Join(", ", Current._playerToGroup.ToList().ConvertAll(kv => $"(Slot: {kv.Key}, Zone: {kv.Value.Zone}, Area: {kv.Value.Area})"));
            var reachableNodes = string.Join(", ", Current._areaToNode.Values.Where(a => a.IsReachable()).Select(a => $"({a.Zone.Zone.NavInfo.ToString()}{a.Area.m_navInfo.ToString()}:{a.Group})"));
            var reachableZones = string.Join(", ", Current._zoneToNode.Values.Where(z => z.IsReachable()).Select(z => $"({z.Zone.NavInfo.ToString()}:[{string.Join(", ", z.AreaGroups)}])"));
            var zoneReachableZones = string.Join(", ", Current._zoneToNode.Values.Where(z => z.IsZoneReachable()).Select(z => $"({z.Zone.NavInfo.ToString()}:{z.Group})"));
            Logger.Info($"Graph update | Player groups: [{playerGroups}] | Reachable nodes: [{reachableNodes}] | Reachable zones: [{reachableZones}], ZoneReachable zones: [{zoneReachableZones}]");
        };
    }
    
    private void BuildZoneGraph()
    {
        foreach (var zone in Builder.CurrentFloor.allZones)
        {
            _zoneToNode.TryAdd(zone.ID, new(zone));
            foreach (var area in zone.m_areas)
                _areaToNode.TryAdd(area.UID, new(area));
        }

        foreach (var zone in _zoneToNode.Values)
            zone.OnNodesCreated();
        foreach (var area in _areaToNode.Values)
            area.OnNodesCreated();

        IsReady = true;
    }

    private void Cleanup()
    {
        _zoneToNode.Clear();
        _areaToNode.Clear();
        _playerToGroup.Clear();
        _lastGroup.Reset();
        IsReady = false;
    }

    private void RefreshOnCheckpoint()
    {
        foreach (var zone in _zoneToNode.Values)
            zone.UpdateEdges();
        foreach (var area in _areaToNode.Values)
            area.UpdateEdges();
        RefreshGraph();
    }

    // Reset all nodes to be unreachable, then discover reachable nodes from all players.
    private void RefreshGraph()
    {
        foreach (var zone in _zoneToNode.Values)
            zone.Reset();
        foreach (var area in _areaToNode.Values)
            area.Reset();
        _playerToGroup.Clear();
        _lastGroup.Reset();

        foreach (var player in PlayerManager.PlayerAgentsInLevel)
        {
            if (player.CourseNode == null) continue;

            UpdateOrCreateGroup(player, out _);
        }

        OnReachableUpdate?.Invoke();
    }

    // Updates the player's group to match the node they're in, or create one if it doesn't exist.
    private bool UpdateOrCreateGroup(PlayerAgent player, out ZoneGraphGroup group)
    {
        group = new ZoneGraphGroup();
        var areaNode = GetAreaNode(player);
        var zoneNode = areaNode.Zone;

        if (areaNode.IsReachable())
        {
            group.Area = areaNode.Group;
        }
        else
        {
            group.Area = ++_lastGroup.Area;
            PropagateGroup(areaNode, group.Area);
        }

        if (zoneNode.IsZoneReachable())
        {
            group.Zone = zoneNode.Group;
        }
        else
        {
            group.Zone = ++_lastGroup.Zone;
            PropagateGroup(zoneNode, group.Zone);
        }

        if (!_playerToGroup.TryGetValue(player.PlayerSlotIndex, out var oldGroup) || oldGroup != group)
        {
            _playerToGroup[player.PlayerSlotIndex] = group;
            return true;
        }
        return false;
    }

    // Propagate the group to all connected areas with open doors.
    private static void PropagateGroup(AreaNode areaNode, ushort group)
    {
        areaNode.SetGroup(group);
        foreach (var edge in areaNode.Edges)
            if (edge.IsOpen && edge.Neighbor.Group != group)
                PropagateGroup(edge.Neighbor, group);
    }

    // Propagate the group to all connected zones with open security doors.
    private static void PropagateGroup(ZoneNode zoneNode, ushort group)
    {
        zoneNode.SetGroup(group);
        foreach (var edge in zoneNode.Edges)
            if (edge.IsOpen && edge.Neighbor.Group != group)
                PropagateGroup(edge.Neighbor, group);
    }

    internal void Internal_OnPlayerNodeChanged(PlayerAgent player, AIG_CourseNode oldNode)
    {
        if (!IsReady) return;

        bool runCallback = UpdateOrCreateGroup(player, out var newGroup);
        if (oldNode != null)
        {
            var oldAreaNode = GetAreaNode(oldNode.m_area);
            // Movement within the same group, no update needed.
            if (!oldAreaNode.IsReachable(newGroup.Area))
            {
                // If no players reachable, revoke reachable status.
                if (_playerToGroup.Values.All(group => !oldAreaNode.IsReachable(group.Area)))
                {
                    PropagateGroup(oldAreaNode, NoGroup);
                    runCallback = true;
                }
            }

            var oldZoneNode = oldAreaNode.Zone;
            if (!oldZoneNode.IsZoneReachable(newGroup.Zone))
            {
                if (_playerToGroup.Values.All(group => !oldZoneNode.IsZoneReachable(group.Zone)))
                {
                    PropagateGroup(oldZoneNode, NoGroup);
                    runCallback = true;
                }
            }
        }

        if (runCallback)
            OnReachableUpdate?.Invoke();
    }

    internal void Internal_OnDoorStateChanged(LG_Gate gate, bool isOpen)
    {
        if (!IsReady) return;

        var from = GetAreaNode(gate.m_linksFrom);
        var to = GetAreaNode(gate.m_linksTo);
        bool crossZones = from.Zone != to.Zone;

        // Update graph edges to match door state.
        from.UpdateEdges();
        to.UpdateEdges();
        if (crossZones)
        {
            from.Zone.UpdateEdges();
            to.Zone.UpdateEdges();
        }

        if (!isOpen)
        {
            // Door was open; either both are reachable (needs update) or neither is.
            if (from.IsReachable() || (crossZones && from.Zone.IsZoneReachable()))
                RefreshGraph();
        }
        else
        {
            bool TryPropagate(AreaNode oldArea, AreaNode newArea)
            {
                if (!newArea.IsReachable()) return false;

                foreach ((var index, var group) in _playerToGroup.ToArray())
                    if (group.Area == oldArea.Group)
                        _playerToGroup[index] = group with { Area = newArea.Group };

                PropagateGroup(oldArea, newArea.Group);
                return true;
            }

            bool TryPropagateZone(ZoneNode oldZone, ZoneNode newZone)
            {
                if (!newZone.IsZoneReachable()) return false;

                foreach ((var index, var group) in _playerToGroup.ToArray())
                    if (group.Zone == oldZone.Group)
                        _playerToGroup[index] = group with { Zone = newZone.Group };

                PropagateGroup(oldZone, newZone.Group);
                return true;
            }

            // Expand the reachable side or merge the two sides if both are reachable.
            bool areaChanged = TryPropagate(to, from) || TryPropagate(from, to);
            bool zoneChanged = crossZones && (TryPropagateZone(to.Zone, from.Zone) || TryPropagateZone(from.Zone, to.Zone));
            if (areaChanged || zoneChanged)
                OnReachableUpdate?.Invoke();
        }
    }
}
