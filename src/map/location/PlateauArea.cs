using Leopotam.Ecs;

public struct PlateauArea : IEcsAutoReset<PlateauArea>
{
    public float SolidFactor;
    public float BlendFactor { get { return 1f - SolidFactor; }}

    public PlateauArea(float solidFactor)
    {
        SolidFactor = solidFactor;
    }

    public void AutoReset(ref PlateauArea c)
    {
        c.SolidFactor = 0f;
    }
}