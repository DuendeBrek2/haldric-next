using System.Collections.Generic;
using Godot;
using Leopotam.Ecs;

public struct UpdateTerrainMeshEvent
{
    public List<Vector3i> Chunks;

    public UpdateTerrainMeshEvent(List<Vector3i> chunks = null)
    {
        Chunks = chunks;
    }
}

public struct EdgeVertices
{
    public Vector3 v1, v2, v3, v4, v5;

    public EdgeVertices(Vector3 corner1, Vector3 corner2)
    {
        v1 = corner1;
        v2 = corner1.Lerp(corner2, 0.25f);
        v3 = corner1.Lerp(corner2, 0.5f);
        v4 = corner1.Lerp(corner2, 0.75f);
        v5 = corner2;
    }

    public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
    {
        v1 = corner1;
        v2 = corner1.Lerp(corner2, outerStep);
        v3 = corner1.Lerp(corner2, 0.5f);
        v4 = corner1.Lerp(corner2, 1f - outerStep);
        v5 = corner2;
    }
}

public class UpdateTerrainMeshEventSystem : IEcsRunSystem
{
    public static readonly Color ColorRed = new Color(1.0f, 0f, 0f);
	public static readonly Color ColorGreen = new Color(0f, 1.0f, 0f);
	public static readonly Color ColorBlue = new Color(0f, 0f, 1.0f);

    EcsFilter<UpdateTerrainMeshEvent> _events;
    EcsFilter<Locations, NodeHandle<TerrainMesh>, NodeHandle<TerrainCollider>> _chunks;

    TerrainMesh _terrainMesh;

    public void Run()
    {
        foreach (var i in _events)
        {
            foreach (var j in _chunks)
            {
                var eventEntity = _events.GetEntity(i);
                var chunkEntity = _chunks.GetEntity(j);

                var updateEvent = eventEntity.Get<UpdateTerrainMeshEvent>();

                var chunkCell = chunkEntity.Get<Vector3i>();

                if (updateEvent.Chunks != null && !updateEvent.Chunks.Contains(chunkCell))
                {
                    continue;
                }

                _terrainMesh = chunkEntity.Get<NodeHandle<TerrainMesh>>().Node;

                ref var locations = ref chunkEntity.Get<Locations>();

                Triangulate(locations);

                var terrainCollider = chunkEntity.Get<NodeHandle<TerrainCollider>>().Node;

                terrainCollider.UpdateCollisionShape(_terrainMesh.Mesh.CreateTrimeshShape());
            }

            _events.GetEntity(i).Destroy();
        }
    }

    private void Triangulate(Locations locations)
    {
        _terrainMesh.Clear();

        foreach (var item in locations.Dict)
        {
            Triangulate(item.Value);
        }

        _terrainMesh.Apply();
    }

    private void Triangulate(EcsEntity locEntity)
    {
        for (Direction direction = Direction.NE; direction <= Direction.SE; direction++)
        {
            Triangulate(direction, locEntity);
        }
    }

    private void Triangulate(Direction direction, EcsEntity locEntity)
    {
        ref var elevation = ref locEntity.Get<Elevation>();
        ref var plateauArea = ref locEntity.Get<PlateauArea>();
        ref var terrainEntity = ref locEntity.Get<HasBaseTerrain>().Entity;
        ref var terrainCode = ref terrainEntity.Get<TerrainCode>().Value;

        Vector3 center = locEntity.Get<Coords>().World;
        center.y = elevation.Height;

        EdgeVertices e = new EdgeVertices(
            center + Metrics.GetFirstSolidCorner(direction, plateauArea),
            center + Metrics.GetSecondSolidCorner(direction, plateauArea)
        );

        TriangulatePlateau(center, e, Data.Instance.TextureArrayIds[terrainCode]);

        if (direction <= Direction.W)
        {
            TriangulateConnection(direction, locEntity, e);
        }
    }

    private void TriangulateConnection(Direction direction, EcsEntity locEntity, EdgeVertices e1)
    {
        ref var coords = ref locEntity.Get<Coords>();
        ref var elevation = ref locEntity.Get<Elevation>();
        ref var neighbors = ref locEntity.Get<Neighbors>();
        ref var terrainEntity = ref locEntity.Get<HasBaseTerrain>().Entity;
        ref var terrainCode = ref terrainEntity.Get<TerrainCode>().Value;

        if (!neighbors.Has(direction))
        {
            return;
        }

        var nLocEntity = neighbors.Get(direction);

        ref var nCoords = ref nLocEntity.Get<Coords>();
        ref var nElevation = ref nLocEntity.Get<Elevation>();
        ref var nPlateauArea = ref nLocEntity.Get<PlateauArea>();
        ref var nTerrainEntity = ref nLocEntity.Get<HasBaseTerrain>().Entity;
        ref var nTerrainCode = ref nTerrainEntity.Get<TerrainCode>().Value;

        var bridge = Metrics.GetBridge(direction, nPlateauArea);
        bridge.y = (nCoords.World.y + nElevation.Height) - (coords.World.y + elevation.Height);

        var e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v5 + bridge
        );

