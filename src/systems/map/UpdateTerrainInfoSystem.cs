using Bitron.Ecs;

public class UpdateTerrainInfoSystem : IEcsSystem
{
    public void Run(EcsWorld world)
    {
        if (!world.TryGetResource<HoveredLocation>(out var hoveredLocation))
        {
            return;
        }

        if (!world.TryGetResource<TerrainPanel>(out var terrainPanel))
        {
            return;
        }

        var locEntity = hoveredLocation.Entity;

        if (!locEntity.IsAlive())
        {
            return;
        }

        ref Coords coords = ref locEntity.Get<Coords>();

        ref var elevation = ref locEntity.Get<Elevation>();
        ref var baseTerrain = ref locEntity.Get<HasBaseTerrain>();
        ref var baseTerrainCode = ref baseTerrain.Entity.Get<TerrainCode>();
        var overlayTerrainCode = "";

        var terrainTypes = TerrainTypes.FromLocEntity(locEntity);

        if (locEntity.Has<HasOverlayTerrain>())
        {
            ref var overlayTerrain = ref locEntity.Get<HasOverlayTerrain>();
            ref var overlayCode = ref overlayTerrain.Entity.Get<TerrainCode>();
            overlayTerrainCode = "^" + overlayCode.Value;
        }

        string text = $"Coords: {coords.Offset().x}, {coords.Offset().z}";
        text += $"\nElevation: {elevation.Value}";
        text += $"\nTerrain: {baseTerrainCode.Value}{overlayTerrainCode}";
        text += $"\nTypes: {terrainTypes}";
        text += $"\nDefense: {(int)(100 * terrainTypes.GetDefense())}%";
        text += $"\nCost: {terrainTypes.GetMovementCost()}";

        if (locEntity.Has<Castle>())
        {
            ref var castle = ref locEntity.Get<Castle>();

            text += "\nCastle: " + castle.List.Count;
        }

        if (locEntity.Has<Village>())
        {
            ref var village = ref locEntity.Get<Village>();

            text += "\nVillage: " + village.List.Count;
        }

        terrainPanel.UpdateInfo(text);
    }
}