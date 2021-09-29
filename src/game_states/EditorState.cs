using Bitron.Ecs;

public partial class EditorState : GameState
{
    EcsEntity _editorEntity;

    public EditorState(EcsWorld world) : base(world)
    {
        AddInitSystem(new SpawnCameraOperatorSystem(this));

        AddInputSystem(new UpdateMapCursorSystem(this));
        AddInputSystem(new EditorEditSystem(this));
        AddInputSystem(new SelectLocationSystem(this));
        AddInputSystem(new UpdateTerrainInfoSystem());
        AddInputSystem(new LocationHighlightSystem());

        AddUpdateSystem(new UpdateCameraOperatorSystem());
        AddUpdateSystem(new UpdateStatsInfoSystem(this));

        AddEventSystem<UpdateMapEvent>(new UpdateMapEventSystem());
        AddEventSystem<UpdateTerrainMeshEvent>(new UpdateTerrainMeshEventSystem());
        AddEventSystem<UpdateTerrainFeaturePopulatorEvent>(new UpdateTerrainFeaturePopulatorEventSystem());
        AddEventSystem<SaveMapEvent>(new SaveMapEventSystem());
        AddEventSystem<LoadMapEvent>(new LoadMapEventSystem());
        AddEventSystem<DestroyMapEvent>(new DestroyMapEventSystem());
        AddEventSystem<CreateMapEvent>(new CreateMapEventSystem(this));

        AddDestroySystem(new DestroyCameraOperatorSystem());
    }

    public override void Enter(GameStateController gameStates)
    {

        var editorView = Scenes.Instance.EditorView.Instantiate<EditorView>();
        AddChild(editorView);

        _editorEntity = _world.Spawn().Add(new NodeHandle<EditorView>(editorView));

        _world.Spawn().Add(new CreateMapEvent(40, 40));
    }

    public override void Exit(GameStateController gameStates)
    {
        _editorEntity.Despawn();
        _world.Spawn().Add(new DestroyMapEvent());
    }

    public override void Input(GameStateController gameStates, Godot.InputEvent e)
    {
        if (e.IsActionPressed("ui_cancel"))
        {
            gameStates.PopState();
        }
    }
}