        TriangulateSlope(e1, e2, Data.Instance.TextureArrayIds[terrainCode], Data.Instance.TextureArrayIds[nTerrainCode]);

        if (direction <= Direction.SW && neighbors.Has(direction.Next()))
        {
            var nextLocEntity = neighbors.Get(direction.Next());

            ref var nextElevation = ref nextLocEntity.Get<Elevation>();
            ref var nextPlateauArea = ref nextLocEntity.Get<PlateauArea>();
            ref var nextTerrainEntity = ref nextLocEntity.Get<HasBaseTerrain>().Entity;
            ref var nextTerrainCode = ref nextTerrainEntity.Get<TerrainCode>().Value;

            var v6 = e1.v5 + Metrics.GetBridge(direction.Next(), nextPlateauArea);
            v6.y = nextElevation.Height;

            var types = new Vector3();

            if (elevation.Level <= nElevation.Level)
            {
                if (elevation.Level <= nElevation.Level)
                {
                    types.x = Data.Instance.TextureArrayIds[nTerrainCode];
                    types.y = Data.Instance.TextureArrayIds[terrainCode];
                    types.z = Data.Instance.TextureArrayIds[nextTerrainCode];
                    TriangulateCorner(e1.v5, e2.v5, v6, types);
                }
                else
                {
                    types.x = Data.Instance.TextureArrayIds[terrainCode];
                    types.y = Data.Instance.TextureArrayIds[nextTerrainCode];
                    types.z = Data.Instance.TextureArrayIds[nTerrainCode];
                    TriangulateCorner(v6, e1.v5, e2.v5, types);
                }
            }
            else if (nElevation.Level <= nextElevation.Level)
            {
                types.x = Data.Instance.TextureArrayIds[nextTerrainCode];
                types.y = Data.Instance.TextureArrayIds[nTerrainCode];
                types.z = Data.Instance.TextureArrayIds[terrainCode];
                TriangulateCorner(e2.v5, v6, e1.v5, types);
            }
            else
            {
                types.x = Data.Instance.TextureArrayIds[terrainCode];
                types.y = Data.Instance.TextureArrayIds[nextTerrainCode];
                types.z = Data.Instance.TextureArrayIds[nTerrainCode];
                TriangulateCorner(v6, e1.v5, e2.v5, types);
            }
        }
    }

    private void TriangulateCorner(Vector3 bottom, Vector3 left, Vector3 right, Vector3 types)
    {
        // _terrain.AddTrianglePerturbed(bottom, left, right);
        _terrainMesh.AddTriangle(left, bottom, right);
        _terrainMesh.AddTriangleColor(ColorRed, ColorGreen, ColorBlue);
        _terrainMesh.AddTriangleTerrainTypes(types);
    }

    private void TriangulatePlateau(Vector3 center, EdgeVertices edge, float type)
    {
        _terrainMesh.AddTriangle(edge.v1, center, edge.v5);
        _terrainMesh.AddTriangleColor(ColorRed);
        _terrainMesh.AddTriangleTerrainTypes(new Vector3(type, type, type));
        // _terrainMesh.AddTriangle(edge.v1, center, edge.v2);
        // _terrainMesh.AddTriangle(edge.v2, center, edge.v3);
        // _terrainMesh.AddTriangle(edge.v3, center, edge.v4);
        // _terrainMesh.AddTriangle(edge.v4, center, edge.v5);
    }

    void TriangulateSlope(EdgeVertices e1, EdgeVertices e2, float type1, float type2)
    {
        _terrainMesh.AddQuad(e1.v1, e1.v5, e2.v1, e2.v5);
        _terrainMesh.AddQuadColor(ColorRed, ColorGreen);
        _terrainMesh.AddQuadTerrainTypes(new Vector3(type1, type2, type1));
        // _terrainMesh.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        // _terrainMesh.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        // _terrainMesh.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        // _terrainMesh.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
    }
}